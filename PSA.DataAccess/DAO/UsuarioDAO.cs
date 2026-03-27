using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.Entidades;

namespace PSA.DataAccess.DAO
{
    public class UsuarioDAO
    {
        private readonly string _connectionString;

        public UsuarioDAO(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> CrearUsuarioAsync(Usuario usuario)
        {
            const string sql = @"
INSERT INTO Usuarios
(
    NombreCompleto,
    Email,
    PasswordHash,
    IdRol,
    Estado,
    FechaCreacion,
    UltimoAcceso
)
VALUES
(
    @NombreCompleto,
    @Email,
    @PasswordHash,
    @IdRol,
    @Estado,
    @FechaCreacion,
    @UltimoAcceso
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@NombreCompleto", usuario.NombreCompleto);
            command.Parameters.AddWithValue("@Email", usuario.Email);
            command.Parameters.AddWithValue("@PasswordHash", (object?)usuario.PasswordHash ?? DBNull.Value);
            command.Parameters.AddWithValue("@IdRol", usuario.IdRol);
            command.Parameters.AddWithValue("@Estado", usuario.Estado);
            command.Parameters.AddWithValue("@FechaCreacion", usuario.FechaCreacion);
            command.Parameters.AddWithValue("@UltimoAcceso", (object?)usuario.UltimoAcceso ?? DBNull.Value);

            await connection.OpenAsync();
            var resultado = await command.ExecuteScalarAsync();

            return resultado != null ? Convert.ToInt32(resultado) : 0;
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            const string sql = @"
SELECT TOP 1
    IdUsuario,
    NombreCompleto,
    Email,
    PasswordHash,
    IdRol,
    Estado,
    FechaCreacion,
    UltimoAcceso
FROM Usuarios
WHERE Email = @Email;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Email", email);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new Usuario
            {
                IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                NombreCompleto = reader["NombreCompleto"]?.ToString() ?? string.Empty,
                Email = reader["Email"]?.ToString() ?? string.Empty,
                PasswordHash = reader["PasswordHash"] == DBNull.Value ? null : reader["PasswordHash"]?.ToString(),
                IdRol = reader.GetInt32(reader.GetOrdinal("IdRol")),
                Estado = reader["Estado"]?.ToString() ?? string.Empty,
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                UltimoAcceso = reader["UltimoAcceso"] == DBNull.Value
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("UltimoAcceso"))
            };
        }

        public async Task<bool> ExisteRolAsync(int idRol)
        {
            const string sql = @"
SELECT 1
FROM Roles
WHERE IdRol = @IdRol;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdRol", idRol);

            await connection.OpenAsync();
            var resultado = await command.ExecuteScalarAsync();

            return resultado != null;
        }
    }
}
