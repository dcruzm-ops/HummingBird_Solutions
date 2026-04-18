using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Administracion;
using RolDTO = PSA.EntidadesDTO.DTOs.Usuarios.RolDTO;

namespace PSA.DataAccess.DAO;

public class RolPermisoDAO(IDbConnectionFactory connectionFactory)
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<List<RolPermisoDTO>> ObtenerRolesConPermisosAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var tablaRolPermisos = await ObtenerTablaRolPermisosAsync(connection);
        var metadataEstado = await ObtenerMetadataColumnaAsync(connection, "Roles", "Estado");
        var metadataActivo = await ObtenerMetadataColumnaAsync(connection, "Roles", "Activo");
        var expresionActivo = ObtenerExpresionActivo("r", metadataEstado, metadataActivo);
        var sql = $@"
SELECT
    r.IdRol,
    r.Nombre AS NombreRol,
    r.Descripcion AS DescripcionRol,
    {expresionActivo} AS Activo,
    p.Codigo AS CodigoPermisoAsignado,
    p.IdPermiso,
    p.Nombre,
    p.Descripcion
FROM dbo.Roles r
LEFT JOIN {tablaRolPermisos} rp ON rp.IdRol = r.IdRol
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
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.IdRol <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.IdRol), dto.IdRol, "Debe indicar un rol válido.");
        }

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        var tablaRolPermisos = await ObtenerTablaRolPermisosAsync(connection);
        var sqlDelete = $"DELETE FROM {tablaRolPermisos} WHERE IdRol = @IdRol;";
        var sqlInsert = $@"
INSERT INTO {tablaRolPermisos} (IdRol, IdPermiso)
SELECT @IdRol, p.IdPermiso
FROM dbo.Permisos p
WHERE p.Codigo = @CodigoPermiso;";

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

    public async Task<List<PermisoDTO>> ObtenerPermisosAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var permisos = new List<PermisoDTO>();

        if (await ExisteStoredProcedureAsync(connection, "usp_Admin_ObtenerPermisos"))
        {
            using var command = new SqlCommand("dbo.usp_Admin_ObtenerPermisos", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                permisos.Add(MapPermiso(reader));
            }

            return permisos;
        }

        const string sql = @"
SELECT p.IdPermiso, p.Codigo, p.Nombre, p.Descripcion
FROM dbo.Permisos p
ORDER BY p.Codigo;";

        using var fallbackCommand = new SqlCommand(sql, connection);
        using var fallbackReader = await fallbackCommand.ExecuteReaderAsync();
        while (await fallbackReader.ReadAsync())
        {
            permisos.Add(MapPermiso(fallbackReader));
        }

        return permisos;
    }

    public async Task<List<RolDTO>> ObtenerRolesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var metadataEstado = await ObtenerMetadataColumnaAsync(connection, "Roles", "Estado");
        var metadataActivo = await ObtenerMetadataColumnaAsync(connection, "Roles", "Activo");
        var expresionActivo = ObtenerExpresionActivo("dbo.Roles", metadataEstado, metadataActivo);
        var sql = $@"
	SELECT IdRol, Nombre, Descripcion
	FROM dbo.Roles
	WHERE {expresionActivo} = CAST(1 AS bit)
	ORDER BY Nombre;";

        var roles = new List<RolDTO>();

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

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

    public async Task<int> CrearRolAsync(CrearRolDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new ArgumentException("El nombre del rol es obligatorio.", nameof(dto.Nombre));
        }

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var metadataEstado = await ObtenerMetadataColumnaAsync(connection, "Roles", "Estado");
        var metadataActivo = await ObtenerMetadataColumnaAsync(connection, "Roles", "Activo");

        var columnas = new List<string> { "Nombre", "Descripcion" };
        var valores = new List<string> { "@Nombre", "@Descripcion" };

        var usaEstado = metadataEstado.Exists;
        var usaActivo = !usaEstado && metadataActivo.Exists;
        if (usaEstado)
        {
            columnas.Add("Estado");
            valores.Add("@Estado");
        }
        else if (usaActivo)
        {
            columnas.Add("Activo");
            valores.Add("@Activo");
        }

        var sql = $@"
