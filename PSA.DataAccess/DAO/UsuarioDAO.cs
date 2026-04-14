using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.Entidades;
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
    }
}
