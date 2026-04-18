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
