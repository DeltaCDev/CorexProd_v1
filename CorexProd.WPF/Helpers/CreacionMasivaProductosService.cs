using CorexProd.Entidad.Entidades;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CorexProd.WPF.Helpers
{
    public class CreacionMasivaProductoFila : BaseViewModel
    {
        private string _estado = string.Empty;
        private string _validacion = string.Empty;
        private bool _creado;

        public int NumeroLinea { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string EtiquetaCliente { get; set; } = string.Empty;
        public int IdSuperCategoriaProducto { get; set; }
        public string NombreSuperCategoria { get; set; } = string.Empty;
        public int IdCategoriaProducto { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public int IdUnidadMedida { get; set; }
        public string NombreUnidad { get; set; } = string.Empty;
        public decimal StockMinimo { get; set; }
        public bool EsValido { get; set; }

        public string Estado
        {
            get => _estado;
            set
            {
                _estado = value;
                OnPropertyChanged();
            }
        }

        public string Validacion
        {
            get => _validacion;
            set
            {
                _validacion = value;
                OnPropertyChanged();
            }
        }

        public bool Creado
        {
            get => _creado;
            set
            {
                _creado = value;
                OnPropertyChanged();
            }
        }

        public Producto CrearProducto()
        {
            return new Producto
            {
                Codigo = Codigo,
                NombreProducto = NombreProducto,
                EtiquetaCliente = EtiquetaCliente,
                Descripcion = string.Empty,
                IdSuperCategoriaProducto = IdSuperCategoriaProducto,
                IdCategoriaProducto = IdCategoriaProducto,
                IdUnidadMedida = IdUnidadMedida,
                StockMinimo = StockMinimo,
                Estado = true
            };
        }
    }

    public class CreacionMasivaProductosResultado
    {
        public ObservableCollection<CreacionMasivaProductoFila> Filas { get; } = [];
        public int TotalLineas => Filas.Count;
        public int ProductosValidos => Filas.Count(fila => fila.EsValido);
        public int ProductosRechazados => Filas.Count(fila => !fila.EsValido);
        public string DetalleErrores => string.Join(
            Environment.NewLine,
            Filas.Where(fila => !fila.EsValido)
                .Select(fila => $"Linea {fila.NumeroLinea}: {fila.Validacion}"));
    }

    public static class CreacionMasivaProductosService
    {
        public static CreacionMasivaProductosResultado Procesar(
            string texto,
            IEnumerable<Producto> productosExistentes,
            IEnumerable<SuperCategoriaProducto> superCategorias,
            IEnumerable<CategoriaProducto> categorias,
            IEnumerable<UnidadMedida> unidadesMedida)
        {
            CreacionMasivaProductosResultado resultado = new();
            HashSet<string> codigosExistentes = productosExistentes
                .Select(producto => producto.Codigo.Trim())
                .Where(codigo => !string.IsNullOrWhiteSpace(codigo))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            HashSet<string> codigosCarga = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<int, SuperCategoriaProducto> superCategoriasPorId = superCategorias.ToDictionary(superCategoria => superCategoria.IdSuperCategoriaProducto);
            Dictionary<string, List<SuperCategoriaProducto>> superCategoriasPorNombre = superCategorias
                .GroupBy(superCategoria => superCategoria.NombreSuperCategoria.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList(), StringComparer.OrdinalIgnoreCase);
            Dictionary<int, CategoriaProducto> categoriasPorId = categorias.ToDictionary(categoria => categoria.IdCategoriaProducto);
            Dictionary<string, List<CategoriaProducto>> categoriasPorNombre = categorias
                .GroupBy(categoria => categoria.NombreCategoria.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList(), StringComparer.OrdinalIgnoreCase);
            Dictionary<int, UnidadMedida> unidadesPorId = unidadesMedida.ToDictionary(unidad => unidad.IdUnidadMedida);

            string[] lineas = (texto ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            for (int indice = 0; indice < lineas.Length; indice++)
            {
                int numeroLinea = indice + 1;
                string linea = lineas[indice];

                if (string.IsNullOrWhiteSpace(linea))
                {
                    resultado.Filas.Add(CrearError(numeroLinea, string.Empty, "Cantidad incorrecta de columnas"));
                    continue;
                }

                string[] columnas = linea.Contains('\t')
                    ? linea.Split('\t', StringSplitOptions.None)
                    : linea.Split(';', StringSplitOptions.None);

                if (columnas.Length != 5 && columnas.Length != 6 && columnas.Length != 7)
                {
                    resultado.Filas.Add(CrearError(numeroLinea, columnas.FirstOrDefault()?.Trim().ToUpperInvariant() ?? string.Empty, "Cantidad incorrecta de columnas. Use 5 columnas, 6 si incluye supercategoria o 7 si incluye etiqueta y supercategoria"));
                    continue;
                }

                CreacionMasivaProductoFila fila = ValidarFila(
                    numeroLinea,
                    columnas,
                    codigosExistentes,
                    codigosCarga,
                    superCategoriasPorId,
                    superCategoriasPorNombre,
                    categoriasPorId,
                    categoriasPorNombre,
                    unidadesPorId);

                resultado.Filas.Add(fila);
            }

            return resultado;
        }

        private static CreacionMasivaProductoFila ValidarFila(
            int numeroLinea,
            string[] columnas,
            HashSet<string> codigosExistentes,
            HashSet<string> codigosCarga,
            Dictionary<int, SuperCategoriaProducto> superCategoriasPorId,
            Dictionary<string, List<SuperCategoriaProducto>> superCategoriasPorNombre,
            Dictionary<int, CategoriaProducto> categoriasPorId,
            Dictionary<string, List<CategoriaProducto>> categoriasPorNombre,
            Dictionary<int, UnidadMedida> unidadesPorId)
        {
            List<string> errores = [];
            string codigoOriginal = columnas[0];
            string codigo = codigoOriginal.Trim().ToUpperInvariant();
            string nombre = columnas[1].Trim();
            bool incluyeEtiqueta = columnas.Length == 7;
            bool incluyeSuperCategoria = columnas.Length >= 6;
            string etiquetaCliente = incluyeEtiqueta ? columnas[2].Trim() : string.Empty;
            int indiceSuperCategoria = incluyeEtiqueta ? 3 : 2;
            string superCategoriaTexto = incluyeSuperCategoria ? columnas[indiceSuperCategoria].Trim() : string.Empty;
            string categoriaTexto = columnas[incluyeSuperCategoria ? indiceSuperCategoria + 1 : 2].Trim();
            string unidadTexto = columnas[incluyeSuperCategoria ? indiceSuperCategoria + 2 : 3].Trim();
            string stockTexto = columnas[incluyeSuperCategoria ? indiceSuperCategoria + 3 : 4].Trim();

            if (string.IsNullOrWhiteSpace(codigo))
            {
                errores.Add("Codigo vacio");
            }
            else
            {
                if (codigo.Any(char.IsWhiteSpace))
                {
                    errores.Add("Codigo con espacios");
                }

                if (codigosCarga.Contains(codigo))
                {
                    errores.Add("Codigo duplicado");
                }

                if (codigosExistentes.Contains(codigo))
                {
                    errores.Add("Codigo existente");
                }
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                errores.Add("Nombre vacio");
            }

            SuperCategoriaProducto? superCategoria = null;
            if (!string.IsNullOrWhiteSpace(superCategoriaTexto))
            {
                superCategoria = ResolverSuperCategoria(superCategoriaTexto, superCategoriasPorId, superCategoriasPorNombre, errores);
            }

            CategoriaProducto? categoria = ResolverCategoria(
                categoriaTexto,
                categoriasPorId,
                categoriasPorNombre,
                superCategoria,
                errores);

            if (categoria != null && !categoria.Estado)
            {
                errores.Add("Categoria inactiva");
            }

            if (categoria != null)
            {
                if (superCategoria == null)
                {
                    superCategoriasPorId.TryGetValue(categoria.IdSuperCategoriaProducto, out superCategoria);
                }
                else if (categoria.IdSuperCategoriaProducto != superCategoria.IdSuperCategoriaProducto)
                {
                    errores.Add("La categoria no pertenece a la supercategoria indicada");
                }
            }

            if (superCategoria != null && !superCategoria.Estado)
            {
                errores.Add("Supercategoria inactiva");
            }

            UnidadMedida? unidad = null;
            if (!int.TryParse(unidadTexto, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idUnidad))
            {
                errores.Add("Unidad de medida ID invalida");
            }
            else if (!unidadesPorId.TryGetValue(idUnidad, out unidad))
            {
                errores.Add("Unidad de medida inexistente");
            }
            else if (!unidad.Estado)
            {
                errores.Add("Unidad de medida inactiva");
            }

            if (!decimal.TryParse(stockTexto, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal stockMinimo)
                && !decimal.TryParse(stockTexto, NumberStyles.Number, new CultureInfo("es-PE"), out stockMinimo)
                && !decimal.TryParse(stockTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out stockMinimo))
            {
                errores.Add("Stock minimo invalido");
            }
            else if (stockMinimo < 0)
            {
                errores.Add("Stock minimo negativo");
            }

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                codigosCarga.Add(codigo);
            }

            bool valido = errores.Count == 0;

            return new CreacionMasivaProductoFila
            {
                NumeroLinea = numeroLinea,
                Codigo = codigo,
                NombreProducto = nombre,
                EtiquetaCliente = etiquetaCliente,
                IdSuperCategoriaProducto = superCategoria?.IdSuperCategoriaProducto ?? 0,
                NombreSuperCategoria = superCategoria?.NombreSuperCategoria ?? string.Empty,
                IdCategoriaProducto = categoria?.IdCategoriaProducto ?? 0,
                NombreCategoria = categoria?.NombreCategoria ?? string.Empty,
                IdUnidadMedida = unidad?.IdUnidadMedida ?? 0,
                NombreUnidad = unidad?.NombreUnidad ?? string.Empty,
                StockMinimo = stockMinimo,
                EsValido = valido,
                Estado = valido ? "Valido" : "Error",
                Validacion = valido ? "Correcto" : string.Join("; ", errores)
            };
        }

        private static SuperCategoriaProducto? ResolverSuperCategoria(
            string texto,
            Dictionary<int, SuperCategoriaProducto> superCategoriasPorId,
            Dictionary<string, List<SuperCategoriaProducto>> superCategoriasPorNombre,
            List<string> errores)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idSuperCategoria))
            {
                if (superCategoriasPorId.TryGetValue(idSuperCategoria, out SuperCategoriaProducto? superCategoriaPorId))
                {
                    return superCategoriaPorId;
                }

                errores.Add("Supercategoria inexistente");
                return null;
            }

            if (!superCategoriasPorNombre.TryGetValue(texto.Trim(), out List<SuperCategoriaProducto>? coincidencias)
                || coincidencias.Count == 0)
            {
                errores.Add("Supercategoria inexistente");
                return null;
            }

            if (coincidencias.Count > 1)
            {
                errores.Add("Supercategoria ambigua");
                return null;
            }

            return coincidencias[0];
        }

        private static CategoriaProducto? ResolverCategoria(
            string texto,
            Dictionary<int, CategoriaProducto> categoriasPorId,
            Dictionary<string, List<CategoriaProducto>> categoriasPorNombre,
            SuperCategoriaProducto? superCategoria,
            List<string> errores)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                errores.Add("Categoria vacia");
                return null;
            }

            if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idCategoria))
            {
                if (categoriasPorId.TryGetValue(idCategoria, out CategoriaProducto? categoriaPorId))
                {
                    return categoriaPorId;
                }

                errores.Add("Categoria inexistente");
                return null;
            }

            if (!categoriasPorNombre.TryGetValue(texto.Trim(), out List<CategoriaProducto>? coincidencias)
                || coincidencias.Count == 0)
            {
                errores.Add("Categoria inexistente");
                return null;
            }

            if (superCategoria != null)
            {
                coincidencias = coincidencias
                    .Where(categoria => categoria.IdSuperCategoriaProducto == superCategoria.IdSuperCategoriaProducto)
                    .ToList();
            }

            if (coincidencias.Count == 0)
            {
                errores.Add("Categoria inexistente para la supercategoria indicada");
                return null;
            }

            if (coincidencias.Count > 1)
            {
                errores.Add("Categoria ambigua. Indique la supercategoria o use el ID de categoria");
                return null;
            }

            return coincidencias[0];
        }

        private static CreacionMasivaProductoFila CrearError(int numeroLinea, string codigo, string mensaje)
        {
            return new CreacionMasivaProductoFila
            {
                NumeroLinea = numeroLinea,
                Codigo = codigo,
                Estado = "Error",
                Validacion = mensaje,
                EsValido = false
            };
        }
    }
}