INSERT INTO dbo.Roles ({string.Join(", ", columnas)})
VALUES ({string.Join(", ", valores)});
SELECT CAST(SCOPE_IDENTITY() AS int);";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Nombre", dto.Nombre.Trim());
        command.Parameters.AddWithValue("@Descripcion", (object?)dto.Descripcion?.Trim() ?? DBNull.Value);
        if (usaEstado)
        {
            command.Parameters.AddWithValue("@Estado", ConvertirEstadoParametro(metadataEstado, dto.Activo));
        }
        else if (usaActivo)
        {
            command.Parameters.AddWithValue("@Activo", dto.Activo);
        }

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0);
    }

    private static async Task<(bool Exists, string? SqlType)> ObtenerMetadataColumnaAsync(
        SqlConnection connection,
        string nombreTabla,
        string nombreColumna)
    {
        const string sql = @"
SELECT TOP 1 c.DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_SCHEMA = 'dbo'
  AND c.TABLE_NAME = @NombreTabla
  AND c.COLUMN_NAME = @NombreColumna;";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@NombreTabla", nombreTabla);
        command.Parameters.AddWithValue("@NombreColumna", nombreColumna);

        var result = await command.ExecuteScalarAsync();
        if (result is null || result is DBNull)
        {
            return (false, null);
        }

        return (true, result.ToString());
    }

    private static async Task<bool> ExisteStoredProcedureAsync(SqlConnection connection, string nombreProcedimiento)
    {
        const string sql = @"
SELECT COUNT(1)
FROM sys.procedures
WHERE schema_id = SCHEMA_ID('dbo')
  AND name = @NombreProcedimiento;";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@NombreProcedimiento", nombreProcedimiento);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0) > 0;
    }

    private static PermisoDTO MapPermiso(SqlDataReader reader)
    {
        return new PermisoDTO
        {
            IdPermiso = reader.GetInt32(reader.GetOrdinal("IdPermiso")),
            Codigo = reader["Codigo"]?.ToString() ?? string.Empty,
            Nombre = reader["Nombre"]?.ToString() ?? string.Empty,
            Descripcion = reader["Descripcion"] == DBNull.Value ? null : reader["Descripcion"]?.ToString()
        };
    }

    private static string ObtenerExpresionActivo(
        string aliasTabla,
        (bool Exists, string? SqlType) metadataEstado,
        (bool Exists, string? SqlType) metadataActivo)
    {
        if (metadataEstado.Exists)
        {
            if (EsTipoBooleano(metadataEstado.SqlType))
            {
                return $"CAST(ISNULL({aliasTabla}.Estado, 0) AS bit)";
            }

            return $"CAST(CASE WHEN {aliasTabla}.Estado = 'Activo' THEN 1 ELSE 0 END AS bit)";
        }

        if (metadataActivo.Exists)
        {
            return $"CAST(ISNULL({aliasTabla}.Activo, 1) AS bit)";
        }

        return "CAST(1 AS bit)";
    }

    private static object ConvertirEstadoParametro((bool Exists, string? SqlType) metadataEstado, bool activo)
        => EsTipoBooleano(metadataEstado.SqlType)
            ? activo
            : activo ? "Activo" : "Inactivo";

    private static bool EsTipoBooleano(string? sqlType)
        => string.Equals(sqlType, "bit", StringComparison.OrdinalIgnoreCase);

    private static async Task<string> ObtenerTablaRolPermisosAsync(SqlConnection connection)
    {
        if (await ExisteTablaAsync(connection, "RolesPermisos"))
        {
            return "dbo.RolesPermisos";
        }

        if (await ExisteTablaAsync(connection, "RolPermisos"))
        {
            return "dbo.RolPermisos";
        }

        throw new InvalidOperationException("No se encontró la tabla de relación de roles/permisos (RolesPermisos o RolPermisos).");
    }

    private static async Task<bool> ExisteTablaAsync(SqlConnection connection, string nombreTabla)
    {
        const string sql = @"
SELECT COUNT(1)
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = 'dbo'
  AND t.name = @NombreTabla;";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@NombreTabla", nombreTabla);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0) > 0;
    }
}
