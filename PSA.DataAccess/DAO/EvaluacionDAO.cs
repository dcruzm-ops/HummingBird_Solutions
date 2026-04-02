using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.Entidades.Evaluaciones;

namespace PSA.DataAccess.DAO
{
    public class EvaluacionDAO
    {
        private readonly string _connectionString;

        public EvaluacionDAO(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> CrearEvaluacionAsync(EvaluacionTecnica evaluacion)
        {
            const string sql = @"
                INSERT INTO EvaluacionesTecnicas
                (
                    IdFinca,
                    IdIngeniero,
                    FechaVisita,
                    EstadoEvaluacion,
                    Observaciones,
                    DecisionTecnica
                )
                VALUES
                (
                    @IdFinca,
                    @IdIngeniero,
                    @FechaVisita,
                    @EstadoEvaluacion,
                    @Observaciones,
                    @DecisionTecnica
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@IdFinca", evaluacion.FincaId);
            command.Parameters.AddWithValue("@IdIngeniero", evaluacion.IngenieroForestalId);
            command.Parameters.AddWithValue("@FechaVisita", evaluacion.FechaEvaluacion);
            command.Parameters.AddWithValue("@EstadoEvaluacion", evaluacion.Estado);
            command.Parameters.AddWithValue("@Observaciones", (object?)evaluacion.Observaciones ?? DBNull.Value);
            command.Parameters.AddWithValue("@DecisionTecnica", (object?)evaluacion.Decision ?? DBNull.Value);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();

            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<EvaluacionTecnica?> ObtenerPorIdAsync(int id)
        {
            const string sql = @"
                SELECT *
                FROM EvaluacionesTecnicas
                WHERE IdEvaluacion = @IdEvaluacion";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdEvaluacion", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            return Map(reader);
        }

        public async Task<bool> ActualizarEvaluacionAsync(EvaluacionTecnica evaluacion)
        {
            const string sql = @"
                UPDATE EvaluacionesTecnicas
                SET 
                    EstadoEvaluacion = @EstadoEvaluacion,
                    DecisionTecnica = @DecisionTecnica,
                    Observaciones = @Observaciones,
                    FechaDecision = @FechaDecision
                WHERE IdEvaluacion = @IdEvaluacion";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@IdEvaluacion", evaluacion.Id);
            command.Parameters.AddWithValue("@EstadoEvaluacion", evaluacion.Estado);
            command.Parameters.AddWithValue("@DecisionTecnica", (object?)evaluacion.Decision ?? DBNull.Value);
            command.Parameters.AddWithValue("@Observaciones", (object?)evaluacion.Observaciones ?? DBNull.Value);
            command.Parameters.AddWithValue("@FechaDecision", DateTime.Now);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static EvaluacionTecnica Map(SqlDataReader reader)
        {
            return new EvaluacionTecnica
            {
                Id = Convert.ToInt32(reader["IdEvaluacion"]),
                FincaId = Convert.ToInt32(reader["IdFinca"]),
                IngenieroForestalId = Convert.ToInt32(reader["IdIngeniero"]),
                FechaEvaluacion = Convert.ToDateTime(reader["FechaVisita"]),
                Estado = reader["EstadoEvaluacion"]?.ToString() ?? "",
                Decision = reader["DecisionTecnica"] == DBNull.Value ? null : reader["DecisionTecnica"]?.ToString(),
                Observaciones = reader["Observaciones"] == DBNull.Value ? null : reader["Observaciones"]?.ToString()
            };
        }
    }
}