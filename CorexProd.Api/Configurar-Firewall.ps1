$ErrorActionPreference = 'Stop'

$regla = Get-NetFirewallRule `
    -DisplayName 'CorexProd API Local 5000' `
    -ErrorAction SilentlyContinue

if ($regla) {
    Set-NetFirewallRule `
        -DisplayName 'CorexProd API Local 5000' `
        -Enabled True `
        -Profile Private,Domain `
        -Action Allow
}
else {
    New-NetFirewallRule `
        -DisplayName 'CorexProd API Local 5000' `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort 5000 `
        -Action Allow `
        -Profile Private,Domain
}

Write-Host 'Firewall configurado para CorexProd API en el puerto TCP 5000.'
