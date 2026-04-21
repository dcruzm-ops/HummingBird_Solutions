/*
    Normalización de roles y permisos para login/autorización.
    Fecha: 2026-04-21

    Objetivo:
    1) Garantizar que existan los permisos requeridos por políticas del API.
    2) Corregir asignaciones iniciales erróneas donde roles no admin recibían permisos ADMIN_*.
    3) Mantener compatibilidad con ambos nombres de rol de ingeniería:
       - Ingeniero
       - Ingeniero Forestal
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.Permisos', 'U') IS NULL
BEGIN
    PRINT 'No existe dbo.Permisos. Se omite normalización.';
    RETURN;
END
GO

IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    PRINT 'No existe dbo.Roles. Se omite normalización.';
    RETURN;
END
GO

DECLARE @TablaRolPermisos SYSNAME = CASE
    WHEN OBJECT_ID('dbo.RolesPermisos', 'U') IS NOT NULL THEN 'dbo.RolesPermisos'
    WHEN OBJECT_ID('dbo.RolPermisos', 'U') IS NOT NULL THEN 'dbo.RolPermisos'
    ELSE NULL
END;

IF @TablaRolPermisos IS NULL
BEGIN
    PRINT 'No existe tabla de relación RolesPermisos/RolPermisos. Se omite normalización.';
    RETURN;
END
GO

/* 1) Permisos faltantes para políticas activas */
IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ADMIN_REPORTES_CONSULTAR')
    INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('ADMIN_REPORTES_CONSULTAR', 'Consultar reportes administrativos', 'Permite consultar reportes globales del sistema', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ING_PLAN_APROBAR')
    INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('ING_PLAN_APROBAR', 'Aprobar plan de pago', 'Permite al ingeniero aprobar o rechazar planes de pago', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'DUENO_FINCAS_RENOVAR')
    INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion, Activo)
    VALUES ('DUENO_FINCAS_RENOVAR', 'Renovar fincas', 'Permite al propietario renovar solicitudes de finca', 1);
GO

/* 2) Quitar permisos ADMIN_* de roles no administrativos */
DECLARE @SqlCleanup NVARCHAR(MAX) = N'
DELETE rp
FROM ' + @TablaRolPermisos + N' rp
INNER JOIN dbo.Roles r ON r.IdRol = rp.IdRol
INNER JOIN dbo.Permisos p ON p.IdPermiso = rp.IdPermiso
WHERE p.Codigo LIKE ''ADMIN[_]%''
  AND r.Nombre <> ''Administrador'';';

EXEC sp_executesql @SqlCleanup;
GO

/* 3) Asignaciones esperadas por rol */
IF OBJECT_ID('tempdb..#AsignacionesEsperadas') IS NOT NULL
    DROP TABLE #AsignacionesEsperadas;

CREATE TABLE #AsignacionesEsperadas
(
    NombreRol NVARCHAR(100) NOT NULL,
    CodigoPermiso NVARCHAR(100) NOT NULL
);

INSERT INTO #AsignacionesEsperadas (NombreRol, CodigoPermiso)
VALUES
    ('Administrador', 'ADMIN_USUARIOS_VER'),
    ('Administrador', 'ADMIN_USUARIOS_CREAR'),
    ('Administrador', 'ADMIN_USUARIOS_EDITAR'),
    ('Administrador', 'ADMIN_USUARIOS_ELIMINAR'),
    ('Administrador', 'ADMIN_CLIENTES_REASIGNAR'),
    ('Administrador', 'ADMIN_PAGOS_CONFIGURAR'),
    ('Administrador', 'ADMIN_CUENTAS_VALIDAR'),
    ('Administrador', 'ADMIN_AUDITORIA_CONSULTAR'),
    ('Administrador', 'ADMIN_REPORTES_CONSULTAR'),
    ('Ingeniero', 'ING_PLAN_APROBAR'),
    ('Ingeniero Forestal', 'ING_PLAN_APROBAR'),
    ('Propietario', 'DUENO_FINCAS_RENOVAR');

DECLARE @SqlInsert NVARCHAR(MAX) = N'
INSERT INTO ' + @TablaRolPermisos + N' (IdRol, IdPermiso)
SELECT r.IdRol, p.IdPermiso
FROM #AsignacionesEsperadas e
INNER JOIN dbo.Roles r ON r.Nombre = e.NombreRol
INNER JOIN dbo.Permisos p ON p.Codigo = e.CodigoPermiso
WHERE NOT EXISTS (
    SELECT 1
    FROM ' + @TablaRolPermisos + N' rp
    WHERE rp.IdRol = r.IdRol
      AND rp.IdPermiso = p.IdPermiso
);';

EXEC sp_executesql @SqlInsert;

DROP TABLE #AsignacionesEsperadas;
GO
