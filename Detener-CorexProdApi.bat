@echo off
title Detener CorexProd API

echo Deteniendo el API que escucha en el puerto 5000...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$conexion = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue; if ($conexion) { $conexion | Select-Object -ExpandProperty OwningProcess -Unique | ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }; Write-Host 'API detenido correctamente.' -ForegroundColor Green } else { Write-Host 'No hay ningun API escuchando en el puerto 5000.' -ForegroundColor Yellow }"

pause
