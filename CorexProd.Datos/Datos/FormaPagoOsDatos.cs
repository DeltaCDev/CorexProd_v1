using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class FormaPagoOsDatos
    {
        public List<FormaPagoOs> Listar(bool soloActivos = false)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);

            using SqlCommand cmd = new("""
                SELECT IdFormaPagoOs, Nombre, Estado, FechaRegistro
                FROM dbo.FormasPagoOS
                WHERE @SoloActivos = 0 OR Estado = 1
                ORDER BY Nombre;
                """, cn);
            cmd.Parameters.Add("@SoloActivos", SqlDbType.Bit).Value = soloActivos;

            List<FormaPagoOs> lista = [];
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new FormaPagoOs
                {
                    IdFormaPagoOs = Convert.ToInt32(dr["IdFormaPagoOs"]),
                    Nombre = dr["Nombre"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string Guardar(FormaPagoOs forma)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);

            if (forma.IdFormaPagoOs == 0)
            {
                using SqlCommand cmd = new("""
                    IF EXISTS (SELECT 1 FROM dbo.FormasPagoOS WHERE UPPER(Nombre) = UPPER(@Nombre))
                    BEGIN
                        SELECT 'Ya existe una forma de pago OS con ese nombre.';
                        RETURN;
                    END;

                    INSERT INTO dbo.FormasPagoOS (Nombre, Estado)
                    VALUES (@Nombre, @Estado);
                    SELECT 'Forma de pago OS registrada correctamente.';
                    """, cn);
                AgregarParametros(cmd, forma);
                return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
            }

            using (SqlCommand cmd = new("""
                IF EXISTS (SELECT 1 FROM dbo.FormasPagoOS WHERE UPPER(Nombre) = UPPER(@Nombre) AND IdFormaPagoOs <> @IdFormaPagoOs)
                BEGIN
                    SELECT 'Ya existe una forma de pago OS con ese nombre.';
                    RETURN;
                END;

                UPDATE dbo.FormasPagoOS
                SET Nombre = @Nombre,
                    Estado = @Estado
                WHERE IdFormaPagoOs = @IdFormaPagoOs;
                SELECT 'Forma de pago OS actualizada correctamente.';
                """, cn))
            {
                cmd.Parameters.Add("@IdFormaPagoOs", SqlDbType.Int).Value = forma.IdFormaPagoOs;
                AgregarParametros(cmd, forma);
                return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
            }
        }

        public string Eliminar(int idFormaPagoOs)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);

            using SqlCommand cmd = new("""
                UPDATE dbo.FormasPagoOS
                SET Estado = 0
                WHERE IdFormaPagoOs = @IdFormaPagoOs;
                SELECT CASE WHEN @@ROWCOUNT > 0
                    THEN 'Forma de pago OS desactivada correctamente.'
                    ELSE 'No se encontro la forma de pago OS seleccionada.'
                END;
                """, cn);
            cmd.Parameters.Add("@IdFormaPagoOs", SqlDbType.Int).Value = idFormaPagoOs;
            return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
        }

        private static void AgregarParametros(SqlCommand cmd, FormaPagoOs forma)
        {
            cmd.Parameters.Add("@Nombre", SqlDbType.VarChar, 80).Value = forma.Nombre.Trim().ToUpperInvariant();
            cmd.Parameters.Add("@Estado", SqlDbType.Bit).Value = forma.Estado;
        }

        private static void AsegurarEsquema(SqlConnection cn)
        {
            using SqlCommand cmd = new("""
                IF OBJECT_ID('dbo.FormasPagoOS', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.FormasPagoOS
                    (
                        IdFormaPagoOs INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FormasPagoOS PRIMARY KEY,
                        Nombre VARCHAR(80) NOT NULL,
                        Estado BIT NOT NULL CONSTRAINT DF_FormasPagoOS_Estado DEFAULT(1),
                        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_FormasPagoOS_Fecha DEFAULT(GETDATE())
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM dbo.FormasPagoOS WHERE UPPER(Nombre) = 'YAPE')
                    INSERT INTO dbo.FormasPagoOS (Nombre, Estado) VALUES ('YAPE', 1);
                IF NOT EXISTS (SELECT 1 FROM dbo.FormasPagoOS WHERE UPPER(Nombre) = 'PLIN')
                    INSERT INTO dbo.FormasPagoOS (Nombre, Estado) VALUES ('PLIN', 1);
                IF NOT EXISTS (SELECT 1 FROM dbo.FormasPagoOS WHERE UPPER(Nombre) = 'TRANSFERENCIA')
                    INSERT INTO dbo.FormasPagoOS (Nombre, Estado) VALUES ('TRANSFERENCIA', 1);
                IF NOT EXISTS (SELECT 1 FROM dbo.FormasPagoOS WHERE UPPER(Nombre) = 'EFECTIVO')
                    INSERT INTO dbo.FormasPagoOS (Nombre, Estado) VALUES ('EFECTIVO', 1);
                """, cn);
            cmd.ExecuteNonQuery();
        }
    }
}
