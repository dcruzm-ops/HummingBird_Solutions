
/*
    Script SQL Server (SSMS) basado en la hoja "Tablas Final" del archivo "tablas sql.xlsx".
    Nota:
    - Se agregaron DEFAULT, CHECK, IDENTITY e índices técnicos para que sea ejecutable.
    - La tabla TransaccionesPago está incluida, aunque en la plantilla aparece como opcional para MVP.
    - Se incluye una vista vw_FincasMapa para consumir fincas en el módulo de mapa de la app.
*/

IF DB_ID(N'PSA_CostaRica') IS NULL
BEGIN
    CREATE DATABASE PSA_CostaRica;
END;
GO

USE PSA_CostaRica;
GO

/* =========================================
   Limpieza de objetos si ya existen
   ========================================= */
IF OBJECT_ID(N'dbo.vw_FincasMapa', N'V') IS NOT NULL
    DROP VIEW dbo.vw_FincasMapa;
IF OBJECT_ID(N'dbo.vw_ReportePagosMensualesDueno', N'V') IS NOT NULL
    DROP VIEW dbo.vw_ReportePagosMensualesDueno;
IF OBJECT_ID(N'dbo.vw_ReporteTransaccionesPagosDueno', N'V') IS NOT NULL
    DROP VIEW dbo.vw_ReporteTransaccionesPagosDueno;
IF OBJECT_ID(N'dbo.vw_ReporteEvaluacionesIngeniero', N'V') IS NOT NULL
    DROP VIEW dbo.vw_ReporteEvaluacionesIngeniero;
IF OBJECT_ID(N'dbo.vw_ReportePagosPorUbicacion', N'V') IS NOT NULL
    DROP VIEW dbo.vw_ReportePagosPorUbicacion;
IF OBJECT_ID(N'dbo.vw_ResumenActividadSistema', N'V') IS NOT NULL
    DROP VIEW dbo.vw_ResumenActividadSistema;
GO

IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NOT NULL DROP TABLE dbo.Notificaciones;
IF OBJECT_ID(N'dbo.EvaluacionEvidencias', N'U') IS NOT NULL DROP TABLE dbo.EvaluacionEvidencias;
IF OBJECT_ID(N'dbo.FincaEvidencias', N'U') IS NOT NULL DROP TABLE dbo.FincaEvidencias;
IF OBJECT_ID(N'dbo.AuditoriaLog', N'U') IS NOT NULL DROP TABLE dbo.AuditoriaLog;
IF OBJECT_ID(N'dbo.CatalogoFincaValores', N'U') IS NOT NULL DROP TABLE dbo.CatalogoFincaValores;
IF OBJECT_ID(N'dbo.TransaccionesPago', N'U') IS NOT NULL DROP TABLE dbo.TransaccionesPago;
IF OBJECT_ID(N'dbo.CuotasPago', N'U') IS NOT NULL DROP TABLE dbo.CuotasPago;
IF OBJECT_ID(N'dbo.PlanesPago', N'U') IS NOT NULL DROP TABLE dbo.PlanesPago;
IF OBJECT_ID(N'dbo.ConfiguracionPagoDetalle', N'U') IS NOT NULL DROP TABLE dbo.ConfiguracionPagoDetalle;
IF OBJECT_ID(N'dbo.ConfiguracionesPago', N'U') IS NOT NULL DROP TABLE dbo.ConfiguracionesPago;
IF OBJECT_ID(N'dbo.CuentasBancarias', N'U') IS NOT NULL DROP TABLE dbo.CuentasBancarias;
IF OBJECT_ID(N'dbo.EvaluacionesTecnicas', N'U') IS NOT NULL DROP TABLE dbo.EvaluacionesTecnicas;
IF OBJECT_ID(N'dbo.Fincas', N'U') IS NOT NULL DROP TABLE dbo.Fincas;
IF OBJECT_ID(N'dbo.TokensRecuperacion', N'U') IS NOT NULL DROP TABLE dbo.TokensRecuperacion;
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL DROP TABLE dbo.Usuarios;
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

/* =========================================
   4.1 Catálogos configurables para Finca
   ========================================= */
