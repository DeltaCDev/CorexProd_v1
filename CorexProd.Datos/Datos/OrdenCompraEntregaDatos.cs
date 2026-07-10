using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public sealed class OrdenCompraEntregaDatos
    {
        public DateTime? ObtenerFechaEntrega(int idOrdenCompraInterna)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(
                "SELECT FechaEntrega FROM dbo.OrdenesCompraInterna WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna",
                conexion);
            cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = idOrdenCompraInterna;
            conexion.Open();
            object? valor = cmd.ExecuteScalar();
            return valor == null || valor == DBNull.Value ? null : Convert.ToDateTime(valor);
        }

        public void ActualizarFechaEntrega(int idOrdenCompraInterna, DateTime fechaEntrega)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(
                """
                UPDATE dbo.OrdenesCompraInterna
                SET FechaEntrega = @FechaEntrega
                WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna;
                """,
                conexion);
            cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = idOrdenCompraInterna;
            cmd.Parameters.Add("@FechaEntrega", SqlDbType.Date).Value = fechaEntrega.Date;
            conexion.Open();
            cmd.ExecuteNonQuery();
        }

        public void ActualizarFechaEntregaPorNumero(string numeroOci, DateTime fechaEntrega)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(
                """
                UPDATE dbo.OrdenesCompraInterna
                SET FechaEntrega = @FechaEntrega
                WHERE NumeroOci = @NumeroOci;
                """,
                conexion);
            cmd.Parameters.Add("@NumeroOci", SqlDbType.VarChar, 40).Value = numeroOci;
            cmd.Parameters.Add("@FechaEntrega", SqlDbType.Date).Value = fechaEntrega.Date;
            conexion.Open();
            cmd.ExecuteNonQuery();
        }

        public List<OrdenCompraAlertaEntrega> ListarAlertas(DateTime hoy)
        {
            List<OrdenCompraAlertaEntrega> lista = [];
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(
                """
                SELECT
                    IdOrdenCompraInterna,
                    NumeroOci,
                    NombreCliente,
                    Estado,
                    FechaEntrega,
                    DATEDIFF(DAY, @Hoy, FechaEntrega) AS DiasRestantes
                FROM dbo.OrdenesCompraInterna
                WHERE UPPER(REPLACE(LTRIM(RTRIM(Estado)), ' ', '_')) NOT IN ('ENTREGADO','ENTREGADA','ANULADO','ANULADA')
                  AND FechaEntrega IS NOT NULL
                  AND FechaEntrega <= DATEADD(DAY, 14, @Hoy)
                ORDER BY FechaEntrega, IdOrdenCompraInterna;
                """,
                conexion);
            cmd.Parameters.Add("@Hoy", SqlDbType.Date).Value = hoy.Date;
            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new OrdenCompraAlertaEntrega
                {
                    IdOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
                    NumeroOci = dr["NumeroOci"]?.ToString() ?? string.Empty,
                    NombreCliente = dr["NombreCliente"]?.ToString() ?? string.Empty,
                    Estado = dr["Estado"]?.ToString() ?? string.Empty,
                    FechaEntrega = Convert.ToDateTime(dr["FechaEntrega"]),
                    DiasRestantes = Convert.ToInt32(dr["DiasRestantes"])
                });
            }

            return lista;
        }

        public int ContarEntregadasATiempo(DateTime desde, DateTime hasta)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(
                """
                SELECT COUNT(1)
                FROM dbo.OrdenesCompraInterna
                WHERE UPPER(REPLACE(LTRIM(RTRIM(Estado)), ' ', '_')) IN ('ENTREGADO','ENTREGADA')
                  AND FechaEntrega IS NOT NULL
                  AND FechaEmision >= @Desde
                  AND FechaEmision < DATEADD(DAY, 1, @Hasta)
                  AND ISNULL(FechaCierre, FechaRegistro) <= DATEADD(DAY, 1, FechaEntrega);
                """,
                conexion);
            cmd.Parameters.Add("@Desde", SqlDbType.Date).Value = desde.Date;
            cmd.Parameters.Add("@Hasta", SqlDbType.Date).Value = hasta.Date;
            conexion.Open();
            try
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (SqlException)
            {
                using SqlCommand fallback = new(
                    """
                    SELECT COUNT(1)
                    FROM dbo.OrdenesCompraInterna
                    WHERE UPPER(REPLACE(LTRIM(RTRIM(Estado)), ' ', '_')) IN ('ENTREGADO','ENTREGADA')
                      AND FechaEntrega IS NOT NULL
                      AND FechaEmision >= @Desde
                      AND FechaEmision < DATEADD(DAY, 1, @Hasta)
                      AND FechaRegistro <= DATEADD(DAY, 1, FechaEntrega);
                    """,
                    conexion);
                fallback.Parameters.Add("@Desde", SqlDbType.Date).Value = desde.Date;
                fallback.Parameters.Add("@Hasta", SqlDbType.Date).Value = hasta.Date;
                return Convert.ToInt32(fallback.ExecuteScalar());
            }
        }
    }

    public sealed class OrdenCompraAlertaEntrega
    {
        public int IdOrdenCompraInterna { get; set; }
        public string NumeroOci { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaEntrega { get; set; }
        public int DiasRestantes { get; set; }

        public string AlertaTexto => DiasRestantes < 0
            ? $"Vencida hace {Math.Abs(DiasRestantes)} día(s)"
            : DiasRestantes == 0
                ? "Vence hoy"
                : DiasRestantes == 1
                    ? "Vence mañana"
                    : $"Vence en {DiasRestantes} días";

        public string Color => DiasRestantes < 0
            ? "#DC2626"
            : DiasRestantes == 0
                ? "#F97316"
                : DiasRestantes <= 3
                    ? "#EAB308"
                    : "#0EA5E9";
    }
}
