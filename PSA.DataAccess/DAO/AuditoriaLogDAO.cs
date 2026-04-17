using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Administracion;

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

        public async Task<List<AuditoriaEventoDTO>> ObtenerEventosAsync(AuditoriaFiltroDTO filtro)
        {
            var sql = @"
SELECT TOP (@MaximoRegistros)
    a.IdLog,
    a.IdUsuario,
    u.NombreCompleto AS NombreUsuario,
    a.Modulo,
    a.TablaAfectada,
    a.IdRegistroAfectado,
    a.Accion,
    a.Detalle,
    a.IpOrigen,
    a.FechaAccion,
    a.ValorAnterior,
    a.ValorNuevo
FROM dbo.AuditoriaLog a
LEFT JOIN dbo.Usuarios u ON u.IdUsuario = a.IdUsuario
WHERE (@Modulo IS NULL OR a.Modulo = @Modulo)
  AND (@Accion IS NULL OR a.Accion = @Accion)
  AND (@FechaDesde IS NULL OR a.FechaAccion >= @FechaDesde)
  AND (@FechaHasta IS NULL OR a.FechaAccion <= @FechaHasta)
ORDER BY a.IdLog DESC;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@MaximoRegistros", filtro.MaximoRegistros <= 0 ? 50 : filtro.MaximoRegistros);
            command.Parameters.AddWithValue("@Modulo", (object?)filtro.Modulo ?? DBNull.Value);
            command.Parameters.AddWithValue("@Accion", (object?)filtro.Accion ?? DBNull.Value);
            command.Parameters.AddWithValue("@FechaDesde", (object?)filtro.FechaDesde ?? DBNull.Value);
            command.Parameters.AddWithValue("@FechaHasta", (object?)filtro.FechaHasta ?? DBNull.Value);

            var eventos = new List<AuditoriaEventoDTO>();
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                eventos.Add(new AuditoriaEventoDTO
                {
                    IdLog = reader.GetInt32(reader.GetOrdinal("IdLog")),
                    IdUsuario = reader["IdUsuario"] == DBNull.Value ? null : reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                    NombreUsuario = reader["NombreUsuario"] == DBNull.Value ? null : reader["NombreUsuario"]?.ToString(),
                    Modulo = reader["Modulo"]?.ToString() ?? string.Empty,
                    TablaAfectada = reader["TablaAfectada"]?.ToString() ?? string.Empty,
                    IdRegistroAfectado = reader["IdRegistroAfectado"] == DBNull.Value ? null : reader.GetInt32(reader.GetOrdinal("IdRegistroAfectado")),
                    Accion = reader["Accion"]?.ToString() ?? string.Empty,
                    Detalle = reader["Detalle"] == DBNull.Value ? null : reader["Detalle"]?.ToString(),
                    IpOrigen = reader["IpOrigen"] == DBNull.Value ? null : reader["IpOrigen"]?.ToString(),
                    FechaAccion = reader.GetDateTime(reader.GetOrdinal("FechaAccion")),
                    ValorAnterior = reader["ValorAnterior"] == DBNull.Value ? null : reader["ValorAnterior"]?.ToString(),
                    ValorNuevo = reader["ValorNuevo"] == DBNull.Value ? null : reader["ValorNuevo"]?.ToString()
                });
            }

            return eventos;
        }
    }
}
