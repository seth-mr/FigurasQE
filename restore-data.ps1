$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$postgresDump = Join-Path $root "docker-data\postgres\figurasqe.sql"
$mongoDump = Join-Path $root "docker-data\mongo\logsdb.archive.gz"
$rabbitDefinitions = Join-Path $root "docker-data\rabbitmq\definitions.json"

if (-not (Test-Path $postgresDump)) { throw "Missing $postgresDump" }
if (-not (Test-Path $mongoDump)) { throw "Missing $mongoDump" }
if (-not (Test-Path $rabbitDefinitions)) { throw "Missing $rabbitDefinitions" }

Write-Host "Starting infrastructure containers..."
docker compose -f (Join-Path $root "docker-compose.infra.yml") up -d

Write-Host "Waiting for PostgreSQL..."
for ($i = 0; $i -lt 30; $i++) {
    docker exec postgres-db pg_isready -U postgres -d figurasqe *> $null
    if ($LASTEXITCODE -eq 0) { break }
    Start-Sleep -Seconds 2
}

Write-Host "Restoring PostgreSQL database..."
Get-Content $postgresDump | docker exec -i -e PGPASSWORD=1234 postgres-db psql -U postgres -d figurasqe

Write-Host "Waiting for MongoDB..."
for ($i = 0; $i -lt 30; $i++) {
    docker exec mongo-logs mongosh --quiet -u seth -p 1234 --authenticationDatabase admin --eval "db.adminCommand('ping').ok" *> $null
    if ($LASTEXITCODE -eq 0) { break }
    Start-Sleep -Seconds 2
}

Write-Host "Restoring MongoDB database..."
docker cp $mongoDump mongo-logs:/tmp/logsdb.archive.gz
docker exec mongo-logs mongorestore `
    --username seth `
    --password 1234 `
    --authenticationDatabase admin `
    --archive=/tmp/logsdb.archive.gz `
    --gzip `
    --drop
docker exec mongo-logs rm -f /tmp/logsdb.archive.gz | Out-Null

Write-Host "Waiting for RabbitMQ..."
for ($i = 0; $i -lt 30; $i++) {
    docker exec rabbitmq rabbitmq-diagnostics -q ping *> $null
    if ($LASTEXITCODE -eq 0) { break }
    Start-Sleep -Seconds 2
}

Write-Host "Restoring RabbitMQ definitions..."
docker cp $rabbitDefinitions rabbitmq:/tmp/definitions.json
docker exec rabbitmq rabbitmqctl import_definitions /tmp/definitions.json
docker exec rabbitmq rm -f /tmp/definitions.json | Out-Null

Write-Host "Infrastructure data restored."
Write-Host "PostgreSQL: localhost:5432 database=figurasqe user=postgres password=1234"
Write-Host "MongoDB: mongodb://seth:1234@localhost:27017/logsdb?authSource=admin"
Write-Host "RabbitMQ: amqp://guest:guest@localhost:5672"
Write-Host "RabbitMQ Management: http://localhost:15672 user=guest password=guest"
