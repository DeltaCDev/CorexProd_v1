using CorexProd.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CorexProd.WPF.Helpers
{
    public class CargaMasivaProductoInfo
    {
        public int IdProducto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public object Producto { get; set; } = new();
    }

    public class CargaMasivaProductoFila : BaseViewModel
    {
        private bool _seleccionado;

        public int NumeroLinea { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public bool EsValido { get; set; }
        public object? Producto { get; set; }

        public bool Seleccionado
        {
            get => _seleccionado;
            set
            {
                _seleccionado = value;
                OnPropertyChanged();
            }
        }

        public void AcumularCantidad(decimal cantidad)
        {
            Cantidad += cantidad;
            Mensaje = "Correcto. Cantidad acumulada por codigo repetido";
            OnPropertyChanged(nameof(Cantidad));
            OnPropertyChanged(nameof(Mensaje));
        }
    }

    public class CargaMasivaProductoResultado
    {
        public ObservableCollection<CargaMasivaProductoFila> Filas { get; } = [];
        public int ProductosProcesados => Filas.Count(fila => fila.EsValido && fila.Seleccionado);
        public decimal UnidadesAgregadas => Filas.Where(fila => fila.EsValido && fila.Seleccionado).Sum(fila => fila.Cantidad);
        public int Errores => Filas.Count(fila => !fila.EsValido);
    }

    public static class CargaMasivaProductoService
    {
        private static readonly Regex SeparadorEspacios = new(@"\s+", RegexOptions.Compiled);

        public static CargaMasivaProductoResultado Procesar(
            string texto,
            Func<string, CargaMasivaProductoInfo?> buscarProducto)
        {
            CargaMasivaProductoResultado resultado = new();
            Dictionary<string, CargaMasivaProductoFila> validosPorCodigo = new(StringComparer.OrdinalIgnoreCase);

            string[] lineas = (texto ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            if (lineas.Length == 0)
            {
                resultado.Filas.Add(CrearError(1, string.Empty, "Fila vacia"));
                return resultado;
            }

            for (int indice = 0; indice < lineas.Length; indice++)
            {
                int numeroLinea = indice + 1;
                string linea = lineas[indice];
                string limpia = linea.Trim();

                if (string.IsNullOrWhiteSpace(limpia))
                {
                    resultado.Filas.Add(CrearError(numeroLinea, string.Empty, "Fila vacia"));
                    continue;
                }

                string[] columnas = limpia.Contains('\t')
                    ? limpia.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : SeparadorEspacios.Split(limpia);

                if (columnas.Length < 2)
                {
                    resultado.Filas.Add(CrearError(numeroLinea, columnas.FirstOrDefault() ?? string.Empty, "Linea incompleta"));
                    continue;
                }

                string codigo = columnas[0].Trim();
                string cantidadTexto = columnas[1].Trim();

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    resultado.Filas.Add(CrearError(numeroLinea, codigo, "Codigo vacio"));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(cantidadTexto))
                {
                    resultado.Filas.Add(CrearError(numeroLinea, codigo, "Cantidad vacia"));
                    continue;
                }

                if (!TryParseCantidad(cantidadTexto, out decimal cantidad))
                {
                    resultado.Filas.Add(CrearError(numeroLinea, codigo, "Cantidad con formato invalido"));
                    continue;
                }

                if (cantidad <= 0)
                {
                    resultado.Filas.Add(CrearError(numeroLinea, codigo, "La cantidad debe ser mayor que cero"));
                    continue;
                }

                CargaMasivaProductoInfo? producto = buscarProducto(codigo);

                if (producto == null)
                {
                    resultado.Filas.Add(CrearError(numeroLinea, codigo, "Codigo inexistente"));
                    continue;
                }

                if (validosPorCodigo.TryGetValue(producto.Codigo, out CargaMasivaProductoFila? filaExistente))
                {
                    filaExistente.AcumularCantidad(cantidad);
                    continue;
                }

                CargaMasivaProductoFila fila = new()
                {
                    NumeroLinea = numeroLinea,
                    Codigo = producto.Codigo,
                    NombreProducto = producto.NombreProducto,
                    Cantidad = cantidad,
                    Estado = "Valido",
                    Mensaje = "Correcto",
                    EsValido = true,
                    Seleccionado = true,
                    Producto = producto.Producto
                };

                validosPorCodigo[producto.Codigo] = fila;
                resultado.Filas.Add(fila);
            }

            return resultado;
        }

        private static CargaMasivaProductoFila CrearError(int numeroLinea, string codigo, string mensaje)
        {
            return new CargaMasivaProductoFila
            {
                NumeroLinea = numeroLinea,
                Codigo = codigo,
                Estado = "Error",
                Mensaje = mensaje,
                EsValido = false,
                Seleccionado = false
            };
        }

        private static bool TryParseCantidad(string valor, out decimal cantidad)
        {
            NumberStyles estilo = NumberStyles.Number;
            CultureInfo culturaLocal = CultureInfo.CurrentCulture;
            CultureInfo culturaPeru = new("es-PE");
            CultureInfo culturaInvariante = CultureInfo.InvariantCulture;

            return decimal.TryParse(valor, estilo, culturaLocal, out cantidad)
                || decimal.TryParse(valor, estilo, culturaPeru, out cantidad)
                || decimal.TryParse(valor, estilo, culturaInvariante, out cantidad);
        }
    }
}