CREATE TABLE dbo.CatalogoFincaValores
(
    IdCatalogoFincaValor     INT IDENTITY(1,1) NOT NULL,
    TipoCatalogo             VARCHAR(50) NOT NULL,
    Valor                    VARCHAR(100) NOT NULL,
    Activo                   BIT NOT NULL CONSTRAINT DF_CatalogoFincaValores_Activo DEFAULT (1),
    OrdenVisual              INT NOT NULL CONSTRAINT DF_CatalogoFincaValores_OrdenVisual DEFAULT (0),
    FechaRegistro            DATETIME2 NOT NULL CONSTRAINT DF_CatalogoFincaValores_FechaRegistro DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_CatalogoFincaValores PRIMARY KEY (IdCatalogoFincaValor),
    CONSTRAINT UQ_CatalogoFincaValores UNIQUE (TipoCatalogo, Valor),
    CONSTRAINT CK_CatalogoFincaValores_TipoCatalogo CHECK (TipoCatalogo IN ('Pendiente', 'Vegetacion', 'UsoSuelo'))
);
GO

/* =========================================
   1. Roles
   ========================================= */
CREATE TABLE dbo.Roles
(
    IdRol           INT IDENTITY(1,1) NOT NULL,
    Nombre          VARCHAR(50) NOT NULL,
    Descripcion     VARCHAR(150) NULL,
    Activo          BIT NOT NULL CONSTRAINT DF_Roles_Activo DEFAULT (1),
    CONSTRAINT PK_Roles PRIMARY KEY (IdRol),
    CONSTRAINT UQ_Roles_Nombre UNIQUE (Nombre)
);
GO

/* =========================================
   2. Usuarios
   ========================================= */
CREATE TABLE dbo.Usuarios
(
    IdUsuario       INT IDENTITY(1,1) NOT NULL,
    NombreCompleto  VARCHAR(150) NOT NULL,
    Email           VARCHAR(150) NOT NULL,
    PasswordHash    VARCHAR(255) NOT NULL,
    IdRol           INT NOT NULL,
    Estado          VARCHAR(20) NOT NULL CONSTRAINT DF_Usuarios_Estado DEFAULT ('Activo'),
    FechaCreacion   DATETIME2 NOT NULL CONSTRAINT DF_Usuarios_FechaCreacion DEFAULT (SYSDATETIME()),
    UltimoAcceso    DATETIME2 NULL,
    CONSTRAINT PK_Usuarios PRIMARY KEY (IdUsuario),
    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
    CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (IdRol) REFERENCES dbo.Roles(IdRol),
    CONSTRAINT CK_Usuarios_Estado CHECK (Estado IN ('Activo', 'Inactivo', 'Bloqueado'))
);
GO

ALTER TABLE Usuarios
ALTER COLUMN PasswordHash NVARCHAR(500) NULL;
GO

/* =========================================
   3. TokensRecuperacion
   ========================================= */
CREATE TABLE dbo.TokensRecuperacion
(
    IdToken             INT IDENTITY(1,1) NOT NULL,
    IdUsuario           INT NOT NULL,
    Token               VARCHAR(100) NOT NULL,
    FechaCreacion       DATETIME2 NOT NULL CONSTRAINT DF_Tokens_FechaCreacion DEFAULT (SYSDATETIME()),
    FechaExpiracion     DATETIME2 NOT NULL,
    Usado               BIT NOT NULL CONSTRAINT DF_Tokens_Usado DEFAULT (0),
    FechaUso            DATETIME2 NULL,
    CONSTRAINT PK_TokensRecuperacion PRIMARY KEY (IdToken),
    CONSTRAINT UQ_TokensRecuperacion_Token UNIQUE (Token),
    CONSTRAINT FK_TokensRecuperacion_Usuarios FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_Tokens_Fechas CHECK (FechaExpiracion > FechaCreacion)
);
GO

/* =========================================
   4. Fincas
   ========================================= */
