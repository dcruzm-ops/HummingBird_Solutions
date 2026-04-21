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
