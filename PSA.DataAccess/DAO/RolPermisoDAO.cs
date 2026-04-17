using Microsoft.Data.SqlClient;
using PSA.DataAccess;
using PSA.EntidadesDTO.DTOs.Administracion;

namespace PSA.DataAccess.DAO;

public class RolPermisoDAO
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RolPermisoDAO(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<RolPermisoDTO>> ObtenerRolesConPermisosAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var tieneEstado = await ExisteColumnaEnRolesAsync(connection, "Estado");
        var sql = $@"
SELECT
    r.IdRol,
    r.Nombre AS NombreRol,
    r.Descripcion AS DescripcionRol,
    {(tieneEstado ? "CAST(CASE WHEN r.Estado = 'Activo' THEN 1 ELSE 0 END AS bit)" : "CAST(1 AS bit)")} AS Activo,
    p.Codigo AS CodigoPermisoAsignado,
    p.IdPermiso,
    p.Nombre,
    p.Descripcion
FROM dbo.Roles r
LEFT JOIN dbo.RolesPermisos rp ON rp.IdRol = r.IdRol
LEFT JOIN dbo.Permisos p ON p.IdPermiso = rp.IdPermiso
ORDER BY r.Nombre, p.Codigo;";

        var roles = new Dictionary<int, RolPermisoDTO>();

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var idRol = reader.GetInt32(reader.GetOrdinal("IdRol"));
            if (!roles.TryGetValue(idRol, out var rol))
            {
                rol = new RolPermisoDTO
                {
                    IdRol = idRol,
                    NombreRol = reader["NombreRol"]?.ToString() ?? string.Empty,
                    DescripcionRol = reader["DescripcionRol"] == DBNull.Value ? null : reader["DescripcionRol"]?.ToString(),
                    Activo = reader.GetBoolean(reader.GetOrdinal("Activo"))
                };

                roles[idRol] = rol;
            }

            if (reader["CodigoPermisoAsignado"] != DBNull.Value)
            {
                var codigo = reader["CodigoPermisoAsignado"]?.ToString();
                if (!string.IsNullOrWhiteSpace(codigo))
                {
                    rol.CodigosPermisoAsignados.Add(codigo);
                }
            }

            if (reader["IdPermiso"] != DBNull.Value)
            {
                rol.PermisosDisponibles.Add(new PermisoDTO
                {
                    IdPermiso = reader.GetInt32(reader.GetOrdinal("IdPermiso")),
                    Codigo = reader["CodigoPermisoAsignado"]?.ToString() ?? string.Empty,
                    Nombre = reader["Nombre"]?.ToString() ?? string.Empty,
                    Descripcion = reader["Descripcion"] == DBNull.Value ? null : reader["Descripcion"]?.ToString()
                });
            }
        }

        foreach (var rol in roles.Values)
        {
            rol.CodigosPermisoAsignados = rol.CodigosPermisoAsignados
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            rol.PermisosDisponibles = rol.PermisosDisponibles
                .GroupBy(x => x.IdPermiso)
                .Select(g => g.First())
                .OrderBy(x => x.Codigo)
                .ToList();
        }

        return roles.Values.OrderBy(x => x.NombreRol).ToList();
    }

    public async Task GuardarPermisosRolAsync(GuardarPermisosRolDTO dto)
    {
        if (dto.IdRol <= 0)
        {
            throw new ArgumentException("Debe indicar un rol válido.", nameof(dto.IdRol));
        }

        const string sqlDelete = "DELETE FROM dbo.RolesPermisos WHERE IdRol = @IdRol;";
        const string sqlInsert = @"
INSERT INTO dbo.RolesPermisos (IdRol, IdPermiso)
SELECT @IdRol, p.IdPermiso
FROM dbo.Permisos p
WHERE p.Codigo = @CodigoPermiso;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var tx = connection.BeginTransaction();

        try
        {
            using (var deleteCommand = new SqlCommand(sqlDelete, connection, tx))
            {
                deleteCommand.Parameters.AddWithValue("@IdRol", dto.IdRol);
                await deleteCommand.ExecuteNonQueryAsync();
            }

            foreach (var codigoPermiso in dto.CodigosPermiso.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                using var insertCommand = new SqlCommand(sqlInsert, connection, tx);
                insertCommand.Parameters.AddWithValue("@IdRol", dto.IdRol);
                insertCommand.Parameters.AddWithValue("@CodigoPermiso", codigoPermiso);
                await insertCommand.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<PSA.EntidadesDTO.DTOs.Usuarios.RolDTO>> ObtenerRolesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var tieneEstado = await ExisteColumnaEnRolesAsync(connection, "Estado");
        var sql = $@"
SELECT IdRol, Nombre, Descripcion
FROM dbo.Roles
{(tieneEstado ? "WHERE Estado = 'Activo'" : string.Empty)}
ORDER BY Nombre;";

        var roles = new List<PSA.EntidadesDTO.DTOs.Usuarios.RolDTO>();

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            roles.Add(new PSA.EntidadesDTO.DTOs.Usuarios.RolDTO
            {
                Id = reader.GetInt32(reader.GetOrdinal("IdRol")),
                Nombre = reader["Nombre"]?.ToString() ?? string.Empty,
                Descripcion = reader["Descripcion"] == DBNull.Value ? null : reader["Descripcion"]?.ToString()
            });
        }

        return roles;
    }

    private static async Task<bool> ExisteColumnaEnRolesAsync(SqlConnection connection, string nombreColumna)
    {
        const string sql = @"
SELECT COUNT(1)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'Roles'
  AND COLUMN_NAME = @NombreColumna;";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@NombreColumna", nombreColumna);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0) > 0;
    }

    public async Task<List<PSA.EntidadesDTO.DTOs.Usuarios.RolDTO>> ObtenerRolesAsync()
    {
        const string sql = @"
SELECT IdRol, Nombre, Descripcion
FROM dbo.Roles
WHERE Estado = 'Activo'
ORDER BY Nombre;";

        var roles = new List<PSA.EntidadesDTO.DTOs.Usuarios.RolDTO>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand(sql, connection);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            roles.Add(new PSA.EntidadesDTO.DTOs.Usuarios.RolDTO
            {
                Id = reader.GetInt32(reader.GetOrdinal("IdRol")),
                Nombre = reader["Nombre"]?.ToString() ?? string.Empty,
                Descripcion = reader["Descripcion"] == DBNull.Value ? null : reader["Descripcion"]?.ToString()
            });
        }

        return roles;
    }
}
