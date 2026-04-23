
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Administrador')
        INSERT INTO dbo.Roles (Nombre, Descripcion, Activo) VALUES ('Administrador', 'Acceso total al sistema', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Propietario')
        INSERT INTO dbo.Roles (Nombre, Descripcion, Activo) VALUES ('Propietario', 'Dueño de finca', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Ingeniero Forestal')
        INSERT INTO dbo.Roles (Nombre, Descripcion, Activo) VALUES ('Ingeniero Forestal', 'Evalúa fincas', 1);

    DECLARE @IdRolAdmin INT = (SELECT IdRol FROM dbo.Roles WHERE Nombre = 'Administrador');
    DECLARE @IdRolProp INT = (SELECT IdRol FROM dbo.Roles WHERE Nombre = 'Propietario');
    DECLARE @IdRolIng INT = (SELECT IdRol FROM dbo.Roles WHERE Nombre = 'Ingeniero Forestal');

    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = 'admin@psa.local')
        INSERT INTO dbo.Usuarios (NombreCompleto, Email, PasswordHash, IdRol, Estado)
        VALUES ('Admin PSA', 'admin@psa.local', CONVERT(VARCHAR(255), HASHBYTES('SHA2_256', 'Admin123!'), 2), @IdRolAdmin, 'Activo');

    DECLARE @i INT = 1;
    WHILE @i <= 3
    BEGIN
        DECLARE @emailIng VARCHAR(150) = CONCAT('ingeniero', FORMAT(@i,'00'), '@psa.local');
        IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = @emailIng)
            INSERT INTO dbo.Usuarios (NombreCompleto, Email, PasswordHash, IdRol, Estado)
            VALUES (CONCAT('Ingeniero ', @i), @emailIng, CONVERT(VARCHAR(255), HASHBYTES('SHA2_256', 'Ingeniero123!'), 2), @IdRolIng, 'Activo');
        SET @i += 1;
    END

    DECLARE @Ubicaciones TABLE
    (
        IdOrden INT IDENTITY(1,1) PRIMARY KEY,
        Provincia VARCHAR(100),
        Canton VARCHAR(100),
        Distrito VARCHAR(100),
        Latitud DECIMAL(10,7),
        Longitud DECIMAL(10,7)
    );

    INSERT INTO @Ubicaciones (Provincia, Canton, Distrito, Latitud, Longitud)
    VALUES
        ('San José', 'Pérez Zeledón', 'General', 9.3731200, -83.7045100),
        ('Cartago', 'Turrialba', 'Santa Cruz', 9.8965400, -83.6082300),
        ('Alajuela', 'San Carlos', 'Quesada', 10.3238100, -84.4271400),
        ('Puntarenas', 'Corredores', 'Corredor', 8.6402400, -82.9467600),
        ('Puntarenas', 'Buenos Aires', 'Buenos Aires', 9.1650300, -83.3341700),
        ('Guanacaste', 'Nandayure', 'Carmona', 9.8664000, -85.2506000),
        ('Limón', 'Talamanca', 'Bratsi', 9.6201400, -82.8701400),
        ('Heredia', 'Sarapiquí', 'Puerto Viejo', 10.4400100, -84.0021900);

    DECLARE @Nombres TABLE (Id INT IDENTITY(1,1) PRIMARY KEY, Nombre VARCHAR(80), Apellido1 VARCHAR(80), Apellido2 VARCHAR(80));
    INSERT INTO @Nombres (Nombre, Apellido1, Apellido2)
    VALUES
        ('Juan', 'Vargas', 'Mora'), ('Daniela', 'Solano', 'Rojas'), ('Andrés', 'Jiménez', 'Arce'),
        ('María', 'Quesada', 'Soto'), ('Fernando', 'Castro', 'Alfaro'), ('Adriana', 'Salazar', 'Pérez'),
        ('Carlos', 'Madrigal', 'Elizondo'), ('Paola', 'Ramírez', 'Chaves'), ('Luis', 'Cordero', 'Campos'),
        ('Sofía', 'Arias', 'Herrera'), ('Gerardo', 'Montero', 'Vega'), ('Melissa', 'Villalobos', 'Cruz'),
        ('Óscar', 'Murillo', 'Navarro'), ('Karla', 'León', 'Valverde'), ('José', 'Gamboa', 'Núñez'),
        ('Ana', 'Araya', 'Porras'), ('Diego', 'Blanco', 'Sánchez'), ('Valeria', 'Paniagua', 'Mora'),
        ('Esteban', 'Campos', 'Hidalgo'), ('Gabriela', 'Rivas', 'Mendoza');

    IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionesPago WHERE Version = 1)
        INSERT INTO dbo.ConfiguracionesPago (Version, NombreVersion, PrecioBasePorHectarea, TopePorcentajeAjuste, FechaVigenciaDesde, Activa, CreadoPor)
        VALUES (1, 'Configuración Base', 25000, 30, DATEFROMPARTS(YEAR(GETDATE()),1,1), 1, (SELECT TOP 1 IdUsuario FROM dbo.Usuarios WHERE IdRol = @IdRolAdmin));

    DECLARE @idConfig INT = (SELECT TOP 1 IdConfiguracionPago FROM dbo.ConfiguracionesPago WHERE Version = 1 ORDER BY IdConfiguracionPago DESC);

    SET @i = 1;
    WHILE @i <= 20
    BEGIN
        DECLARE @emailProp VARCHAR(150) = CONCAT('dueno', FORMAT(@i,'00'), '@psa.local');
        DECLARE @nombreProp VARCHAR(150);
        DECLARE @idProp INT;
        DECLARE @idIng INT;
        DECLARE @idFinca INT;
        DECLARE @idEval INT;
        DECLARE @idCuenta INT;
        DECLARE @j INT = 1;
        DECLARE @idxUbicacion INT = ((@i - 1) % 8) + 1;
        DECLARE @provincia VARCHAR(100);
        DECLARE @canton VARCHAR(100);
        DECLARE @distrito VARCHAR(100);
        DECLARE @latitudBase DECIMAL(10,7);
        DECLARE @longitudBase DECIMAL(10,7);
        DECLARE @direccionBase VARCHAR(250);

        SELECT
            @provincia = u.Provincia,
            @canton = u.Canton,
            @distrito = u.Distrito,
            @latitudBase = u.Latitud,
            @longitudBase = u.Longitud
        FROM @Ubicaciones u
        WHERE u.IdOrden = @idxUbicacion;

        SELECT @nombreProp = CONCAT(n.Nombre, ' ', n.Apellido1, ' ', n.Apellido2)
        FROM @Nombres n
        WHERE n.Id = @i;

        SET @direccionBase = CONCAT('De la escuela de ', @distrito, ' 400m norte y 150m este, ', @distrito, ', ', @canton);

        IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = @emailProp)
            INSERT INTO dbo.Usuarios (NombreCompleto, Email, PasswordHash, IdRol, Estado)
            VALUES (@nombreProp, @emailProp, CONVERT(VARCHAR(255), HASHBYTES('SHA2_256', 'Dueno123!'), 2), @IdRolProp, CASE WHEN @i % 10 = 0 THEN 'Inactivo' ELSE 'Activo' END);

        SELECT @idProp = IdUsuario FROM dbo.Usuarios WHERE Email = @emailProp;

        IF NOT EXISTS (SELECT 1 FROM dbo.CuentasBancarias WHERE IdUsuario = @idProp AND NumeroCuenta = CONCAT('CR', RIGHT(CONCAT('00000000000000000000', @i), 20)))
            INSERT INTO dbo.CuentasBancarias (IdUsuario, Banco, NumeroCuenta, TipoCuenta, Titular, EstadoValidacion, Activa)
            VALUES (@idProp, CASE WHEN @i % 2 = 0 THEN 'Banco Nacional' ELSE 'Banco de Costa Rica' END, CONCAT('CR', RIGHT(CONCAT('00000000000000000000', @i), 20)), 'IBAN', @nombreProp, 'Validada', 1);

        SELECT TOP 1 @idCuenta = IdCuentaBancaria FROM dbo.CuentasBancarias WHERE IdUsuario = @idProp ORDER BY IdCuentaBancaria DESC;

        WHILE @j <= 3
        BEGIN
            DECLARE @nombreFinca VARCHAR(150) = CONCAT('Finca ', PARSENAME(REPLACE(@nombreProp, ' ', '.'), 3), ' Lote ', @j);

            IF NOT EXISTS (SELECT 1 FROM dbo.Fincas WHERE IdPropietario = @idProp AND NombreFinca = @nombreFinca)
                INSERT INTO dbo.Fincas (IdPropietario, NombreFinca, Provincia, Canton, Distrito, DireccionExacta, Latitud, Longitud, Hectareas, Vegetacion, TieneRecursosHidricos, TieneRiosOQuebradas, TieneNacientes, CantidadNacientes, UsoSuelo, Pendiente, EstadoFinca)
                VALUES (
                    @idProp,
                    @nombreFinca,
                    @provincia,
                    @canton,
                    @distrito,
                    CONCAT(@direccionBase, ', lote ', @j),
                    @latitudBase + ((@i * 3 + @j) * 0.0001),
                    @longitudBase + ((@i * 3 + @j) * 0.0001),
                    8 + (@i % 7) + @j,
                    CASE WHEN @j = 1 THEN 'Bosque secundario' WHEN @j = 2 THEN 'Pasto' ELSE 'Plantación forestal' END,
                    CASE WHEN @j IN (1,3) THEN 1 ELSE 0 END,
                    CASE WHEN @j IN (1,3) THEN 1 ELSE 0 END,
                    CASE WHEN @j = 1 THEN 1 ELSE 0 END,
                    CASE WHEN @j = 1 THEN 2 ELSE 0 END,
                    CASE WHEN @j = 1 THEN 'Conservación' WHEN @j = 2 THEN 'Ganadería' ELSE 'Agroforestal' END,
                    CASE WHEN @j = 1 THEN 'Inclinada' WHEN @j = 2 THEN 'Plana' ELSE 'Muy inclinada' END,
                    CASE WHEN @j = 1 THEN 'Aprobada' WHEN @j = 2 THEN 'Registrada' ELSE 'En proceso' END
                );

            SELECT TOP 1 @idFinca = IdFinca FROM dbo.Fincas WHERE IdPropietario = @idProp AND NombreFinca = @nombreFinca ORDER BY IdFinca DESC;

            SELECT TOP 1 @idIng = IdUsuario FROM dbo.Usuarios WHERE IdRol = @IdRolIng ORDER BY ABS(CHECKSUM(NEWID()));

            IF NOT EXISTS (SELECT 1 FROM dbo.EvaluacionesTecnicas WHERE IdFinca = @idFinca)
            BEGIN
                INSERT INTO dbo.EvaluacionesTecnicas
                (IdFinca, IdIngeniero, EstadoEvaluacion, FechaVisita, Observaciones, DecisionTecnica, HectareasAjustadas, VegetacionAjustada, RecursosHidricosAjustado, UsoSueloAjustado, PendienteAjustada, FechaDecision)
                VALUES
                (
                    @idFinca,
                    CASE WHEN @j = 2 THEN NULL ELSE @idIng END,
                    CASE WHEN @j = 1 THEN 'Evaluada – Califica' WHEN @j = 2 THEN 'Pendiente' ELSE 'En proceso' END,
                    CASE WHEN @j IN (1,3) THEN DATEADD(DAY, -((@i * 2) + @j), CAST(GETDATE() AS DATE)) ELSE NULL END,
                    CONCAT('Evaluación semilla propietario ', @i, ', finca ', @j),
                    CASE WHEN @j = 1 THEN 'Califica' ELSE NULL END,
                    CASE WHEN @j = 1 THEN 8 + (@i % 7) + @j ELSE NULL END,
                    CASE WHEN @j = 1 THEN 'Bosque secundario' ELSE NULL END,
                    CASE WHEN @j = 1 THEN 1 ELSE NULL END,
                    CASE WHEN @j = 1 THEN 'Conservación' ELSE NULL END,
                    CASE WHEN @j = 1 THEN 'Inclinada' ELSE NULL END,
                    CASE WHEN @j = 1 THEN SYSDATETIME() ELSE NULL END
                );
            END

            SELECT TOP 1 @idEval = IdEvaluacion FROM dbo.EvaluacionesTecnicas WHERE IdFinca = @idFinca ORDER BY IdEvaluacion DESC;

            -- Finca 1 de cada propietario: evaluación calificada + plan pendiente por cuenta bancaria (sin cuenta asociada)
            IF @j = 1
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM dbo.PlanesPago WHERE IdFinca = @idFinca AND Anio = YEAR(GETDATE()))
                    INSERT INTO dbo.PlanesPago (IdFinca, IdEvaluacion, IdConfiguracionPago, IdCuentaBancaria, Anio, MontoBaseMensual, PorcentajeAjusteTotal, MontoMensualCalculado, EstadoPlan)
                    VALUES (@idFinca, @idEval, @idConfig, NULL, YEAR(GETDATE()), 250000, 18, 295000, 'PendienteDatosBancarios');

                DECLARE @idPlan INT = (SELECT TOP 1 IdPlanPago FROM dbo.PlanesPago WHERE IdFinca = @idFinca AND Anio = YEAR(GETDATE()) ORDER BY IdPlanPago DESC);
                DECLARE @mes INT = 1;
                WHILE @mes <= 12
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM dbo.CuotasPago WHERE IdPlanPago = @idPlan AND Mes = @mes)
                        INSERT INTO dbo.CuotasPago (IdPlanPago, Mes, FechaProgramada, MontoProgramado, MontoPendiente, EstadoCuota, FechaPago)
                        VALUES (@idPlan, @mes, DATEFROMPARTS(YEAR(GETDATE()), @mes, 15), 295000,
                                CASE WHEN @mes <= 3 THEN 0 ELSE 295000 END,
                                CASE WHEN @mes <= 3 THEN 'Ejecutada' ELSE 'Pendiente' END,
                                CASE WHEN @mes <= 3 THEN DATEFROMPARTS(YEAR(GETDATE()), @mes, 16) ELSE NULL END);
                    SET @mes += 1;
                END
            END

            SET @j += 1;
        END

        SET @i += 1;
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* =========================================
   Migración integrada: actualización de ubicaciones para datos semilla existentes
   ========================================= */
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @UbicacionesMigracion TABLE
    (
        IdOrden INT PRIMARY KEY,
        Provincia VARCHAR(100),
        Canton VARCHAR(100),
        Distrito VARCHAR(100),
        Latitud DECIMAL(10,7),
        Longitud DECIMAL(10,7)
    );

    INSERT INTO @UbicacionesMigracion (IdOrden, Provincia, Canton, Distrito, Latitud, Longitud)
    VALUES
        (1, 'San José', 'Pérez Zeledón', 'General', 9.3731200, -83.7045100),
        (2, 'Cartago', 'Turrialba', 'Santa Cruz', 9.8965400, -83.6082300),
        (3, 'Alajuela', 'San Carlos', 'Quesada', 10.3238100, -84.4271400),
        (4, 'Puntarenas', 'Corredores', 'Corredor', 8.6402400, -82.9467600),
        (5, 'Puntarenas', 'Buenos Aires', 'Buenos Aires', 9.1650300, -83.3341700),
        (6, 'Guanacaste', 'Nandayure', 'Carmona', 9.8664000, -85.2506000),
        (7, 'Limón', 'Talamanca', 'Bratsi', 9.6201400, -82.8701400),
        (8, 'Heredia', 'Sarapiquí', 'Puerto Viejo', 10.4400100, -84.0021900);

    ;WITH FincasSemilla AS
    (
        SELECT
            f.IdFinca,
            CAST(RIGHT(f.NombreFinca, 2) AS INT) AS NumeroFinca
        FROM dbo.Fincas f
        INNER JOIN dbo.Usuarios u ON u.IdUsuario = f.IdPropietario
        WHERE u.Email LIKE 'dueno__@psa.local'
          AND f.NombreFinca LIKE 'Finca __'
    )
    UPDATE f
       SET f.Provincia = u.Provincia,
           f.Canton = u.Canton,
           f.Distrito = u.Distrito,
           f.Latitud = u.Latitud + (fs.NumeroFinca * 0.001),
           f.Longitud = u.Longitud + (fs.NumeroFinca * 0.001)
    FROM dbo.Fincas f
    INNER JOIN FincasSemilla fs ON fs.IdFinca = f.IdFinca
    INNER JOIN @UbicacionesMigracion u ON u.IdOrden = ((fs.NumeroFinca - 1) % 8) + 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
