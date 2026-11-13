# Container production deployment

This path targets the new production VPS (162.246.17.53), using the shared
Caddy instance already running on hosting-edge. Existing QA/test archive
deployments and the legacy systemd production workflow remain unchanged.
Do not run the legacy production workflow against this Docker host.

## Publish an image

Commit the Dockerfile, .dockerignore, container workflow, proxy change and
deployment files to the intended GitHub repository. The checkout currently has
no configured remote; an authenticated repository owner must establish it.
Build and publish container runs on main/develop pushes and manually. Pull
requests build and smoke-test but do not publish.

The image name is ghcr.io/<lowercase-owner>/<lowercase-repository>. Its tag is
the full commit SHA. The workflow summary gives the exact APP_IMAGE digest.
Always deploy the digest, not a moving tag. The job exercises non-root,
read-only startup, health, homepage and volume persistence before pushing.
It does not substitute for QA functional acceptance or deploy production.

Allow GitHub Actions to write packages. If the package is private, authenticate
the server using a registry credential with read:packages only and appropriate
organization authorization. Use docker login ghcr.io --username YOUR_USER
interactively; do not put tokens in .env or commands committed to Git.

## Prepare production storage

Keep each client's/environment's data independent. A fresh production database
uses application seed content; do not copy QA/test records into production.
Review the seed/admin configuration and production readiness requirements.

As deployer on the VPS:

```bash
sudo install -d -o deployer -g deployer -m 0750 /opt/hosting/apps/vitalreach-prod
sudo install -d -o 1654 -g 1654 -m 0700 /opt/hosting/apps/vitalreach-prod/data
```

Copy compose.production.yaml and .env.example from deploy/containers to this
directory. Rename .env.example to .env, chmod 600, then edit production values.
No working-copy .env or local database should be uploaded.

Find Caddy's current Docker address on the existing network:

```bash
cd /opt/hosting/edge
docker inspect "$(docker compose -f compose.edge.yaml ps -q caddy)" --format '{{(index .NetworkSettings.Networks "hosting-edge").IPAddress}}'
```

Set CADDY_PROXY_IP in the application .env to that address. It is not the public
VPS IP. If Caddy is recreated its address may change: recheck and recreate the
app with the new setting. Prefer assigning a reviewed static Caddy address on
the existing Docker network before automating deployments. Do not recreate an
in-use network to assign one. Only that configured address plus framework
loopback defaults are trusted for forwarded headers. Do not enable unrestricted
ASPNETCORE_FORWARDEDHEADERS_ENABLED as a workaround.

Set APP_IMAGE to the workflow's published digest and configure Google,
reCAPTCHA and payment credentials as appropriate. Register the production
Google callback https://vitalreachhub.com/signin-google. Intake and chatbot
default disabled. Follow docs/production-deployment.md readiness requirements.

## First activation

The existing Caddy route is also versioned in deploy/containers/vitalreach-prod.caddy.
It points at the app's vitalreach-prod alias on hosting-edge:8080. There is no
published application port. Do not launch a second Caddy on ports 80/443.

```bash
cd /opt/hosting/apps/vitalreach-prod
docker network inspect hosting-edge
docker compose -f compose.production.yaml config --quiet
docker compose -f compose.production.yaml pull
docker compose -f compose.production.yaml up -d --wait --wait-timeout 180
docker compose -f compose.production.yaml ps
curl --fail --show-error https://vitalreachhub.com/health
curl --fail --show-error https://www.vitalreachhub.com/health
```

Verify login, secure cookies, uploads, product images and Blazor WebSockets.
Health alone is not acceptance. Review resource limits against actual VPS
capacity. Retain Caddy's persistent certificate volumes.

## Subsequent deployments and recovery

Before an update, save the previous APP_IMAGE digest and take a consistent
backup of data and .env with the app stopped. Startup can update database
schema/seed content. Keep encrypted off-server backups; handle .env as secret.
Restart the old image if backup fails and investigate before activation.

After installing the new digest, use up --wait and verify public health.
A failed health check does not automatically restore the previous release.
Code rollback uses the recorded previous digest, but may require deliberate
database restoration after reviewing schema compatibility and any newer data.
Never blindly overwrite a running database. Do not use down -v or delete data.

Automated SSH deployment, backup/rollback and QA-approved image promotion
are not wired into this publishing workflow yet. They should use the same
tested image digest, separate CI credentials and protected production
environment approvals, rather than rebuilding during promotion.

References:
- [GitHub image publishing](https://docs.github.com/en/actions/tutorials/publish-packages/publish-docker-images)
- [ASP.NET proxy trust](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
