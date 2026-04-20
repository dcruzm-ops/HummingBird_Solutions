/*
Segunda pasada de corrección orientada a cobertura funcional auditada (PSA Costa Rica)
- Seguridad JWT + permisos funcionales
- Estados de cuota: habilita Atrasada en BD
- Estado inicial finca: PendienteEvaluacion
- No retroactividad: unicidad finca+año en planes
*/

SET NOCOUNT ON;
GO

/* 1) Estados de cuota */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CuotasPago_Estado' AND parent_object_id = OBJECT_ID('dbo.CuotasPago'))
BEGIN
    ALTER TABLE dbo.CuotasPago DROP CONSTRAINT CK_CuotasPago_Estado;
END
GO

ALTER TABLE dbo.CuotasPago
ADD CONSTRAINT CK_CuotasPago_Estado
CHECK (EstadoCuota IN ('Pendiente', 'Ejecutada', 'Notificada', 'Atrasada'));
GO

/* 2) Estado de finca alineado al proceso */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Fincas_Estado' AND parent_object_id = OBJECT_ID('dbo.Fincas'))
BEGIN
    ALTER TABLE dbo.Fincas DROP CONSTRAINT CK_Fincas_Estado;
END
GO

ALTER TABLE dbo.Fincas
ADD CONSTRAINT CK_Fincas_Estado
CHECK (EstadoFinca IN ('Registrada', 'Pendiente', 'PendienteEvaluacion', 'EnRevision', 'En proceso', 'Aprobada', 'Rechazada', 'Suspendida', 'Inactiva'));
GO

UPDATE dbo.Fincas
SET EstadoFinca = 'PendienteEvaluacion'
WHERE EstadoFinca = 'Registrada';
GO

/* 3) No retroactividad de plan: una finca por año */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_PlanesPago_IdFinca_Anio'
      AND object_id = OBJECT_ID('dbo.PlanesPago'))
BEGIN
    CREATE UNIQUE INDEX UX_PlanesPago_IdFinca_Anio
        ON dbo.PlanesPago (IdFinca, Anio);
END
GO

/* 4) Permisos funcionales para enforcement runtime */
IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ADMIN_REPORTES_CONSULTAR')
    INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion) VALUES ('ADMIN_REPORTES_CONSULTAR', 'Consultar reportes administrativos', 'Permite consultar reportes de administración');
IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'ING_PLAN_APROBAR')
    INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion) VALUES ('ING_PLAN_APROBAR', 'Aprobar plan técnico', 'Permite aprobación final de planes por ingeniero');
IF NOT EXISTS (SELECT 1 FROM dbo.Permisos WHERE Codigo = 'DUENO_FINCAS_RENOVAR')
    INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion) VALUES ('DUENO_FINCAS_RENOVAR', 'Renovar finca', 'Permite solicitar renovación anual de finca');
GO

/* Asignaciones base por rol */
DECLARE @IdRolAdmin INT = (SELECT TOP 1 IdRol FROM dbo.Roles WHERE Nombre = 'Administrador');
DECLARE @IdRolIng INT = (SELECT TOP 1 IdRol FROM dbo.Roles WHERE Nombre = 'Ingeniero');
DECLARE @IdRolDueno INT = (SELECT TOP 1 IdRol FROM dbo.Roles WHERE Nombre = 'Propietario');
DECLARE @TablaRolPermisos SYSNAME = CASE
    WHEN OBJECT_ID('dbo.RolesPermisos', 'U') IS NOT NULL THEN 'dbo.RolesPermisos'
    WHEN OBJECT_ID('dbo.RolPermisos', 'U') IS NOT NULL THEN 'dbo.RolPermisos'
    ELSE NULL
END;

IF @IdRolAdmin IS NOT NULL AND @TablaRolPermisos IS NOT NULL
BEGIN
    DECLARE @SqlAdmin NVARCHAR(MAX) = N'
    INSERT INTO ' + @TablaRolPermisos + N' (IdRol, IdPermiso)
    SELECT @IdRol, p.IdPermiso
    FROM dbo.Permisos p
    WHERE p.Codigo = @Codigo
      AND NOT EXISTS (SELECT 1 FROM ' + @TablaRolPermisos + N' rp WHERE rp.IdRol = @IdRol AND rp.IdPermiso = p.IdPermiso);';
    EXEC sp_executesql @SqlAdmin, N'@IdRol INT, @Codigo NVARCHAR(100)', @IdRolAdmin, N'ADMIN_REPORTES_CONSULTAR';
END

IF @IdRolIng IS NOT NULL AND @TablaRolPermisos IS NOT NULL
BEGIN
    DECLARE @SqlIng NVARCHAR(MAX) = N'
    INSERT INTO ' + @TablaRolPermisos + N' (IdRol, IdPermiso)
    SELECT @IdRol, p.IdPermiso
    FROM dbo.Permisos p
    WHERE p.Codigo = @Codigo
      AND NOT EXISTS (SELECT 1 FROM ' + @TablaRolPermisos + N' rp WHERE rp.IdRol = @IdRol AND rp.IdPermiso = p.IdPermiso);';
    EXEC sp_executesql @SqlIng, N'@IdRol INT, @Codigo NVARCHAR(100)', @IdRolIng, N'ING_PLAN_APROBAR';
END

IF @IdRolDueno IS NOT NULL AND @TablaRolPermisos IS NOT NULL
BEGIN
    DECLARE @SqlDueno NVARCHAR(MAX) = N'
    INSERT INTO ' + @TablaRolPermisos + N' (IdRol, IdPermiso)
    SELECT @IdRol, p.IdPermiso
    FROM dbo.Permisos p
    WHERE p.Codigo = @Codigo
      AND NOT EXISTS (SELECT 1 FROM ' + @TablaRolPermisos + N' rp WHERE rp.IdRol = @IdRol AND rp.IdPermiso = p.IdPermiso);';
    EXEC sp_executesql @SqlDueno, N'@IdRol INT, @Codigo NVARCHAR(100)', @IdRolDueno, N'DUENO_FINCAS_RENOVAR';
END
GO
