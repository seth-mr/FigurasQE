$ErrorActionPreference = "Stop"

$root = "C:\fig\newman"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputDir = Join-Path $root "reports\$timestamp"

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$runs = @(
    @{
        Name = "auth-gateway"
        Collection = "C:\fig\newman\auth-gateway\auth-gateway.postman_collection.json"
        Environment = "C:\fig\newman\auth-gateway\auth-gateway.postman_environment.json"
    },
    @{
        Name = "logs-listener"
        Collection = "C:\fig\newman\logs-listener\logs-listener.postman_collection.json"
        Environment = "C:\fig\newman\logs-listener\logs-listener.postman_environment.json"
    },
    @{
        Name = "gateway"
        Collection = "C:\fig\newman\gateway\gateway.postman_collection.json"
        Environment = "C:\fig\newman\gateway\gateway.postman_environment.json"
    },
    @{
        Name = "data-service"
        Collection = "C:\fig\newman\data-service\data-service.postman_collection.json"
        Environment = "C:\fig\newman\data-service\data-service.postman_environment.json"
    }
)

$summary = @()
$hadFailure = $false

foreach ($run in $runs) {
    $jsonOut = Join-Path $outputDir "$($run.Name).report.json"
    $junitOut = Join-Path $outputDir "$($run.Name).report.xml"

    Write-Host ""
    Write-Host "=== Running $($run.Name) ===" -ForegroundColor Cyan

    & npx newman run $run.Collection `
        -e $run.Environment `
        --reporters cli,json,junit `
        --reporter-json-export $jsonOut `
        --reporter-junit-export $junitOut

    $exitCode = $LASTEXITCODE
    $report = Get-Content -Raw -Path $jsonOut | ConvertFrom-Json

    $stats = $report.run.stats
    $assertionsTotal = [int]$stats.assertions.total
    $assertionsFailed = [int]$stats.assertions.failed
    $requestsTotal = [int]$stats.requests.total
    $requestsFailed = [int]$stats.requests.failed

    $passed = ($exitCode -eq 0 -and $assertionsFailed -eq 0 -and $requestsFailed -eq 0)
    if (-not $passed) {
        $hadFailure = $true
    }

    $summary += [pscustomobject]@{
        name = $run.Name
        passed = $passed
        exitCode = $exitCode
        requestsTotal = $requestsTotal
        requestsFailed = $requestsFailed
        assertionsTotal = $assertionsTotal
        assertionsFailed = $assertionsFailed
        durationMs = [int64]$report.run.timings.completed - [int64]$report.run.timings.started
        jsonReport = [System.IO.Path]::GetFileName($jsonOut)
        junitReport = [System.IO.Path]::GetFileName($junitOut)
    }
}

$summaryJsonPath = Join-Path $outputDir "summary.json"
$summaryMdPath = Join-Path $outputDir "summary.md"

$payload = [pscustomobject]@{
    generatedAt = (Get-Date).ToString("o")
    overallPassed = (-not $hadFailure)
    outputDir = $outputDir
    runs = $summary
}

$payload | ConvertTo-Json -Depth 5 | Set-Content -Path $summaryJsonPath

$md = @()
$md += "# FQE Newman Suite"
$md += ""
$md += ("- Generated at: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))
$md += ("- Overall: {0}" -f $(if ($hadFailure) { 'FAILED' } else { 'PASSED' }))
$md += ("- Reports folder: {0}" -f $outputDir)
$md += ""
$md += "| Suite | Result | Requests | Assertions | JSON | JUnit |"
$md += "|---|---|---:|---:|---|---|"

foreach ($item in $summary) {
    $result = if ($item.passed) { "PASSED" } else { "FAILED" }
    $requestsCell = "$($item.requestsTotal) total / $($item.requestsFailed) failed"
    $assertionsCell = "$($item.assertionsTotal) total / $($item.assertionsFailed) failed"
    $md += "| $($item.name) | $result | $requestsCell | $assertionsCell | $($item.jsonReport) | $($item.junitReport) |"
}

$md += ""
$md += "## Files"
$md += ""
foreach ($item in $summary) {
    $md += "- $($item.name): $($item.jsonReport), $($item.junitReport)"
}

$md -join "`r`n" | Set-Content -Path $summaryMdPath

Write-Host ""
Write-Host "Summary written to:" -ForegroundColor Green
Write-Host "  $summaryMdPath"
Write-Host "  $summaryJsonPath"

if ($hadFailure) {
    exit 1
}

exit 0
