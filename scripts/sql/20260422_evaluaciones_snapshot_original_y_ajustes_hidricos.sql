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
