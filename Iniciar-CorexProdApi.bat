@echo off
title Iniciar CorexProd API

set "API_DIR=D:\CorexProd_v1\CorexProd.Api\publish-oc"
set "API_DLL=%API_DIR%\CorexProd.Api.dll"

if not exist "%API_DLL%" (
    echo No se encontro:
    echo %API_DLL%
    pause
    exit /b 1
)

echo Iniciando CorexProd API en el puerto 5000...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$p = Start-Process -FilePath 'C:\Program Files\dotnet\dotnet.exe' -ArgumentList '%API_DLL% --urls http://0.0.0.0:5000' -WorkingDirectory '%API_DIR%' -WindowStyle Hidden -PassThru; Start-Sleep -Seconds 3; $escucha = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue; if ($escucha) { Write-Host ('API iniciado. PID: ' + $p.Id) -ForegroundColor Green } else { Write-Host 'El API no quedo escuchando en el puerto 5000.' -ForegroundColor Red }"

pause
