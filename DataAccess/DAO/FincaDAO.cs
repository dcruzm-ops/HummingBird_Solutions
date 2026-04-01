using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Fincas;

namespace PSA.DataAccess.DAO
{
    public class FincaDAO
    {
        private readonly DbContextHelper _dbContextHelper;

        public FincaDAO(DbContextHelper dbContextHelper)
        {
            _dbContextHelper = dbContextHelper;
        }

        public List<FincaDTO> RetrieveAll()
        {
            var list = new List<FincaDTO>();

            using (var connection = _dbContextHelper.CreateConnection())
            {
                var query = @"SELECT IdFinca, IdPropietario, NombreFinca, Provincia, Canton, Distrito,
                                     DireccionExacta, Latitud, Longitud, Hectareas, Vegetacion,
                                     TieneRecursosHidricos, UsoSuelo, Pendiente, EstadoFinca,
                                     FechaRegistro, FechaActualizacion
                              FROM dbo.Fincas";

                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var finca = new FincaDTO
                            {
                                IdFinca = Convert.ToInt32(reader["IdFinca"]),
                                IdPropietario = Convert.ToInt32(reader["IdPropietario"]),
                                NombreFinca = reader["NombreFinca"].ToString() ?? string.Empty,
                                Provincia = reader["Provincia"].ToString() ?? string.Empty,
                                Canton = reader["Canton"].ToString() ?? string.Empty,
                                Distrito = reader["Distrito"].ToString() ?? string.Empty,
                                DireccionExacta = reader["DireccionExacta"] == DBNull.Value ? null : reader["DireccionExacta"].ToString(),
                                Latitud = Convert.ToDecimal(reader["Latitud"]),
                                Longitud = Convert.ToDecimal(reader["Longitud"]),
                                Hectareas = Convert.ToDecimal(reader["Hectareas"]),
                                Vegetacion = reader["Vegetacion"].ToString() ?? string.Empty,
                                TieneRecursosHidricos = Convert.ToBoolean(reader["TieneRecursosHidricos"]),
                                UsoSuelo = reader["UsoSuelo"].ToString() ?? string.Empty,
                                Pendiente = reader["Pendiente"].ToString() ?? string.Empty,
                                EstadoFinca = reader["EstadoFinca"].ToString() ?? string.Empty,
                                FechaRegistro = Convert.ToDateTime(reader["FechaRegistro"]),
                                FechaActualizacion = Convert.ToDateTime(reader["FechaActualizacion"])
                            };

                            list.Add(finca);
                        }
                    }
                }
            }

            return list;
        }

        public FincaDTO? RetrieveById(int id)
        {
            FincaDTO? finca = null;

            using (var connection = _dbContextHelper.CreateConnection())
            {
                var query = @"SELECT IdFinca, IdPropietario, NombreFinca, Provincia, Canton, Distrito,
                                     DireccionExacta, Latitud, Longitud, Hectareas, Vegetacion,
                                     TieneRecursosHidricos, UsoSuelo, Pendiente, EstadoFinca,
                                     FechaRegistro, FechaActualizacion
                              FROM dbo.Fincas
                              WHERE IdFinca = @IdFinca";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdFinca", id);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            finca = new FincaDTO
                            {
                                IdFinca = Convert.ToInt32(reader["IdFinca"]),
                                IdPropietario = Convert.ToInt32(reader["IdPropietario"]),
                                NombreFinca = reader["NombreFinca"].ToString() ?? string.Empty,
                                Provincia = reader["Provincia"].ToString() ?? string.Empty,
                                Canton = reader["Canton"].ToString() ?? string.Empty,
                                Distrito = reader["Distrito"].ToString() ?? string.Empty,
                                DireccionExacta = reader["DireccionExacta"] == DBNull.Value ? null : reader["DireccionExacta"].ToString(),
                                Latitud = Convert.ToDecimal(reader["Latitud"]),
                                Longitud = Convert.ToDecimal(reader["Longitud"]),
                                Hectareas = Convert.ToDecimal(reader["Hectareas"]),
                                Vegetacion = reader["Vegetacion"].ToString() ?? string.Empty,
                                TieneRecursosHidricos = Convert.ToBoolean(reader["TieneRecursosHidricos"]),
                                UsoSuelo = reader["UsoSuelo"].ToString() ?? string.Empty,
                                Pendiente = reader["Pendiente"].ToString() ?? string.Empty,
                                EstadoFinca = reader["EstadoFinca"].ToString() ?? string.Empty,
                                FechaRegistro = Convert.ToDateTime(reader["FechaRegistro"]),
                                FechaActualizacion = Convert.ToDateTime(reader["FechaActualizacion"])
                            };
                        }
                    }
                }
            }

            return finca;
        }

        public void Create(FincaDTO finca)
        {
            using (var connection = _dbContextHelper.CreateConnection())
            {
                var query = @"INSERT INTO dbo.Fincas
                              (IdPropietario, NombreFinca, Provincia, Canton, Distrito, DireccionExacta,
                               Latitud, Longitud, Hectareas, Vegetacion, TieneRecursosHidricos,
                               UsoSuelo, Pendiente, EstadoFinca, FechaRegistro, FechaActualizacion)
                              VALUES
                              (@IdPropietario, @NombreFinca, @Provincia, @Canton, @Distrito, @DireccionExacta,
                               @Latitud, @Longitud, @Hectareas, @Vegetacion, @TieneRecursosHidricos,
                               @UsoSuelo, @Pendiente, @EstadoFinca, @FechaRegistro, @FechaActualizacion)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdPropietario", finca.IdPropietario);
                    command.Parameters.AddWithValue("@NombreFinca", finca.NombreFinca);
                    command.Parameters.AddWithValue("@Provincia", finca.Provincia);
                    command.Parameters.AddWithValue("@Canton", finca.Canton);
                    command.Parameters.AddWithValue("@Distrito", finca.Distrito);
                    command.Parameters.AddWithValue("@DireccionExacta", (object?)finca.DireccionExacta ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Latitud", finca.Latitud);
                    command.Parameters.AddWithValue("@Longitud", finca.Longitud);
                    command.Parameters.AddWithValue("@Hectareas", finca.Hectareas);
                    command.Parameters.AddWithValue("@Vegetacion", finca.Vegetacion);
                    command.Parameters.AddWithValue("@TieneRecursosHidricos", finca.TieneRecursosHidricos);
                    command.Parameters.AddWithValue("@UsoSuelo", finca.UsoSuelo);
                    command.Parameters.AddWithValue("@Pendiente", finca.Pendiente);
                    command.Parameters.AddWithValue("@EstadoFinca", finca.EstadoFinca);
                    command.Parameters.AddWithValue("@FechaRegistro", finca.FechaRegistro);
                    command.Parameters.AddWithValue("@FechaActualizacion", finca.FechaActualizacion);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Update(FincaDTO finca)
        {
            using (var connection = _dbContextHelper.CreateConnection())
            {
                var query = @"UPDATE dbo.Fincas
                              SET IdPropietario = @IdPropietario,
                                  NombreFinca = @NombreFinca,
                                  Provincia = @Provincia,
                                  Canton = @Canton,
                                  Distrito = @Distrito,
                                  DireccionExacta = @DireccionExacta,
                                  Latitud = @Latitud,
                                  Longitud = @Longitud,
                                  Hectareas = @Hectareas,
                                  Vegetacion = @Vegetacion,
                                  TieneRecursosHidricos = @TieneRecursosHidricos,
                                  UsoSuelo = @UsoSuelo,
                                  Pendiente = @Pendiente,
                                  EstadoFinca = @EstadoFinca,
                                  FechaRegistro = @FechaRegistro,
                                  FechaActualizacion = @FechaActualizacion
                              WHERE IdFinca = @IdFinca";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdFinca", finca.IdFinca);
                    command.Parameters.AddWithValue("@IdPropietario", finca.IdPropietario);
                    command.Parameters.AddWithValue("@NombreFinca", finca.NombreFinca);
                    command.Parameters.AddWithValue("@Provincia", finca.Provincia);
                    command.Parameters.AddWithValue("@Canton", finca.Canton);
                    command.Parameters.AddWithValue("@Distrito", finca.Distrito);
                    command.Parameters.AddWithValue("@DireccionExacta", (object?)finca.DireccionExacta ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Latitud", finca.Latitud);
                    command.Parameters.AddWithValue("@Longitud", finca.Longitud);
                    command.Parameters.AddWithValue("@Hectareas", finca.Hectareas);
                    command.Parameters.AddWithValue("@Vegetacion", finca.Vegetacion);
                    command.Parameters.AddWithValue("@TieneRecursosHidricos", finca.TieneRecursosHidricos);
                    command.Parameters.AddWithValue("@UsoSuelo", finca.UsoSuelo);
                    command.Parameters.AddWithValue("@Pendiente", finca.Pendiente);
                    command.Parameters.AddWithValue("@EstadoFinca", finca.EstadoFinca);
                    command.Parameters.AddWithValue("@FechaRegistro", finca.FechaRegistro);
                    command.Parameters.AddWithValue("@FechaActualizacion", finca.FechaActualizacion);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var connection = _dbContextHelper.CreateConnection())
            {
                var query = "DELETE FROM dbo.Fincas WHERE IdFinca = @IdFinca";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdFinca", id);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}