USE PSA_CostaRica;
GO

/*
    Triggers base de auditoría para registrar cambios de INSERT/UPDATE/DELETE
    en entidades críticas visibles para el dashboard administrativo.
*/

CREATE OR ALTER TRIGGER dbo.TR_Usuarios_Auditoria
ON dbo.Usuarios
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, FechaAccion, Detalle)
    SELECT
        COALESCE(i.IdUsuario, d.IdUsuario),
        'Usuarios',
        'Usuarios',
        COALESCE(i.IdUsuario, d.IdUsuario),
        CASE
            WHEN i.IdUsuario IS NOT NULL AND d.IdUsuario IS NULL THEN 'INSERT'
            WHEN i.IdUsuario IS NOT NULL AND d.IdUsuario IS NOT NULL THEN 'UPDATE'
            ELSE 'DELETE'
        END,
        SYSDATETIME(),
        CONCAT('Cambio en usuario: ', COALESCE(i.Email, d.Email, 'sin email'))
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.IdUsuario = d.IdUsuario;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_Fincas_Auditoria
ON dbo.Fincas
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, FechaAccion, Detalle)
    SELECT
        COALESCE(i.IdPropietario, d.IdPropietario),
        'Fincas',
        'Fincas',
        COALESCE(i.IdFinca, d.IdFinca),
        CASE
            WHEN i.IdFinca IS NOT NULL AND d.IdFinca IS NULL THEN 'INSERT'
            WHEN i.IdFinca IS NOT NULL AND d.IdFinca IS NOT NULL THEN 'UPDATE'
            ELSE 'DELETE'
        END,
        SYSDATETIME(),
        CONCAT('Cambio en finca: ', COALESCE(i.NombreFinca, d.NombreFinca, 'sin nombre'))
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.IdFinca = d.IdFinca;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_EvaluacionesTecnicas_Auditoria
ON dbo.EvaluacionesTecnicas
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, FechaAccion, Detalle)
    SELECT
        COALESCE(i.IdIngeniero, d.IdIngeniero),
        'Evaluaciones',
        'EvaluacionesTecnicas',
        COALESCE(i.IdEvaluacion, d.IdEvaluacion),
        CASE
            WHEN i.IdEvaluacion IS NOT NULL AND d.IdEvaluacion IS NULL THEN 'INSERT'
            WHEN i.IdEvaluacion IS NOT NULL AND d.IdEvaluacion IS NOT NULL THEN 'UPDATE'
            ELSE 'DELETE'
        END,
        SYSDATETIME(),
        CONCAT('Evaluación técnica estado: ', COALESCE(i.EstadoEvaluacion, d.EstadoEvaluacion, 's/d'))
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.IdEvaluacion = d.IdEvaluacion;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_PlanesPago_Auditoria
ON dbo.PlanesPago
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, FechaAccion, Detalle)
    SELECT
        NULL,
        'Pagos',
        'PlanesPago',
        COALESCE(i.IdPlanPago, d.IdPlanPago),
        CASE
            WHEN i.IdPlanPago IS NOT NULL AND d.IdPlanPago IS NULL THEN 'INSERT'
            WHEN i.IdPlanPago IS NOT NULL AND d.IdPlanPago IS NOT NULL THEN 'UPDATE'
            ELSE 'DELETE'
        END,
        SYSDATETIME(),
        CONCAT('Plan de pago año: ', COALESCE(CONVERT(varchar(10), i.Anio), CONVERT(varchar(10), d.Anio), 's/d'))
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.IdPlanPago = d.IdPlanPago;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_CuentasBancarias_Auditoria
ON dbo.CuentasBancarias
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, FechaAccion, Detalle)
    SELECT
        COALESCE(i.IdUsuario, d.IdUsuario),
        'CuentasBancarias',
        'CuentasBancarias',
        COALESCE(i.IdCuentaBancaria, d.IdCuentaBancaria),
        CASE
            WHEN i.IdCuentaBancaria IS NOT NULL AND d.IdCuentaBancaria IS NULL THEN 'INSERT'
            WHEN i.IdCuentaBancaria IS NOT NULL AND d.IdCuentaBancaria IS NOT NULL THEN 'UPDATE'
            ELSE 'DELETE'
        END,
        SYSDATETIME(),
        CONCAT('Estado validación: ', COALESCE(i.EstadoValidacion, d.EstadoValidacion, 's/d'))
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.IdCuentaBancaria = d.IdCuentaBancaria;
END;
GO
