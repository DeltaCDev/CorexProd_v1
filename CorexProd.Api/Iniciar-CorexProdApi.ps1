param(
    [switch]$ForceRestart
)

$ErrorActionPreference = 'Stop'

$apiDll = Join-Path $PSScriptRoot 'publish-next\CorexProd.Api.dll'
if (-not (Test-Path -LiteralPath $apiDll)) {
    $apiDll = Join-Path $PSScriptRoot 'publish\CorexProd.Api.dll'
}

if (-not (Test-Path -LiteralPath $apiDll)) {
    throw "No se encontro la API publicada: $apiDll"
}

$conexion = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue
if ($conexion) {
    $pids = $conexion | Select-Object -ExpandProperty OwningProcess -Unique

    if (-not $ForceRestart) {
        Write-Host "CorexProd API ya esta activa en el puerto 5000. PID: $($pids -join ', ')"
        Write-Host "Para reemplazarla por la version actual ejecute: .\Iniciar-CorexProdApi.ps1 -ForceRestart"
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
    -ArgumentList "`"$apiDll`" --urls http://0.0.0.0:5000" `
    -WorkingDirectory (Split-Path -Parent $apiDll) `
    -WindowStyle Hidden `
    -PassThru

Start-Sleep -Seconds 2
Write-Host "CorexProd API iniciada. PID: $($proceso.Id)"
Write-Host "URL local: http://localhost:5000"
Write-Host "URL red local: http://192.168.68.112:5000"
