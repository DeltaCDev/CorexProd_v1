param(
    [switch]$ForceRestart
)

$ErrorActionPreference = 'Stop'
$puerto = 5056
$apiDir = Join-Path $PSScriptRoot 'CorexProd.Api\publish-oc'
$apiDll = Join-Path $apiDir 'CorexProd.Api.dll'

if (-not (Test-Path -LiteralPath $apiDll)) {
    throw "No se encontro la API publicada: $apiDll"
}

$conexiones = Get-NetTCPConnection -LocalPort $puerto -State Listen -ErrorAction SilentlyContinue
if ($conexiones) {
    $pids = $conexiones | Select-Object -ExpandProperty OwningProcess -Unique

    if (-not $ForceRestart) {
        Write-Host "CorexProd API ya esta activa en el puerto $puerto. PID: $($pids -join ', ')"
        Write-Host "Para reiniciarla ejecute: .\Iniciar-CorexProdApi.ps1 -ForceRestart"
        exit 0
    }

    foreach ($pidActual in $pids) {
        Stop-Process -Id $pidActual -Force
        Write-Host "CorexProd API detenida. PID: $pidActual"
    }

    Start-Sleep -Seconds 2
}

$proceso = Start-Process `
    -FilePath 'C:\Program Files\dotnet\dotnet.exe' `
    -ArgumentList "`"$apiDll`" --urls http://0.0.0.0:$puerto" `
    -WorkingDirectory $apiDir `
    -WindowStyle Hidden `
    -PassThru

Start-Sleep -Seconds 2
$ip = (Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' } |
    Select-Object -First 1 -ExpandProperty IPAddress)

Write-Host "CorexProd API iniciada. PID: $($proceso.Id)"
Write-Host "URL local: http://localhost:$puerto"
if ($ip) {
    Write-Host "URL para Android: http://$ip`:$puerto"
} else {
    Write-Host "URL para Android: http://IP_DE_TU_PC:$puerto"
}
