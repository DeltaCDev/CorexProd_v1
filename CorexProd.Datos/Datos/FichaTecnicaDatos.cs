using CorexProd.Entidad;
using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class FichaTecnicaDatos
    {
        public List<FichaTecnica> Listar()
        {
            var lista = new List<FichaTecnica>();

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_PROD_FICHA_TECNICA_LISTAR", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new FichaTecnica
                        {
                            IdFichaTecnica = Convert.ToInt32(dr["IdFichaTecnica"]),
                            IdProducto = Convert.ToInt32(dr["IdProducto"]),
                            CodigoProducto = ObtenerTextoOpcional(dr, "CodigoProducto", "Codigo"),
                            NombreProducto = ObtenerTextoOpcional(dr, "NombreProducto", "Producto"),
                            Version = Convert.ToInt32(dr["Version"]),
                            Observacion = ObtenerTextoOpcional(dr, "Observacion"),
                            Estado = Convert.ToBoolean(dr["Estado"])
                        });
                    }
                }
            }

            return lista;
        }

        public bool Registrar(FichaTecnica ficha, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_PROD_FICHA_TECNICA_REGISTRAR", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdProducto", ficha.IdProducto);
                cmd.Parameters.AddWithValue("@Version", ficha.Version);
                cmd.Parameters.AddWithValue("@Observacion", (object?)ficha.Observacion ?? DBNull.Value);

                SqlParameter pResultado = new SqlParameter("@Resultado", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter pMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(pResultado);
                cmd.Parameters.Add(pMensaje);

                cn.Open();
                cmd.ExecuteNonQuery();

                resultado = Convert.ToBoolean(pResultado.Value);
                mensaje = pMensaje.Value.ToString() ?? "";
            }

            return resultado;
        }

        public bool Editar(FichaTecnica ficha, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_PROD_FICHA_TECNICA_EDITAR", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdFichaTecnica", ficha.IdFichaTecnica);
                cmd.Parameters.AddWithValue("@IdProducto", ficha.IdProducto);
                cmd.Parameters.AddWithValue("@Version", ficha.Version);
                cmd.Parameters.AddWithValue("@Observacion", (object?)ficha.Observacion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Estado", ficha.Estado);

                SqlParameter pResultado = new SqlParameter("@Resultado", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter pMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(pResultado);
                cmd.Parameters.Add(pMensaje);

                cn.Open();
                cmd.ExecuteNonQuery();

                resultado = Convert.ToBoolean(pResultado.Value);
                mensaje = pMensaje.Value.ToString() ?? "";
            }

            return resultado;
        }

        public List<FichaTecnicaDetalle> ListarDetalle(int idFichaTecnica)
        {
            var lista = new List<FichaTecnicaDetalle>();

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_PROD_FICHA_TECNICA_DETALLE_LISTAR", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdFichaTecnica", idFichaTecnica);

                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new FichaTecnicaDetalle
                        {
                            IdFichaTecnicaDetalle = Convert.ToInt32(dr["IdFichaTecnicaDetalle"]),
                            IdFichaTecnica = Convert.ToInt32(dr["IdFichaTecnica"]),
                            IdInsumo = Convert.ToInt32(dr["IdInsumo"]),
                            NombreInsumo = dr["NombreInsumo"].ToString() ?? "",
                            CodigoInsumo = ObtenerTextoOpcional(dr, "CodigoInsumo", "Codigo"),
                            Cantidad = Convert.ToDecimal(dr["Cantidad"]),
                            IdUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                            NombreUnidad = dr["NombreUnidad"].ToString() ?? "",
                            Abreviatura = dr["Abreviatura"].ToString() ?? "",
                            Estado = Convert.ToBoolean(dr["Estado"])
                        });
                    }
                }
            }

            return lista;
        }

        public bool RegistrarDetalle(FichaTecnicaDetalle detalle, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_PROD_FICHA_TECNICA_DETALLE_REGISTRAR", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdFichaTecnica", detalle.IdFichaTecnica);
                cmd.Parameters.AddWithValue("@IdInsumo", detalle.IdInsumo);
                cmd.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                cmd.Parameters.AddWithValue("@IdUnidadMedida", detalle.IdUnidadMedida);

                SqlParameter pResultado = new SqlParameter("@Resultado", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter pMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(pResultado);
                cmd.Parameters.Add(pMensaje);

                cn.Open();
                cmd.ExecuteNonQuery();

                resultado = Convert.ToBoolean(pResultado.Value);
                mensaje = pMensaje.Value.ToString() ?? "";
            }

            return resultado;
        }




        private static string ObtenerTextoOpcional(SqlDataReader dr, params string[] nombresColumnas)
        {
            foreach (var nombreColumna in nombresColumnas)
            {
                try
                {
                    int indice = dr.GetOrdinal(nombreColumna);
                    return dr.IsDBNull(indice) ? string.Empty : dr.GetValue(indice)?.ToString() ?? string.Empty;
                }
                catch (IndexOutOfRangeException)
                {
                }
            }

            return string.Empty;
        }


        public bool EditarDetalle(FichaTecnicaDetalle detalle, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_PROD_FICHA_TECNICA_DETALLE_EDITAR", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdFichaTecnicaDetalle", detalle.IdFichaTecnicaDetalle);
                cmd.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                cmd.Parameters.AddWithValue("@IdUnidadMedida", detalle.IdUnidadMedida);

                SqlParameter pResultado = new SqlParameter("@Resultado", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter pMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(pResultado);
                cmd.Parameters.Add(pMensaje);

                cn.Open();
                cmd.ExecuteNonQuery();

                resultado = Convert.ToBoolean(pResultado.Value);
                mensaje = pMensaje.Value.ToString() ?? "";
            }

            return resultado;
        }

        public bool EliminarDetalle(int idFichaTecnicaDetalle, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_PROD_FICHA_TECNICA_DETALLE_ELIMINAR", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdFichaTecnicaDetalle", idFichaTecnicaDetalle);

                SqlParameter pResultado = new SqlParameter("@Resultado", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter pMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(pResultado);
                cmd.Parameters.Add(pMensaje);

                cn.Open();
                cmd.ExecuteNonQuery();

                resultado = Convert.ToBoolean(pResultado.Value);
                mensaje = pMensaje.Value.ToString() ?? "";
            }

            return resultado;
        }
        public List<FichaTecnicaConsumo> CalcularConsumo(int idProducto, decimal cantidadProducir)
        {
            var lista = new List<FichaTecnicaConsumo>();

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_PROD_FICHA_TECNICA_CALCULAR_CONSUMO", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                cmd.Parameters.AddWithValue("@CantidadProducir", cantidadProducir);

                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new FichaTecnicaConsumo
                        {
                            IdFichaTecnica = Convert.ToInt32(dr["IdFichaTecnica"]),
                            IdProducto = Convert.ToInt32(dr["IdProducto"]),
                            CodigoProducto = dr["CodigoProducto"].ToString() ?? "",
                            NombreProducto = dr["NombreProducto"].ToString() ?? "",
                            IdInsumo = Convert.ToInt32(dr["IdInsumo"]),
                            NombreInsumo = dr["NombreInsumo"].ToString() ?? "",
                            CantidadPorUnidad = Convert.ToDecimal(dr["CantidadPorUnidad"]),
                            CantidadProducir = Convert.ToDecimal(dr["CantidadProducir"]),
                            CantidadTotalRequerida = Convert.ToDecimal(dr["CantidadTotalRequerida"]),
                            IdUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                            NombreUnidad = dr["NombreUnidad"].ToString() ?? "",
                            Abreviatura = dr["Abreviatura"].ToString() ?? ""
                        });
                    }
                }
            }

            return lista;
        }
    }

}