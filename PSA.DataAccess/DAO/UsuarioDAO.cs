using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;
using PSA.EntidadesDTO.Entidades;

namespace PSA.DataAccess.DAO
{
    public class UsuarioDAO
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UsuarioDAO(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
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

            using var connection = _connectionFactory.CreateConnection();
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
SELECT TOP 1 IdUsuario, NombreCompleto, Email, PasswordHash, IdRol, Estado, FechaCreacion, UltimoAcceso
FROM Usuarios
WHERE Email = @Email;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Email", email);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return MapearUsuario(reader);
        }

        public async Task<Usuario?> ObtenerPorIdAsync(int idUsuario)
        {
            const string sql = @"
SELECT TOP 1 IdUsuario, NombreCompleto, Email, PasswordHash, IdRol, Estado, FechaCreacion, UltimoAcceso
FROM Usuarios
WHERE IdUsuario = @IdUsuario;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return MapearUsuario(reader);
        }

        public async Task<bool> ExisteRolAsync(int idRol)
        {
            const string sql = @"
SELECT 1 FROM Roles WHERE IdRol = @IdRol AND Activo = 1;";
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdRol", idRol);
            await connection.OpenAsync();
            var resultado = await command.ExecuteScalarAsync();
            return resultado != null;
        }

        public async Task ActualizarPasswordHashPorEmailAsync(string email, string passwordHash)
        {
            const string sql = @"
UPDATE Usuarios
SET PasswordHash = @PasswordHash
WHERE Email = @Email;";
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@PasswordHash", passwordHash);
            command.Parameters.AddWithValue("@Email", email);
            await connection.OpenAsync();
            var filas = await command.ExecuteNonQueryAsync();
            if (filas <= 0) throw new InvalidOperationException("No fue posible actualizar la contraseña para el correo indicado.");
        }

        public async Task ActualizarUltimoAccesoAsync(int idUsuario, DateTime fechaAcceso)
        {
            const string sql = @"
UPDATE Usuarios
SET UltimoAcceso = @UltimoAcceso
WHERE IdUsuario = @IdUsuario;";
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UltimoAcceso", fechaAcceso);
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
            await connection.OpenAsync();
            var filas = await command.ExecuteNonQueryAsync();
            if (filas <= 0) throw new Exception("No se pudo actualizar el último acceso del usuario.");
        }

        public async Task<List<UsuarioAdminListadoDTO>> ObtenerUsuariosAdministracionAsync(int? idRol = null)
        {
            const string sql = @"
SELECT
    u.IdUsuario,
    u.NombreCompleto,
    u.Email,
    u.IdRol,
    r.Nombre AS NombreRol,
    u.Estado,
    u.FechaCreacion,
    u.UltimoAcceso,
    (SELECT COUNT(1) FROM Fincas f WHERE f.IdPropietario = u.IdUsuario) AS CantidadFincas,
    (SELECT COUNT(1) FROM EvaluacionesTecnicas e WHERE e.IdIngeniero = u.IdUsuario AND e.EstadoEvaluacion IN ('Pendiente', 'En Proceso', 'En proceso')) AS CantidadEvaluacionesActivas
FROM Usuarios u
INNER JOIN Roles r ON r.IdRol = u.IdRol
WHERE (@IdRol IS NULL OR u.IdRol = @IdRol)
ORDER BY u.NombreCompleto;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdRol", (object?)idRol ?? DBNull.Value);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            var usuarios = new List<UsuarioAdminListadoDTO>();
            while (await reader.ReadAsync())
            {
                usuarios.Add(new UsuarioAdminListadoDTO
                {
                    IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                    NombreCompleto = reader["NombreCompleto"]?.ToString() ?? string.Empty,
                    Email = reader["Email"]?.ToString() ?? string.Empty,
                    IdRol = reader.GetInt32(reader.GetOrdinal("IdRol")),
                    NombreRol = reader["NombreRol"]?.ToString() ?? string.Empty,
                    Estado = reader["Estado"]?.ToString() ?? string.Empty,
                    FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                    UltimoAcceso = reader["UltimoAcceso"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("UltimoAcceso")),
                    CantidadFincas = Convert.ToInt32(reader["CantidadFincas"]),
                    CantidadEvaluacionesActivas = Convert.ToInt32(reader["CantidadEvaluacionesActivas"])
                });
            }
            return usuarios;
        }