CREATE TABLE dbo.Fincas
(
    IdFinca                  INT IDENTITY(1,1) NOT NULL,
    IdPropietario            INT NOT NULL,
    NombreFinca              VARCHAR(150) NOT NULL,
    Provincia                VARCHAR(100) NOT NULL,
    Canton                   VARCHAR(100) NOT NULL,
    Distrito                 VARCHAR(100) NOT NULL,
    DireccionExacta          VARCHAR(250) NULL,
    Latitud                  DECIMAL(10,7) NOT NULL,
    Longitud                 DECIMAL(10,7) NOT NULL,
    Hectareas                DECIMAL(10,2) NOT NULL,
    Vegetacion               VARCHAR(100) NOT NULL,
    TieneRecursosHidricos    BIT NOT NULL CONSTRAINT DF_Fincas_TieneRecursosHidricos DEFAULT (0),
    TieneRiosOQuebradas      BIT NOT NULL CONSTRAINT DF_Fincas_TieneRiosOQuebradas DEFAULT (0),
    TieneNacientes           BIT NOT NULL CONSTRAINT DF_Fincas_TieneNacientes DEFAULT (0),
    CantidadNacientes        INT NOT NULL CONSTRAINT DF_Fincas_CantidadNacientes DEFAULT (0),
    UsoSuelo                 VARCHAR(100) NOT NULL,
    Pendiente                VARCHAR(50) NOT NULL,
    EstadoFinca              VARCHAR(30) NOT NULL CONSTRAINT DF_Fincas_EstadoFinca DEFAULT ('Registrada'),
    FechaRegistro            DATETIME2 NOT NULL CONSTRAINT DF_Fincas_FechaRegistro DEFAULT (SYSDATETIME()),
    FechaActualizacion       DATETIME2 NOT NULL CONSTRAINT DF_Fincas_FechaActualizacion DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Fincas PRIMARY KEY (IdFinca),
    CONSTRAINT FK_Fincas_Usuarios_Propietario FOREIGN KEY (IdPropietario) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_Fincas_Latitud CHECK (Latitud BETWEEN -90 AND 90),
    CONSTRAINT CK_Fincas_Longitud CHECK (Longitud BETWEEN -180 AND 180),
    CONSTRAINT CK_Fincas_Hectareas CHECK (Hectareas > 0),
    CONSTRAINT CK_Fincas_CantidadNacientes CHECK (CantidadNacientes >= 0 AND (TieneNacientes = 1 OR CantidadNacientes = 0)),
    CONSTRAINT CK_Fincas_Estado CHECK (EstadoFinca IN ('Registrada', 'Pendiente', 'EnRevision', 'En proceso', 'Aprobada', 'Rechazada', 'Suspendida', 'Inactiva'))
);
GO

/* =========================================
   5. EvaluacionesTecnicas
   ========================================= */
CREATE TABLE dbo.EvaluacionesTecnicas
(
    IdEvaluacion                 INT IDENTITY(1,1) NOT NULL,
    IdFinca                      INT NOT NULL,
    IdIngeniero                  INT NULL,
    EstadoEvaluacion             VARCHAR(30) NOT NULL CONSTRAINT DF_Evaluaciones_Estado DEFAULT ('Pendiente'),
    FechaVisita                  DATE NULL,
    Observaciones                NVARCHAR(MAX) NULL,
    DecisionTecnica              VARCHAR(20) NULL,
    HectareasAjustadas           DECIMAL(10,2) NULL,
    VegetacionAjustada           VARCHAR(100) NULL,
    RecursosHidricosAjustado     BIT NULL,
    UsoSueloAjustado             VARCHAR(100) NULL,
    PendienteAjustada            VARCHAR(50) NULL,
    FechaDecision                DATETIME2 NULL,
    CONSTRAINT PK_EvaluacionesTecnicas PRIMARY KEY (IdEvaluacion),
    CONSTRAINT FK_Evaluaciones_Fincas FOREIGN KEY (IdFinca) REFERENCES dbo.Fincas(IdFinca),
    CONSTRAINT FK_Evaluaciones_Usuarios_Ingeniero FOREIGN KEY (IdIngeniero) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_Evaluaciones_Estado CHECK (
        EstadoEvaluacion IN (
            'Pendiente',
            'En Proceso',
            'Finalizada',
            'En proceso',
            'Evaluada – No califica',
            'Evaluada – Califica',
            'Pendiente de cuenta bancaria',
            'Pendiente de aprobación final de pago',
            'Pagos activos'
        )
    ),
    CONSTRAINT CK_Evaluaciones_Decision CHECK (DecisionTecnica IS NULL OR DecisionTecnica IN ('Califica', 'No Califica')),
    CONSTRAINT CK_Evaluaciones_HectareasAjustadas CHECK (HectareasAjustadas IS NULL OR HectareasAjustadas > 0)
);
GO

/* =========================================
   6. CuentasBancarias
   ========================================= */
