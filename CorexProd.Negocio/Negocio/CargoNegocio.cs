using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class CargoNegocio
    {
        private readonly CargoDatos _cargoDatos = new CargoDatos();

        public List<Cargo> Listar()
        {
            return _cargoDatos.Listar();
        }

        public string Guardar(Cargo cargo)
        {
            if (string.IsNullOrWhiteSpace(cargo.NombreCargo))
                return "El nombre del cargo es obligatorio";

            cargo.NombreCargo = cargo.NombreCargo.Trim();

            if (cargo.IdCargo == 0)
                return _cargoDatos.Registrar(cargo);

            return _cargoDatos.Editar(cargo);
        }

        public string Eliminar(int idCargo)
        {
            if (idCargo <= 0)
                return "Debe seleccionar un cargo válido";

            return _cargoDatos.Eliminar(idCargo);
        }
    }
}