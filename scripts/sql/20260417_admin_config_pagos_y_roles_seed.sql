/*
    Script:
    - Garantiza una sola configuración de pago activa (columna Activa BIT)
    - Crea catálogo mínimo de permisos administrativos
    - Crea roles base si no existen
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* 1) Restricción de una sola configuración activa (según esquema real) */
IF OBJECT_ID('dbo.ConfiguracionesPago', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ConfiguracionesPago', 'Activa') IS NOT NULL
BEGIN
    /* 1.1) Si existen múltiples activas, conservar solo la más reciente */
    ;WITH ConfiguracionesActivas AS
    (
        SELECT
            cp.IdConfiguracionPago,
            ROW_NUMBER() OVER (
                ORDER BY
                    ISNULL(cp.Version, 0) DESC,
                    cp.IdConfiguracionPago DESC
            ) AS Orden
        FROM dbo.ConfiguracionesPago cp
        WHERE cp.Activa = 1
    )
    UPDATE cp
    SET Activa = 0
    FROM dbo.ConfiguracionesPago cp
    INNER JOIN ConfiguracionesActivas ca
        ON ca.IdConfiguracionPago = cp.IdConfiguracionPago
    WHERE ca.Orden > 1;

    /* 1.2) Índice filtrado para impedir más de una activa */
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ConfiguracionesPago')
          AND name = 'UX_ConfiguracionesPago_Activa')
    BEGIN
        CREATE UNIQUE INDEX UX_ConfiguracionesPago_Activa
            ON dbo.ConfiguracionesPago(Activa)
            WHERE Activa = 1;
    END

    /* 1.3) Seed mínimo de configuración de pago si la tabla está vacía */
    IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionesPago)
    BEGIN
        DECLARE @IdUsuarioCreador INT;
        SELECT TOP 1 @IdUsuarioCreador = u.IdUsuario
        FROM dbo.Usuarios u
        ORDER BY u.IdUsuario;

        IF @IdUsuarioCreador IS NOT NULL
        BEGIN
            INSERT INTO dbo.ConfiguracionesPago
            (
                Version,
                NombreVersion,
                PrecioBasePorHectarea,
                TopePorcentajeAjuste,
                FechaVigenciaDesde,
                FechaVigenciaHasta,
                Activa,
                CreadoPor
            )
            VALUES
            (
                1,
                'Configuración Base',
                25000.00,
                30.00,
                CAST(GETDATE() AS date),
                NULL,
                1,
                @IdUsuarioCreador
            );
        END
    END
END
GO

/* 2) Roles base */
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    DECLARE @RolesTieneEstado BIT = CASE WHEN COL_LENGTH('dbo.Roles', 'Estado') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @RolesTieneActivo BIT = CASE WHEN COL_LENGTH('dbo.Roles', 'Activo') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @SqlInsertRol NVARCHAR(MAX);

    /*
      IMPORTANTE:
      Evitamos referencias directas a columnas opcionales (Estado/Activo) porque
      SQL Server valida nombres de columnas en tiempo de compilación del batch.
      Por eso usamos SQL dinámico para los INSERT de Roles.
    */
    IF @RolesTieneEstado = 1
        SET @SqlInsertRol = N'
            INSERT INTO dbo.Roles (Nombre, Descripcion, Estado)
            VALUES (@Nombre, @Descripcion, ''Activo'');';
    ELSE IF @RolesTieneActivo = 1
        SET @SqlInsertRol = N'
            INSERT INTO dbo.Roles (Nombre, Descripcion, Activo)
            VALUES (@Nombre, @Descripcion, 1);';
    ELSE
        SET @SqlInsertRol = N'
            INSERT INTO dbo.Roles (Nombre, Descripcion)
            VALUES (@Nombre, @Descripcion);';

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Administrador')
        EXEC sp_executesql
            @SqlInsertRol,
            N'@Nombre VARCHAR(50), @Descripcion VARCHAR(150)',
            @Nombre = 'Administrador',
            @Descripcion = 'Acceso total al sistema';

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Ingeniero')
        EXEC sp_executesql
            @SqlInsertRol,
            N'@Nombre VARCHAR(50), @Descripcion VARCHAR(150)',
            @Nombre = 'Ingeniero',
            @Descripcion = 'Operación técnica de campo';

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Propietario')
        EXEC sp_executesql
            @SqlInsertRol,
            N'@Nombre VARCHAR(50), @Descripcion VARCHAR(150)',
            @Nombre = 'Propietario',
            @Descripcion = 'Consulta y gestión de sus fincas';