CREATE TABLE dbo.CuentasBancarias
(
    IdCuentaBancaria         INT IDENTITY(1,1) NOT NULL,
    IdUsuario                INT NOT NULL,
    Banco                    VARCHAR(100) NOT NULL,
    NumeroCuenta             VARCHAR(50) NOT NULL,
    TipoCuenta               VARCHAR(30) NOT NULL,
    Titular                  VARCHAR(150) NOT NULL,
    EstadoValidacion         VARCHAR(20) NOT NULL CONSTRAINT DF_Cuentas_EstadoValidacion DEFAULT ('Pendiente'),
    ValidadoPor              INT NULL,
    FechaValidacion          DATETIME2 NULL,
    ObservacionesValidacion  VARCHAR(250) NULL,
    Activa                   BIT NOT NULL CONSTRAINT DF_Cuentas_Activa DEFAULT (0),
    FechaRegistro            DATETIME2 NOT NULL CONSTRAINT DF_Cuentas_FechaRegistro DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_CuentasBancarias PRIMARY KEY (IdCuentaBancaria),
    CONSTRAINT FK_CuentasBancarias_Usuarios FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT FK_CuentasBancarias_Usuarios_Validador FOREIGN KEY (ValidadoPor) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_Cuentas_TipoCuenta CHECK (TipoCuenta IN ('Ahorro', 'Corriente', 'IBAN', 'SINPE', 'Otra')),
    CONSTRAINT CK_Cuentas_EstadoValidacion CHECK (EstadoValidacion IN ('Pendiente', 'Validada', 'Rechazada'))
);
GO

/* =========================================
   7. ConfiguracionesPago
   ========================================= */
CREATE TABLE dbo.ConfiguracionesPago
(
    IdConfiguracionPago      INT IDENTITY(1,1) NOT NULL,
    Version                  INT NOT NULL,
    NombreVersion            VARCHAR(100) NOT NULL,
    PrecioBasePorHectarea    DECIMAL(10,2) NOT NULL,
    TopePorcentajeAjuste     DECIMAL(5,2) NOT NULL,
    FechaVigenciaDesde       DATE NOT NULL,
    FechaVigenciaHasta       DATE NULL,
    Activa                   BIT NOT NULL CONSTRAINT DF_ConfiguracionesPago_Activa DEFAULT (1),
    CreadoPor                INT NOT NULL,
    FechaCreacion            DATETIME2 NOT NULL CONSTRAINT DF_ConfiguracionesPago_FechaCreacion DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_ConfiguracionesPago PRIMARY KEY (IdConfiguracionPago),
    CONSTRAINT UQ_ConfiguracionesPago_Version UNIQUE (Version),
    CONSTRAINT FK_ConfiguracionesPago_Usuarios FOREIGN KEY (CreadoPor) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT CK_ConfiguracionesPago_PrecioBase CHECK (PrecioBasePorHectarea >= 0),
    CONSTRAINT CK_ConfiguracionesPago_Tope CHECK (TopePorcentajeAjuste >= 0),
    CONSTRAINT CK_ConfiguracionesPago_Fechas CHECK (FechaVigenciaHasta IS NULL OR FechaVigenciaHasta >= FechaVigenciaDesde)
);
GO

/* =========================================
   8. ConfiguracionPagoDetalle
   ========================================= */
CREATE TABLE dbo.ConfiguracionPagoDetalle
(
    IdDetalleConfiguracion   INT IDENTITY(1,1) NOT NULL,
    IdConfiguracionPago      INT NOT NULL,
    TipoFactor               VARCHAR(50) NOT NULL,
    ValorFactor              VARCHAR(100) NOT NULL,
    PorcentajeAjuste         DECIMAL(5,2) NOT NULL,
    CONSTRAINT PK_ConfiguracionPagoDetalle PRIMARY KEY (IdDetalleConfiguracion),
    CONSTRAINT FK_ConfiguracionPagoDetalle_ConfiguracionesPago FOREIGN KEY (IdConfiguracionPago) REFERENCES dbo.ConfiguracionesPago(IdConfiguracionPago),
    CONSTRAINT CK_ConfiguracionPagoDetalle_TipoFactor CHECK (TipoFactor IN ('Vegetacion', 'RecursosHidricos', 'Pendiente', 'UsoSuelo')),
    CONSTRAINT UQ_ConfiguracionPagoDetalle UNIQUE (IdConfiguracionPago, TipoFactor, ValorFactor)
);
GO

