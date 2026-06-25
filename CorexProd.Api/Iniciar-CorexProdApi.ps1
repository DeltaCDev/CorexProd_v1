$ErrorActionPreference = 'Stop'

$apiDll = Join-Path $PSScriptRoot 'publish\CorexProd.Api.dll'

if (-not (Test-Path -LiteralPath $apiDll)) {
    throw "No se encontró la API publicada: $apiDll"
}

$conexion = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue
if ($conexion) {
    Write-Host "CorexProd API ya está activa en el puerto 5000. PID: $($conexion.OwningProcess)"
    exit 0
}

$proceso = Start-Process `
    -FilePath 'C:\Program Files\dotnet\dotnet.exe' `
    -ArgumentList "`"$apiDll`"" `
    -WorkingDirectory (Split-Path -Parent $apiDll) `
    -WindowStyle Hidden `
    -PassThru

Start-Sleep -Seconds 2
Write-Host "CorexProd API iniciada. PID: $($proceso.Id)"
Write-Host "URL local: http://localhost:5000"
