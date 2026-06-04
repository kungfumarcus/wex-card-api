#!/usr/bin/env pwsh
# Starts the stack, waits for the API to be healthy, then opens the console UI.
#
#   ./run.ps1                              # uses "docker compose"
#   ./run.ps1 -Compose "podman compose"    # for Podman
#
# Ctrl+C stops the log tail; the containers keep running. Stop them with:
#   docker compose down        (or: podman compose down)

param(
    [string]$Compose = "docker compose",
    [string]$Url = "http://localhost:8080/"
)

$ErrorActionPreference = "Stop"

Invoke-Expression "$Compose up -d --build"

Write-Host "Waiting for the API to become healthy..."
$healthy = $false
for ($i = 0; $i -lt 60; $i++) {
    try {
        $r = Invoke-WebRequest "${Url}health" -UseBasicParsing -TimeoutSec 2
        if ($r.StatusCode -eq 200) { $healthy = $true; break }
    } catch { }
    Start-Sleep -Seconds 1
}

if ($healthy) {
    Write-Host "API is up. Opening $Url"
    Start-Process $Url
} else {
    Write-Warning "API did not become healthy in time; check '$Compose logs'."
}

Invoke-Expression "$Compose logs -f"
