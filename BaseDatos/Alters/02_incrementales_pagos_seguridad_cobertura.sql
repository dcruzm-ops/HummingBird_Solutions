/* Consolidado de scripts incrementales de pagos, seguridad y cobertura */

/* ==== Fuente: scripts/sql/20260418_pagos_motor_calculo.sql ==== */
USE PSA_CostaRica;
GO

IF OBJECT_ID(N'dbo.PlanesPagoDetalleCalculo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlanesPagoDetalleCalculo
    (
        IdDetalleCalculo            INT IDENTITY(1,1) NOT NULL,
        IdPlanPago                  INT NOT NULL,
        HectareasAprobadas          DECIMAL(12,2) NOT NULL,
        PrecioBasePorHectarea       DECIMAL(10,2) NOT NULL,
        PorcentajeVegetacion        DECIMAL(5,2) NOT NULL,
        PorcentajeHidrico           DECIMAL(5,2) NOT NULL,
        PorcentajeNacientes         DECIMAL(5,2) NOT NULL,
        PorcentajePendiente         DECIMAL(5,2) NOT NULL,
        PorcentajeTotalAntesTope    DECIMAL(5,2) NOT NULL,
        PorcentajeTopeAplicado      DECIMAL(5,2) NOT NULL,
        PorcentajeTotalAplicado     DECIMAL(5,2) NOT NULL,
        MontoBaseMensual            DECIMAL(10,2) NOT NULL,
        MontoAjusteMensual          DECIMAL(10,2) NOT NULL,
        MontoFinalMensual           DECIMAL(10,2) NOT NULL,
        VegetacionFinal             VARCHAR(100) NOT NULL,
        TieneRecursosHidricosFinal  BIT NOT NULL,
        CantidadNacientesFinal      INT NOT NULL,
        PendienteFinal              VARCHAR(50) NOT NULL,
        FechaCalculo                DATETIME2 NOT NULL CONSTRAINT DF_PlanesPagoDetalleCalculo_FechaCalculo DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_PlanesPagoDetalleCalculo PRIMARY KEY (IdDetalleCalculo),
        CONSTRAINT FK_PlanesPagoDetalleCalculo_PlanesPago FOREIGN KEY (IdPlanPago) REFERENCES dbo.PlanesPago(IdPlanPago),
        CONSTRAINT UQ_PlanesPagoDetalleCalculo_IdPlanPago UNIQUE (IdPlanPago)
    );
END;
GO

PRINT 'Ejecute también BaseDatos/04_creacion_stored_procedures.sql (SP de pagos), BaseDatos/05_creacion_triggers.sql (autogeneración) y scripts/sql/20260418_pagos_plan_sin_cuenta.sql (plan sin cuenta inicial).';

/* ==== Fuente: scripts/sql/20260418_pagos_estados_capas.sql ==== */
USE PSA_CostaRica;
GO

ALTER TABLE dbo.PlanesPago
ALTER COLUMN EstadoPlan VARCHAR(40) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PlanesPago_Estado' AND parent_object_id = OBJECT_ID('dbo.PlanesPago'))
BEGIN
    ALTER TABLE dbo.PlanesPago DROP CONSTRAINT CK_PlanesPago_Estado;
END
GO

ALTER TABLE dbo.PlanesPago
ADD CONSTRAINT CK_PlanesPago_Estado
CHECK (EstadoPlan IN ('BorradorGenerado', 'PendienteDatosBancarios', 'PendienteAprobacionFinal', 'Activo', 'Finalizado', 'Cancelado'));
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CuotasPago_Estado' AND parent_object_id = OBJECT_ID('dbo.CuotasPago'))
BEGIN
    ALTER TABLE dbo.CuotasPago DROP CONSTRAINT CK_CuotasPago_Estado;
END
GO

ALTER TABLE dbo.CuotasPago
ADD CONSTRAINT CK_CuotasPago_Estado
CHECK (EstadoCuota IN ('Pendiente', 'Ejecutada', 'Notificada'));
GO

UPDATE dbo.PlanesPago
SET EstadoPlan = CASE EstadoPlan
    WHEN 'Suspendido' THEN 'PendienteAprobacionFinal'
    ELSE EstadoPlan
END
WHERE EstadoPlan IN ('Suspendido');
GO

UPDATE dbo.CuotasPago
SET EstadoCuota = CASE EstadoCuota
    WHEN 'Programada' THEN 'Pendiente'
    WHEN 'Pagada' THEN 'Ejecutada'
    ELSE EstadoCuota
END
WHERE EstadoCuota IN ('Programada', 'Pagada');
GO

PRINT 'Estados de pago migrados a flujo por capas.';

/* ==== Fuente: scripts/sql/20260418_pagos_plan_sin_cuenta.sql ==== */
USE PSA_CostaRica;
GO

IF COL_LENGTH('dbo.PlanesPago', 'IdCuentaBancaria') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.objects o ON o.object_id = c.object_id
        WHERE o.name = 'PlanesPago'
          AND o.schema_id = SCHEMA_ID('dbo')
          AND c.name = 'IdCuentaBancaria'
          AND c.is_nullable = 0
    )
    BEGIN
        ALTER TABLE dbo.PlanesPago
        ALTER COLUMN IdCuentaBancaria INT NULL;
    END
