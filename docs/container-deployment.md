# Docker production deployment

Production runs on 162.246.17.53 behind the existing hosting-edge Caddy.
Release branch: main-release. The production workflow now deploys containers;
the old archive/systemd production workflow has been replaced. QA and test
archive workflows are unchanged. No server is changed by adding these files.

## GitHub setup

The production environment needs:
- Variable PROD_HOST: 162.246.17.53.
- Secret PROD_SSH_PRIVATE_KEY: the dedicated production CI private key.
- Secret PROD_SSH_KNOWN_HOSTS: the verified host-key entry for this VPS.

Allow only main-release to deploy to production and configure required reviewers
where supported. Protect that branch and workflow files. The deployment SSH
username is fixed to vitalreach-ci, not the administrative deployer account.
The existing GitHub secret can hold the same dedicated key; install its public
half on the restricted account below.

The build workflow publishes ghcr.io/abdulmensah/vitalreach:<full-commit-sha>
after its container checks pass. It also uploads a container-image artifact
containing the immutable digest and source commit. No custom GHCR publishing
key is needed; GitHub's job token has packages:write. main-release is now an
automatic build trigger, alongside main/develop.

Run Deploy production container from main-release, supplying the numeric ID
of a successful main-release Build and publish container run. Attest that the
release passed QA including login, uploads and idle/reconnection. The workflow
checks the source repository, workflow, branch, event, result, commit and image
record before using production SSH. It does not automatically prove functional
QA acceptance or deploy the image to QA. Production never rebuilds the image.
Expired image artifacts require a new successful build.

## One-time server setup (administrator)

These commands are for the existing deployer administrator session. Keep that
administrator key separate. Transfer the following repository files together
to a temporary directory on the VPS, plus the CI PUBLIC key:

- deploy/server/install-container-ci
- deploy/server/vitalreach-ci-ssh
- deploy/server/vitalreach-deploy-container
- deploy/server/deploy-container-engine
- deploy/containers/compose.production.yaml
- deploy/containers/.env.example

Run from that directory, using the actual public-key filename:

```bash
sudo bash install-container-ci ./vitalreach_prod_ci_ed25519.pub
sudo install -o root -g root -m 0600 compose.production.yaml /etc/vitalreach-container/compose.production.yaml
# FIRST INSTALL ONLY. Do not overwrite an existing configured .env.
sudo test -e /etc/vitalreach-container/.env || sudo install -o root -g root -m 0600 .env.example /etc/vitalreach-container/.env
sudoedit /etc/vitalreach-container/.env
```

The installer creates a dedicated vitalreach-ci account, with a root-owned home
and authorized_keys. It has no Docker/sudo group membership. The key has
restrict plus a forced command; only "deploy sha256:<64 lowercase hex digits>"
is accepted. The narrow sudo rule runs a root-owned wrapper that validates its
argument and starts the engine with an empty environment. Shell sessions, SCP,
forwarding and arbitrary commands are denied. There is no upload endpoint.

If this CI public key was previously added to deployer/root authorized_keys,
remove THAT key's unrestricted entry after checking the restricted connection.
Do not remove the separate administrator key. Additional SSH AllowUsers/Match
policies must permit key login for vitalreach-ci. Root console access remains
available for recovery. The installer does not rewrite global SSH policy.

Configuration is root-owned under /etc/vitalreach-container, state under
/var/lib/vitalreach-container, app data under its data directory owned by UID
1654. These paths intentionally avoid administrator-owned /opt/hosting parents
for privileged Compose inputs. The Caddy stack stays in /opt/hosting/edge.

The .env contains application secrets and memory/CPU budgets. APP_IMAGE and
CADDY_PROXY_IP placeholders are overridden by the deployment command; do not
hardcode a tag for promotion. The command looks up the running hosting-edge
Caddy IP and trusts that exact address. If Caddy is recreated between app
deployments, redeploy the current digest to refresh trust. Consider a reviewed
static Caddy address later. App containers have no published host ports.

