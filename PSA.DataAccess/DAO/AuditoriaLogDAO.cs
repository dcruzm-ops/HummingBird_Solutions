using Microsoft.Data.SqlClient;

using PSA.DataAccess;

namespace PSA.DataAccess.DAO
{
    public class AuditoriaLogDAO
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AuditoriaLogDAO(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task RegistrarEventoAsync(
            int? idUsuario,
            string modulo,
            string tablaAfectada,
            string accion,
            string? detalle = null,
            int? idRegistroAfectado = null,
            string? ipOrigen = null,
            string? valorAnterior = null,
            string? valorNuevo = null)
        {
            const string sql = @"
INSERT INTO AuditoriaLog
(
    IdUsuario,
    Modulo,
    TablaAfectada,
    IdRegistroAfectado,
    Accion,
    ValorAnterior,
    ValorNuevo,
    IpOrigen,
    Detalle
)
VALUES
(
    @IdUsuario,
    @Modulo,
    @TablaAfectada,
    @IdRegistroAfectado,
    @Accion,
    @ValorAnterior,
    @ValorNuevo,
    @IpOrigen,
    @Detalle
);";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@IdUsuario", (object?)idUsuario ?? DBNull.Value);
            command.Parameters.AddWithValue("@Modulo", modulo);
            command.Parameters.AddWithValue("@TablaAfectada", tablaAfectada);
            command.Parameters.AddWithValue("@IdRegistroAfectado", (object?)idRegistroAfectado ?? DBNull.Value);
            command.Parameters.AddWithValue("@Accion", accion);
            command.Parameters.AddWithValue("@ValorAnterior", (object?)valorAnterior ?? DBNull.Value);
            command.Parameters.AddWithValue("@ValorNuevo", (object?)valorNuevo ?? DBNull.Value);
            command.Parameters.AddWithValue("@IpOrigen", (object?)ipOrigen ?? DBNull.Value);
            command.Parameters.AddWithValue("@Detalle", (object?)detalle ?? DBNull.Value);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}