/* =========================================
   9. PlanesPago
   ========================================= */
CREATE TABLE dbo.PlanesPago
(
    IdPlanPago                  INT IDENTITY(1,1) NOT NULL,
    IdFinca                     INT NOT NULL,
    IdEvaluacion                INT NOT NULL,
    IdConfiguracionPago         INT NOT NULL,
    IdCuentaBancaria            INT NOT NULL,
    Anio                        INT NOT NULL,
    MontoBaseMensual            DECIMAL(10,2) NOT NULL,
    PorcentajeAjusteTotal       DECIMAL(5,2) NOT NULL,
    MontoMensualCalculado       DECIMAL(10,2) NOT NULL,
    EstadoPlan                  VARCHAR(20) NOT NULL CONSTRAINT DF_PlanesPago_EstadoPlan DEFAULT ('Activo'),
    FechaGeneracion             DATETIME2 NOT NULL CONSTRAINT DF_PlanesPago_FechaGeneracion DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_PlanesPago PRIMARY KEY (IdPlanPago),
    CONSTRAINT FK_PlanesPago_Fincas FOREIGN KEY (IdFinca) REFERENCES dbo.Fincas(IdFinca),
    CONSTRAINT FK_PlanesPago_Evaluaciones FOREIGN KEY (IdEvaluacion) REFERENCES dbo.EvaluacionesTecnicas(IdEvaluacion),
    CONSTRAINT FK_PlanesPago_ConfiguracionesPago FOREIGN KEY (IdConfiguracionPago) REFERENCES dbo.ConfiguracionesPago(IdConfiguracionPago),
    CONSTRAINT FK_PlanesPago_CuentasBancarias FOREIGN KEY (IdCuentaBancaria) REFERENCES dbo.CuentasBancarias(IdCuentaBancaria),
    CONSTRAINT CK_PlanesPago_Anio CHECK (Anio BETWEEN 2000 AND 2100),
    CONSTRAINT CK_PlanesPago_Montos CHECK (MontoBaseMensual >= 0 AND MontoMensualCalculado >= 0),
    CONSTRAINT CK_PlanesPago_Estado CHECK (EstadoPlan IN ('Activo', 'Suspendido', 'Finalizado', 'Cancelado'))
);
GO

/* =========================================
   10. CuotasPago
   ========================================= */
CREATE TABLE dbo.CuotasPago
(
    IdCuotaPago             INT IDENTITY(1,1) NOT NULL,
    IdPlanPago              INT NOT NULL,
    Mes                     INT NOT NULL,
    FechaProgramada         DATE NOT NULL,
    MontoProgramado         DECIMAL(10,2) NOT NULL,
    MontoPendiente          DECIMAL(10,2) NOT NULL,
    EstadoCuota             VARCHAR(20) NOT NULL CONSTRAINT DF_CuotasPago_EstadoCuota DEFAULT ('Programada'),
    FechaPago               DATE NULL,
    CONSTRAINT PK_CuotasPago PRIMARY KEY (IdCuotaPago),
    CONSTRAINT FK_CuotasPago_PlanesPago FOREIGN KEY (IdPlanPago) REFERENCES dbo.PlanesPago(IdPlanPago),
    CONSTRAINT UQ_CuotasPago_Plan_Mes UNIQUE (IdPlanPago, Mes),
    CONSTRAINT CK_CuotasPago_Mes CHECK (Mes BETWEEN 1 AND 12),
    CONSTRAINT CK_CuotasPago_Montos CHECK (MontoProgramado >= 0 AND MontoPendiente >= 0),
    CONSTRAINT CK_CuotasPago_Estado CHECK (EstadoCuota IN ('Programada', 'Pagada', 'Atrasada', 'Acumulada'))
);
GO

/* =========================================
   11. TransaccionesPago (opcional para MVP)
   ========================================= */
