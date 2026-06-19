using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CorexProd.Entidad.Entidades
{
    public class GuiaInterna
    {
        public int IdGuiaInterna { get; set; }
        public string NumeroGuia { get; set; } = string.Empty;
        public int IdOrdenCompraInterna { get; set; }
        public string NumeroOci { get; set; } = string.Empty;
        public string OrdenCompraCliente { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; } = DateTime.Today;
        public int IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; } = string.Empty;
        public string RucEmisor { get; set; } = string.Empty;
        public string EmpresaEmisora { get; set; } = string.Empty;
        public string RucDestino { get; set; } = string.Empty;
        public string EmpresaDestino { get; set; } = string.Empty;
        public string UsuarioEmisor { get; set; } = string.Empty;
        public string UsuarioAutorizador { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public string Estado { get; set; } = "Borrador";
        public List<GuiaInternaDetalle> Detalles { get; set; } = [];
    }

    public class GuiaInternaDetalle : INotifyPropertyChanged
    {
        private decimal _cantidadDespachar;
        private string _observacion = string.Empty;

        public int IdOrdenCompraInternaDetalle { get; set; }
        public int IdProducto { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public int IdUnidadMedida { get; set; }
        public string NombreUnidad { get; set; } = string.Empty;
        public decimal CantidadRequerida { get; set; }
        public decimal CantidadEntregada { get; set; }
        public decimal CantidadPendiente { get; set; }
        public decimal StockActual { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal CantidadDespachar
        {
            get => _cantidadDespachar;
            set
            {
                if (_cantidadDespachar == value) return;
                _cantidadDespachar = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(EstadoDespacho));
            }
        }
        public string Observacion
        {
            get => _observacion;
            set { _observacion = value; OnPropertyChanged(); }
        }
        public decimal Total => CantidadDespachar * PrecioUnitario;
        public string EstadoDespacho => CantidadDespachar <= 0
            ? "No despacha"
            : CantidadDespachar < CantidadPendiente ? "Parcial" : "Completo";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? nombre = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
    }
}
