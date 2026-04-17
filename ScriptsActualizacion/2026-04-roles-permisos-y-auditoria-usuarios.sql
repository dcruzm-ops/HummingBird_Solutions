/*
    Script de actualización puntual - Roles, permisos y auditoría de usuarios
    Fecha: 2026-04-17
    Objetivo:
    - Introducir catálogo de permisos y relación RolPermisos.
    - Sembrar permisos administrativos base.
    - Asignar permisos al rol Administrador.
    - Asegurar trigger de auditoría de Usuarios con CREATE OR ALTER.
*/

USE PSA_CostaRica;
GO

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
GO

IF OBJECT_ID('dbo.RolPermisos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolPermisos
    (
        IdRolPermiso INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdRol INT NOT NULL,
        IdPermiso INT NOT NULL,
        CONSTRAINT FK_RolPermisos_Roles
            FOREIGN KEY (IdRol) REFERENCES dbo.Roles(IdRol),
        CONSTRAINT FK_RolPermisos_Permisos
            FOREIGN KEY (IdPermiso) REFERENCES dbo.Permisos(IdPermiso),
        CONSTRAINT UQ_RolPermisos UNIQUE (IdRol, IdPermiso)
    );
END;
GO

MERGE dbo.Permisos AS target
USING (VALUES
    ('ADMIN_USUARIOS_VER',       'Ver usuarios',             'Consulta el listado administrativo de usuarios'),
    ('ADMIN_USUARIOS_CREAR',     'Crear usuarios',           'Permite crear usuarios desde el módulo administrativo'),
    ('ADMIN_USUARIOS_EDITAR',    'Editar usuarios',          'Permite modificar datos, rol y estado de usuarios'),
    ('ADMIN_USUARIOS_ELIMINAR',  'Eliminar usuarios',        'Permite eliminar usuarios sin dependencias operativas'),
    ('ADMIN_USUARIOS_REASIGNAR', 'Reasignar clientes',       'Permite mover clientes a otro asesor'),
    ('ADMIN_PAGOS_CONFIGURAR',   'Configurar pagos',         'Permite crear configuraciones de pago vigentes'),
    ('ADMIN_CUENTAS_VALIDAR',    'Validar cuentas bancarias','Permite aprobar o rechazar cuentas bancarias'),
    ('ADMIN_AUDITORIA_VER',      'Consultar auditoría',      'Permite revisar eventos y trazabilidad del sistema')
) AS source (Codigo, Nombre, Descripcion)
ON target.Codigo = source.Codigo
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Codigo, Nombre, Descripcion, Activo)
    VALUES (source.Codigo, source.Nombre, source.Descripcion, 1);
GO

DECLARE @IdRolAdministrador INT =
(
    SELECT TOP 1 IdRol
    FROM dbo.Roles
    WHERE Nombre = 'Administrador'
);

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
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_Usuarios_Auditoria
ON dbo.Usuarios
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaLog
    (
        IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado,
        Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle
    )
    SELECT i.IdUsuario, 'Usuarios', 'Usuarios', i.IdUsuario, 'INSERT', NULL,
           (SELECT i.IdUsuario, i.NombreCompleto, i.Email, i.IdRol, i.Estado, i.FechaCreacion, i.UltimoAcceso
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Usuario creado: ', i.Email)
    FROM inserted i
    LEFT JOIN deleted d ON d.IdUsuario = i.IdUsuario
    WHERE d.IdUsuario IS NULL;

    INSERT INTO dbo.AuditoriaLog
    (
        IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado,
        Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle
    )
    SELECT i.IdUsuario, 'Usuarios', 'Usuarios', i.IdUsuario, 'UPDATE',
           (SELECT d.IdUsuario, d.NombreCompleto, d.Email, d.IdRol, d.Estado, d.FechaCreacion, d.UltimoAcceso
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           (SELECT i.IdUsuario, i.NombreCompleto, i.Email, i.IdRol, i.Estado, i.FechaCreacion, i.UltimoAcceso
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Usuario actualizado: ', i.Email)
    FROM inserted i
    INNER JOIN deleted d ON d.IdUsuario = i.IdUsuario;

    INSERT INTO dbo.AuditoriaLog
    (
        IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado,
        Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle
    )
    SELECT d.IdUsuario, 'Usuarios', 'Usuarios', d.IdUsuario, 'DELETE',
           (SELECT d.IdUsuario, d.NombreCompleto, d.Email, d.IdRol, d.Estado, d.FechaCreacion, d.UltimoAcceso
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           NULL, SYSDATETIME(), CONCAT('Usuario eliminado: ', d.Email)
    FROM deleted d
    LEFT JOIN inserted i ON i.IdUsuario = d.IdUsuario
    WHERE i.IdUsuario IS NULL;
END;
GO
