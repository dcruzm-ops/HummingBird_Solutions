/*
    Variante Azure SQL Database para vistas de reportes.
    - No usa USE [BaseDeDatos]
    - Valida prerequisitos antes de crear/actualizar vistas
*/

/* =========================================
   Validación de prerequisitos
   ========================================= */
IF OBJECT_ID(N'dbo.Fincas', N'U') IS NULL
    THROW 50001, 'Falta tabla dbo.Fincas. Ejecute primero Azure/01_creacion_tablas_azure_safe.sql', 1;

IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
    THROW 50002, 'Falta tabla dbo.Usuarios. Ejecute primero Azure/01_creacion_tablas_azure_safe.sql', 1;

IF OBJECT_ID(N'dbo.PlanesPago', N'U') IS NULL
    THROW 50003, 'Falta tabla dbo.PlanesPago. Verifique que el script de tablas terminó sin errores.', 1;

IF OBJECT_ID(N'dbo.CuotasPago', N'U') IS NULL
    THROW 50004, 'Falta tabla dbo.CuotasPago. Verifique que el script de tablas terminó sin errores.', 1;

IF OBJECT_ID(N'dbo.TransaccionesPago', N'U') IS NULL
    THROW 50005, 'Falta tabla dbo.TransaccionesPago. Verifique que el script de tablas terminó sin errores.', 1;

IF OBJECT_ID(N'dbo.EvaluacionesTecnicas', N'U') IS NULL
    THROW 50006, 'Falta tabla dbo.EvaluacionesTecnicas. Ejecute primero Azure/01_creacion_tablas_azure_safe.sql', 1;
GO

/* =========================================
   Vista para mapa
   ========================================= */
CREATE OR ALTER VIEW dbo.vw_FincasMapa
AS
SELECT
    f.IdFinca,
    f.NombreFinca,
    f.IdPropietario,
    u.NombreCompleto AS Propietario,
    f.Provincia,
    f.Canton,
    f.Distrito,
    f.DireccionExacta,
    f.Latitud,
    f.Longitud,
    f.Hectareas,
    f.Vegetacion,
    f.TieneRecursosHidricos,
    f.UsoSuelo,
    f.Pendiente,
    f.EstadoFinca
FROM dbo.Fincas f
INNER JOIN dbo.Usuarios u
    ON u.IdUsuario = f.IdPropietario;
GO

/* =========================================
   Vistas para reportes y análisis de datos
   ========================================= */
CREATE OR ALTER VIEW dbo.vw_ReportePagosMensualesDueno
AS
SELECT
    pp.IdPlanPago,
    f.IdFinca,
    f.IdPropietario,
    f.NombreFinca,
    pp.Anio,
    cp.Mes,
    cp.FechaProgramada,
    cp.FechaPago,
    pp.MontoBaseMensual,
    pp.PorcentajeAjusteTotal,
    cp.MontoProgramado AS MontoMensualCalculado,
    cp.MontoPendiente,
    cp.EstadoCuota
FROM dbo.PlanesPago pp
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
INNER JOIN dbo.CuotasPago cp ON cp.IdPlanPago = pp.IdPlanPago;
GO

CREATE OR ALTER VIEW dbo.vw_ReporteTransaccionesPagosDueno
AS
SELECT
    tp.IdTransaccionPago,
    tp.FechaTransaccion,
    tp.MontoTotal,
    tp.EstadoTransaccion,
    tp.ReferenciaExterna,
    tp.Observaciones,
    pp.IdPlanPago,
    pp.Anio,
    f.IdFinca,
    f.IdPropietario,
    f.NombreFinca,
    f.Provincia,
    f.Canton,
    f.Distrito
FROM dbo.TransaccionesPago tp
INNER JOIN dbo.PlanesPago pp ON pp.IdPlanPago = tp.IdPlanPago
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca;
GO

CREATE OR ALTER VIEW dbo.vw_ReporteEvaluacionesIngeniero
AS
SELECT
    e.IdEvaluacion,
    e.IdIngeniero,
    u.NombreCompleto AS Ingeniero,
    e.EstadoEvaluacion,
    e.DecisionTecnica,
    e.FechaVisita,
    e.FechaDecision,
    f.IdFinca,
    f.NombreFinca,
    f.Provincia,
    f.Canton,
    f.Distrito
FROM dbo.EvaluacionesTecnicas e
LEFT JOIN dbo.Usuarios u ON u.IdUsuario = e.IdIngeniero
INNER JOIN dbo.Fincas f ON f.IdFinca = e.IdFinca;
GO

CREATE OR ALTER VIEW dbo.vw_ReportePagosPorUbicacion
AS
SELECT
    f.Provincia,
    f.Canton,
    f.Distrito,
    pp.Anio,
    cp.Mes,
    SUM(cp.MontoProgramado - cp.MontoPendiente) AS MontoPagadoMes,
    SUM(cp.MontoProgramado) AS MontoProgramadoMes
FROM dbo.CuotasPago cp
INNER JOIN dbo.PlanesPago pp ON pp.IdPlanPago = cp.IdPlanPago
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
GROUP BY
    f.Provincia,
    f.Canton,
    f.Distrito,
    pp.Anio,
    cp.Mes;
GO

CREATE OR ALTER VIEW dbo.vw_ResumenActividadSistema
AS
SELECT 'Usuarios activos' AS Indicador, CAST(COUNT_BIG(1) AS BIGINT) AS Total
FROM dbo.Usuarios
WHERE Estado = 'Activo'
UNION ALL
SELECT 'Fincas registradas', CAST(COUNT_BIG(1) AS BIGINT)
FROM dbo.Fincas
UNION ALL
SELECT 'Evaluaciones pendientes', CAST(COUNT_BIG(1) AS BIGINT)
FROM dbo.EvaluacionesTecnicas
WHERE EstadoEvaluacion IN ('Pendiente', 'En proceso', 'En Proceso')
UNION ALL
SELECT 'Planes de pago activos', CAST(COUNT_BIG(1) AS BIGINT)
FROM dbo.PlanesPago
WHERE EstadoPlan = 'Activo'
UNION ALL
SELECT 'Transacciones procesadas', CAST(COUNT_BIG(1) AS BIGINT)
FROM dbo.TransaccionesPago
WHERE EstadoTransaccion = 'Procesada';
GO