Register https://vitalreachhub.com/signin-google with Google OAuth. Review
production secrets, seed content, administrator roles and payment setup.
Intake/chatbot remain disabled until explicitly configured. No QA data is copied.

If GHCR is private, configure ROOT's registry login once (the engine runs as
root). Use a credential with read:packages only:

```bash
sudo docker login ghcr.io --username YOUR_GITHUB_USERNAME
```

Enter the credential at the prompt, not in a command line or Git-tracked file.
The admin deployer's Docker login alone will not authenticate root.

No application should already run under the vitalreach-prod Compose project on
first installation. If a manually deployed container/data exists at
/opt/hosting/apps/vitalreach-prod, stop and migrate it deliberately: back it up,
copy database/keys/uploads together into the new data directory, preserve UID
1654 ownership, and record its exact running digest as current-image before
adoption. The engine refuses an existing app with no matching image record.

## Verify the restriction before deployment

From your local computer, with the dedicated CI key:

```bash
ssh -T -o BatchMode=yes -o IdentitiesOnly=yes -i /path/to/vitalreach_prod_ci_ed25519 vitalreach-ci@162.246.17.53 id
```

Expected result: "Only deploy sha256:..." and exit status 64. A shell or id output
means the key is not restricted correctly. Verify the SSH host fingerprint via
the hosting console before adding it to the GitHub known-hosts secret.

Then run the protected production workflow. It locks deployments, downloads
the candidate before downtime, stops the existing app, backs up data plus .env
and Compose, starts the new digest and waits for Docker health. It checks
HTTPS for BOTH domains through this server's Caddy using loopback resolution.
The workflow then verifies public DNS/HTTPS separately.

After deployment, verify login, uploads, product images and Blazor WebSockets.
Health is availability, not full functional acceptance.

## Recovery and maintenance

The current successful digest is in /var/lib/vitalreach-container/current-image.
Backups are in /var/lib/vitalreach-container/backups/<timestamp>-<digest>-<pid>.
They contain secrets and sensitive data; arrange encrypted off-server copies
and retention separately. The local backup is not disaster recovery.

Pull failure leaves the existing app running. Backup failure restarts previous
code. Candidate-startup or local HTTPS failure stops the candidate and attempts
to restart the previous digest, leaving the successful-image record unchanged.
On a first-deployment failure, the candidate stays stopped for investigation.
Remove that failed container deliberately before a first-deployment retry;
do not remove its data. Database/schema changes are NOT automatically reversed.
Review compatibility and backup contents before any data restoration. Automatic
code recovery can fail; an operator must handle that case.

A public HTTPS failure AFTER successful server activation does not automatically
undo the deployment: inspect DNS/proxy connectivity. Loss of SSH, host power or
SIGKILL can interrupt recovery; inspect container state, current-image and
backups before retrying. No trap can guarantee recovery from host failure.

To redeploy a previously accepted digest, an administrator may run:
```bash
sudo /usr/local/sbin/vitalreach-deploy-container sha256:THE_64_CHARACTER_DIGEST
```

Only this repository's images can be selected, but deployment still grants code
access to production application secrets/data. The forced command is not an
image-signing policy or protection against a malicious publisher. Keep package
publishing and production-environment access tightly controlled.

Compose, the server scripts and Caddy routes remain reviewed administrative
configuration; CI cannot upload/replace privileged files or proxy routes.
Install reviewed updates with the administrator account. The installer never
starts/reloads Caddy. Keep its persistent certificate volumes. Do not run
compose down -v or delete the data directory during normal updates.

## Validation

tests/container_deploy_checks.py tests forced-command rejection and pull,
backup, activation, HTTPS, first-deployment and successful-recording paths using
mock host tools. It never touches Docker or SSH. Privilege/permission enforcement,
flock and actual container/TLS behavior still need Ubuntu acceptance testing.
The publishing workflow separately runs the real container smoke checks.

References:
- [GitHub artifact download](https://github.com/actions/download-artifact)
- [OpenSSH authorized key restrictions](https://man.openbsd.org/sshd.8)
