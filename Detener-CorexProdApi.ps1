$ErrorActionPreference = 'Stop'
$puerto = 5056

$conexiones = Get-NetTCPConnection -LocalPort $puerto -State Listen -ErrorAction SilentlyContinue
if (-not $conexiones) {
    Write-Host "CorexProd API no esta activa en el puerto $puerto."
    exit 0
}

$conexiones |
    Select-Object -ExpandProperty OwningProcess -Unique |
    ForEach-Object {
        Stop-Process -Id $_ -Force
        Write-Host "CorexProd API detenida. PID: $_"
    }
