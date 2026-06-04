#!/usr/bin/env bash
# Starts the stack, waits for the API to be healthy, then opens the console UI.
#
#   ./run.sh                          # uses "docker compose"
#   COMPOSE="podman compose" ./run.sh # for Podman
#
# Ctrl+C stops the log tail; the containers keep running. Stop them with:
#   docker compose down   (or: podman compose down)

set -euo pipefail

COMPOSE="${COMPOSE:-docker compose}"
URL="${URL:-http://localhost:8080/}"

$COMPOSE up -d --build

echo "Waiting for the API to become healthy..."
healthy=false
for _ in $(seq 1 60); do
  if curl -fsS "${URL}health" >/dev/null 2>&1; then healthy=true; break; fi
  sleep 1
done

if [ "$healthy" = true ]; then
  echo "API is up. Opening ${URL}"
  if command -v xdg-open >/dev/null 2>&1; then xdg-open "$URL"
  elif command -v open >/dev/null 2>&1; then open "$URL"
  else echo "Open ${URL} in your browser."; fi
else
  echo "API did not become healthy in time; check '$COMPOSE logs'." >&2
fi

$COMPOSE logs -f
