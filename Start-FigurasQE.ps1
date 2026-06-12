param(
    [string]$LanIp
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectRoot

function Get-PrimaryLanIp {
    $virtualInterfacePattern = "Loopback|vEthernet|Docker|Radmin|VirtualBox|VMware|Tailscale|ZeroTier"
    $routes = Get-NetRoute -DestinationPrefix "0.0.0.0/0" -ErrorAction SilentlyContinue |
        Where-Object { $_.NextHop -ne "0.0.0.0" } |
        Sort-Object @{ Expression = { $_.RouteMetric + $_.InterfaceMetric } }

    foreach ($route in $routes) {
        $adapter = Get-NetAdapter -InterfaceIndex $route.InterfaceIndex -ErrorAction SilentlyContinue
        if (-not $adapter -or $adapter.Status -ne "Up" -or $adapter.Name -match $virtualInterfacePattern) {
            continue
        }

        $address = Get-NetIPAddress -InterfaceIndex $route.InterfaceIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue |
            Where-Object {
                $_.IPAddress -notlike "127.*" -and
                $_.IPAddress -notlike "169.254.*"
            } |
            Select-Object -ExpandProperty IPAddress -First 1

        if ($address) {
            return $address
        }
    }

    foreach ($route in $routes) {
        $address = Get-NetIPAddress -InterfaceIndex $route.InterfaceIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue |
            Where-Object {
                $_.IPAddress -notlike "127.*" -and
                $_.IPAddress -notlike "169.254.*"
            } |
            Select-Object -ExpandProperty IPAddress -First 1

        if ($address) {
            return $address
        }
    }

    return $null
}

if (-not $LanIp) {
    $LanIp = Get-PrimaryLanIp
}

if (-not $LanIp) {
    throw "No se pudo detectar la interfaz de red. Ejecuta: .\Start-FigurasQE.ps1 -LanIp 192.168.x.x"
}

$parsedIp = $null
if (-not [System.Net.IPAddress]::TryParse($LanIp, [ref]$parsedIp) -or
    $parsedIp.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork) {
    throw "La direccion '$LanIp' no es una IPv4 valida."
}

$env:LAN_IP = $LanIp
$postgresPassword = if ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { "1234" }
$mongoUsername = if ($env:MONGO_ROOT_USERNAME) { $env:MONGO_ROOT_USERNAME } else { "seth" }
$mongoPassword = if ($env:MONGO_ROOT_PASSWORD) { $env:MONGO_ROOT_PASSWORD } else { "1234" }
Write-Host "Levantando FigurasQE para la IP $LanIp..." -ForegroundColor Cyan
docker compose up -d --build
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose no pudo iniciar el proyecto."
}

$levelsTable = docker exec -e "PGPASSWORD=$postgresPassword" postgres-db psql `
    -U postgres `
    -d figurasqe `
    -tAc "SELECT to_regclass('public.levels') IS NOT NULL"

if ($levelsTable.Trim() -ne "t") {
    Write-Host "Base de datos vacia. Restaurando los datos incluidos..." -ForegroundColor Cyan

    Get-Content (Join-Path $projectRoot "docker-data\postgres\figurasqe.sql") |
        docker exec -i -e "PGPASSWORD=$postgresPassword" postgres-db psql `
            -v ON_ERROR_STOP=1 `
            -U postgres `
            -d figurasqe
    if ($LASTEXITCODE -ne 0) { throw "No se pudo restaurar PostgreSQL." }

    $mongoBackup = Join-Path $projectRoot "docker-data\mongo\logsdb.archive.gz"
    docker cp $mongoBackup mongo-logs:/tmp/logsdb.archive.gz
    docker exec mongo-logs mongorestore `
        --username $mongoUsername `
        --password $mongoPassword `
        --authenticationDatabase admin `
        --archive=/tmp/logsdb.archive.gz `
        --gzip `
        --drop
    if ($LASTEXITCODE -ne 0) { throw "No se pudo restaurar MongoDB." }
    docker exec mongo-logs rm -f /tmp/logsdb.archive.gz | Out-Null

    $rabbitBackup = Join-Path $projectRoot "docker-data\rabbitmq\definitions.json"
    docker cp $rabbitBackup rabbitmq:/tmp/definitions.json
    docker exec rabbitmq rabbitmqctl import_definitions /tmp/definitions.json
    if ($LASTEXITCODE -ne 0) { throw "No se pudo restaurar RabbitMQ." }
    docker exec rabbitmq rm -f /tmp/definitions.json | Out-Null

    docker compose restart microservicio-figuras
}

$certificateDirectory = Join-Path $projectRoot "certificados"
$certificatePath = Join-Path $certificateDirectory "FigurasQE-CA.crt"
New-Item -ItemType Directory -Path $certificateDirectory -Force | Out-Null

$ready = $false
for ($attempt = 1; $attempt -le 30; $attempt++) {
    docker exec figurasqe-https-proxy test -f /data/caddy/pki/authorities/local/root.crt 2>$null
    if ($LASTEXITCODE -eq 0) {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 2
}

if (-not $ready) {
    throw "El proxy HTTPS inicio, pero no genero el certificado a tiempo. Revisa: docker compose logs https-proxy"
}

docker cp "figurasqe-https-proxy:/data/caddy/pki/authorities/local/root.crt" $certificatePath
if ($LASTEXITCODE -ne 0) {
    throw "No se pudo extraer el certificado HTTPS."
}

Copy-Item `
    -LiteralPath (Join-Path $projectRoot "Instalar-Certificado-Cliente.ps1") `
    -Destination (Join-Path $certificateDirectory "Instalar-Certificado-Cliente.ps1") `
    -Force

Write-Host ""
Write-Host "Proyecto disponible en: https://${LanIp}:8443" -ForegroundColor Green
Write-Host "Certificado para los clientes: $certificatePath" -ForegroundColor Yellow

$publicUrl = $null
for ($attempt = 1; $attempt -le 30; $attempt++) {
    $tunnelLogs = docker compose logs --no-color public-tunnel 2>$null
    $urlMatches = [regex]::Matches(
        ($tunnelLogs -join "`n"),
        "https://[a-z0-9-]+\.trycloudflare\.com"
    )
    if ($urlMatches.Count -gt 0) {
        $publicUrl = $urlMatches[$urlMatches.Count - 1].Value
        break
    }
    Start-Sleep -Seconds 2
}

if ($publicUrl) {
    Set-Content `
        -Path (Join-Path $projectRoot "URL-PUBLICA-ACTUAL.txt") `
        -Value $publicUrl `
        -Encoding ascii
    Write-Host "URL HTTPS sin certificado manual: $publicUrl" -ForegroundColor Green
} else {
    Write-Host "El tunel publico aun no reporta su URL. Revisa: docker compose logs public-tunnel" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "En cada Windows cliente, instala FigurasQE-CA.crt en:" -ForegroundColor Cyan
Write-Host "Equipo local > Entidades de certificacion raiz de confianza."
Write-Host "Tambien puedes ejecutar Instalar-Certificado-Cliente.ps1 como administrador."
Write-Host "Luego cierra y vuelve a abrir el navegador."

$firewallRule = Get-NetFirewallRule -DisplayName "FigurasQE HTTPS 8443" -ErrorAction SilentlyContinue
if (-not $firewallRule) {
    Write-Host ""
    Write-Host "AVISO: falta habilitar el puerto 8443 en Firewall de Windows." -ForegroundColor Yellow
    Write-Host "Ejecuta Configurar-Firewall.ps1 como administrador una sola vez."
}