        public async Task<UsuarioAdminEdicionDTO?> ObtenerUsuarioEdicionAsync(int idUsuario)
        {
            const string sql = @"
SELECT TOP 1 IdUsuario, NombreCompleto, Email, IdRol, Estado
FROM Usuarios
WHERE IdUsuario = @IdUsuario;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return new UsuarioAdminEdicionDTO
            {
                IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                NombreCompleto = reader["NombreCompleto"]?.ToString() ?? string.Empty,
                Email = reader["Email"]?.ToString() ?? string.Empty,
                IdRol = reader.GetInt32(reader.GetOrdinal("IdRol")),
                Estado = reader["Estado"]?.ToString() ?? "Activo"
            };
        }

        public async Task<bool> ActualizarUsuarioAsync(Usuario usuario)
        {
            const string sql = @"
UPDATE Usuarios
SET NombreCompleto = @NombreCompleto,
    Email = @Email,
    IdRol = @IdRol,
    Estado = @Estado,
    PasswordHash = CASE WHEN @PasswordHash IS NULL OR LTRIM(RTRIM(@PasswordHash)) = '' THEN PasswordHash ELSE @PasswordHash END
WHERE IdUsuario = @IdUsuario;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);
            command.Parameters.AddWithValue("@NombreCompleto", usuario.NombreCompleto);
            command.Parameters.AddWithValue("@Email", usuario.Email);
            command.Parameters.AddWithValue("@IdRol", usuario.IdRol);
            command.Parameters.AddWithValue("@Estado", usuario.Estado);
            command.Parameters.AddWithValue("@PasswordHash", (object?)usuario.PasswordHash ?? DBNull.Value);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> EliminarUsuarioAsync(int idUsuario)
        {
            const string sql = @"DELETE FROM Usuarios WHERE IdUsuario = @IdUsuario;";
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> TieneDependenciasAsync(int idUsuario)
        {
            const string sql = @"
SELECT CASE WHEN EXISTS (SELECT 1 FROM Fincas WHERE IdPropietario = @IdUsuario)
          OR EXISTS (SELECT 1 FROM EvaluacionesTecnicas WHERE IdIngeniero = @IdUsuario)
          OR EXISTS (SELECT 1 FROM CuentasBancarias WHERE IdUsuario = @IdUsuario OR ValidadoPor = @IdUsuario)
          OR EXISTS (SELECT 1 FROM AuditoriaLog WHERE IdUsuario = @IdUsuario)
          OR EXISTS (SELECT 1 FROM TokensRecuperacion WHERE IdUsuario = @IdUsuario)
     THEN 1 ELSE 0 END;";
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
            await connection.OpenAsync();
            var resultado = await command.ExecuteScalarAsync();
            return Convert.ToInt32(resultado ?? 0) == 1;
        }

        public async Task<List<RolDTO>> ObtenerRolesAsync()
        {
            const string sql = @"SELECT IdRol, Nombre, Descripcion FROM Roles WHERE Activo = 1 ORDER BY Nombre;";
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            var roles = new List<RolDTO>();
            while (await reader.ReadAsync())
            {
                roles.Add(new RolDTO
                {
                    Id = reader.GetInt32(reader.GetOrdinal("IdRol")),
                    Nombre = reader["Nombre"]?.ToString() ?? string.Empty,
                    Descripcion = reader["Descripcion"] == DBNull.Value ? null : reader["Descripcion"]?.ToString()
                });
            }
            return roles;
        }

        public async Task<int> ReasignarPropietarioAIngenieroAsync(int idPropietario, int idIngenieroDestino)
        {
            const string sql = @"
UPDATE et
SET et.IdIngeniero = @IdIngenieroDestino
FROM EvaluacionesTecnicas et
INNER JOIN Fincas f ON f.IdFinca = et.IdFinca
WHERE f.IdPropietario = @IdPropietario
  AND et.EstadoEvaluacion IN ('Pendiente', 'En Proceso', 'En proceso');";
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdPropietario", idPropietario);
            command.Parameters.AddWithValue("@IdIngenieroDestino", idIngenieroDestino);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }

        private static Usuario MapearUsuario(SqlDataReader reader)
        {
            return new Usuario
            {
                IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                NombreCompleto = reader["NombreCompleto"]?.ToString() ?? string.Empty,
                Email = reader["Email"]?.ToString() ?? string.Empty,
                PasswordHash = reader["PasswordHash"] == DBNull.Value ? null : reader["PasswordHash"]?.ToString(),
                IdRol = reader.GetInt32(reader.GetOrdinal("IdRol")),
                Estado = reader["Estado"]?.ToString() ?? string.Empty,
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                UltimoAcceso = reader["UltimoAcceso"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("UltimoAcceso"))
            };
        }
    }
}
