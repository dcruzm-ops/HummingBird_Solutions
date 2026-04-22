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
