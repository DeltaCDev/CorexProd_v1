$ErrorActionPreference = 'Stop'

$conexiones = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue
if (-not $conexiones) {
    Write-Host 'CorexProd API no está activa.'
    exit 0
}

$conexiones |
    Select-Object -ExpandProperty OwningProcess -Unique |
    ForEach-Object {
        Stop-Process -Id $_ -Force
        Write-Host "CorexProd API detenida. PID: $_"
    }