CREATE TABLE dbo.TransaccionesPago
(
    IdTransaccionPago       INT IDENTITY(1,1) NOT NULL,
    IdPlanPago              INT NOT NULL,
    FechaTransaccion        DATETIME2 NOT NULL CONSTRAINT DF_TransaccionesPago_Fecha DEFAULT (SYSDATETIME()),
    MontoTotal              DECIMAL(10,2) NOT NULL,
    EstadoTransaccion       VARCHAR(20) NOT NULL,
    ReferenciaExterna       VARCHAR(100) NULL,
    Observaciones           VARCHAR(250) NULL,
    CONSTRAINT PK_TransaccionesPago PRIMARY KEY (IdTransaccionPago),
    CONSTRAINT FK_TransaccionesPago_PlanesPago FOREIGN KEY (IdPlanPago) REFERENCES dbo.PlanesPago(IdPlanPago),
    CONSTRAINT CK_TransaccionesPago_Monto CHECK (MontoTotal >= 0),
    CONSTRAINT CK_TransaccionesPago_Estado CHECK (EstadoTransaccion IN ('Pendiente', 'Procesada', 'Rechazada', 'Anulada'))
);
GO

/* =========================================
   12. AuditoriaLog
   ========================================= */
CREATE TABLE dbo.AuditoriaLog
(
    IdLog                   INT IDENTITY(1,1) NOT NULL,
    IdUsuario               INT NULL,
    Modulo                  VARCHAR(50) NOT NULL,
    TablaAfectada           VARCHAR(50) NOT NULL,
    IdRegistroAfectado      INT NULL,
    Accion                  VARCHAR(50) NOT NULL,
    ValorAnterior           NVARCHAR(MAX) NULL,
    ValorNuevo              NVARCHAR(MAX) NULL,
    FechaAccion             DATETIME2 NOT NULL CONSTRAINT DF_AuditoriaLog_FechaAccion DEFAULT (SYSDATETIME()),
    IpOrigen                VARCHAR(50) NULL,
    Detalle                 VARCHAR(250) NULL,
    CONSTRAINT PK_AuditoriaLog PRIMARY KEY (IdLog),
    CONSTRAINT FK_AuditoriaLog_Usuarios FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario)
);
GO

/* =========================================
   13. FincaEvidencias
   ========================================= */
CREATE TABLE dbo.FincaEvidencias
(
    IdEvidencia             INT IDENTITY(1,1) NOT NULL,
    IdFinca                 INT NOT NULL,
    NombreArchivo           NVARCHAR(255) NOT NULL,
    RutaArchivo             NVARCHAR(500) NOT NULL,
    TipoArchivo             VARCHAR(50) NOT NULL,
    FechaCarga              DATETIME2 NOT NULL CONSTRAINT DF_FincaEvidencias_FechaCarga DEFAULT (SYSDATETIME()),
    CargadoPor              INT NOT NULL,
    CONSTRAINT PK_FincaEvidencias PRIMARY KEY (IdEvidencia),
    CONSTRAINT FK_FincaEvidencias_Fincas FOREIGN KEY (IdFinca) REFERENCES dbo.Fincas(IdFinca),
    CONSTRAINT FK_FincaEvidencias_Usuarios FOREIGN KEY (CargadoPor) REFERENCES dbo.Usuarios(IdUsuario)
);
GO

/* =========================================
   14. EvaluacionEvidencias
   ========================================= */
CREATE TABLE dbo.EvaluacionEvidencias
(
    IdEvidenciaEvaluacion   INT IDENTITY(1,1) NOT NULL,
    IdEvaluacion            INT NOT NULL,
    NombreArchivo           NVARCHAR(255) NOT NULL,
    RutaArchivo             NVARCHAR(500) NOT NULL,
    TipoArchivo             VARCHAR(50) NOT NULL,
    FechaCarga              DATETIME2 NOT NULL CONSTRAINT DF_EvaluacionEvidencias_FechaCarga DEFAULT (SYSDATETIME()),
    CargadoPor              INT NOT NULL,
    CONSTRAINT PK_EvaluacionEvidencias PRIMARY KEY (IdEvidenciaEvaluacion),
    CONSTRAINT FK_EvaluacionEvidencias_Evaluaciones FOREIGN KEY (IdEvaluacion) REFERENCES dbo.EvaluacionesTecnicas(IdEvaluacion),
    CONSTRAINT FK_EvaluacionEvidencias_Usuarios FOREIGN KEY (CargadoPor) REFERENCES dbo.Usuarios(IdUsuario)
);
GO

/* =========================================
   15. Notificaciones
   ========================================= */
