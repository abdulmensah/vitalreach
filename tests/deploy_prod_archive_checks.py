"""Exercise the actual deploy-prod archive validator without touching a VPS."""
import io
import pathlib
import sys
import tarfile
import tempfile

script_path = pathlib.Path(__file__).resolve().parents[1] / (sys.argv[1] if len(sys.argv) > 1 else "deploy/server/deploy-prod")
script = script_path.read_text()
validator = compile(script.split("<<'PY'\n", 1)[1].split("\nPY", 1)[0], "deploy-prod-validator", "exec")
cases = [("assets/site.css", False, False), ("../escape", False, True),
         ("/tmp/escape", False, True), ("link", True, True),
         (".env", False, True), ("App_Data/keys/key.xml", False, True)]
for name, link, reject in cases:
    with tempfile.TemporaryDirectory() as temp:
        root = pathlib.Path(temp)
        archive = root / "release.tar.gz"
        with tarfile.open(archive, "w:gz") as tar:
            for member_name in ["VitalReach.Web.dll", name]:
                member = tarfile.TarInfo(member_name)
                if link and member_name == name:
                    member.type = tarfile.SYMTYPE
                    member.linkname = "../escape"
                    tar.addfile(member)
                else:
                    member.size = 4
                    tar.addfile(member, io.BytesIO(b"test"))
        previous = sys.argv
        sys.argv = ["validator", str(archive), str(root / "app")]
        rejected = False
        try:
            exec(validator, {})
        except SystemExit:
            rejected = True
        finally:
            sys.argv = previous
        assert rejected == reject, name
print(f"PASS: {len(cases)} archive checks for {script_path.name}")
