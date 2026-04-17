using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;

namespace PSA.DataAccess.DAO
{
    public class RolPermisoDAO
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public RolPermisoDAO(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<RolDTO>> ObtenerRolesAsync()
        {
            const string sql = @"
SELECT IdRol, Nombre, Descripcion
FROM Roles
WHERE Activo = 1
ORDER BY Nombre;";

            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await AsegurarEstructuraAsync(connection);
            using var command = new SqlCommand(sql, connection);
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

        public async Task<List<PermisoDTO>> ObtenerCatalogoPermisosAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await AsegurarEstructuraAsync(connection);
            return await ObtenerCatalogoPermisosInternoAsync(connection);
        }

        public async Task<List<RolPermisoDTO>> ObtenerRolesConPermisosAsync()
        {
            const string sql = @"
SELECT
    r.IdRol,
    r.Nombre,
    r.Descripcion,
    r.Activo,
    p.IdPermiso,
    p.Codigo,
    p.Nombre AS NombrePermiso,
    p.Descripcion AS DescripcionPermiso
FROM Roles r
LEFT JOIN RolPermisos rp ON rp.IdRol = r.IdRol
LEFT JOIN Permisos p ON p.IdPermiso = rp.IdPermiso
ORDER BY r.Nombre, p.Nombre;";

            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await AsegurarEstructuraAsync(connection);
            var catalogoPermisos = await ObtenerCatalogoPermisosInternoAsync(connection);
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            var roles = new Dictionary<int, RolPermisoDTO>();

            while (await reader.ReadAsync())
            {
                var idRol = reader.GetInt32(reader.GetOrdinal("IdRol"));
                if (!roles.TryGetValue(idRol, out var rol))
                {
                    rol = new RolPermisoDTO
                    {
                        IdRol = idRol,
                        NombreRol = reader["Nombre"]?.ToString() ?? string.Empty,
                        DescripcionRol = reader["Descripcion"] == DBNull.Value ? null : reader["Descripcion"]?.ToString(),
                        Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
                        PermisosDisponibles = catalogoPermisos.Select(x => new PermisoDTO
                        {
                            IdPermiso = x.IdPermiso,
                            Codigo = x.Codigo,
                            Nombre = x.Nombre,
                            Descripcion = x.Descripcion
                        }).ToList()
                    };
                    roles[idRol] = rol;
                }

                if (reader["IdPermiso"] != DBNull.Value)
                {
                    var codigo = reader["Codigo"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(codigo) && !rol.CodigosPermisoAsignados.Contains(codigo, StringComparer.OrdinalIgnoreCase))
                    {
                        rol.CodigosPermisoAsignados.Add(codigo);
                    }
                }
            }

            return roles.Values.OrderBy(x => x.NombreRol).ToList();
        }

        public async Task GuardarPermisosRolAsync(int idRol, IEnumerable<string> codigosPermiso)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await AsegurarEstructuraAsync(connection);
            using var transaction = connection.BeginTransaction();

            try
            {
                using (var commandEliminar = new SqlCommand("DELETE FROM RolPermisos WHERE IdRol = @IdRol;", connection, transaction))
                {
                    commandEliminar.Parameters.AddWithValue("@IdRol", idRol);
                    await commandEliminar.ExecuteNonQueryAsync();
                }

                var codigos = codigosPermiso?
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();

                foreach (var codigo in codigos)
                {
                    using var commandInsert = new SqlCommand(@"
INSERT INTO RolPermisos (IdRol, IdPermiso)
SELECT @IdRol, p.IdPermiso
FROM Permisos p
WHERE p.Codigo = @Codigo;", connection, transaction);
                    commandInsert.Parameters.AddWithValue("@IdRol", idRol);
                    commandInsert.Parameters.AddWithValue("@Codigo", codigo);
                    await commandInsert.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static async Task<List<PermisoDTO>> ObtenerCatalogoPermisosInternoAsync(SqlConnection connection)
        {
            const string sql = @"
SELECT IdPermiso, Codigo, Nombre, Descripcion
FROM Permisos
ORDER BY Nombre;";
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            var permisos = new List<PermisoDTO>();
            while (await reader.ReadAsync())
            {
                permisos.Add(new PermisoDTO
                {
                    IdPermiso = reader.GetInt32(reader.GetOrdinal("IdPermiso")),
                    Codigo = reader["Codigo"]?.ToString() ?? string.Empty,
                    Nombre = reader["Nombre"]?.ToString() ?? string.Empty,
                    Descripcion = reader["Descripcion"] == DBNull.Value ? null : reader["Descripcion"]?.ToString()
                });
            }
            return permisos;
        }

        private static async Task AsegurarEstructuraAsync(SqlConnection connection)
        {
            const string sql = @"
IF OBJECT_ID('dbo.Permisos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permisos
    (
        IdPermiso INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Codigo VARCHAR(80) NOT NULL,
        Nombre VARCHAR(120) NOT NULL,
        Descripcion VARCHAR(250) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Permisos_Activo DEFAULT (1),
        CONSTRAINT UQ_Permisos_Codigo UNIQUE (Codigo)
    );
END;

IF OBJECT_ID('dbo.RolPermisos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolPermisos
    (
        IdRolPermiso INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdRol INT NOT NULL,
        IdPermiso INT NOT NULL,
        CONSTRAINT FK_RolPermisos_Roles FOREIGN KEY (IdRol) REFERENCES dbo.Roles(IdRol),
        CONSTRAINT FK_RolPermisos_Permisos FOREIGN KEY (IdPermiso) REFERENCES dbo.Permisos(IdPermiso),
        CONSTRAINT UQ_RolPermisos UNIQUE (IdRol, IdPermiso)
    );
END;

MERGE dbo.Permisos AS target
USING (VALUES
    ('ADMIN_USUARIOS_VER', 'Ver usuarios', 'Consulta el listado administrativo de usuarios'),
    ('ADMIN_USUARIOS_CREAR', 'Crear usuarios', 'Permite crear usuarios desde el módulo administrativo'),
    ('ADMIN_USUARIOS_EDITAR', 'Editar usuarios', 'Permite modificar datos, rol y estado de usuarios'),
    ('ADMIN_USUARIOS_ELIMINAR', 'Eliminar usuarios', 'Permite eliminar usuarios sin dependencias operativas'),
    ('ADMIN_USUARIOS_REASIGNAR', 'Reasignar clientes', 'Permite mover clientes a otro asesor'),
    ('ADMIN_PAGOS_CONFIGURAR', 'Configurar pagos', 'Permite crear configuraciones de pago vigentes'),
    ('ADMIN_CUENTAS_VALIDAR', 'Validar cuentas bancarias', 'Permite aprobar o rechazar cuentas bancarias'),
    ('ADMIN_AUDITORIA_VER', 'Consultar auditoría', 'Permite revisar eventos y trazabilidad del sistema')
) AS source (Codigo, Nombre, Descripcion)
ON target.Codigo = source.Codigo
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Codigo, Nombre, Descripcion, Activo)
    VALUES (source.Codigo, source.Nombre, source.Descripcion, 1);

DECLARE @IdRolAdministrador INT = (SELECT TOP 1 IdRol FROM dbo.Roles WHERE Nombre = 'Administrador');
IF @IdRolAdministrador IS NOT NULL
BEGIN
    INSERT INTO dbo.RolPermisos (IdRol, IdPermiso)
    SELECT @IdRolAdministrador, p.IdPermiso
    FROM dbo.Permisos p
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.RolPermisos rp
        WHERE rp.IdRol = @IdRolAdministrador
          AND rp.IdPermiso = p.IdPermiso
    );
END;";

            using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