CREATE TABLE dbo.Notificaciones
(
    IdNotificacion          INT IDENTITY(1,1) NOT NULL,
    IdUsuario               INT NOT NULL,
    Tipo                    VARCHAR(50) NOT NULL,
    Titulo                  VARCHAR(150) NOT NULL,
    Mensaje                 NVARCHAR(500) NOT NULL,
    Leida                   BIT NOT NULL CONSTRAINT DF_Notificaciones_Leida DEFAULT (0),
    FechaCreacion           DATETIME2 NOT NULL CONSTRAINT DF_Notificaciones_FechaCreacion DEFAULT (SYSDATETIME()),
    EntidadReferencia       VARCHAR(50) NULL,
    IdEntidadReferencia     INT NULL,
    CONSTRAINT PK_Notificaciones PRIMARY KEY (IdNotificacion),
    CONSTRAINT FK_Notificaciones_Usuarios FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario)
);
GO

/* =========================================
   Índices recomendados
   ========================================= */
CREATE INDEX IX_Usuarios_IdRol ON dbo.Usuarios(IdRol);
CREATE INDEX IX_TokensRecuperacion_IdUsuario ON dbo.TokensRecuperacion(IdUsuario);
CREATE INDEX IX_Fincas_IdPropietario ON dbo.Fincas(IdPropietario);
CREATE INDEX IX_Fincas_Latitud_Longitud ON dbo.Fincas(Latitud, Longitud);
CREATE INDEX IX_EvaluacionesTecnicas_IdFinca ON dbo.EvaluacionesTecnicas(IdFinca);
CREATE INDEX IX_EvaluacionesTecnicas_IdIngeniero ON dbo.EvaluacionesTecnicas(IdIngeniero);
CREATE INDEX IX_CuentasBancarias_IdUsuario ON dbo.CuentasBancarias(IdUsuario);
CREATE INDEX IX_ConfiguracionPagoDetalle_IdConfiguracionPago ON dbo.ConfiguracionPagoDetalle(IdConfiguracionPago);
CREATE INDEX IX_PlanesPago_IdFinca ON dbo.PlanesPago(IdFinca);
CREATE INDEX IX_PlanesPago_IdEvaluacion ON dbo.PlanesPago(IdEvaluacion);
CREATE INDEX IX_CuotasPago_IdPlanPago ON dbo.CuotasPago(IdPlanPago);
CREATE INDEX IX_TransaccionesPago_IdPlanPago ON dbo.TransaccionesPago(IdPlanPago);
CREATE INDEX IX_AuditoriaLog_IdUsuario ON dbo.AuditoriaLog(IdUsuario);
CREATE INDEX IX_FincaEvidencias_IdFinca ON dbo.FincaEvidencias(IdFinca);
CREATE INDEX IX_EvaluacionEvidencias_IdEvaluacion ON dbo.EvaluacionEvidencias(IdEvaluacion);
CREATE INDEX IX_Notificaciones_IdUsuario ON dbo.Notificaciones(IdUsuario);
GO

/* =========================================
   Vista para mapa
   ========================================= */
CREATE VIEW dbo.vw_FincasMapa
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
CREATE VIEW dbo.vw_ReportePagosMensualesDueno
AS
SELECT
    pp.IdPlanPago,
    f.IdFinca,
    f.IdPropietario,
    f.NombreFinca,
    pp.Anio,
    cp.Mes,
    cp.FechaProgramada,
    pp.MontoBaseMensual,
    pp.PorcentajeAjusteTotal,
    cp.MontoProgramado AS MontoMensualCalculado,
    cp.MontoPendiente,
    cp.EstadoCuota
FROM dbo.PlanesPago pp
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
INNER JOIN dbo.CuotasPago cp ON cp.IdPlanPago = pp.IdPlanPago;
GO

CREATE VIEW dbo.vw_ReporteTransaccionesPagosDueno
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

CREATE VIEW dbo.vw_ReporteEvaluacionesIngeniero
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

CREATE VIEW dbo.vw_ReportePagosPorUbicacion
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

CREATE VIEW dbo.vw_ResumenActividadSistema
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

/* =========================================
   Datos semilla mínimos
   ========================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Administrador')
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES ('Administrador', 'Acceso total al sistema');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Propietario')
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES ('Propietario', 'Dueño de finca que registra información');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Ingeniero Forestal')
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES ('Ingeniero Forestal', 'Responsable de evaluar técnicamente la finca');
GO

/* =========================================
   Consulta de prueba para el mapa
   ========================================= */
SELECT *
FROM dbo.vw_FincasMapa
ORDER BY IdFinca;
GO
