USE PSA_CostaRica;
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
