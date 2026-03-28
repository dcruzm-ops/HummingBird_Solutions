using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs;

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
    }
}
