$ErrorActionPreference = "Stop"

function Invoke-DockerCompose {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $composePlugin = docker compose version 2>$null
    if ($LASTEXITCODE -eq 0) {
        docker compose @Arguments
        return
    }

    docker-compose @Arguments
}

Write-Host "=== Reconstruyendo MicroservicioFiguras ===" -ForegroundColor Cyan
Invoke-DockerCompose -Arguments @("up", "-d", "--build", "--force-recreate")

Write-Host ""
Write-Host "=== Esperando health check ===" -ForegroundColor Cyan

$healthUrl = "http://localhost:5124/health"
$maxAttempts = 24

for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    try {
        $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "OK: $healthUrl responde 200" -ForegroundColor Green
            Write-Host ""
            Write-Host "Swagger: http://localhost:5124/swagger" -ForegroundColor Green
            Write-Host "Contenedor: fqe-microservicio-figuras" -ForegroundColor Green
            exit 0
        }
    }
    catch {
        Write-Host "Intento $attempt/${maxAttempts}: servicio aun no disponible..."
        Start-Sleep -Seconds 3
    }
}

Write-Host ""
Write-Host "No se pudo validar el health check. Ultimos logs:" -ForegroundColor Red
Invoke-DockerCompose -Arguments @("logs", "--tail", "80", "microservicio-figuras")
exit 1
