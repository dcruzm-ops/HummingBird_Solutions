using Microsoft.Data.SqlClient;
using PSA.DataAccess;
using PSA.EntidadesDTO.DTOs.Notificaciones;

namespace PSA.DataAccess.DAO;

public class NotificacionDAO
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NotificacionDAO(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CrearAsync(int idUsuario, string titulo, string mensaje, string tipo, int? idEntidadReferencia = null)
    {
        const string sql = @"
INSERT INTO dbo.Notificaciones (IdUsuario, Titulo, Mensaje, Tipo, IdEntidadReferencia, Leida, FechaCreacion)
VALUES (@IdUsuario, @Titulo, @Mensaje, @Tipo, @IdEntidadReferencia, 0, SYSDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS int);";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdUsuario", idUsuario);
        command.Parameters.AddWithValue("@Titulo", titulo.Trim());
        command.Parameters.AddWithValue("@Mensaje", mensaje.Trim());
        command.Parameters.AddWithValue("@Tipo", string.IsNullOrWhiteSpace(tipo) ? "info" : tipo.Trim());
        command.Parameters.AddWithValue("@IdEntidadReferencia", (object?)idEntidadReferencia ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return result == null ? 0 : Convert.ToInt32(result);
    }

    public async Task<List<NotificacionDTO>> ObtenerPorUsuarioAsync(int idUsuario, int maximo = 30)
    {
        const string sql = @"
SELECT TOP (@Maximo)
    IdNotificacion,
    IdUsuario,
    Titulo,
    Mensaje,
    Leida,
    FechaCreacion,
    Tipo
FROM dbo.Notificaciones
WHERE IdUsuario = @IdUsuario
ORDER BY FechaCreacion DESC, IdNotificacion DESC;";

        var resultado = new List<NotificacionDTO>();
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdUsuario", idUsuario);
        command.Parameters.AddWithValue("@Maximo", maximo <= 0 ? 30 : maximo);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            resultado.Add(new NotificacionDTO
            {
                Id = reader.GetInt32(reader.GetOrdinal("IdNotificacion")),
                UsuarioId = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                Titulo = reader["Titulo"]?.ToString() ?? string.Empty,
                Mensaje = reader["Mensaje"]?.ToString() ?? string.Empty,
                Leida = reader.GetBoolean(reader.GetOrdinal("Leida")),
                FechaEnvio = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                Tipo = reader["Tipo"] == DBNull.Value ? null : reader["Tipo"]?.ToString()
            });
        }

        return resultado;
    }

    public async Task<int> MarcarLeidasAsync(int idUsuario)
    {
        const string sql = @"
UPDATE dbo.Notificaciones
SET Leida = 1
WHERE IdUsuario = @IdUsuario
  AND Leida = 0;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdUsuario", idUsuario);
        return await command.ExecuteNonQueryAsync();
    }
}
