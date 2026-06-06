# start-all.ps1 - Levanta todos los servicios de FigurasQE en ventanas separadas

$root = $PSScriptRoot

# Limpieza preventiva: evita errores MSB3021/MSB3027 por ejecutables bloqueados.
$portsToFree = @(5041, 5124, 3000, 5028, 5186, 5190)
foreach ($port in $portsToFree) {
    $pids = netstat -ano | Select-String ":$port" | Select-String "LISTENING" | ForEach-Object {
        ($_ -split '\s+')[-1]
    } | Select-Object -Unique

    foreach ($procId in $pids) {
        if ($procId -match '^\d+$') {
            try {
                Stop-Process -Id ([int]$procId) -Force -ErrorAction Stop
                Write-Host "Puerto $port liberado (PID $procId)" -ForegroundColor DarkYellow
            } catch {
                Write-Host "No se pudo detener PID $procId para puerto $port" -ForegroundColor Yellow
            }
        }
    }
}

# También cerramos procesos host conocidos que suelen dejar el .exe bloqueado.
$hostProcessNames = @("MicroservicioFiguras", "FigurasQE-AuthenticationService")
foreach ($procName in $hostProcessNames) {
    Get-Process -Name $procName -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            Stop-Process -Id $_.Id -Force -ErrorAction Stop
            Write-Host "Proceso $procName detenido (PID $($_.Id))" -ForegroundColor DarkYellow
        } catch {
            Write-Host "No se pudo detener $procName (PID $($_.Id))" -ForegroundColor Yellow
        }
    }
}

$services = @(
    @{ Name = "Auth Service    :5041"; Dir = "$root\FigurasQE-AuthenticationService"; Cmd = "dotnet run --launch-profile http" },
    @{ Name = "Microservicio   :5124"; Dir = "$root\FiguerasQueEnse-anBD\MicroservicioFiguras"; Cmd = "dotnet run --launch-profile http" },
    @{ Name = "Gateway         :3000"; Dir = "$root\FigurasQE-Gateway"; Cmd = "node src/server.js" },
    @{ Name = "Frontend        :5028"; Dir = "$root\FigurasQE-Frontend"; Cmd = "dotnet run --launch-profile http" },
    @{ Name = "Logs Service    :5186"; Dir = "$root\FQE-LogsService\FQE.LogsService"; Cmd = "npm start" },
    @{ Name = "Rabbit Listener :Node"; Dir = "$root\FQE-RabbitMongoListener-Node"; Cmd = "npm start" },
    @{ Name = "Admin Client    :WPF"; Dir = "$root\FQE-ClienteWPF\FQE.AdminClient"; Cmd = "dotnet run" }
)

foreach ($svc in $services) {
    if (-not (Test-Path $svc.Dir)) {
        Write-Host "No se encontro la carpeta para $($svc.Name): $($svc.Dir)" -ForegroundColor Yellow
        continue
    }

    $bootstrap = "Set-Location '$($svc.Dir)'; Write-Host '=== $($svc.Name) ===' -ForegroundColor Cyan; "

    if (($svc.Cmd -like "npm*") -and (-not (Test-Path (Join-Path $svc.Dir "node_modules")))) {
        $bootstrap += "if (-not (Test-Path 'node_modules')) { npm install }; "
    }

    $bootstrap += $svc.Cmd
    Start-Process powershell -ArgumentList "-NoExit", "-Command", $bootstrap
    Start-Sleep -Milliseconds 300
}

Write-Host ""
Write-Host "Servicios iniciados:" -ForegroundColor Green
Write-Host "  Auth Service   -> http://localhost:5041" -ForegroundColor White
Write-Host "    Swagger      -> http://localhost:5041/swagger/index.html" -ForegroundColor DarkCyan
Write-Host "  Microservicio  -> http://localhost:5124" -ForegroundColor White
Write-Host "    Swagger      -> http://localhost:5124/swagger/index.html" -ForegroundColor DarkCyan
Write-Host "  Gateway        -> http://localhost:3000" -ForegroundColor White
Write-Host "    Swagger      -> http://localhost:3000/swagger" -ForegroundColor DarkCyan
Write-Host "  Frontend       -> http://localhost:5028" -ForegroundColor White
Write-Host "  Logs Service   -> http://localhost:5186" -ForegroundColor White
Write-Host "    Swagger      -> http://localhost:5186/swagger" -ForegroundColor DarkCyan
Write-Host "  Rabbit Listener-> Node worker RabbitMQ -> MongoDB" -ForegroundColor White
Write-Host "    Health       -> http://localhost:5190/health" -ForegroundColor DarkCyan
Write-Host "    Swagger      -> http://localhost:5191/swagger" -ForegroundColor DarkCyan
Write-Host "  Admin Client   -> ventana WPF" -ForegroundColor White