$ErrorActionPreference = "Stop"

$certificatePath = Join-Path $PSScriptRoot "FigurasQE-CA.crt"
if (-not (Test-Path $certificatePath)) {
    throw "No se encontro FigurasQE-CA.crt junto a este script."
}

Get-ChildItem Cert:\LocalMachine\Root |
    Where-Object { $_.Subject -like "CN=Caddy Local Authority*" } |
    ForEach-Object {
        Remove-Item -LiteralPath $_.PSPath -Force
    }

$installed = Import-Certificate `
    -FilePath $certificatePath `
    -CertStoreLocation Cert:\LocalMachine\Root

Write-Host "Certificado instalado correctamente." -ForegroundColor Green
Write-Host "Huella: $($installed.Thumbprint)"
Write-Host "Cierra completamente todos los navegadores y vuelve a abrirlos."
