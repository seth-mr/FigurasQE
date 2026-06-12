$ErrorActionPreference = "Stop"

$displayName = "FigurasQE HTTPS 8443"
$existingRule = Get-NetFirewallRule -DisplayName $displayName -ErrorAction SilentlyContinue

if ($existingRule) {
    Set-NetFirewallRule `
        -DisplayName $displayName `
        -Enabled True `
        -Direction Inbound `
        -Action Allow `
        -Profile Any
} else {
    New-NetFirewallRule `
        -DisplayName $displayName `
        -Direction Inbound `
        -Action Allow `
        -Protocol TCP `
        -LocalPort 8443 `
        -Profile Any
}

Write-Host "Puerto TCP 8443 habilitado para FigurasQE." -ForegroundColor Green
