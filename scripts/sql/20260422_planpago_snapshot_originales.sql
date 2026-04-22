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
