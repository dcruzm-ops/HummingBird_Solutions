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
       3. Usuarios semilla (20 en total)
       Distribución:
       - 3 administradores
       - 3 ingenieros forestales
       - 14 propietarios ficticios
       Password inicial para todos: 123456789
       ========================================= */
    DECLARE @PasswordSemilla NVARCHAR(500) = CONVERT(VARCHAR(255), HASHBYTES('SHA2_256', '123456789'), 2);

    DECLARE @UsuariosSemilla TABLE
    (
        NombreCompleto NVARCHAR(150) NOT NULL,
        Email NVARCHAR(150) NOT NULL,
        IdRol INT NOT NULL,
        Estado VARCHAR(20) NOT NULL,
        UltimoAcceso DATETIME2 NULL
    );

    INSERT INTO @UsuariosSemilla (NombreCompleto, Email, IdRol, Estado, UltimoAcceso)
    VALUES
        -- Administradores (3)
        (N'Administrador General PSA', N'admin@psa.local', @IdRolAdministrador, 'Activo', SYSDATETIME()),
        (N'Ana Lucía Quesada', N'admin2@psa.local', @IdRolAdministrador, 'Activo', NULL),
        (N'Roberto Brenes Solís', N'admin3@psa.local', @IdRolAdministrador, 'Activo', NULL),

        -- Ingenieros forestales (3)
        (N'Carlos Andrés Solano', N'ingeniero1@psa.local', @IdRolIngeniero, 'Activo', NULL),
        (N'Mariana Arce Villalobos', N'ingeniero2@psa.local', @IdRolIngeniero, 'Activo', NULL),
        (N'Esteban Jiménez Chacón', N'ingeniero3@psa.local', @IdRolIngeniero, 'Activo', NULL),

        -- Propietarios ficticios (14)
        (N'María Fernanda Rojas', N'propietario1@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'José Pablo Mora', N'propietario2@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Sofía Calderón Vega', N'propietario3@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Luis Diego Ureña', N'propietario4@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Karla Sánchez Campos', N'propietario5@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Andrés Chaves León', N'propietario6@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Paola Cordero Paniagua', N'propietario7@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Gilberto Navarro Arias', N'propietario8@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Natalia Herrera Segura', N'propietario9@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Daniel Álvarez Castro', N'propietario10@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Rebeca Salazar Arias', N'propietario11@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Miguel Zúñiga Quesada', N'propietario12@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Laura Fallas Rivas', N'propietario13@psa.local', @IdRolPropietario, 'Activo', NULL),
        (N'Jorge Mena Solano', N'propietario14@psa.local', @IdRolPropietario, 'Activo', NULL);

    INSERT INTO dbo.Usuarios (NombreCompleto, Email, PasswordHash, IdRol, Estado, UltimoAcceso)
    SELECT u.NombreCompleto, u.Email, @PasswordSemilla, u.IdRol, u.Estado, u.UltimoAcceso
    FROM @UsuariosSemilla u
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Usuarios existente
        WHERE existente.Email = u.Email
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

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.CuentasBancarias
        WHERE IdUsuario = @IdPropietario
          AND NumeroCuenta = 'CR15000000000000000001'
    )
        INSERT INTO dbo.CuentasBancarias
        (
            IdUsuario,
            Banco,
            NumeroCuenta,
            TipoCuenta,
            Titular,
            EstadoValidacion,
            Activa
        )
        VALUES
        (
            @IdPropietario,
            'Banco de Costa Rica',
            'CR15000000000000000001',
            'IBAN',
            'María Fernanda Rojas',
            'Pendiente',
            0
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
          AND ValorFactor = 'Muy inclinada'
    )
        INSERT INTO dbo.ConfiguracionPagoDetalle (IdConfiguracionPago, TipoFactor, ValorFactor, PorcentajeAjuste)
        VALUES (@IdConfiguracionPago, 'Pendiente', 'Muy inclinada', 3.00);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.ConfiguracionPagoDetalle
        WHERE IdConfiguracionPago = @IdConfiguracionPago
          AND TipoFactor = 'UsoSuelo'
          AND ValorFactor = 'Conservación'
    )
        INSERT INTO dbo.ConfiguracionPagoDetalle (IdConfiguracionPago, TipoFactor, ValorFactor, PorcentajeAjuste)
        VALUES (@IdConfiguracionPago, 'UsoSuelo', 'Conservación', 7.00);

    /* =========================================
       6.1 Catálogos configurables para finca
       ========================================= */
    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'Pendiente' AND Valor = 'Plana')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('Pendiente', 'Plana', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'Pendiente' AND Valor = 'Inclinada')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('Pendiente', 'Inclinada', 1, 2);

    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'Pendiente' AND Valor = 'Muy inclinada')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('Pendiente', 'Muy inclinada', 1, 3);

    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'Vegetacion' AND Valor = 'Bosque primario')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('Vegetacion', 'Bosque primario', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'Vegetacion' AND Valor = 'Bosque secundario')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('Vegetacion', 'Bosque secundario', 1, 2);

    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'Vegetacion' AND Valor = 'Plantación forestal')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('Vegetacion', 'Plantación forestal', 1, 3);

    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'Vegetacion' AND Valor = 'Pasto')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('Vegetacion', 'Pasto', 1, 4);

    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'UsoSuelo' AND Valor = 'Conservación')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('UsoSuelo', 'Conservación', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'UsoSuelo' AND Valor = 'Producción forestal')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('UsoSuelo', 'Producción forestal', 1, 2);

    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'UsoSuelo' AND Valor = 'Agroforestal')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('UsoSuelo', 'Agroforestal', 1, 3);

    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'UsoSuelo' AND Valor = 'Ganadería')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('UsoSuelo', 'Ganadería', 1, 4);

    IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoFincaValores WHERE TipoCatalogo = 'UsoSuelo' AND Valor = 'Uso mixto')
        INSERT INTO dbo.CatalogoFincaValores (TipoCatalogo, Valor, Activo, OrdenVisual)
        VALUES ('UsoSuelo', 'Uso mixto', 1, 5);

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
            TieneRiosOQuebradas,
            TieneNacientes,
            CantidadNacientes,
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
            'Bosque primario',
            1,
            1,
            1,
            2,
            'Conservación',
            'Muy inclinada',
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
            TieneRiosOQuebradas,
            TieneNacientes,
            CantidadNacientes,
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
            'Bosque secundario',
            0,
            0,
            0,
            0,
            'Conservación',
            'Inclinada',
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
       7.1 Fincas masivas para propietarios ficticios
       Regla:
       - cada propietario tiene 1 finca
       - propietarios pares tienen una segunda finca
       ========================================= */
    ;WITH Propietarios AS
    (
        SELECT
            u.IdUsuario,
            u.NombreCompleto,
            ROW_NUMBER() OVER (ORDER BY u.IdUsuario) AS NumeroPropietario
        FROM dbo.Usuarios u
        WHERE u.IdRol = @IdRolPropietario
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
        TieneRiosOQuebradas,
        TieneNacientes,
        CantidadNacientes,
        UsoSuelo,
        Pendiente,
        EstadoFinca
    )
    SELECT
        p.IdUsuario,
        CONCAT('Finca Modelo ', p.NumeroPropietario, '-', v.NumFinca) AS NombreFinca,
        CASE (p.NumeroPropietario % 7)
            WHEN 0 THEN 'San José'
            WHEN 1 THEN 'Alajuela'
            WHEN 2 THEN 'Cartago'
            WHEN 3 THEN 'Heredia'
            WHEN 4 THEN 'Guanacaste'
            WHEN 5 THEN 'Puntarenas'
            ELSE 'Limón'
        END AS Provincia,
        CONCAT('Cantón ', p.NumeroPropietario) AS Canton,
        CONCAT('Distrito ', v.NumFinca) AS Distrito,
        CONCAT('Referencia lote ', p.NumeroPropietario, '-', v.NumFinca) AS DireccionExacta,
        CAST(8.9500000 + (p.NumeroPropietario * 0.0300000) + (v.NumFinca * 0.0050000) AS DECIMAL(10,7)) AS Latitud,
        CAST(-84.3000000 + (p.NumeroPropietario * 0.0400000) + (v.NumFinca * 0.0030000) AS DECIMAL(10,7)) AS Longitud,
        CAST(6.50 + (p.NumeroPropietario * 1.20) + (v.NumFinca * 0.75) AS DECIMAL(10,2)) AS Hectareas,
        CASE ((p.NumeroPropietario + v.NumFinca) % 4)
            WHEN 0 THEN 'Bosque primario'
            WHEN 1 THEN 'Bosque secundario'
            WHEN 2 THEN 'Plantación forestal'
            ELSE 'Pasto'
        END AS Vegetacion,
        CASE WHEN (p.NumeroPropietario % 2) = 0 THEN 1 ELSE 0 END AS TieneRecursosHidricos,
        CASE WHEN (p.NumeroPropietario % 3) = 0 THEN 1 ELSE 0 END AS TieneRiosOQuebradas,
        CASE WHEN (p.NumeroPropietario % 4) = 0 THEN 1 ELSE 0 END AS TieneNacientes,
        CASE WHEN (p.NumeroPropietario % 4) = 0 THEN 1 ELSE 0 END AS CantidadNacientes,
        CASE ((p.NumeroPropietario + v.NumFinca) % 5)
            WHEN 0 THEN 'Conservación'
            WHEN 1 THEN 'Producción forestal'
            WHEN 2 THEN 'Agroforestal'
            WHEN 3 THEN 'Ganadería'
            ELSE 'Uso mixto'
        END AS UsoSuelo,
        CASE ((p.NumeroPropietario + v.NumFinca) % 3)
            WHEN 0 THEN 'Plana'
            WHEN 1 THEN 'Inclinada'
            ELSE 'Muy inclinada'
        END AS Pendiente,
        CASE ((p.NumeroPropietario + v.NumFinca) % 4)
            WHEN 0 THEN 'Aprobada'
            WHEN 1 THEN 'EnRevision'
            WHEN 2 THEN 'Registrada'
            ELSE 'Registrada'
        END AS EstadoFinca
    FROM Propietarios p
    CROSS APPLY
    (
        SELECT 1 AS NumFinca
        UNION ALL
        SELECT 2
        WHERE (p.NumeroPropietario % 2) = 0
    ) v
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Fincas f
        WHERE f.IdPropietario = p.IdUsuario
          AND f.NombreFinca = CONCAT('Finca Modelo ', p.NumeroPropietario, '-', v.NumFinca)
    );

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
            'Bosque primario',
            1,
            'Conservación',
            'Muy inclinada',
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

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.AuditoriaLog
        WHERE Modulo = 'Usuarios'
          AND TablaAfectada = 'Usuarios'
          AND Accion = 'UPDATE'
          AND IdRegistroAfectado = @IdPropietario
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
            'Usuarios',
            'Usuarios',
            @IdPropietario,
            'UPDATE',
            N'{"Estado":"Inactivo"}',
            N'{"Estado":"Activo"}',
            '127.0.0.1',
            'Activación de usuario propietario'
        );

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.AuditoriaLog
        WHERE Modulo = 'CuentasBancarias'
          AND TablaAfectada = 'CuentasBancarias'
          AND Accion = 'UPDATE'
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
            'CuentasBancarias',
            'CuentasBancarias',
            @IdCuentaBancaria,
            'UPDATE',
            N'{"EstadoValidacion":"Pendiente"}',
            N'{"EstadoValidacion":"Validada"}',
            '127.0.0.1',
            'Aprobación de cuenta bancaria semilla'
        );

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.AuditoriaLog
        WHERE Modulo = 'Fincas'
          AND TablaAfectada = 'Fincas'
          AND Accion = 'INSERT'
          AND IdRegistroAfectado = @IdFincaPendiente
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
            @IdPropietario,
            'Fincas',
            'Fincas',
            @IdFincaPendiente,
            'INSERT',
            NULL,
            N'{"EstadoFinca":"Registrada","NombreFinca":"Finca Bosque Azul"}',
            '127.0.0.1',
            'Registro de finca pendiente en semilla'
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

SELECT
    r.Nombre AS Rol,
    COUNT(*) AS TotalUsuarios
FROM dbo.Usuarios u
INNER JOIN dbo.Roles r ON r.IdRol = u.IdRol
GROUP BY r.Nombre
ORDER BY r.Nombre;
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

SELECT * FROM dbo.Roles
GO

SELECT * FROM dbo.Usuarios
GO
