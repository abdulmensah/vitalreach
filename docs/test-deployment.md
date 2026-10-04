# Develop preview deployment

`develop` → `test.vitalreachhub.com`; `main` → existing QA. This preview is independent of production promotion. It is publicly reachable and marked noindex; noindex is not access control. Use synthetic data only.

Initial rollout completed October 2, 2026 from the local POC working copy. Archive identifier: `b6ef2db40f5834fce9ea069926af28928c2d3658`. TLS, local/public health, noindex headers and a live Blazor menu interaction were verified; QA health remained successful. GitHub environment/secrets are not yet configured by this setup because local `gh` is unauthenticated. Commit/push the workflow and configure the secrets before expecting automatic deployments. Admin Google sign-in is unconfigured until test OAuth credentials are supplied.

| Setting | Test preview |
| --- | --- |
| Workflow | `.github/workflows/test.yml` (push to develop or manual from develop) |
| GitHub environment | `test`, restrict deployment branches to `develop` |
| Runtime/service | `vitalreach-test` |
| Loopback port | `5090` |
| Environment file | `/etc/vitalreach/test.env` (root:root, 0600) |
| Release root | `/opt/vitalreach/test/releases` |
| Persistent DB | `/opt/vitalreach/test/shared/vitalreach.db` |
| Encryption keys | `/opt/vitalreach/test/shared/keys`, application name `VitalReach.Test` |
| Product uploads | `/opt/vitalreach/test/shared/uploads/products` |
| Deployment entry point | `/opt/vitalreach/bin/deploy-test` |

## Initial server setup

DNS A record must point to `162.35.104.81`; remove or correct any unrelated AAAA record. Upload `provision-test`, `deploy-test`, `vitalreach-test.service`, `test.env.example`, and `test.vitalreachhub.com.nginx` together to a temporary directory on the VPS, then run `sudo bash /path/to/provision-test`. The installer preserves an existing environment file and Nginx vhost, including Certbot changes. It does not modify QA or production services/storage.

The runtime uses the Production ASP.NET environment for secure error handling, with independent configuration and storage. Intake defaults off, provider credentials are blank, and a new database uses repository seed content. Do not copy QA clinical data or encryption keys into test. Store overrides only in `test.env`; never set paths to QA/production storage. Optional admin sign-in requires Google credentials and the redirect `https://test.vitalreachhub.com/signin-google`.

After installing the HTTP vhost, obtain TLS using the VPS's existing Certbot account:

```bash
sudo certbot --nginx -d test.vitalreachhub.com --redirect --non-interactive
```

Verify `/health`, the homepage, `/robots.txt`, the `X-Robots-Tag` response header, and Blazor WebSockets over HTTPS.

## GitHub setup

Create the environment **test** in repository Settings → Environments; allow only `develop`. Add:

- `TEST_SSH_PRIVATE_KEY`: an authorized deployment key for `deployer` on the VPS.
- `TEST_SSH_KNOWN_HOSTS`: the VPS host-key entry from a trusted local known_hosts file, after verifying its fingerprint. Do not use an unchecked ssh-keyscan result.

GitHub CLI must be authenticated before it can configure these settings. Secrets may be entered through the GitHub UI or `gh secret set --env test` using a local file/stdin; never paste secrets into chat or commit them. Deployment executes trusted branch code with access to test data; restrict write access accordingly.

Commit and push the POC and workflow to `develop`. Each push builds, runs regression checks, uploads an archive, activates test and checks HTTPS health. `main` and QA are not deployment targets of this workflow. A manual run also enforces the `develop` branch. Test deployment runs are serialized so an active deployment is not interrupted by the next push.

## Manual working-copy preview

To preview uncommitted work, publish the current project to a clean output directory, package only published files and upload as `/tmp/vitalreach-test-<release-id>.tar.gz`. Use the archive's SHA-1 digest as the 40-character release ID (an artifact identifier, not a claim that the working tree is committed):

```bash
sudo /opt/vitalreach/bin/deploy-test <release-id>
```

CI uses the Git commit SHA instead. Do not include `.env`, `App_Data`, local databases, or keys in archives. The server validator rejects local config, mutable data, links, unsafe paths and missing application DLLs.

## Recovery and eventual adoption

Activation briefly stops test, backs up shared data and its environment, then swaps the current release link and verifies local health. Failure attempts to restore the preceding code. Database changes are never automatically reverted. Backups remain in `/opt/vitalreach/test/backups`; establish retention and monitor disk usage. A repeat release ID is rejected; investigate before retrying or use a new commit. Public HTTPS failure after a successful local check requires investigation.

After design approval, merge develop into main through the normal QA process. Keep production promotion separate. Retire the test service, DNS and secrets deliberately when no longer needed; merging does not remove them automatically.
