using Microsoft.Data.SqlClient;
    using System.Security.Cryptography;

    namespace PSA.DataAccess.DAO
    {
        public class RecuperacionContrasenaDAO
        {
            private readonly string _connectionString;

            public RecuperacionContrasenaDAO(string connectionString)
            {
                _connectionString = connectionString;
            }

            public (int IdUsuario, string NombreCompleto, string Email)? ObtenerUsuarioActivoPorCorreo(string correo)
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var sql = @"
SELECT IdUsuario, NombreCompleto, Email
FROM Usuarios
WHERE Email = @Email
  AND Estado = 'Activo';";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Email", correo);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return (
                        Convert.ToInt32(reader["IdUsuario"]),
                        reader["NombreCompleto"]?.ToString() ?? string.Empty,
                        reader["Email"]?.ToString() ?? string.Empty
                    );
                }

                return null;
            }

            public void InvalidarTokensAnteriores(int idUsuario)
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var sql = @"
UPDATE TokensRecuperacion
SET Usado = 1,
    FechaUso = SYSDATETIME()
WHERE IdUsuario = @IdUsuario
  AND Usado = 0;";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                cmd.ExecuteNonQuery();
            }

            public void GuardarToken(int idUsuario, string token, DateTime fechaExpiracion)
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var sql = @"
INSERT INTO TokensRecuperacion
(IdUsuario, Token, FechaCreacion, FechaExpiracion, Usado)
VALUES
(@IdUsuario, @Token, SYSDATETIME(), @FechaExpiracion, 0);";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                cmd.Parameters.AddWithValue("@Token", token);
                cmd.Parameters.AddWithValue("@FechaExpiracion", fechaExpiracion);
                cmd.ExecuteNonQuery();
            }

            public bool TokenEsValido(string token)
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var sql = @"
SELECT COUNT(1)
FROM TokensRecuperacion
WHERE Token = @Token
  AND Usado = 0
  AND FechaExpiracion > SYSDATETIME();";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Token", token);

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }

            public int? ObtenerIdUsuarioPorToken(string token)
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var sql = @"
SELECT TOP 1 IdUsuario
FROM TokensRecuperacion
WHERE Token = @Token
  AND Usado = 0
  AND FechaExpiracion > SYSDATETIME();";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Token", token);

                var result = cmd.ExecuteScalar();
                return result == null ? null : Convert.ToInt32(result);
            }

            public void ActualizarPassword(int idUsuario, string passwordHash)
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var sql = @"
UPDATE Usuarios
SET PasswordHash = @PasswordHash
WHERE IdUsuario = @IdUsuario;";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                cmd.ExecuteNonQuery();
            }

            public void MarcarTokenComoUsado(string token)
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var sql = @"
UPDATE TokensRecuperacion
SET Usado = 1,
    FechaUso = SYSDATETIME()
WHERE Token = @Token;";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Token", token);
                cmd.ExecuteNonQuery();
            }

            public static string GenerarTokenSeguro()
            {
                var bytes = RandomNumberGenerator.GetBytes(32);
                return Convert.ToBase64String(bytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", string.Empty);
            }
        }
    }
