USE PSA_CostaRica;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* =====================================================
   Datos semilla - PSA Costa Rica
   Objetivo:
   - Cargar roles iniciales
   - Cargar usuario administrador base
   - Cargar datos mínimos para desarrollo y pruebas
   - Permitir reejecución sin duplicar registros clave
   ===================================================== */

BEGIN TRY
    BEGIN TRANSACTION;

    /* =========================================
       1. Roles iniciales
       ========================================= */
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Administrador')
        INSERT INTO dbo.Roles (Nombre, Descripcion, Activo)
        VALUES ('Administrador', 'Acceso total al sistema', 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Propietario')
        INSERT INTO dbo.Roles (Nombre, Descripcion, Activo)
        VALUES ('Propietario', 'Dueño de finca que registra información', 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Ingeniero Forestal')
        INSERT INTO dbo.Roles (Nombre, Descripcion, Activo)
        VALUES ('Ingeniero Forestal', 'Responsable de evaluar técnicamente la finca', 1);

    /* =========================================
       2. Variables base
       ========================================= */
    DECLARE @IdRolAdministrador INT;
    DECLARE @IdRolPropietario INT;
    DECLARE @IdRolIngeniero INT;

    DECLARE @IdAdmin INT;
    DECLARE @IdPropietario INT;
    DECLARE @IdIngeniero INT;

    DECLARE @IdCuentaBancaria INT;
    DECLARE @IdConfiguracionPago INT;
    DECLARE @IdFincaAprobada INT;
    DECLARE @IdFincaPendiente INT;
    DECLARE @IdEvaluacion INT;
    DECLARE @IdPlanPago INT;
    DECLARE @AnioBase INT = YEAR(GETDATE());

    SELECT @IdRolAdministrador = IdRol FROM dbo.Roles WHERE Nombre = 'Administrador';
    SELECT @IdRolPropietario = IdRol FROM dbo.Roles WHERE Nombre = 'Propietario';
    SELECT @IdRolIngeniero = IdRol FROM dbo.Roles WHERE Nombre = 'Ingeniero Forestal';

    /* =========================================
       3. Usuarios base
       Nota:
       PasswordHash usa SHA2_256 en hexadecimal como valor semilla.
       Si tu autenticación usa otro algoritmo, debes reemplazarlo.
       ========================================= */
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = 'admin@psa.local')
        INSERT INTO dbo.Usuarios (NombreCompleto, Email, PasswordHash, IdRol, Estado, UltimoAcceso)
        VALUES (
            'Administrador General PSA',
            'admin@psa.local',
            CONVERT(VARCHAR(255), HASHBYTES('SHA2_256', 'Admin123!'), 2),
            @IdRolAdministrador,
            'Activo',
            SYSDATETIME()
        );

    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = 'propietario1@psa.local')
        INSERT INTO dbo.Usuarios (NombreCompleto, Email, PasswordHash, IdRol, Estado, UltimoAcceso)
        VALUES (
            'María Fernanda Rojas',
            'propietario1@psa.local',
            CONVERT(VARCHAR(255), HASHBYTES('SHA2_256', 'Propietario123!'), 2),
            @IdRolPropietario,
            'Activo',
            NULL
        );

    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = 'ingeniero1@psa.local')
        INSERT INTO dbo.Usuarios (NombreCompleto, Email, PasswordHash, IdRol, Estado, UltimoAcceso)
        VALUES (
            'Carlos Andrés Solano',
            'ingeniero1@psa.local',
            CONVERT(VARCHAR(255), HASHBYTES('SHA2_256', 'Ingeniero123!'), 2),
            @IdRolIngeniero,
            'Activo',
            NULL
        );

    SELECT @IdAdmin = IdUsuario FROM dbo.Usuarios WHERE Email = 'admin@psa.local';
    SELECT @IdPropietario = IdUsuario FROM dbo.Usuarios WHERE Email = 'propietario1@psa.local';
    SELECT @IdIngeniero = IdUsuario FROM dbo.Usuarios WHERE Email = 'ingeniero1@psa.local';

    /* =========================================
       4. Token de recuperación de ejemplo
       ========================================= */
    IF NOT EXISTS (SELECT 1 FROM dbo.TokensRecuperacion WHERE Token = 'seed-token-admin-001')
        INSERT INTO dbo.TokensRecuperacion (IdUsuario, Token, FechaExpiracion, Usado, FechaUso)
        VALUES (
            @IdAdmin,
            'seed-token-admin-001',
            DATEADD(DAY, 2, SYSDATETIME()),
            0,
            NULL
        );

    /* =========================================
       5. Cuenta bancaria del propietario
       ========================================= */
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.CuentasBancarias
        WHERE IdUsuario = @IdPropietario
          AND NumeroCuenta = 'CR23015108410026012345'
    )
        INSERT INTO dbo.CuentasBancarias
        (
            IdUsuario,
            Banco,
            NumeroCuenta,
            TipoCuenta,
            Titular,
            EstadoValidacion,
            ValidadoPor,
            FechaValidacion,
            ObservacionesValidacion,
            Activa
        )
        VALUES
        (
            @IdPropietario,
            'Banco Nacional de Costa Rica',
            'CR23015108410026012345',
            'IBAN',
            'María Fernanda Rojas',
            'Validada',
            @IdAdmin,
            SYSDATETIME(),
            'Cuenta validada para ambiente de desarrollo',
            1
        );

    SELECT @IdCuentaBancaria = IdCuentaBancaria
    FROM dbo.CuentasBancarias
    WHERE IdUsuario = @IdPropietario
      AND NumeroCuenta = 'CR23015108410026012345';

    /* =========================================
       6. Configuración de pago vigente
       ========================================= */
    IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionesPago WHERE Version = 1)
        INSERT INTO dbo.ConfiguracionesPago
        (
            Version,
            NombreVersion,
            PrecioBasePorHectarea,
            TopePorcentajeAjuste,
            FechaVigenciaDesde,
            FechaVigenciaHasta,
            Activa,
            CreadoPor
        )
        VALUES
        (
            1,
            'Configuración Base Desarrollo',
            25000.00,
            30.00,
            DATEFROMPARTS(@AnioBase, 1, 1),
            NULL,
            1,
            @IdAdmin
        );

    SELECT @IdConfiguracionPago = IdConfiguracionPago
    FROM dbo.ConfiguracionesPago
    WHERE Version = 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.ConfiguracionPagoDetalle
        WHERE IdConfiguracionPago = @IdConfiguracionPago
          AND TipoFactor = 'Vegetacion'
          AND ValorFactor = 'Bosque Primario'
    )
        INSERT INTO dbo.ConfiguracionPagoDetalle (IdConfiguracionPago, TipoFactor, ValorFactor, PorcentajeAjuste)
        VALUES (@IdConfiguracionPago, 'Vegetacion', 'Bosque Primario', 20.00);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.ConfiguracionPagoDetalle
        WHERE IdConfiguracionPago = @IdConfiguracionPago
          AND TipoFactor = 'Vegetacion'
          AND ValorFactor = 'Bosque Secundario'
    )
        INSERT INTO dbo.ConfiguracionPagoDetalle (IdConfiguracionPago, TipoFactor, ValorFactor, PorcentajeAjuste)
        VALUES (@IdConfiguracionPago, 'Vegetacion', 'Bosque Secundario', 10.00);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.ConfiguracionPagoDetalle
        WHERE IdConfiguracionPago = @IdConfiguracionPago
          AND TipoFactor = 'RecursosHidricos'
          AND ValorFactor = 'Si'
    )
        INSERT INTO dbo.ConfiguracionPagoDetalle (IdConfiguracionPago, TipoFactor, ValorFactor, PorcentajeAjuste)
        VALUES (@IdConfiguracionPago, 'RecursosHidricos', 'Si', 5.00);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.ConfiguracionPagoDetalle
        WHERE IdConfiguracionPago = @IdConfiguracionPago
          AND TipoFactor = 'Pendiente'
          AND ValorFactor = 'Alta'
    )
        INSERT INTO dbo.ConfiguracionPagoDetalle (IdConfiguracionPago, TipoFactor, ValorFactor, PorcentajeAjuste)
        VALUES (@IdConfiguracionPago, 'Pendiente', 'Alta', 3.00);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.ConfiguracionPagoDetalle
        WHERE IdConfiguracionPago = @IdConfiguracionPago
          AND TipoFactor = 'UsoSuelo'
          AND ValorFactor = 'Conservacion'
    )
        INSERT INTO dbo.ConfiguracionPagoDetalle (IdConfiguracionPago, TipoFactor, ValorFactor, PorcentajeAjuste)
        VALUES (@IdConfiguracionPago, 'UsoSuelo', 'Conservacion', 7.00);

    /* =========================================
       7. Fincas de ejemplo
       ========================================= */
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Fincas
        WHERE IdPropietario = @IdPropietario
          AND NombreFinca = 'Finca La Esperanza'
    )
        INSERT INTO dbo.Fincas
        (
            IdPropietario,
            NombreFinca,
            Provincia,
            Canton,
            Distrito,
            DireccionExacta,
            Latitud,
            Longitud,
            Hectareas,
            Vegetacion,
            TieneRecursosHidricos,
            UsoSuelo,
            Pendiente,
            EstadoFinca
        )
        VALUES
        (
            @IdPropietario,
            'Finca La Esperanza',
            'San José',
            'Pérez Zeledón',
            'General',
            '800 metros al norte de la escuela local',
            9.3731200,
            -83.7045100,
            18.50,
            'Bosque Primario',
            1,
            'Conservacion',
            'Alta',
            'Aprobada'
        );

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Fincas
        WHERE IdPropietario = @IdPropietario
          AND NombreFinca = 'Finca Los Robles'
    )
        INSERT INTO dbo.Fincas
        (
            IdPropietario,
            NombreFinca,
            Provincia,
            Canton,
            Distrito,
            DireccionExacta,
            Latitud,
            Longitud,
            Hectareas,
            Vegetacion,
            TieneRecursosHidricos,
            UsoSuelo,
            Pendiente,
            EstadoFinca
        )
        VALUES
        (
            @IdPropietario,
            'Finca Los Robles',
            'Cartago',
            'Turrialba',
            'Santa Cruz',
            'Camino rural contiguo a naciente protegida',
            9.8965400,
            -83.6082300,
            12.75,
            'Bosque Secundario',
            0,
            'Conservacion',
            'Media',
            'Registrada'
        );

    SELECT @IdFincaAprobada = IdFinca
    FROM dbo.Fincas
    WHERE IdPropietario = @IdPropietario
      AND NombreFinca = 'Finca La Esperanza';

    SELECT @IdFincaPendiente = IdFinca
    FROM dbo.Fincas
    WHERE IdPropietario = @IdPropietario
      AND NombreFinca = 'Finca Los Robles';

    /* =========================================
       8. Evaluación técnica para finca aprobada
       ========================================= */
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.EvaluacionesTecnicas
        WHERE IdFinca = @IdFincaAprobada
          AND IdIngeniero = @IdIngeniero
    )
        INSERT INTO dbo.EvaluacionesTecnicas
        (
            IdFinca,
            IdIngeniero,
            EstadoEvaluacion,
            FechaVisita,
            Observaciones,
            DecisionTecnica,
            HectareasAjustadas,
            VegetacionAjustada,
            RecursosHidricosAjustado,
            UsoSueloAjustado,
            PendienteAjustada,
            FechaDecision
        )
        VALUES
        (
            @IdFincaAprobada,
            @IdIngeniero,
            'Finalizada',
            DATEFROMPARTS(@AnioBase, 2, 10),
            N'La finca presenta cobertura boscosa continua y condiciones favorables para PSA.',
            'Califica',
            18.50,
            'Bosque Primario',
            1,
            'Conservacion',
            'Alta',
            SYSDATETIME()
        );

    SELECT TOP (1) @IdEvaluacion = IdEvaluacion
    FROM dbo.EvaluacionesTecnicas
    WHERE IdFinca = @IdFincaAprobada
      AND IdIngeniero = @IdIngeniero
    ORDER BY IdEvaluacion;

    /* =========================================
       9. Plan de pago para finca aprobada
       ========================================= */
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.PlanesPago
        WHERE IdFinca = @IdFincaAprobada
          AND IdEvaluacion = @IdEvaluacion
          AND Anio = @AnioBase
    )
        INSERT INTO dbo.PlanesPago
        (
            IdFinca,
            IdEvaluacion,
            IdConfiguracionPago,
            IdCuentaBancaria,
            Anio,
            MontoBaseMensual,
            PorcentajeAjusteTotal,
            MontoMensualCalculado,
            EstadoPlan
        )
        VALUES
        (
            @IdFincaAprobada,
            @IdEvaluacion,
            @IdConfiguracionPago,
            @IdCuentaBancaria,
            @AnioBase,
            462500.00,
            30.00,
            601250.00,
            'Activo'
        );

    SELECT TOP (1) @IdPlanPago = IdPlanPago
    FROM dbo.PlanesPago
    WHERE IdFinca = @IdFincaAprobada
      AND IdEvaluacion = @IdEvaluacion
      AND Anio = @AnioBase
    ORDER BY IdPlanPago;

    /* =========================================
       10. Cuotas programadas del plan
       ========================================= */
    DECLARE @Mes INT = 1;
    WHILE @Mes <= 12
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM dbo.CuotasPago
            WHERE IdPlanPago = @IdPlanPago
              AND Mes = @Mes
        )
            INSERT INTO dbo.CuotasPago
            (
                IdPlanPago,
                Mes,
                FechaProgramada,
                MontoProgramado,
                MontoPendiente,
                EstadoCuota,
                FechaPago
            )
            VALUES
            (
                @IdPlanPago,
                @Mes,
                DATEFROMPARTS(@AnioBase, @Mes, 15),
                601250.00,
                CASE WHEN @Mes = 1 THEN 0 ELSE 601250.00 END,
                CASE WHEN @Mes = 1 THEN 'Pagada' ELSE 'Programada' END,
                CASE WHEN @Mes = 1 THEN DATEFROMPARTS(@AnioBase, 1, 15) ELSE NULL END
            );

        SET @Mes = @Mes + 1;
    END;

    /* =========================================
       11. Transacción de pago de ejemplo
       ========================================= */
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.TransaccionesPago
        WHERE IdPlanPago = @IdPlanPago
          AND ReferenciaExterna = 'TRX-SEED-0001'
    )
        INSERT INTO dbo.TransaccionesPago
        (
            IdPlanPago,
            MontoTotal,
            EstadoTransaccion,
            ReferenciaExterna,
            Observaciones
        )
        VALUES
        (
            @IdPlanPago,
            601250.00,
            'Procesada',
            'TRX-SEED-0001',
            'Transacción semilla para pruebas funcionales'
        );

    /* =========================================
       12. Auditoría de ejemplo
       ========================================= */
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.AuditoriaLog
        WHERE Modulo = 'Seguridad'
          AND TablaAfectada = 'Usuarios'
          AND Accion = 'INSERT'
          AND Detalle = 'Carga semilla de usuario administrador'
    )
        INSERT INTO dbo.AuditoriaLog
        (
            IdUsuario,
            Modulo,
            TablaAfectada,
            IdRegistroAfectado,
            Accion,
            ValorAnterior,
            ValorNuevo,
            IpOrigen,
            Detalle
        )
        VALUES
        (
            @IdAdmin,
            'Seguridad',
            'Usuarios',
            @IdAdmin,
            'INSERT',
            NULL,
            N'{"Email":"admin@psa.local","Rol":"Administrador"}',
            '127.0.0.1',
            'Carga semilla de usuario administrador'
        );

    /* =========================================
       13. Evidencias de finca
       ========================================= */
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.FincaEvidencias
        WHERE IdFinca = @IdFincaAprobada
          AND NombreArchivo = 'plano_finca_esperanza.pdf'
    )
        INSERT INTO dbo.FincaEvidencias
        (
            IdFinca,
            NombreArchivo,
            RutaArchivo,
            TipoArchivo,
            CargadoPor
        )
        VALUES
        (
            @IdFincaAprobada,
            N'plano_finca_esperanza.pdf',
            N'/seed/fincas/plano_finca_esperanza.pdf',
            'application/pdf',
            @IdPropietario
        );

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.EvaluacionEvidencias
        WHERE IdEvaluacion = @IdEvaluacion
          AND NombreArchivo = 'visita_tecnica_esperanza.jpg'
    )
        INSERT INTO dbo.EvaluacionEvidencias
        (
            IdEvaluacion,
            NombreArchivo,
            RutaArchivo,
            TipoArchivo,
            CargadoPor
        )
        VALUES
        (
            @IdEvaluacion,
            N'visita_tecnica_esperanza.jpg',
            N'/seed/evaluaciones/visita_tecnica_esperanza.jpg',
            'image/jpeg',
            @IdIngeniero
        );

    /* =========================================
       14. Notificaciones iniciales
       ========================================= */
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Notificaciones
        WHERE IdUsuario = @IdPropietario
          AND Titulo = 'Registro de finca completado'
          AND IdEntidadReferencia = @IdFincaAprobada
    )
        INSERT INTO dbo.Notificaciones
        (
            IdUsuario,
            Tipo,
            Titulo,
            Mensaje,
            Leida,
            EntidadReferencia,
            IdEntidadReferencia
        )
        VALUES
        (
            @IdPropietario,
            'Sistema',
            'Registro de finca completado',
            N'Se registró correctamente la finca Finca La Esperanza en el ambiente de desarrollo.',
            0,
            'Fincas',
            @IdFincaAprobada
        );

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Notificaciones
        WHERE IdUsuario = @IdIngeniero
          AND Titulo = 'Evaluación técnica disponible'
          AND IdEntidadReferencia = @IdEvaluacion
    )
        INSERT INTO dbo.Notificaciones
        (
            IdUsuario,
            Tipo,
            Titulo,
            Mensaje,
            Leida,
            EntidadReferencia,
            IdEntidadReferencia
        )
        VALUES
        (
            @IdIngeniero,
            'Tarea',
            'Evaluación técnica disponible',
            N'Se dejó cargada una evaluación finalizada para pruebas del módulo técnico.',
            0,
            'EvaluacionesTecnicas',
            @IdEvaluacion
        );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

/* =========================================
   Validaciones rápidas post-seed
   ========================================= */
SELECT 'Roles' AS Tabla, COUNT(*) AS Total FROM dbo.Roles
UNION ALL
SELECT 'Usuarios', COUNT(*) FROM dbo.Usuarios
UNION ALL
SELECT 'Fincas', COUNT(*) FROM dbo.Fincas
UNION ALL
SELECT 'EvaluacionesTecnicas', COUNT(*) FROM dbo.EvaluacionesTecnicas
UNION ALL
SELECT 'PlanesPago', COUNT(*) FROM dbo.PlanesPago
UNION ALL
SELECT 'CuotasPago', COUNT(*) FROM dbo.CuotasPago
UNION ALL
SELECT 'Notificaciones', COUNT(*) FROM dbo.Notificaciones;
GO

SELECT u.IdUsuario, u.NombreCompleto, u.Email, r.Nombre AS Rol, u.Estado
FROM dbo.Usuarios u
INNER JOIN dbo.Roles r ON r.IdRol = u.IdRol
ORDER BY u.IdUsuario;
GO

SELECT *
FROM dbo.vw_FincasMapa
ORDER BY IdFinca;
GO