END;
GO

PRINT 'Ejecute también BaseDatos/04_creacion_stored_procedures.sql y BaseDatos/05_creacion_triggers.sql para alinear SP/Triggers al modelo de plan sin cuenta inicial.';

/* ==== Fuente: scripts/sql/20260420_segunda_pasada_cobertura.sql ==== */
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

/* ==== Fuente: scripts/sql/20260421_seguridad_y_configuracion_pagos.sql ==== */
/*
Objetivo:
1) Blindar ConfiguracionesPago para no permitir TopePorcentajeAjuste > 40.
2) Garantizar un único registro activo cuando exista columna Activa.
*/

IF COL_LENGTH('dbo.ConfiguracionesPago', 'TopePorcentajeAjuste') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ConfiguracionesPago_TopePorcentajeAjuste_Max40'
)
BEGIN
    ALTER TABLE dbo.ConfiguracionesPago
    ADD CONSTRAINT CK_ConfiguracionesPago_TopePorcentajeAjuste_Max40
        CHECK (TopePorcentajeAjuste >= 0 AND TopePorcentajeAjuste <= 40);
END;
GO

IF COL_LENGTH('dbo.ConfiguracionesPago', 'Activa') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ConfiguracionesPago_ActivaUnica'
      AND object_id = OBJECT_ID('dbo.ConfiguracionesPago')
)
BEGIN
    CREATE UNIQUE INDEX UX_ConfiguracionesPago_ActivaUnica
    ON dbo.ConfiguracionesPago (Activa)
    WHERE Activa = 1;
END;
GO

/* ==== Fuente: scripts/sql/20260422_evaluaciones_snapshot_original_y_ajustes_hidricos.sql ==== */
/* Snapshot original y ajustes hidricos específicos en evaluación técnica */

IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'OriginalHectareas') IS NULL
    ALTER TABLE dbo.EvaluacionesTecnicas ADD OriginalHectareas DECIMAL(10,2) NULL;
GO
IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'OriginalVegetacion') IS NULL
    ALTER TABLE dbo.EvaluacionesTecnicas ADD OriginalVegetacion VARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'OriginalUsoSuelo') IS NULL
    ALTER TABLE dbo.EvaluacionesTecnicas ADD OriginalUsoSuelo VARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'OriginalPendiente') IS NULL
    ALTER TABLE dbo.EvaluacionesTecnicas ADD OriginalPendiente VARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'OriginalTieneRiosOQuebradas') IS NULL
    ALTER TABLE dbo.EvaluacionesTecnicas ADD OriginalTieneRiosOQuebradas BIT NULL;
GO
IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'OriginalCantidadNacientes') IS NULL
    ALTER TABLE dbo.EvaluacionesTecnicas ADD OriginalCantidadNacientes INT NULL;
GO
IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'TieneRiosOQuebradasAjustado') IS NULL
    ALTER TABLE dbo.EvaluacionesTecnicas ADD TieneRiosOQuebradasAjustado BIT NULL;
GO
IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'CantidadNacientesAjustada') IS NULL
    ALTER TABLE dbo.EvaluacionesTecnicas ADD CantidadNacientesAjustada INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Evaluaciones_CantidadNacientesAjustada_NN')
    ALTER TABLE dbo.EvaluacionesTecnicas ADD CONSTRAINT CK_Evaluaciones_CantidadNacientesAjustada_NN CHECK (CantidadNacientesAjustada IS NULL OR CantidadNacientesAjustada >= 0);
