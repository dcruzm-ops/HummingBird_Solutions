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
