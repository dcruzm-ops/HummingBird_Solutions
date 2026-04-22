using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Evaluaciones;
using System.Linq;
using System.Text;

using PSA.DataAccess;

namespace PSA.DataAccess.DAO
{
    public class EvaluacionTecnicaDAO
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public EvaluacionTecnicaDAO(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
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

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdFinca", idFinca);
            command.Parameters.AddWithValue("@EstadoPendiente", EstadosEvaluacionTecnica.Pendiente);
            command.Parameters.AddWithValue("@EstadoEnProceso", EstadosEvaluacionTecnica.EnProceso);

            try
            {
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine($"Error SQL al crear evaluación pendiente para la finca {idFinca}: {ex.Message}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error inesperado al crear evaluación pendiente para la finca {idFinca}: {ex.Message}");
                return 0;
            }
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

            using var connection = _connectionFactory.CreateConnection();
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
    f.EstadoFinca,
    e.FechaVisita,
    e.Observaciones,
    e.DecisionTecnica,
    e.HectareasAjustadas,
    e.VegetacionAjustada,
    e.RecursosHidricosAjustado,
    e.UsoSueloAjustado,
    e.PendienteAjustada,
    e.FechaDecision
FROM EvaluacionesTecnicas e
INNER JOIN Fincas f ON f.IdFinca = e.IdFinca
WHERE e.IdEvaluacion = @IdEvaluacion;";

            using var connection = _connectionFactory.CreateConnection();
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
                EstadoFinca = reader["EstadoFinca"]?.ToString() ?? string.Empty,
                FechaVisita = reader["FechaVisita"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("FechaVisita")),
                Observaciones = reader["Observaciones"] == DBNull.Value ? null : reader["Observaciones"]?.ToString(),
                DecisionTecnica = reader["DecisionTecnica"] == DBNull.Value ? null : reader["DecisionTecnica"]?.ToString(),
                HectareasAjustadas = reader["HectareasAjustadas"] == DBNull.Value ? null : reader.GetDecimal(reader.GetOrdinal("HectareasAjustadas")),
                VegetacionAjustada = reader["VegetacionAjustada"] == DBNull.Value ? null : reader["VegetacionAjustada"]?.ToString(),
                RecursosHidricosAjustado = reader["RecursosHidricosAjustado"] == DBNull.Value ? null : reader.GetBoolean(reader.GetOrdinal("RecursosHidricosAjustado")),
                UsoSueloAjustado = reader["UsoSueloAjustado"] == DBNull.Value ? null : reader["UsoSueloAjustado"]?.ToString(),
                PendienteAjustada = reader["PendienteAjustada"] == DBNull.Value ? null : reader["PendienteAjustada"]?.ToString(),
                FechaDecision = reader["FechaDecision"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("FechaDecision"))
            };
        }

        public async Task<bool> AsignarIngenieroAsync(int idEvaluacion, int idIngeniero)
        {
            const string sql = @"
BEGIN TRAN;

UPDATE e
SET e.IdIngeniero = @IdIngeniero,
    e.EstadoEvaluacion = @EstadoEnProceso
FROM EvaluacionesTecnicas e
WHERE e.IdEvaluacion = @IdEvaluacion
  AND e.EstadoEvaluacion = @EstadoPendiente;

IF @@ROWCOUNT = 0
BEGIN
    ROLLBACK TRAN;
    SELECT CAST(0 AS bit);
    RETURN;
END

UPDATE f
SET f.EstadoFinca = @EstadoFincaEnProceso,
    f.FechaActualizacion = SYSDATETIME()
FROM Fincas f
INNER JOIN EvaluacionesTecnicas e ON e.IdFinca = f.IdFinca
WHERE e.IdEvaluacion = @IdEvaluacion;

COMMIT TRAN;
SELECT CAST(1 AS bit);";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdEvaluacion", idEvaluacion);
            command.Parameters.AddWithValue("@IdIngeniero", idIngeniero);
            command.Parameters.AddWithValue("@EstadoPendiente", EstadosEvaluacionTecnica.Pendiente);
            command.Parameters.AddWithValue("@EstadoEnProceso", EstadosEvaluacionTecnica.EnProceso);
            command.Parameters.AddWithValue("@EstadoFincaEnProceso", "En proceso");

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result is bool boolResult && boolResult;
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
BEGIN TRAN;

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
    e.EstadoEvaluacion = @EstadoEvaluacion
FROM EvaluacionesTecnicas e
WHERE e.IdEvaluacion = @IdEvaluacion
  AND e.EstadoEvaluacion IN (@EstadoPendiente, @EstadoEnProceso);

IF @@ROWCOUNT = 0
BEGIN
    ROLLBACK TRAN;
    SELECT CAST(0 AS bit);
    RETURN;
END

UPDATE f
SET f.Hectareas = COALESCE(@HectareasAjustadas, f.Hectareas),
    f.Vegetacion = COALESCE(@VegetacionAjustada, f.Vegetacion),
    f.TieneRecursosHidricos = COALESCE(@RecursosHidricosAjustado, f.TieneRecursosHidricos),
    f.UsoSuelo = COALESCE(@UsoSueloAjustado, f.UsoSuelo),
    f.Pendiente = COALESCE(@PendienteAjustada, f.Pendiente),
    f.EstadoFinca = @EstadoFinca,
    f.FechaActualizacion = SYSDATETIME()
