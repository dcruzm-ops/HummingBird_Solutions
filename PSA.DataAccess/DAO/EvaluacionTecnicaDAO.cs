using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Evaluaciones;

namespace PSA.DataAccess.DAO
{
    public class EvaluacionTecnicaDAO
    {
        private readonly string _connectionString;

        public EvaluacionTecnicaDAO(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> CrearEvaluacionPendienteAsync(int idFinca)
        {
            const string sql = @"
INSERT INTO EvaluacionesTecnicas (IdFinca, IdIngeniero, EstadoEvaluacion)
SELECT @IdFinca, NULL, @EstadoPendiente
WHERE NOT EXISTS (
    SELECT 1
    FROM EvaluacionesTecnicas
    WHERE IdFinca = @IdFinca
      AND EstadoEvaluacion IN (@EstadoPendiente, @EstadoEnProceso)
);

SELECT TOP 1 IdEvaluacion
FROM EvaluacionesTecnicas
WHERE IdFinca = @IdFinca
ORDER BY IdEvaluacion DESC;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdFinca", idFinca);
            command.Parameters.AddWithValue("@EstadoPendiente", EstadosEvaluacionTecnica.Pendiente);
            command.Parameters.AddWithValue("@EstadoEnProceso", EstadosEvaluacionTecnica.EnProceso);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<List<BandejaEvaluacionPendienteDTO>> ObtenerBandejaPendientesAsync()
        {
            const string sql = @"
SELECT e.IdEvaluacion, e.IdFinca, e.IdIngeniero, e.EstadoEvaluacion,
       f.NombreFinca, f.Provincia, f.Canton, f.Distrito, f.Hectareas
FROM EvaluacionesTecnicas e
INNER JOIN Fincas f ON f.IdFinca = e.IdFinca
WHERE e.EstadoEvaluacion IN (@EstadoPendiente, @EstadoEnProceso)
ORDER BY e.IdEvaluacion ASC;";

            var resultado = new List<BandejaEvaluacionPendienteDTO>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@EstadoPendiente", EstadosEvaluacionTecnica.Pendiente);
            command.Parameters.AddWithValue("@EstadoEnProceso", EstadosEvaluacionTecnica.EnProceso);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.Add(new BandejaEvaluacionPendienteDTO
                {
                    IdEvaluacion = reader.GetInt32(reader.GetOrdinal("IdEvaluacion")),
                    IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                    IdIngeniero = reader["IdIngeniero"] == DBNull.Value ? null : reader.GetInt32(reader.GetOrdinal("IdIngeniero")),
                    EstadoEvaluacion = reader["EstadoEvaluacion"]?.ToString() ?? string.Empty,
                    NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                    Provincia = reader["Provincia"]?.ToString() ?? string.Empty,
                    Canton = reader["Canton"]?.ToString() ?? string.Empty,
                    Distrito = reader["Distrito"]?.ToString() ?? string.Empty,
                    Hectareas = reader.GetDecimal(reader.GetOrdinal("Hectareas"))
                });
            }

            return resultado;
        }

        public async Task<DetalleFincaParaEvaluacionDTO?> ObtenerDetalleParaEvaluacionAsync(int idEvaluacion)
        {
            const string sql = @"
SELECT TOP 1
    e.IdEvaluacion,
    e.IdFinca,
    e.IdIngeniero,
    e.EstadoEvaluacion,
    f.IdPropietario,
    f.NombreFinca,
    f.Provincia,
    f.Canton,
    f.Distrito,
    f.Hectareas,
    f.Vegetacion,
    f.TieneRecursosHidricos,
    f.UsoSuelo,
    f.Pendiente,
    f.EstadoFinca
FROM EvaluacionesTecnicas e
INNER JOIN Fincas f ON f.IdFinca = e.IdFinca
WHERE e.IdEvaluacion = @IdEvaluacion;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdEvaluacion", idEvaluacion);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new DetalleFincaParaEvaluacionDTO
            {
                IdEvaluacion = reader.GetInt32(reader.GetOrdinal("IdEvaluacion")),
                IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                IdIngeniero = reader["IdIngeniero"] == DBNull.Value ? null : reader.GetInt32(reader.GetOrdinal("IdIngeniero")),
                EstadoEvaluacion = reader["EstadoEvaluacion"]?.ToString() ?? string.Empty,
                IdPropietario = reader.GetInt32(reader.GetOrdinal("IdPropietario")),
                NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                Provincia = reader["Provincia"]?.ToString() ?? string.Empty,
                Canton = reader["Canton"]?.ToString() ?? string.Empty,
                Distrito = reader["Distrito"]?.ToString() ?? string.Empty,
                Hectareas = reader.GetDecimal(reader.GetOrdinal("Hectareas")),
                Vegetacion = reader["Vegetacion"]?.ToString() ?? string.Empty,
                TieneRecursosHidricos = reader.GetBoolean(reader.GetOrdinal("TieneRecursosHidricos")),
                UsoSuelo = reader["UsoSuelo"]?.ToString() ?? string.Empty,
                Pendiente = reader["Pendiente"]?.ToString() ?? string.Empty,
                EstadoFinca = reader["EstadoFinca"]?.ToString() ?? string.Empty
            };
        }

        public async Task<bool> AsignarIngenieroAsync(int idEvaluacion, int idIngeniero)
        {
            const string sql = @"
UPDATE e
SET e.IdIngeniero = @IdIngeniero,
    e.EstadoEvaluacion = @EstadoEnProceso,
    f.EstadoFinca = @EstadoFincaEnProceso,
    f.FechaActualizacion = SYSDATETIME()
FROM EvaluacionesTecnicas e
INNER JOIN Fincas f ON f.IdFinca = e.IdFinca
WHERE e.IdEvaluacion = @IdEvaluacion
  AND e.EstadoEvaluacion = @EstadoPendiente;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdEvaluacion", idEvaluacion);
            command.Parameters.AddWithValue("@IdIngeniero", idIngeniero);
            command.Parameters.AddWithValue("@EstadoPendiente", EstadosEvaluacionTecnica.Pendiente);
            command.Parameters.AddWithValue("@EstadoEnProceso", EstadosEvaluacionTecnica.EnProceso);
            command.Parameters.AddWithValue("@EstadoFincaEnProceso", "En proceso");

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> RegistrarResultadoAsync(int idEvaluacion, RegistrarResultadoEvaluacionDTO dto)
        {
            var estadoEvaluacion = dto.DecisionTecnica.Equals("Califica", StringComparison.OrdinalIgnoreCase)
                ? EstadosEvaluacionTecnica.EvaluadaCalifica
                : EstadosEvaluacionTecnica.EvaluadaNoCalifica;

            var estadoFinca = dto.DecisionTecnica.Equals("Califica", StringComparison.OrdinalIgnoreCase)
                ? "Aprobada"
                : "Rechazada";

            const string sql = @"
UPDATE e
SET e.FechaVisita = @FechaVisita,
    e.Observaciones = @Observaciones,
    e.DecisionTecnica = @DecisionTecnica,
    e.HectareasAjustadas = @HectareasAjustadas,
    e.VegetacionAjustada = @VegetacionAjustada,
    e.RecursosHidricosAjustado = @RecursosHidricosAjustado,
    e.UsoSueloAjustado = @UsoSueloAjustado,
    e.PendienteAjustada = @PendienteAjustada,
    e.FechaDecision = SYSDATETIME(),
    e.EstadoEvaluacion = @EstadoEvaluacion,
    f.Hectareas = COALESCE(@HectareasAjustadas, f.Hectareas),
    f.Vegetacion = COALESCE(@VegetacionAjustada, f.Vegetacion),
    f.TieneRecursosHidricos = COALESCE(@RecursosHidricosAjustado, f.TieneRecursosHidricos),
    f.UsoSuelo = COALESCE(@UsoSueloAjustado, f.UsoSuelo),
    f.Pendiente = COALESCE(@PendienteAjustada, f.Pendiente),
    f.EstadoFinca = @EstadoFinca,
    f.FechaActualizacion = SYSDATETIME()
FROM EvaluacionesTecnicas e
INNER JOIN Fincas f ON f.IdFinca = e.IdFinca
WHERE e.IdEvaluacion = @IdEvaluacion
  AND e.EstadoEvaluacion IN (@EstadoPendiente, @EstadoEnProceso);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdEvaluacion", idEvaluacion);
            command.Parameters.AddWithValue("@FechaVisita", dto.FechaVisita);
            command.Parameters.AddWithValue("@Observaciones", (object?)dto.Observaciones ?? DBNull.Value);
            command.Parameters.AddWithValue("@DecisionTecnica", dto.DecisionTecnica);
            command.Parameters.AddWithValue("@HectareasAjustadas", (object?)dto.HectareasAjustadas ?? DBNull.Value);
            command.Parameters.AddWithValue("@VegetacionAjustada", (object?)dto.VegetacionAjustada ?? DBNull.Value);
            command.Parameters.AddWithValue("@RecursosHidricosAjustado", (object?)dto.RecursosHidricosAjustado ?? DBNull.Value);
            command.Parameters.AddWithValue("@UsoSueloAjustado", (object?)dto.UsoSueloAjustado ?? DBNull.Value);
            command.Parameters.AddWithValue("@PendienteAjustada", (object?)dto.PendienteAjustada ?? DBNull.Value);
            command.Parameters.AddWithValue("@EstadoEvaluacion", estadoEvaluacion);
            command.Parameters.AddWithValue("@EstadoFinca", estadoFinca);
            command.Parameters.AddWithValue("@EstadoPendiente", EstadosEvaluacionTecnica.Pendiente);
            command.Parameters.AddWithValue("@EstadoEnProceso", EstadosEvaluacionTecnica.EnProceso);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ActualizarEstadoEvaluacionAsync(int idEvaluacion, string nuevoEstado)
        {
            const string sql = @"
UPDATE EvaluacionesTecnicas
SET EstadoEvaluacion = @EstadoEvaluacion
WHERE IdEvaluacion = @IdEvaluacion;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdEvaluacion", idEvaluacion);
            command.Parameters.AddWithValue("@EstadoEvaluacion", nuevoEstado);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}