END
GO

/* 3) Permisos administrativos */
IF OBJECT_ID('dbo.Permisos', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ADMIN_USUARIOS_VER')
        INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion) VALUES ('ADMIN_USUARIOS_VER', 'Ver usuarios', 'Consulta el listado administrativo de usuarios');

    IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ADMIN_USUARIOS_CREAR')
        INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion) VALUES ('ADMIN_USUARIOS_CREAR', 'Crear usuarios', 'Permite crear usuarios desde el módulo administrativo');

    IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ADMIN_USUARIOS_EDITAR')
        INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion) VALUES ('ADMIN_USUARIOS_EDITAR', 'Editar usuarios', 'Permite modificar datos, rol y estado de usuarios');

    IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ADMIN_USUARIOS_ELIMINAR')
        INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion) VALUES ('ADMIN_USUARIOS_ELIMINAR', 'Eliminar usuarios', 'Permite inactivar/eliminar usuarios');

    IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ADMIN_CLIENTES_REASIGNAR')
        INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion) VALUES ('ADMIN_CLIENTES_REASIGNAR', 'Reasignar clientes', 'Permite mover clientes a otro asesor');

    IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ADMIN_PAGOS_CONFIGURAR')
        INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion) VALUES ('ADMIN_PAGOS_CONFIGURAR', 'Configurar pagos', 'Permite crear configuraciones de pago vigentes');

    IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ADMIN_CUENTAS_VALIDAR')
        INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion) VALUES ('ADMIN_CUENTAS_VALIDAR', 'Validar cuentas bancarias', 'Permite aprobar o rechazar cuentas bancarias');

    IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ADMIN_AUDITORIA_CONSULTAR')
        INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion) VALUES ('ADMIN_AUDITORIA_CONSULTAR', 'Consultar auditoría', 'Permite revisar eventos y trazabilidad del sistema');
END
GO

/* 4) Asignación inicial por rol */
IF OBJECT_ID('dbo.RolesPermisos', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Permisos', 'U') IS NOT NULL
BEGIN
    DECLARE @Asignaciones TABLE
    (
        NombreRol NVARCHAR(50) NOT NULL,
        CodigoPermiso NVARCHAR(100) NOT NULL
    );

    /* Administrador: todos los permisos */
    INSERT INTO @Asignaciones (NombreRol, CodigoPermiso)
    VALUES
        ('Administrador', 'ADMIN_USUARIOS_VER'),
        ('Administrador', 'ADMIN_USUARIOS_CREAR'),
        ('Administrador', 'ADMIN_USUARIOS_EDITAR'),
        ('Administrador', 'ADMIN_USUARIOS_ELIMINAR'),
        ('Administrador', 'ADMIN_CLIENTES_REASIGNAR'),
        ('Administrador', 'ADMIN_PAGOS_CONFIGURAR'),
        ('Administrador', 'ADMIN_CUENTAS_VALIDAR'),
        ('Administrador', 'ADMIN_AUDITORIA_CONSULTAR');

    /* Ingeniero: lectura de usuarios y consulta de auditoría */
    INSERT INTO @Asignaciones (NombreRol, CodigoPermiso)
    VALUES
        ('Ingeniero', 'ADMIN_USUARIOS_VER'),
        ('Ingeniero', 'ADMIN_AUDITORIA_CONSULTAR');

    /* Propietario: lectura de usuarios (mínimo para poblar pantalla) */
    INSERT INTO @Asignaciones (NombreRol, CodigoPermiso)
    VALUES
        ('Propietario', 'ADMIN_USUARIOS_VER');

    INSERT INTO dbo.RolesPermisos (IdRol, IdPermiso)
    SELECT r.IdRol, p.IdPermiso
    FROM @Asignaciones a
    INNER JOIN dbo.Roles r
        ON r.Nombre = a.NombreRol
    INNER JOIN dbo.Permisos p
        ON p.Codigo = a.CodigoPermiso
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.RolesPermisos rp
        WHERE rp.IdRol = r.IdRol
          AND rp.IdPermiso = p.IdPermiso
    );
END
GO