FROM Fincas f
INNER JOIN EvaluacionesTecnicas e ON e.IdFinca = f.IdFinca
WHERE e.IdEvaluacion = @IdEvaluacion;

COMMIT TRAN;
SELECT CAST(1 AS bit);";

            using var connection = _connectionFactory.CreateConnection();
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

            try
            {
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result is bool boolResult && boolResult;
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine($"Error SQL al registrar resultado de evaluación #{idEvaluacion}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error inesperado al registrar resultado de evaluación #{idEvaluacion}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ActualizarEstadoEvaluacionAsync(int idEvaluacion, string nuevoEstado)
        {
            const string sql = @"
UPDATE EvaluacionesTecnicas
SET EstadoEvaluacion = @EstadoEvaluacion
WHERE IdEvaluacion = @IdEvaluacion;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdEvaluacion", idEvaluacion);
            command.Parameters.AddWithValue("@EstadoEvaluacion", nuevoEstado);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<ReporteEvaluacionesDTO> ObtenerReporteEvaluacionesAsync(FiltroReporteEvaluacionesDTO filtro)
        {
            var sql = new StringBuilder(@"
SELECT
    e.IdEvaluacion,
    e.IdFinca,
    CASE WHEN pp.IdPlanPago IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS TienePlanPago,
    f.NombreFinca,
    e.EstadoEvaluacion,
    e.DecisionTecnica,
    e.FechaVisita,
    e.FechaDecision,
    f.Provincia,
    f.Canton,
    f.Distrito
FROM EvaluacionesTecnicas e
INNER JOIN Fincas f ON f.IdFinca = e.IdFinca
OUTER APPLY (
    SELECT TOP 1 p.IdPlanPago
    FROM PlanesPago p
    WHERE p.IdEvaluacion = e.IdEvaluacion
    ORDER BY p.IdPlanPago DESC
) pp
WHERE 1 = 1");

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand();
            command.Connection = connection;

            if (filtro.Anio.HasValue)
            {
                sql.Append(" AND YEAR(COALESCE(e.FechaDecision, e.FechaVisita)) = @Anio");
                command.Parameters.AddWithValue("@Anio", filtro.Anio.Value);
            }

            if (filtro.Mes.HasValue)
            {
                sql.Append(" AND MONTH(COALESCE(e.FechaDecision, e.FechaVisita)) = @Mes");
                command.Parameters.AddWithValue("@Mes", filtro.Mes.Value);
            }

            if (!string.IsNullOrWhiteSpace(filtro.EstadoEvaluacion))
            {
                sql.Append(" AND e.EstadoEvaluacion = @EstadoEvaluacion");
                command.Parameters.AddWithValue("@EstadoEvaluacion", filtro.EstadoEvaluacion.Trim());
            }

            if (!string.IsNullOrWhiteSpace(filtro.DecisionTecnica))
            {
                sql.Append(" AND e.DecisionTecnica = @DecisionTecnica");
                command.Parameters.AddWithValue("@DecisionTecnica", filtro.DecisionTecnica.Trim());
            }

            if (filtro.IdIngeniero.HasValue && filtro.IdIngeniero.Value > 0)
            {
                sql.Append(" AND e.IdIngeniero = @IdIngeniero");
                command.Parameters.AddWithValue("@IdIngeniero", filtro.IdIngeniero.Value);
            }

            sql.Append(" ORDER BY COALESCE(e.FechaDecision, e.FechaVisita) DESC, e.IdEvaluacion DESC;");
            command.CommandText = sql.ToString();

            var resultado = new ReporteEvaluacionesDTO();
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.Evaluaciones.Add(new ItemReporteEvaluacionDTO
                {
                    IdEvaluacion = reader.GetInt32(reader.GetOrdinal("IdEvaluacion")),
                    IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                    TienePlanPago = reader.GetBoolean(reader.GetOrdinal("TienePlanPago")),
                    NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                    EstadoEvaluacion = reader["EstadoEvaluacion"]?.ToString() ?? string.Empty,
                    DecisionTecnica = reader["DecisionTecnica"] == DBNull.Value ? null : reader["DecisionTecnica"]?.ToString(),
                    FechaVisita = reader["FechaVisita"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("FechaVisita")),
                    FechaDecision = reader["FechaDecision"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("FechaDecision")),
                    Provincia = reader["Provincia"]?.ToString() ?? string.Empty,
                    Canton = reader["Canton"]?.ToString() ?? string.Empty,
                    Distrito = reader["Distrito"]?.ToString() ?? string.Empty
                });
            }

            resultado.TotalEvaluaciones = resultado.Evaluaciones.Count;
            resultado.TotalCalifica = resultado.Evaluaciones.Count(x => string.Equals(x.DecisionTecnica, "Califica", StringComparison.OrdinalIgnoreCase));
            resultado.TotalNoCalifica = resultado.Evaluaciones.Count(x => string.Equals(x.DecisionTecnica, "No califica", StringComparison.OrdinalIgnoreCase));
            resultado.TotalPendientes = resultado.Evaluaciones.Count(x =>
                string.Equals(x.EstadoEvaluacion, EstadosEvaluacionTecnica.Pendiente, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.EstadoEvaluacion, EstadosEvaluacionTecnica.EnProceso, StringComparison.OrdinalIgnoreCase));

            return resultado;
        }
    }
}
