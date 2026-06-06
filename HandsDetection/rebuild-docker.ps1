$ErrorActionPreference = "Stop"

function Invoke-DockerCompose {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    docker compose version 1>$null 2>$null
    if ($LASTEXITCODE -eq 0) {
        docker compose @Arguments
        return
    }

    docker-compose @Arguments
}

Write-Host "=== Reconstruyendo HandsDetection ===" -ForegroundColor Cyan
Invoke-DockerCompose -Arguments @("up", "-d", "--build", "--force-recreate")

Write-Host ""
Write-Host "=== Esperando healthcheck Docker ===" -ForegroundColor Cyan

$maxAttempts = 40
for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    $health = docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' fqe-hands-detection 2>$null
    if ($health -eq "healthy") {
        Write-Host "OK: healthcheck Docker en estado healthy" -ForegroundColor Green
        Write-Host "Contenedor: fqe-hands-detection" -ForegroundColor Green
        exit 0
    }

    Write-Host "Intento $attempt/${maxAttempts}: estado actual = $health"
    Start-Sleep -Seconds 3
}

Write-Host ""
Write-Host "No se pudo validar el healthcheck. Ultimos logs:" -ForegroundColor Red
Invoke-DockerCompose -Arguments @("logs", "--tail", "80", "hands-detection")
exit 1
