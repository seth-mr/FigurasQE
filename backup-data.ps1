$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$postgresDir = Join-Path $root "docker-data\postgres"
$mongoDir = Join-Path $root "docker-data\mongo"
$rabbitDir = Join-Path $root "docker-data\rabbitmq"

New-Item -ItemType Directory -Force -Path $postgresDir, $mongoDir, $rabbitDir | Out-Null

Write-Host "Exporting PostgreSQL database..."
$pgDump = docker exec -e PGPASSWORD=1234 postgres-db pg_dump `
    -U postgres `
    -d figurasqe `
    --clean `
    --if-exists `
    --no-owner `
    --no-privileges
$pgDump | Set-Content -Path (Join-Path $postgresDir "figurasqe.sql") -Encoding UTF8

Write-Host "Exporting MongoDB database..."
if (-not $env:MONGO_SOURCE_PASSWORD) {
    throw "Set MONGO_SOURCE_PASSWORD before running this script."
}
docker exec mongo-logs mongodump `
    --username seth `
    --password $env:MONGO_SOURCE_PASSWORD `
    --authenticationDatabase admin `
    --db logsdb `
    --archive=/tmp/logsdb.archive.gz `
    --gzip
docker cp mongo-logs:/tmp/logsdb.archive.gz (Join-Path $mongoDir "logsdb.archive.gz")
docker exec mongo-logs rm -f /tmp/logsdb.archive.gz | Out-Null

Write-Host "Exporting RabbitMQ definitions..."
docker exec rabbitmq rabbitmqctl export_definitions /tmp/definitions.json
docker cp rabbitmq:/tmp/definitions.json (Join-Path $rabbitDir "definitions.json")
docker exec rabbitmq rm -f /tmp/definitions.json | Out-Null

Write-Host "Backups written to docker-data."
