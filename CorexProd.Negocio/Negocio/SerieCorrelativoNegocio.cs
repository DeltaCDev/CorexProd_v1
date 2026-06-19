using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class SerieCorrelativoNegocio
    {
        private readonly SerieCorrelativoDatos _datos = new();
        public List<TipoDocumentoNumeracion> ListarTipos() => _datos.ListarTipos();
        public List<SerieCorrelativo> Listar(string? tipo = null) => _datos.Listar(tipo);
        public List<SerieCorrelativoHistorial> ListarHistorial(int id) => id > 0 ? _datos.ListarHistorial(id) : [];
        public string Guardar(SerieCorrelativo serie, string usuario)
        {
            serie.Serie = serie.Serie.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(serie.CodigoTipoDocumento)) return "Debe seleccionar el tipo de documento.";
            if (string.IsNullOrWhiteSpace(serie.Serie)) return "Debe ingresar la serie.";
            if (serie.CantidadDigitos is < 1 or > 12) return "La cantidad de dígitos debe estar entre 1 y 12.";
            if (serie.UltimoCorrelativo < 0) return "El correlativo no puede ser negativo.";
            if (serie.Predeterminada) serie.Activa = true;
            return _datos.Guardar(serie, string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario);
        }
    }
}
