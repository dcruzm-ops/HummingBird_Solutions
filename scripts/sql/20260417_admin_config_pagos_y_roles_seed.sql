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
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Administrador')
        INSERT INTO dbo.Roles (Nombre, Descripcion, Estado)
        VALUES ('Administrador', 'Acceso total al sistema', 'Activo');

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Ingeniero')
        INSERT INTO dbo.Roles (Nombre, Descripcion, Estado)
        VALUES ('Ingeniero', 'Operación técnica de campo', 'Activo');

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Propietario')
        INSERT INTO dbo.Roles (Nombre, Descripcion, Estado)
        VALUES ('Propietario', 'Consulta y gestión de sus fincas', 'Activo');
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

/* 4) Asignación inicial para Administrador */
IF OBJECT_ID('dbo.RolesPermisos', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Permisos', 'U') IS NOT NULL
BEGIN
    DECLARE @IdRolAdmin int;
    SELECT TOP 1 @IdRolAdmin = IdRol FROM dbo.Roles WHERE Nombre = 'Administrador';

    IF @IdRolAdmin IS NOT NULL
    BEGIN
        INSERT INTO dbo.RolesPermisos (IdRol, IdPermiso)
        SELECT @IdRolAdmin, p.IdPermiso
        FROM dbo.Permisos p
        WHERE p.Codigo IN (
            'ADMIN_USUARIOS_VER',
            'ADMIN_USUARIOS_CREAR',
            'ADMIN_USUARIOS_EDITAR',
            'ADMIN_USUARIOS_ELIMINAR',
            'ADMIN_CLIENTES_REASIGNAR',
            'ADMIN_PAGOS_CONFIGURAR',
            'ADMIN_CUENTAS_VALIDAR',
            'ADMIN_AUDITORIA_CONSULTAR'
        )
        AND NOT EXISTS (
            SELECT 1 FROM dbo.RolesPermisos rp
            WHERE rp.IdRol = @IdRolAdmin
              AND rp.IdPermiso = p.IdPermiso
        );
    END
END
GO
