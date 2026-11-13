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

### October 8, 2026 working-copy release

Activated artifact `c2fe900b360338fe3c707f21ecbc190a8aa0f2ec` (archive SHA-1, not a Git commit) with the mission slideshow, compact overlay footer, updated imagery and mission content. SHA-256: `b68b9cc67a019454ee0c892ff3af529c2030582ac111e655b4d1c065948e95a9`. The mission activity anchor was corrected during browser verification.

Backup: `/opt/vitalreach/test/backups/20261008T031856Z-c2fe900b360338fe3c707f21ecbc190a8aa0f2ec`. Existing test database, uploads and environment were preserved; application startup applied its normal schema/seed updates. No local database or environment file was uploaded. Release publish and the .NET/browser regression checks passed. Public HTTPS health, mission rendering, interactive photo selection and image loading passed. Test was active with zero automatic restarts, and QA health remained successful. Deployment was manual from the working copy; GitHub was not changed.

Activation briefly stops test, backs up shared data and its environment, then swaps the current release link and verifies local health. Failure attempts to restore the preceding code. Database changes are never automatically reverted. Backups remain in `/opt/vitalreach/test/backups`; establish retention and monitor disk usage. A repeat release ID is rejected; investigate before retrying or use a new commit. Public HTTPS failure after a successful local check requires investigation.

After design approval, merge develop into main through the normal QA process. Keep production promotion separate. Retire the test service, DNS and secrets deliberately when no longer needed; merging does not remove them automatically.

## October 6 stabilization

The test deployment helper now requires three consecutive process/health checks followed by successful rendered contact, privacy, terms, wellness-center and shop pages. Failures during backup, activation or page checks attempt to recover the preceding code; database backups remain available for deliberate recovery. The workflow includes site-settings, reCAPTCHA/chatbot and browser verification checks and exercises public HTTPS pages after activation.

Activated working-copy artifact: `8acb6e35df56ef07ee2445f7c5e031e188af091f` (archive SHA-1 identifier, not a commit). SHA-256: `bc0db4636b361613ba1e543ed5ffdb3b043c762ef1e52994313cd3e477c4a43f`. Archive: `/tmp/vitalreach-test-8acb6e35df56ef07ee2445f7c5e031e188af091f.tar.gz`. Activation succeeded October 6 after the operator supplied credentials. Existing test data and uploads were preserved; the pre-activation backup is `/opt/vitalreach/test/backups/20261006T231618Z-8acb6e35df56ef07ee2445f7c5e031e188af091f`. Public HTTPS pages, noindex headers, updated policies and the interactive Vira panel passed verification. The service was active with zero automatic restarts, and QA remained healthy. Live contact submission and a complete AI conversation still require an end-to-end user check.

Before activating this release, set the reCAPTCHA v3 site/secret pair and `Chatbot__ApiKey` in `/etc/vitalreach/test.env`. Use `Recaptcha__AllowedHosts=test.vitalreachhub.com`, `Recaptcha__ScoreThreshold=0.5`, `Recaptcha__DevelopmentBypass=false`, `Chatbot__Enabled=true`, `Chatbot__Model=gpt-5-nano`, `Chatbot__MaxOutputTokens=2048`, `Chatbot__ReasoningEffort=low`, and `Chatbot__MaxRequestsPerHour=60`. Keep the file root-owned with mode 0600. Do not copy the local `.env` wholesale: it contains local storage and hosting settings. Activation restarts the service and loads the updated environment.

Admin sign-in additionally requires Google OAuth credentials with the test callback registered. Intake remains disabled until its own readiness requirements are satisfied. GitHub deployment credentials still need to be verified by an authenticated repository administrator; staging a manual release does not configure GitHub Actions.
