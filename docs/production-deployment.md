# Production deployment preparation

These files prepare production; they do not install anything on the VPS or change DNS. The proposed canonical host is `vitalreachhub.com`. Confirm it before provisioning. QA continues at `qa.vitalreachhub.com`.

| Setting | QA | Production |
| --- | --- | --- |
| Environment file | `/etc/vitalreach/qa.env` | `/etc/vitalreach/prod.env` |
| Service | `vitalreach-qa` | `vitalreach-prod` |
| Loopback port | 5088 | 5089 |
| Runtime user | deployer | vitalreach-prod |
| Release root | `/opt/vitalreach/qa` | `/opt/vitalreach/prod` |
| Mutable data | `qa/shared` | `prod/shared` |
| Key application name | VitalReach.QA | VitalReach.Production |

## Provisioning (operator, once)

Requirements: .NET 10 ASP.NET Core runtime, Nginx, Python 3.12+ (tar extraction filters), curl, tar, util-linux/flock, and a TLS certificate. Commands below assume the deployer account already exists. Do not overwrite existing service files or environment values without reviewing them.

1. Create an unprivileged runtime account and independent storage:

   ```bash
   sudo useradd --system --home-dir /opt/vitalreach/prod --shell /usr/sbin/nologin vitalreach-prod
   sudo install -d -o root -g root -m 0755 /opt/vitalreach/prod /opt/vitalreach/prod/releases
   sudo install -d -o vitalreach-prod -g vitalreach-prod -m 0700 /opt/vitalreach/prod/shared
   sudo install -d -o vitalreach-prod -g vitalreach-prod -m 0700 /opt/vitalreach/prod/shared/keys /opt/vitalreach/prod/shared/uploads/products
   sudo install -d -o root -g root -m 0700 /opt/vitalreach/prod/backups
   sudo install -d -o root -g root -m 0755 /etc/vitalreach /opt/vitalreach/bin
   ```

2. Install `deploy/server/vitalreach-prod.service` in `/etc/systemd/system/`, and `deploy/server/deploy-prod` as `/opt/vitalreach/bin/deploy-prod` (root:root, 0755). Install `prod.env.example` as `/etc/vitalreach/prod.env` (root:root, 0600), then fill in production values. Keep the database/key/upload overrides in the service; do not point them at QA. Run `sudo systemctl daemon-reload` and `sudo systemctl enable vitalreach-prod.service`. The first promotion starts it after creating the `current` link.

3. Review the root-owned deployment script, then use `visudo` to allow the deployment account only this entry point:

   ```sudoers
   deployer ALL=(root) NOPASSWD: /opt/vitalreach/bin/deploy-prod
   ```

   The script validates a single SHA argument. It must never be writable by deployer or the runtime account. Protect the production SSH key and restrict GitHub environment access: release deployment grants the ability to run application code with access to production data.

4. Install `vitalreachhub.com.nginx` as an enabled Nginx site and run `sudo nginx -t`. Configure DNS and obtain/install TLS with the existing VPS certificate tooling before public launch. The supplied vhost is an HTTP bootstrap template, not the finished HTTPS configuration. Add `www` only if that hostname and its redirect/certificate are intentionally configured. Verify WebSocket connections for Blazor over HTTPS.

5. Register the production Google OAuth redirect `https://vitalreachhub.com/signin-google`. Configure production payment credentials and webhooks at `/payments/stripe/webhook` and `/payments/paystack/webhook` only when ready. Confirm seller identity, tax configuration, and the order/quote workflow. Keep intake disabled until clinical/privacy readiness is confirmed; see `consultation-intake.md`.

## Manual promotion from QA

Create a GitHub environment named **production**. Restrict deployments to main and configure required reviewers/environment protection where supported. The workflow is manual; repository code cannot create those GitHub protection settings.

Set environment configuration:

- Variable `PROD_HOST`: VPS hostname or IPv4 address (without username or port).
- Secret `PROD_SSH_PRIVATE_KEY`: dedicated production deployment key for deployer.
- Secret `PROD_SSH_KNOWN_HOSTS`: trusted SSH host-key entry obtained and fingerprint-verified out of band; never accept an unchecked key during deployment.

Run **Promote QA release to production** from main with the numeric ID of a successful main-branch QA deployment run. The explicit idle/reconnection verification checkbox must be checked after testing that release; otherwise promotion fails. See `qa-connections.md` for the test procedure. This is an operator attestation, not an automated browser test. The workflow checks the originating workflow, repository, branch, event and success result, then downloads that run's exact artifact. There is no production rebuild and merging to main alone does not deploy production. QA artifacts currently expire after 14 days; expired artifacts require a fresh successful QA run.

The server serializes deployments, validates the archive, creates an immutable release and stops production briefly for a consistent backup of the database, keys and uploads. It also backs up the environment file. It switches the release link, starts the service, and checks local health; the workflow then checks public HTTPS health. A failing local deployment attempts to restore previous code. A public HTTPS failure after successful local health requires operator investigation; it does not automatically undo a working service.

## Recovery and storage

- Code fallback does **not** revert schema/data changes. Review compatibility before rolling back. A failed first deployment leaves the service stopped for investigation.
- Backups are under `/opt/vitalreach/prod/backups/<UTC timestamp>-<SHA>/`. They contain sensitive production data and secrets. Restrict access, arrange encrypted off-server copies and a retention policy, and rehearse restoration before real patient submissions. Keeping backups only on the VPS is insufficient disaster recovery.
- Releases, backups and uploaded archives are retained; monitor disk space and establish reviewed retention/cleanup. Deployment fails if the SHA's release directory already exists. After investigating a failed deployment, an operator can rename the unused failed release directory to permit a retry. Never remove the active release.
- For manual code rollback, stop the service, atomically repoint `current` to a reviewed compatible prior release, start, and check local/public health. Restore a backup only under a deliberate downtime plan that accounts for records created since the backup. Restore database, keys and uploads together with correct ownership. Never replace production with QA data.

## Launch verification

Use synthetic data first: Google login, superadmin/reviewer permissions, QR on a phone, intake submit/read/review, urgent warning behavior, product images, checkout quote/payment webhooks, and service restart. Verify database/key/upload persistence across a second release. Health reports process availability, not database integrity, payment connectivity, clinical readiness or backup recoverability. Set up external uptime/error monitoring and assign responsibility for the clinical inbox before enabling intake.

The initial production database uses the repository's existing seed content and admin bootstrap. Review product inventory, headquarters/contact information, and initial admin roles before launch; QA test transactions and consultation records are not copied.
