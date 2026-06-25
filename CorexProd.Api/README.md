# CorexProd API local

API HTTP para el sistema CorexProd y la futura aplicación Android.

## Dirección

- PC servidor: `http://localhost:5000`
- Red local: `http://IP-DE-LA-PC:5000`

## Endpoints iniciales

- `GET /api/health`
- `GET /api/stock/productos`
- `GET /api/stock/productos?buscar=SGS006`
- `GET /api/stock/insumos`
- `GET /api/fichas-tecnicas/SGS006M/info`
- `GET /api/fichas-tecnicas/SGS006M`

La ficha documental se resuelve por modelo. Por ejemplo:

- `SGS006M` utiliza `SGS006.pdf`.
- `SGS019T34` utiliza `SGS019.pdf`.

## Iniciar y detener

Ejecutar:

```powershell
.\Iniciar-CorexProdApi.ps1
```

Para detener:

```powershell
.\Detener-CorexProdApi.ps1
```

Los PDF se almacenan físicamente en `D:\FICHAS_TECNICAS`.
La tabla `FichaTecnicaDocumento` registra modelo, archivo, ruta relativa y versión.

## Habilitar acceso desde Android

Ejecutar PowerShell como administrador y luego:

```powershell
.\Configurar-Firewall.ps1
```
