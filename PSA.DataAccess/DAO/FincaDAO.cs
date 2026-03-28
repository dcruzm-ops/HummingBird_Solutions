using Microsoft.Data.SqlClient;
    using PSA.EntidadesDTO.DTOs;
    using PSA.EntidadesDTO.DTOs.Fincas;

    namespace PSA.DataAccess.DAO
    {
        public class FincaDAO
        {
            private readonly string _connectionString;

            public FincaDAO(string connectionString)
            {
                _connectionString = connectionString;
            }

            public async Task<List<FincaResumenDTO>> ObtenerPorPropietarioAsync(int idPropietario)
            {
                const string sql = @"
SELECT
    f.IdFinca,
    f.NombreFinca,
    f.Provincia,
    f.Canton,
    f.Distrito,
    f.Hectareas,
    f.EstadoFinca,
    f.FechaRegistro,
    ISNULL(ev.EstadoEvaluacion, 'Sin iniciar') AS EstadoEvaluacion
FROM Fincas f
OUTER APPLY (
    SELECT TOP 1 e.EstadoEvaluacion
    FROM EvaluacionesTecnicas e
    WHERE e.IdFinca = f.IdFinca
    ORDER BY e.IdEvaluacion DESC
) ev
WHERE f.IdPropietario = @IdPropietario
ORDER BY f.FechaRegistro DESC;";

                var resultado = new List<FincaResumenDTO>();

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@IdPropietario", idPropietario);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    resultado.Add(new FincaResumenDTO
                    {
                        IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                        NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                        Provincia = reader["Provincia"]?.ToString() ?? string.Empty,
                        Canton = reader["Canton"]?.ToString() ?? string.Empty,
                        Distrito = reader["Distrito"]?.ToString() ?? string.Empty,
                        Hectareas = reader.GetDecimal(reader.GetOrdinal("Hectareas")),
                        EstadoFinca = reader["EstadoFinca"]?.ToString() ?? string.Empty,
                        EstadoEvaluacion = reader["EstadoEvaluacion"]?.ToString() ?? "Sin iniciar",
                        FechaRegistro = reader.GetDateTime(reader.GetOrdinal("FechaRegistro"))
                    });
                }

                return resultado;
            }

            public async Task<FincaDetalleDTO?> ObtenerDetalleAsync(int idFinca, int idPropietario)
            {
                const string sql = @"
SELECT
    f.IdFinca,
    f.IdPropietario,
    f.NombreFinca,
    u.NombreCompleto AS PropietarioNombre,
    f.Provincia,
    f.Canton,
    f.Distrito,
    f.DireccionExacta,
    f.Latitud,
    f.Longitud,
    f.Hectareas,
    f.Vegetacion,
    f.TieneRecursosHidricos,
    f.UsoSuelo,
    f.Pendiente,
    f.EstadoFinca,
    f.FechaRegistro,
    f.FechaActualizacion,
    ISNULL(ev.EstadoEvaluacion, 'Sin iniciar') AS EstadoEvaluacion,
    ISNULL(evd.CantidadEvidencias, 0) AS CantidadEvidencias,
    CASE WHEN cb.IdCuentaBancaria IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS TieneCuentaBancaria,
    CASE WHEN pp.IdPlanPago IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS TienePlanPago
FROM Fincas f
INNER JOIN Usuarios u ON u.IdUsuario = f.IdPropietario
OUTER APPLY (
    SELECT TOP 1 e.EstadoEvaluacion
    FROM EvaluacionesTecnicas e
    WHERE e.IdFinca = f.IdFinca
    ORDER BY e.IdEvaluacion DESC
) ev
OUTER APPLY (
    SELECT COUNT(1) AS CantidadEvidencias
    FROM FincaEvidencias fe
    WHERE fe.IdFinca = f.IdFinca
) evd
OUTER APPLY (
    SELECT TOP 1 c.IdCuentaBancaria
    FROM CuentasBancarias c
    WHERE c.IdUsuario = f.IdPropietario
    ORDER BY c.IdCuentaBancaria DESC
) cb
OUTER APPLY (
    SELECT TOP 1 p.IdPlanPago
    FROM PlanesPago p
    WHERE p.IdFinca = f.IdFinca
    ORDER BY p.IdPlanPago DESC
) pp
WHERE f.IdFinca = @IdFinca
  AND f.IdPropietario = @IdPropietario;";

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@IdFinca", idFinca);
                command.Parameters.AddWithValue("@IdPropietario", idPropietario);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return null;
                }

                return new FincaDetalleDTO
                {
                    IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                    IdPropietario = reader.GetInt32(reader.GetOrdinal("IdPropietario")),
                    NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                    PropietarioNombre = reader["PropietarioNombre"]?.ToString() ?? string.Empty,
                    Provincia = reader["Provincia"]?.ToString() ?? string.Empty,
                    Canton = reader["Canton"]?.ToString() ?? string.Empty,
                    Distrito = reader["Distrito"]?.ToString() ?? string.Empty,
                    DireccionExacta = reader["DireccionExacta"] == DBNull.Value ? null : reader["DireccionExacta"]?.ToString(),
                    Latitud = reader.GetDecimal(reader.GetOrdinal("Latitud")),
                    Longitud = reader.GetDecimal(reader.GetOrdinal("Longitud")),
                    Hectareas = reader.GetDecimal(reader.GetOrdinal("Hectareas")),
                    Vegetacion = reader["Vegetacion"]?.ToString() ?? string.Empty,
                    TieneRecursosHidricos = reader.GetBoolean(reader.GetOrdinal("TieneRecursosHidricos")),
                    UsoSuelo = reader["UsoSuelo"]?.ToString() ?? string.Empty,
                    Pendiente = reader["Pendiente"]?.ToString() ?? string.Empty,
                    EstadoFinca = reader["EstadoFinca"]?.ToString() ?? string.Empty,
                    FechaRegistro = reader.GetDateTime(reader.GetOrdinal("FechaRegistro")),
                    FechaActualizacion = reader.GetDateTime(reader.GetOrdinal("FechaActualizacion")),
                    EstadoEvaluacion = reader["EstadoEvaluacion"]?.ToString() ?? "Sin iniciar",
                    CantidadEvidencias = reader.GetInt32(reader.GetOrdinal("CantidadEvidencias")),
                    TieneCuentaBancaria = reader.GetBoolean(reader.GetOrdinal("TieneCuentaBancaria")),
                    TienePlanPago = reader.GetBoolean(reader.GetOrdinal("TienePlanPago"))
                };
            }

            public List<FincaDTO> RetrieveAll()
            {
                var list = new List<FincaDTO>();

                using var connection = new SqlConnection(_connectionString);
                const string query = @"
SELECT IdFinca, IdPropietario, NombreFinca, Provincia, Canton, Distrito,
       DireccionExacta, Latitud, Longitud, Hectareas, Vegetacion,
       TieneRecursosHidricos, UsoSuelo, Pendiente, EstadoFinca,
       FechaRegistro, FechaActualizacion
FROM dbo.Fincas";

                using var command = new SqlCommand(query, connection);
                connection.Open();
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(MapFinca(reader));
                }

                return list;
            }

            public FincaDTO? RetrieveById(int id)
            {
                using var connection = new SqlConnection(_connectionString);
                const string query = @"
SELECT IdFinca, IdPropietario, NombreFinca, Provincia, Canton, Distrito,
       DireccionExacta, Latitud, Longitud, Hectareas, Vegetacion,
       TieneRecursosHidricos, UsoSuelo, Pendiente, EstadoFinca,
       FechaRegistro, FechaActualizacion
FROM dbo.Fincas
WHERE IdFinca = @IdFinca";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@IdFinca", id);
                connection.Open();
                using var reader = command.ExecuteReader();

                return reader.Read() ? MapFinca(reader) : null;
            }

            public void Create(FincaDTO finca)
            {
                using var connection = new SqlConnection(_connectionString);
                const string query = @"
INSERT INTO dbo.Fincas
(IdPropietario, NombreFinca, Provincia, Canton, Distrito, DireccionExacta,
 Latitud, Longitud, Hectareas, Vegetacion, TieneRecursosHidricos,
 UsoSuelo, Pendiente, EstadoFinca, FechaRegistro, FechaActualizacion)
VALUES
(@IdPropietario, @NombreFinca, @Provincia, @Canton, @Distrito, @DireccionExacta,
 @Latitud, @Longitud, @Hectareas, @Vegetacion, @TieneRecursosHidricos,
 @UsoSuelo, @Pendiente, @EstadoFinca, @FechaRegistro, @FechaActualizacion)";

                using var command = new SqlCommand(query, connection);
                BuildFincaParameters(command, finca, includeId: false);
                connection.Open();
                command.ExecuteNonQuery();
            }

            public void Update(FincaDTO finca)
            {
                using var connection = new SqlConnection(_connectionString);
                const string query = @"
UPDATE dbo.Fincas
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

                using var command = new SqlCommand(query, connection);
                BuildFincaParameters(command, finca, includeId: true);
                connection.Open();
                command.ExecuteNonQuery();
            }

            public void Delete(int id)
            {
                using var connection = new SqlConnection(_connectionString);
                const string query = "DELETE FROM dbo.Fincas WHERE IdFinca = @IdFinca";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@IdFinca", id);
                connection.Open();
                command.ExecuteNonQuery();
            }

            private static FincaDTO MapFinca(SqlDataReader reader)
            {
                return new FincaDTO
                {
                    IdFinca = Convert.ToInt32(reader["IdFinca"]),
                    IdPropietario = Convert.ToInt32(reader["IdPropietario"]),
                    NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                    Provincia = reader["Provincia"]?.ToString() ?? string.Empty,
                    Canton = reader["Canton"]?.ToString() ?? string.Empty,
                    Distrito = reader["Distrito"]?.ToString() ?? string.Empty,
                    DireccionExacta = reader["DireccionExacta"] == DBNull.Value ? null : reader["DireccionExacta"]?.ToString(),
                    Latitud = Convert.ToDecimal(reader["Latitud"]),
                    Longitud = Convert.ToDecimal(reader["Longitud"]),
                    Hectareas = Convert.ToDecimal(reader["Hectareas"]),
                    Vegetacion = reader["Vegetacion"]?.ToString() ?? string.Empty,
                    TieneRecursosHidricos = Convert.ToBoolean(reader["TieneRecursosHidricos"]),
                    UsoSuelo = reader["UsoSuelo"]?.ToString() ?? string.Empty,
                    Pendiente = reader["Pendiente"]?.ToString() ?? string.Empty,
                    EstadoFinca = reader["EstadoFinca"]?.ToString() ?? string.Empty,
                    FechaRegistro = Convert.ToDateTime(reader["FechaRegistro"]),
                    FechaActualizacion = Convert.ToDateTime(reader["FechaActualizacion"])
                };
            }

            private static void BuildFincaParameters(SqlCommand command, FincaDTO finca, bool includeId)
            {
                if (includeId)
                {
                    command.Parameters.AddWithValue("@IdFinca", finca.IdFinca);
                }

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
            }
        }
    }
