/*
    Script:
    - Garantiza una sola configuración de pago activa
    - Crea catálogo mínimo de permisos administrativos
    - Crea roles base si no existen
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* 1) Restricción de una sola configuración activa */
IF COL_LENGTH('dbo.ConfiguracionesPago', 'Estado') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ConfiguracionesPago')
          AND name = 'UX_ConfiguracionesPago_EstadoActiva')
    BEGIN
        CREATE UNIQUE INDEX UX_ConfiguracionesPago_EstadoActiva
            ON dbo.ConfiguracionesPago(Estado)
            WHERE Estado = 'Activa';
    END
END
GO

IF COL_LENGTH('dbo.ConfiguracionesPago', 'Activa') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ConfiguracionesPago')
          AND name = 'UX_ConfiguracionesPago_Activa')
    BEGIN
        CREATE UNIQUE INDEX UX_ConfiguracionesPago_Activa
            ON dbo.ConfiguracionesPago(Activa)
            WHERE Activa = 1;
    END
END
GO

/* 2) Roles base */
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    DECLARE @RolesTieneEstado BIT = CASE WHEN COL_LENGTH('dbo.Roles', 'Estado') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @RolesTieneActivo BIT = CASE WHEN COL_LENGTH('dbo.Roles', 'Activo') IS NOT NULL THEN 1 ELSE 0 END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Administrador')
    BEGIN
        IF @RolesTieneEstado = 1
            INSERT INTO dbo.Roles (Nombre, Descripcion, Estado) VALUES ('Administrador', 'Acceso total al sistema', 'Activo');
        ELSE IF @RolesTieneActivo = 1
            INSERT INTO dbo.Roles (Nombre, Descripcion, Activo) VALUES ('Administrador', 'Acceso total al sistema', 1);
        ELSE
            INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES ('Administrador', 'Acceso total al sistema');
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Ingeniero')
    BEGIN
        IF @RolesTieneEstado = 1
            INSERT INTO dbo.Roles (Nombre, Descripcion, Estado) VALUES ('Ingeniero', 'Operación técnica de campo', 'Activo');
        ELSE IF @RolesTieneActivo = 1
            INSERT INTO dbo.Roles (Nombre, Descripcion, Activo) VALUES ('Ingeniero', 'Operación técnica de campo', 1);
        ELSE
            INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES ('Ingeniero', 'Operación técnica de campo');
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Propietario')
    BEGIN
        IF @RolesTieneEstado = 1
            INSERT INTO dbo.Roles (Nombre, Descripcion, Estado) VALUES ('Propietario', 'Consulta y gestión de sus fincas', 'Activo');
        ELSE IF @RolesTieneActivo = 1
            INSERT INTO dbo.Roles (Nombre, Descripcion, Activo) VALUES ('Propietario', 'Consulta y gestión de sus fincas', 1);
        ELSE
            INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES ('Propietario', 'Consulta y gestión de sus fincas');
    END
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
