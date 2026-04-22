using Microsoft.Data.SqlClient;
using System.Text;
using PSA.EntidadesDTO.Entidades;
using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;

using PSA.DataAccess;

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
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Auth_RegistrarUsuario", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

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
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Auth_ObtenerUsuarioPorEmail", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

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


        public async Task<Usuario?> ObtenerPorIdAsync(int idUsuario)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Auth_ObtenerUsuarioPorId", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);

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
                UltimoAcceso = reader["UltimoAcceso"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("UltimoAcceso"))
            };
        }


        public async Task<int?> ObtenerIdRolPorNombreAsync(string nombreRol)
        {
            if (string.IsNullOrWhiteSpace(nombreRol))
            {
                return null;
            }

            const string sql = @"
SELECT TOP 1 r.IdRol
FROM dbo.Roles r
WHERE LTRIM(RTRIM(r.Nombre)) = @NombreRol;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@NombreRol", nombreRol.Trim());

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        public async Task<string?> ObtenerNombreRolPorIdAsync(int idRol)
        {
            if (idRol <= 0)
            {
                return null;
            }

            const string sql = @"
SELECT TOP 1 r.Nombre
FROM dbo.Roles r
WHERE r.IdRol = @IdRol;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdRol", idRol);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? null : result.ToString();
        }

        public async Task<bool> ExisteRolAsync(int idRol)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Auth_ExisteRol", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@IdRol", idRol);

            await connection.OpenAsync();
            var resultado = await command.ExecuteScalarAsync();

            return resultado != null;
        }


        public async Task ActualizarPasswordHashPorEmailAsync(string email, string passwordHash)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Auth_ActualizarPasswordHashPorEmail", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@PasswordHash", passwordHash);
            command.Parameters.AddWithValue("@Email", email);

            await connection.OpenAsync();
            var filas = Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);

            if (filas <= 0)
            {
                throw new InvalidOperationException("No fue posible actualizar la contraseña para el correo indicado.");
            }
        }

        public async Task ActualizarUltimoAccesoAsync(int idUsuario, DateTime fechaUltimoAcceso)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Auth_ActualizarUltimoAcceso", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UltimoAcceso", fechaUltimoAcceso);
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }



        public async Task<MiPerfilDTO?> ObtenerMiPerfilAsync(int idUsuario)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Perfil_ObtenerMiPerfil", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@IdUsuario", idUsuario);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new MiPerfilDTO
            {
                IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                NombreCompleto = reader["NombreCompleto"]?.ToString() ?? string.Empty,
                Email = reader["Email"]?.ToString() ?? string.Empty,
                IdRol = reader.GetInt32(reader.GetOrdinal("IdRol")),
                RolNombre = reader["RolNombre"]?.ToString() ?? string.Empty,
                Estado = reader["Estado"]?.ToString() ?? string.Empty,
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                UltimoAcceso = reader["UltimoAcceso"] == DBNull.Value
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("UltimoAcceso"))
            };
        }

        public async Task<bool> ActualizarMiPerfilAsync(int idUsuario, string nombreCompleto, string email)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Perfil_ActualizarMiPerfil", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
            command.Parameters.AddWithValue("@NombreCompleto", nombreCompleto);
            command.Parameters.AddWithValue("@Email", email);

            await connection.OpenAsync();
            var filas = Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
            return filas > 0;
        }

        public async Task AsignarRolAsync(int idUsuario, int idRol)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_Usuarios_AsignarRol", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
            command.Parameters.AddWithValue("@IdRol", idRol);

            await connection.OpenAsync();
            var filas = Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
            if (filas <= 0)
            {
                throw new InvalidOperationException("No se pudo asignar el rol al usuario indicado.");
            }
        }

        public async Task<List<UsuarioAdminListadoDTO>> ObtenerUsuariosAdminAsync(int? idRol = null)
        {
            var sql = new StringBuilder(@"
SELECT
    u.IdUsuario,
    u.NombreCompleto,
    u.Email,
    u.IdRol,
    r.Nombre AS NombreRol,
    u.Estado,
    u.FechaCreacion,
    u.UltimoAcceso,
    (SELECT COUNT(1) FROM dbo.Fincas f WHERE f.IdPropietario = u.IdUsuario) AS CantidadFincas,
    (SELECT COUNT(1)
     FROM dbo.EvaluacionesTecnicas e
     INNER JOIN dbo.Fincas f ON f.IdFinca = e.IdFinca
     WHERE f.IdPropietario = u.IdUsuario
       AND e.EstadoEvaluacion IN ('Pendiente', 'En Proceso')) AS CantidadEvaluacionesActivas
FROM dbo.Usuarios u
INNER JOIN dbo.Roles r ON r.IdRol = u.IdRol");

            if (idRol.HasValue)
            {
                sql.Append(" WHERE u.IdRol = @IdRol");
            }

            sql.Append(" ORDER BY u.Estado, u.NombreCompleto;");

            var resultado = new List<UsuarioAdminListadoDTO>();

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql.ToString(), connection);
            if (idRol.HasValue)
            {
                command.Parameters.AddWithValue("@IdRol", idRol.Value);
            }

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                resultado.Add(new UsuarioAdminListadoDTO
                {
                    IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                    NombreCompleto = reader["NombreCompleto"]?.ToString() ?? string.Empty,
                    Email = reader["Email"]?.ToString() ?? string.Empty,
                    IdRol = reader.GetInt32(reader.GetOrdinal("IdRol")),
                    NombreRol = reader["NombreRol"]?.ToString() ?? string.Empty,
                    Estado = reader["Estado"]?.ToString() ?? string.Empty,
                    FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                    UltimoAcceso = reader["UltimoAcceso"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("UltimoAcceso")),
                    CantidadFincas = reader.GetInt32(reader.GetOrdinal("CantidadFincas")),
                    CantidadEvaluacionesActivas = reader.GetInt32(reader.GetOrdinal("CantidadEvaluacionesActivas"))
                });
            }

            return resultado;
        }

        public async Task<UsuarioAdminEdicionDTO?> ObtenerUsuarioAdminPorIdAsync(int idUsuario)
        {
            const string sql = @"
SELECT
    IdUsuario,
    NombreCompleto,
    Email,
    IdRol,
    Estado
FROM dbo.Usuarios
WHERE IdUsuario = @IdUsuario;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new UsuarioAdminEdicionDTO
            {
                IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                NombreCompleto = reader["NombreCompleto"]?.ToString() ?? string.Empty,
                Email = reader["Email"]?.ToString() ?? string.Empty,
                IdRol = reader.GetInt32(reader.GetOrdinal("IdRol")),
                Estado = reader["Estado"]?.ToString() ?? "Activo"
            };
        }

        public async Task<int> ActualizarUsuarioAdminAsync(UsuarioAdminEdicionDTO dto, string? nuevoPasswordHash = null)
        {
            const string sql = @"
UPDATE dbo.Usuarios
SET
    NombreCompleto = @NombreCompleto,
    Email = @Email,
    IdRol = @IdRol,
    Estado = @Estado,
    PasswordHash = COALESCE(@PasswordHash, PasswordHash)
WHERE IdUsuario = @IdUsuario;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@IdUsuario", dto.IdUsuario);
            command.Parameters.AddWithValue("@NombreCompleto", dto.NombreCompleto.Trim());
            command.Parameters.AddWithValue("@Email", dto.Email.Trim());
            command.Parameters.AddWithValue("@IdRol", dto.IdRol);
            command.Parameters.AddWithValue("@Estado", dto.Estado.Trim());
            command.Parameters.AddWithValue("@PasswordHash", (object?)nuevoPasswordHash ?? DBNull.Value);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> EliminarUsuarioAdminAsync(int idUsuario)
        {
            const string sql = @"
UPDATE dbo.Usuarios
SET Estado = 'Inactivo'
WHERE IdUsuario = @IdUsuario;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> ReasignarClientesAIngenieroAsync(int idPropietario, int idIngenieroDestino)
        {
            const string sql = @"
UPDATE e
SET e.IdIngeniero = @IdIngenieroDestino
FROM dbo.EvaluacionesTecnicas e
INNER JOIN dbo.Fincas f ON f.IdFinca = e.IdFinca
WHERE f.IdPropietario = @IdPropietario;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdPropietario", idPropietario);
            command.Parameters.AddWithValue("@IdIngenieroDestino", idIngenieroDestino);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }
    }
}
