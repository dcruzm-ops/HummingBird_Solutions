using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.Entidades;

using PSA.DataAccess;

namespace PSA.DataAccess.DAO
{
    public class TokenRecuperacionDAO
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TokenRecuperacionDAO(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InvalidarTokensActivosPorUsuarioAsync(int idUsuario)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Auth_InvalidarTokensActivos", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> CrearTokenAsync(int idUsuario, string token, DateTime fechaExpiracionUtc)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Auth_CrearTokenRecuperacion", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
            command.Parameters.AddWithValue("@Token", token);
            command.Parameters.AddWithValue("@FechaExpiracion", fechaExpiracionUtc);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<TokenRecuperacion?> ObtenerTokenVigenteAsync(string token)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Auth_ObtenerTokenVigente", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Token", token);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new TokenRecuperacion
            {
                IdToken = reader.GetInt32(reader.GetOrdinal("IdToken")),
                IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                Token = reader["Token"]?.ToString() ?? string.Empty,
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                FechaExpiracion = reader.GetDateTime(reader.GetOrdinal("FechaExpiracion")),
                Usado = reader.GetBoolean(reader.GetOrdinal("Usado")),
                FechaUso = reader["FechaUso"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("FechaUso"))
            };
        }

        public async Task MarcarTokenComoUsadoAsync(int idToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Auth_MarcarTokenComoUsado", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@IdToken", idToken);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}
