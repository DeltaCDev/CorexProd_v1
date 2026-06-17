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
                Descripcion = string.Empty,
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
            IEnumerable<CategoriaProducto> categorias,
            IEnumerable<UnidadMedida> unidadesMedida)
        {
            CreacionMasivaProductosResultado resultado = new();
            HashSet<string> codigosExistentes = productosExistentes
                .Select(producto => producto.Codigo.Trim())
                .Where(codigo => !string.IsNullOrWhiteSpace(codigo))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            HashSet<string> codigosCarga = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<int, CategoriaProducto> categoriasPorId = categorias.ToDictionary(categoria => categoria.IdCategoriaProducto);
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

                if (columnas.Length != 5)
                {
                    resultado.Filas.Add(CrearError(numeroLinea, columnas.FirstOrDefault()?.Trim().ToUpperInvariant() ?? string.Empty, "Cantidad incorrecta de columnas"));
                    continue;
                }

                CreacionMasivaProductoFila fila = ValidarFila(
                    numeroLinea,
                    columnas,
                    codigosExistentes,
                    codigosCarga,
                    categoriasPorId,
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
            Dictionary<int, CategoriaProducto> categoriasPorId,
            Dictionary<int, UnidadMedida> unidadesPorId)
        {
            List<string> errores = [];
            string codigoOriginal = columnas[0];
            string codigo = codigoOriginal.Trim().ToUpperInvariant();
            string nombre = columnas[1].Trim();
            string categoriaTexto = columnas[2].Trim();
            string unidadTexto = columnas[3].Trim();
            string stockTexto = columnas[4].Trim();

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

            CategoriaProducto? categoria = null;
            if (!int.TryParse(categoriaTexto, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idCategoria))
            {
                errores.Add("Categoria ID invalida");
            }
            else if (!categoriasPorId.TryGetValue(idCategoria, out categoria))
            {
                errores.Add("Categoria inexistente");
            }
            else if (!categoria.Estado)
            {
                errores.Add("Categoria inactiva");
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
