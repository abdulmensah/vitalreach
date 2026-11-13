"""Mock-host checks for the real deployment shell scripts; no Docker or SSH needed."""
import os
import pathlib
import shutil
import subprocess
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
BASH = r"C:\Program Files\Git\bin\bash.exe" if os.name == "nt" else shutil.which("bash")
REPO = "ghcr.io/abdulmensah/vitalreach"
DIGEST = "sha256:" + "a" * 64
OLD = REPO + "@sha256:" + "b" * 64

def save(path, content):
    path.write_text(content, encoding="utf-8", newline="\n")
    path.chmod(0o755)
    return str(path).replace("\\", "/")

class DeploymentChecks(unittest.TestCase):
    def test_forced_command(self):
        with tempfile.TemporaryDirectory() as folder:
            base = pathlib.Path(folder)
            sudo = save(base / "sudo", '#!/bin/bash\nprintf "%s\\n" "$@"\n')
            source = (ROOT / "deploy/server/vitalreach-ci-ssh").read_text()
            script = save(base / "entry", source.replace("/usr/bin/sudo", f'"{sudo}"'))
            for command in ["", "bash", "scp -t /tmp/x", "deploy latest", "deploy " + DIGEST + "; id",
                            "deploy " + DIGEST + "\nid", "deploy " + DIGEST + " extra"]:
                result = subprocess.run([BASH, script], env={**os.environ, "SSH_ORIGINAL_COMMAND": command},
                                        capture_output=True, text=True)
                self.assertEqual(result.returncode, 64, command)
            result = subprocess.run([BASH, script], env={**os.environ, "SSH_ORIGINAL_COMMAND": "deploy " + DIGEST},
                                    capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout.splitlines(), ["-n", "/usr/local/sbin/vitalreach-deploy-container", DIGEST])

    def run_engine(self, scenario, previous=True):
        with tempfile.TemporaryDirectory() as folder:
            base = pathlib.Path(folder)
            config, state = base / "config", base / "state"
            config.mkdir()
            (state / "data").mkdir(parents=True)
            (state / "backups").mkdir()
            (state / "data/db").write_text("original database")
            (config / ".env").write_text("SECRET=fixture")
            (config / "compose.production.yaml").write_text("fixture")
            if previous:
                (state / "current-image").write_text(OLD)
            events = str(base / "events").replace("\\", "/")
            mock = r'''#!/bin/bash
set -eu
printf '%s %s\n' "$APP_IMAGE" "$*" >> "EVENTS"
case "$1" in
  ps) echo caddy-id ;;
  inspect)
    if [[ "$*" == *".Config.Image"* ]]; then echo "OLD"; else echo 172.18.0.2; fi ;;
  image) echo '["CMD","curl"]' ;;
  compose)
    case "$*" in
      *" ps -aq app") if [[ "PREVIOUS" == yes ]]; then echo app-id; fi ;;
      *" pull app") [[ "SCENARIO" != pull-failure ]] ;;
      *" up -d"*)
        if [[ "SCENARIO" == activation-failure && "$APP_IMAGE" == *"CANDIDATE" ]]; then exit 1; fi ;;
    esac ;;
esac
'''
            mock = mock.replace("EVENTS", events).replace("OLD", OLD).replace("CANDIDATE", DIGEST)
            mock = mock.replace("SCENARIO", scenario).replace("PREVIOUS", "yes" if previous else "no")
            docker = save(base / "docker", mock)
            curl = save(base / "curl", "#!/bin/bash\n" + ("exit 1\n" if scenario == "https-failure" else "exit 0\n"))
            tar = save(base / "tar", "#!/bin/bash\n" + ("exit 1\n" if scenario == "backup-failure" else 'touch "$2"\n'))
            source = (ROOT / "deploy/server/deploy-container-engine").read_text()
            source = source.replace("set -Eeuo pipefail", "set -Eeuo pipefail\nexport PATH=/usr/bin:/bin:$PATH")
            # Temporary fixture only: OS privilege checks/locking require Ubuntu acceptance.
            source = source.replace("&& $EUID == 0", "&& 1 == 1")
            source = source.replace("/etc/vitalreach-container", config.as_posix())
            source = source.replace("/var/lib/vitalreach-container", state.as_posix())
            source = source.replace("/run/lock/vitalreach-container-deploy.lock", (base / "lock").as_posix())
            source = source.replace("flock -n 9", "true")
            if os.name == "nt":
                source = source.replace("install -d -m 0700", "mkdir -p")
            source = source.replace("readonly docker=/usr/bin/docker", f'readonly docker="{docker}"')
            source = source.replace("readonly curl=/usr/bin/curl", f'readonly curl="{curl}"')
            source = source.replace("/usr/bin/python3", '"' + pathlib.Path(sys.executable).as_posix() + '"')
            source = source.replace("tar -czf", f'"{tar}" -czf')
            script = save(base / "engine", source)
            result = subprocess.run([BASH, script, DIGEST], capture_output=True, text=True)
            log = pathlib.Path(events).read_text() if pathlib.Path(events).exists() else ""
            record = (state / "current-image").read_text().strip() if (state / "current-image").exists() else None
            self.assertEqual((state / "data/db").read_text(), "original database")
            return result, log, record

    def test_success(self):
        result, log, record = self.run_engine("success")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(record, REPO + "@" + DIGEST)
        self.assertLess(log.index("pull app"), log.index("stop -t 30"))
        self.assertIn("Healthy production image", result.stdout)

    def test_pull_failure_preserves_service(self):
        result, log, record = self.run_engine("pull-failure")
        self.assertNotEqual(result.returncode, 0)
        self.assertNotIn("stop -t", log)
        self.assertEqual(record, OLD)

    def test_backup_failure_restarts_old_image(self):
        result, log, record = self.run_engine("backup-failure")
        self.assertNotEqual(result.returncode, 0)
        ups = [line for line in log.splitlines() if "up -d" in line]
        self.assertEqual(len(ups), 1, result.stderr)
        self.assertTrue(ups[0].startswith(OLD))
        self.assertEqual(record, OLD)

    def test_activation_and_https_failure_restore_previous_code(self):
        for scenario in ["activation-failure", "https-failure"]:
            result, log, record = self.run_engine(scenario)
            self.assertNotEqual(result.returncode, 0)
            ups = [line for line in log.splitlines() if "up -d" in line]
            self.assertEqual(len(ups), 2, result.stderr)
            self.assertTrue(ups[-1].startswith(OLD))
            self.assertEqual(record, OLD)
            self.assertIn("Database was NOT rolled back", result.stderr)

    def test_failed_first_deployment(self):
        result, log, record = self.run_engine("activation-failure", previous=False)
        self.assertNotEqual(result.returncode, 0)
        self.assertIsNone(record)
        self.assertIn("First deployment failed", result.stderr)

if __name__ == "__main__":
    unittest.main()
