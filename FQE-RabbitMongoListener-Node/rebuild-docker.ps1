Write-Host "=== Reconstruyendo FQE-RabbitMongoListener-Node en Docker ===" -ForegroundColor Cyan

$ErrorActionPreference = "Stop"
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Set-Location $ProjectDir

Write-Host ""
Write-Host "Directorio del proyecto: $ProjectDir" -ForegroundColor DarkGray

Write-Host ""
Write-Host "1) Reconstruyendo imagen y reiniciando contenedor..." -ForegroundColor Yellow
docker compose up -d --build

Write-Host ""
Write-Host "2) Estado de contenedores..." -ForegroundColor Yellow
docker compose ps

Write-Host ""
Write-Host "3) Health check..." -ForegroundColor Yellow
$healthOk = $false
for ($i = 1; $i -le 12; $i++) {
    try {
        $health = Invoke-RestMethod -Uri "http://localhost:5190/health" -TimeoutSec 5
        $health | ConvertTo-Json -Depth 10
        $healthOk = $true
        break
    }
    catch {
        Write-Host "Intento $i/12: esperando health endpoint..." -ForegroundColor DarkYellow
        Start-Sleep -Seconds 2
    }
}

if (-not $healthOk) {
    Write-Host "No se pudo consultar http://localhost:5190/health despues de varios intentos." -ForegroundColor Red
    Write-Host "Revisa logs con: docker compose logs -f listener" -ForegroundColor Red
}

Write-Host ""
Write-Host "Listo. Para ver logs usa:" -ForegroundColor Green
Write-Host "docker compose logs -f listener" -ForegroundColor Green
