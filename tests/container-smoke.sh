#!/usr/bin/env bash
set -Eeuo pipefail
image="${1:?Supply the image to verify}"
name="vitalreach-smoke-$$"
volume="$name-data"
cleanup() {
  docker logs "$name" 2>/dev/null || true
  docker rm -f "$name" >/dev/null 2>&1 || true
  docker volume rm "$volume" >/dev/null 2>&1 || true
}
trap cleanup EXIT
docker volume create "$volume" >/dev/null
# Named volume copies /data ownership from the image on first use.
docker run -d --name "$name" --read-only --tmpfs /tmp:rw,nosuid,size=64m \
  --cap-drop ALL --security-opt no-new-privileges:true \
  --memory 512m --cpus 1 --pids-limit 256 \
  -e AllowedHosts=vitalreachhub.com \
  -e Intake__Enabled=false -e Chatbot__Enabled=false \
  -v "$volume:/data" "$image" >/dev/null
wait_healthy() {
  for attempt in $(seq 1 90); do
    status="$(docker inspect "$name" --format '{{.State.Health.Status}}')"
    [[ "$status" != healthy ]] || return 0
    [[ "$(docker inspect "$name" --format '{{.State.Running}}')" == true ]] || return 1
    sleep 2
  done
  return 1
}
wait_healthy
[[ "$(docker exec "$name" id -u)" == 1654 ]]
docker exec "$name" test -s /data/vitalreach.db
docker exec "$name" sh -c 'test -n "$(ls -A /data/keys)"'
docker exec "$name" sh -c 'echo persistence-check > /data/uploads/products/smoke.txt'
echo 'Checking homepage and its Blazor runtime script...'
homepage="$(docker exec "$name" curl --fail --silent --show-error -H 'Host: vitalreachhub.com' http://127.0.0.1:8080/)"
# Prerendered HTML can work even when the Blazor runtime is missing.
# Follow the rendered URL, including the fingerprint chosen by the asset manifest.
blazor_path="$(printf '%s' "$homepage" | grep -oE 'src="[^"]*_framework/blazor\.web[^"]*\.js"' | sed -E 's/^src="//; s/"$//')"
[[ -n "$blazor_path" && "$blazor_path" != *$'\n'* ]]
echo "Checking Blazor runtime: $blazor_path"
docker exec "$name" curl --fail --silent --show-error -H 'Host: vitalreachhub.com' \
  "http://127.0.0.1:8080/${blazor_path#/}" > /tmp/"$name"-blazor.js
grep -q 'Blazor' /tmp/"$name"-blazor.js
rm -f /tmp/"$name"-blazor.js
# A recreation, not just a restart, must preserve mutable storage.
docker rm -f "$name" >/dev/null
docker run -d --name "$name" --read-only --tmpfs /tmp:rw,nosuid,size=64m \
  --cap-drop ALL --security-opt no-new-privileges:true \
  --memory 512m --cpus 1 --pids-limit 256 \
  -e AllowedHosts=vitalreachhub.com -e Intake__Enabled=false -e Chatbot__Enabled=false \
  -v "$volume:/data" "$image" >/dev/null
wait_healthy
docker exec "$name" grep -qx persistence-check /data/uploads/products/smoke.txt
echo 'Non-root/read-only startup, homepage, Blazor runtime and persistent volume checks passed.'