GO

/* ==== Fuente: scripts/sql/20260422_planpago_snapshot_originales.sql ==== */
/* Snapshot de valores originales usados en cálculo de plan de pago */

IF COL_LENGTH('dbo.PlanesPagoDetalleCalculo', 'HectareasOriginales') IS NULL
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD HectareasOriginales DECIMAL(10,2) NOT NULL CONSTRAINT DF_PlanDet_HectareasOriginales DEFAULT(0);
GO
IF COL_LENGTH('dbo.PlanesPagoDetalleCalculo', 'VegetacionOriginal') IS NULL
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD VegetacionOriginal VARCHAR(100) NOT NULL CONSTRAINT DF_PlanDet_VegetacionOriginal DEFAULT('');
GO
IF COL_LENGTH('dbo.PlanesPagoDetalleCalculo', 'TieneRiosOQuebradasOriginal') IS NULL
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD TieneRiosOQuebradasOriginal BIT NOT NULL CONSTRAINT DF_PlanDet_RiosOriginal DEFAULT(0);
GO
IF COL_LENGTH('dbo.PlanesPagoDetalleCalculo', 'CantidadNacientesOriginal') IS NULL
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD CantidadNacientesOriginal INT NOT NULL CONSTRAINT DF_PlanDet_NacientesOriginal DEFAULT(0);
GO
IF COL_LENGTH('dbo.PlanesPagoDetalleCalculo', 'PendienteOriginal') IS NULL
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD PendienteOriginal VARCHAR(50) NOT NULL CONSTRAINT DF_PlanDet_PendienteOriginal DEFAULT('');
GO

/* ==== Fuente: scripts/sql/20260422_pagos_desglose_hidrico_y_trazabilidad.sql ==== */
/*
  Ajustes de soporte para pagos PSA:
  - Separar ríos/quebradas y nacientes como datos finales aprobados por ingeniería.
  - Extender snapshot de cálculo para desglose auditable y efecto del tope.
*/

IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'TieneRiosOQuebradasAjustado') IS NULL
BEGIN
    ALTER TABLE dbo.EvaluacionesTecnicas
    ADD TieneRiosOQuebradasAjustado BIT NULL;
END;
GO

IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'CantidadNacientesAjustada') IS NULL
BEGIN
    ALTER TABLE dbo.EvaluacionesTecnicas
    ADD CantidadNacientesAjustada INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Evaluaciones_CantidadNacientesAjustada'
)
BEGIN
    ALTER TABLE dbo.EvaluacionesTecnicas
    ADD CONSTRAINT CK_Evaluaciones_CantidadNacientesAjustada
    CHECK (CantidadNacientesAjustada IS NULL OR CantidadNacientesAjustada >= 0);
END;
GO

IF COL_LENGTH('dbo.PlanesPagoDetalleCalculo', 'MontoAjusteVegetacion') IS NULL
BEGIN
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD MontoAjusteVegetacion DECIMAL(12,2) NOT NULL CONSTRAINT DF_PlanDet_MontoAjusteVegetacion DEFAULT(0);
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD PorcentajeRiosQuebradas DECIMAL(5,2) NOT NULL CONSTRAINT DF_PlanDet_PorcentajeRiosQuebradas DEFAULT(0);
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD MontoAjusteRiosQuebradas DECIMAL(12,2) NOT NULL CONSTRAINT DF_PlanDet_MontoAjusteRiosQuebradas DEFAULT(0);
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD MontoAjusteNacientes DECIMAL(12,2) NOT NULL CONSTRAINT DF_PlanDet_MontoAjusteNacientes DEFAULT(0);
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD MontoAjustePendiente DECIMAL(12,2) NOT NULL CONSTRAINT DF_PlanDet_MontoAjustePendiente DEFAULT(0);
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD PorcentajeRecortadoPorTope DECIMAL(5,2) NOT NULL CONSTRAINT DF_PlanDet_PorcRecortado DEFAULT(0);
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD MontoAjusteBrutoMensual DECIMAL(12,2) NOT NULL CONSTRAINT DF_PlanDet_MontoAjusteBruto DEFAULT(0);
    ALTER TABLE dbo.PlanesPagoDetalleCalculo ADD MontoRecortadoPorTope DECIMAL(12,2) NOT NULL CONSTRAINT DF_PlanDet_MontoRecortado DEFAULT(0);
END;
GO

