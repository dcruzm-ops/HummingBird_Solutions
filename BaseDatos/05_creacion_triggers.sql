USE PSA_CostaRica;
GO

/*
  Triggers base de auditoría:
  - INSERT: ValorAnterior = NULL, ValorNuevo = fila insertada (JSON)
  - UPDATE: ValorAnterior = fila anterior (JSON), ValorNuevo = fila nueva (JSON)
  - DELETE: ValorAnterior = fila eliminada (JSON), ValorNuevo = NULL
*/

CREATE OR ALTER TRIGGER dbo.TR_Usuarios_Auditoria
ON dbo.Usuarios
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT i.IdUsuario, 'Usuarios', 'Usuarios', i.IdUsuario, 'INSERT', NULL,
           (SELECT i.IdUsuario, i.NombreCompleto, i.Email, i.IdRol, i.Estado, i.FechaCreacion, i.UltimoAcceso FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Usuario creado: ', i.Email)
    FROM inserted i
    LEFT JOIN deleted d ON d.IdUsuario = i.IdUsuario
    WHERE d.IdUsuario IS NULL;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT i.IdUsuario, 'Usuarios', 'Usuarios', i.IdUsuario, 'UPDATE',
           (SELECT d.IdUsuario, d.NombreCompleto, d.Email, d.IdRol, d.Estado, d.FechaCreacion, d.UltimoAcceso FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           (SELECT i.IdUsuario, i.NombreCompleto, i.Email, i.IdRol, i.Estado, i.FechaCreacion, i.UltimoAcceso FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Usuario actualizado: ', i.Email)
    FROM inserted i
    INNER JOIN deleted d ON d.IdUsuario = i.IdUsuario;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT d.IdUsuario, 'Usuarios', 'Usuarios', d.IdUsuario, 'DELETE',
           (SELECT d.IdUsuario, d.NombreCompleto, d.Email, d.IdRol, d.Estado, d.FechaCreacion, d.UltimoAcceso FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           NULL, SYSDATETIME(), CONCAT('Usuario eliminado: ', d.Email)
    FROM deleted d
    LEFT JOIN inserted i ON i.IdUsuario = d.IdUsuario
    WHERE i.IdUsuario IS NULL;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_EvaluacionesTecnicas_GenerarPlanPago
ON dbo.EvaluacionesTecnicas
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdFinca INT;
    DECLARE @Anio INT = YEAR(SYSDATETIME()) + 1;

    DECLARE cursorFincas CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT i.IdFinca
        FROM inserted i
        INNER JOIN deleted d ON d.IdEvaluacion = i.IdEvaluacion
        INNER JOIN dbo.Fincas f ON f.IdFinca = i.IdFinca
        WHERE i.EstadoEvaluacion = 'Evaluada – Califica'
          AND i.DecisionTecnica = 'Califica'
          AND (d.EstadoEvaluacion <> i.EstadoEvaluacion OR ISNULL(d.DecisionTecnica, '') <> ISNULL(i.DecisionTecnica, ''))
          AND EXISTS (
              SELECT 1
              FROM dbo.ConfiguracionesPago cp
              WHERE cp.FechaVigenciaDesde <= DATEFROMPARTS(YEAR(SYSDATETIME()) + 1, 1, 1)
                AND (cp.FechaVigenciaHasta IS NULL OR cp.FechaVigenciaHasta >= DATEFROMPARTS(YEAR(SYSDATETIME()) + 1, 1, 1))
          );

    OPEN cursorFincas;
    FETCH NEXT FROM cursorFincas INTO @IdFinca;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            IF OBJECT_ID('dbo.SP_Pagos_GenerarPlanPago', 'P') IS NOT NULL
            BEGIN
                EXEC sys.sp_executesql
                    N'EXEC dbo.SP_Pagos_GenerarPlanPago @IdFinca = @IdFinca, @Anio = @Anio, @Simular = 0;',
                    N'@IdFinca INT, @Anio INT',
                    @IdFinca = @IdFinca,
                    @Anio = @Anio;
            END
            ELSE
            BEGIN
                PRINT CONCAT('TR_EvaluacionesTecnicas_GenerarPlanPago omitido para finca ', @IdFinca, ': SP_Pagos_GenerarPlanPago no existe.');
            END
        END TRY
        BEGIN CATCH
            -- Si falta cuenta validada o configuración activa, no se cae la transacción principal.
            PRINT CONCAT('TR_EvaluacionesTecnicas_GenerarPlanPago omitido para finca ', @IdFinca, ': ', ERROR_MESSAGE());
        END CATCH;

        FETCH NEXT FROM cursorFincas INTO @IdFinca;
    END

    CLOSE cursorFincas;
    DEALLOCATE cursorFincas;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_CuentasBancarias_GenerarPlanPagoPendiente
ON dbo.CuentasBancarias
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdFinca INT;
    DECLARE @Anio INT = YEAR(SYSDATETIME()) + 1;

    DECLARE cursorFincas CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT f.IdFinca
        FROM inserted i
        INNER JOIN deleted d ON d.IdCuentaBancaria = i.IdCuentaBancaria
        INNER JOIN dbo.Fincas f ON f.IdPropietario = i.IdUsuario
        WHERE i.EstadoValidacion = 'Validada'
          AND ISNULL(d.EstadoValidacion, '') <> 'Validada'
          AND f.EstadoFinca = 'Aprobada'
          AND EXISTS (
              SELECT 1
              FROM dbo.EvaluacionesTecnicas e
              WHERE e.IdFinca = f.IdFinca
                AND e.EstadoEvaluacion = 'Evaluada – Califica'
                AND e.DecisionTecnica = 'Califica'
          )
          AND EXISTS (
              SELECT 1
              FROM dbo.ConfiguracionesPago cp
              WHERE cp.FechaVigenciaDesde <= DATEFROMPARTS(YEAR(SYSDATETIME()) + 1, 1, 1)
                AND (cp.FechaVigenciaHasta IS NULL OR cp.FechaVigenciaHasta >= DATEFROMPARTS(YEAR(SYSDATETIME()) + 1, 1, 1))
          );

    OPEN cursorFincas;
    FETCH NEXT FROM cursorFincas INTO @IdFinca;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            IF OBJECT_ID('dbo.SP_Pagos_GenerarPlanPago', 'P') IS NOT NULL
            BEGIN
                EXEC sys.sp_executesql
                    N'EXEC dbo.SP_Pagos_GenerarPlanPago @IdFinca = @IdFinca, @Anio = @Anio, @Simular = 0;',
                    N'@IdFinca INT, @Anio INT',
                    @IdFinca = @IdFinca,
                    @Anio = @Anio;
            END
            ELSE
            BEGIN
                PRINT CONCAT('TR_CuentasBancarias_GenerarPlanPagoPendiente omitido para finca ', @IdFinca, ': SP_Pagos_GenerarPlanPago no existe.');
            END
        END TRY
        BEGIN CATCH
            PRINT CONCAT('TR_CuentasBancarias_GenerarPlanPagoPendiente omitido para finca ', @IdFinca, ': ', ERROR_MESSAGE());
        END CATCH;

        FETCH NEXT FROM cursorFincas INTO @IdFinca;
    END

    CLOSE cursorFincas;
    DEALLOCATE cursorFincas;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_Fincas_Auditoria
ON dbo.Fincas
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT i.IdPropietario, 'Fincas', 'Fincas', i.IdFinca, 'INSERT', NULL,
           (SELECT i.IdFinca, i.IdPropietario, i.NombreFinca, i.Provincia, i.Canton, i.Distrito, i.Hectareas, i.EstadoFinca FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Finca creada: ', i.NombreFinca)
    FROM inserted i
    LEFT JOIN deleted d ON d.IdFinca = i.IdFinca
    WHERE d.IdFinca IS NULL;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT COALESCE(i.IdPropietario, d.IdPropietario), 'Fincas', 'Fincas', i.IdFinca, 'UPDATE',
           (SELECT d.IdFinca, d.IdPropietario, d.NombreFinca, d.Provincia, d.Canton, d.Distrito, d.Hectareas, d.EstadoFinca FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           (SELECT i.IdFinca, i.IdPropietario, i.NombreFinca, i.Provincia, i.Canton, i.Distrito, i.Hectareas, i.EstadoFinca FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Finca actualizada: ', i.NombreFinca)
    FROM inserted i
    INNER JOIN deleted d ON d.IdFinca = i.IdFinca;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT d.IdPropietario, 'Fincas', 'Fincas', d.IdFinca, 'DELETE',
           (SELECT d.IdFinca, d.IdPropietario, d.NombreFinca, d.Provincia, d.Canton, d.Distrito, d.Hectareas, d.EstadoFinca FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           NULL, SYSDATETIME(), CONCAT('Finca eliminada: ', d.NombreFinca)
    FROM deleted d
    LEFT JOIN inserted i ON i.IdFinca = d.IdFinca
    WHERE i.IdFinca IS NULL;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_EvaluacionesTecnicas_Auditoria
ON dbo.EvaluacionesTecnicas
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT i.IdIngeniero, 'Evaluaciones', 'EvaluacionesTecnicas', i.IdEvaluacion, 'INSERT', NULL,
           (SELECT i.IdEvaluacion, i.IdFinca, i.IdIngeniero, i.EstadoEvaluacion, i.DecisionTecnica, i.FechaDecision FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Evaluación creada #', i.IdEvaluacion)
    FROM inserted i
    LEFT JOIN deleted d ON d.IdEvaluacion = i.IdEvaluacion
    WHERE d.IdEvaluacion IS NULL;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT COALESCE(i.IdIngeniero, d.IdIngeniero), 'Evaluaciones', 'EvaluacionesTecnicas', i.IdEvaluacion, 'UPDATE',
           (SELECT d.IdEvaluacion, d.IdFinca, d.IdIngeniero, d.EstadoEvaluacion, d.DecisionTecnica, d.FechaDecision FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           (SELECT i.IdEvaluacion, i.IdFinca, i.IdIngeniero, i.EstadoEvaluacion, i.DecisionTecnica, i.FechaDecision FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Evaluación actualizada #', i.IdEvaluacion)
    FROM inserted i
    INNER JOIN deleted d ON d.IdEvaluacion = i.IdEvaluacion;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT d.IdIngeniero, 'Evaluaciones', 'EvaluacionesTecnicas', d.IdEvaluacion, 'DELETE',
           (SELECT d.IdEvaluacion, d.IdFinca, d.IdIngeniero, d.EstadoEvaluacion, d.DecisionTecnica, d.FechaDecision FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           NULL, SYSDATETIME(), CONCAT('Evaluación eliminada #', d.IdEvaluacion)
    FROM deleted d
    LEFT JOIN inserted i ON i.IdEvaluacion = d.IdEvaluacion
    WHERE i.IdEvaluacion IS NULL;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_PlanesPago_Auditoria
ON dbo.PlanesPago
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT NULL, 'Pagos', 'PlanesPago', i.IdPlanPago, 'INSERT', NULL,
           (SELECT i.IdPlanPago, i.IdFinca, i.IdEvaluacion, i.Anio, i.MontoMensualCalculado, i.EstadoPlan FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Plan de pago creado #', i.IdPlanPago)
    FROM inserted i
    LEFT JOIN deleted d ON d.IdPlanPago = i.IdPlanPago
    WHERE d.IdPlanPago IS NULL;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT NULL, 'Pagos', 'PlanesPago', i.IdPlanPago, 'UPDATE',
           (SELECT d.IdPlanPago, d.IdFinca, d.IdEvaluacion, d.Anio, d.MontoMensualCalculado, d.EstadoPlan FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           (SELECT i.IdPlanPago, i.IdFinca, i.IdEvaluacion, i.Anio, i.MontoMensualCalculado, i.EstadoPlan FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Plan de pago actualizado #', i.IdPlanPago)
    FROM inserted i
    INNER JOIN deleted d ON d.IdPlanPago = i.IdPlanPago;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT NULL, 'Pagos', 'PlanesPago', d.IdPlanPago, 'DELETE',
           (SELECT d.IdPlanPago, d.IdFinca, d.IdEvaluacion, d.Anio, d.MontoMensualCalculado, d.EstadoPlan FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           NULL, SYSDATETIME(), CONCAT('Plan de pago eliminado #', d.IdPlanPago)
    FROM deleted d
    LEFT JOIN inserted i ON i.IdPlanPago = d.IdPlanPago
    WHERE i.IdPlanPago IS NULL;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_CuentasBancarias_Auditoria
ON dbo.CuentasBancarias
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT i.IdUsuario, 'CuentasBancarias', 'CuentasBancarias', i.IdCuentaBancaria, 'INSERT', NULL,
           (SELECT i.IdCuentaBancaria, i.IdUsuario, i.Banco, i.NumeroCuenta, i.EstadoValidacion, i.Activa FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Cuenta bancaria creada #', i.IdCuentaBancaria)
    FROM inserted i
    LEFT JOIN deleted d ON d.IdCuentaBancaria = i.IdCuentaBancaria
    WHERE d.IdCuentaBancaria IS NULL;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT COALESCE(i.IdUsuario, d.IdUsuario), 'CuentasBancarias', 'CuentasBancarias', i.IdCuentaBancaria, 'UPDATE',
           (SELECT d.IdCuentaBancaria, d.IdUsuario, d.Banco, d.NumeroCuenta, d.EstadoValidacion, d.Activa FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           (SELECT i.IdCuentaBancaria, i.IdUsuario, i.Banco, i.NumeroCuenta, i.EstadoValidacion, i.Activa FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           SYSDATETIME(), CONCAT('Cuenta bancaria actualizada #', i.IdCuentaBancaria)
    FROM inserted i
    INNER JOIN deleted d ON d.IdCuentaBancaria = i.IdCuentaBancaria;

    INSERT INTO dbo.AuditoriaLog (IdUsuario, Modulo, TablaAfectada, IdRegistroAfectado, Accion, ValorAnterior, ValorNuevo, FechaAccion, Detalle)
    SELECT d.IdUsuario, 'CuentasBancarias', 'CuentasBancarias', d.IdCuentaBancaria, 'DELETE',
           (SELECT d.IdCuentaBancaria, d.IdUsuario, d.Banco, d.NumeroCuenta, d.EstadoValidacion, d.Activa FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           NULL, SYSDATETIME(), CONCAT('Cuenta bancaria eliminada #', d.IdCuentaBancaria)
    FROM deleted d
    LEFT JOIN inserted i ON i.IdCuentaBancaria = d.IdCuentaBancaria
    WHERE i.IdCuentaBancaria IS NULL;
END;
GO
