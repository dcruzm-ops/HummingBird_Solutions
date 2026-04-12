USE PSA_CostaRica;
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

    SET @i = 1;
    WHILE @i <= 20
    BEGIN
        DECLARE @emailProp VARCHAR(150) = CONCAT('dueno', FORMAT(@i,'00'), '@psa.local');
        DECLARE @nombreProp VARCHAR(150) = CONCAT('Dueño Finca ', @i);
        DECLARE @idProp INT;
        DECLARE @idIng INT;
        DECLARE @idFinca INT;
        DECLARE @idEval INT;
        DECLARE @idCuenta INT;
        DECLARE @idConfig INT;
        DECLARE @idxUbicacion INT = ((@i - 1) % 8) + 1;
        DECLARE @provincia VARCHAR(100);
        DECLARE @canton VARCHAR(100);
        DECLARE @distrito VARCHAR(100);
        DECLARE @latitudBase DECIMAL(10,7);
        DECLARE @longitudBase DECIMAL(10,7);

        SELECT
            @provincia = Provincia,
            @canton = Canton,
            @distrito = Distrito,
            @latitudBase = Latitud,
            @longitudBase = Longitud
        FROM @Ubicaciones
        WHERE IdOrden = @idxUbicacion;

        IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = @emailProp)
            INSERT INTO dbo.Usuarios (NombreCompleto, Email, PasswordHash, IdRol, Estado)
            VALUES (@nombreProp, @emailProp, CONVERT(VARCHAR(255), HASHBYTES('SHA2_256', 'Dueno123!'), 2), @IdRolProp, CASE WHEN @i % 10 = 0 THEN 'Inactivo' ELSE 'Activo' END);

        SELECT @idProp = IdUsuario FROM dbo.Usuarios WHERE Email = @emailProp;

        IF NOT EXISTS (SELECT 1 FROM dbo.CuentasBancarias WHERE IdUsuario = @idProp AND NumeroCuenta = CONCAT('CR', RIGHT(CONCAT('00000000000000000000', @i), 20)))
            INSERT INTO dbo.CuentasBancarias (IdUsuario, Banco, NumeroCuenta, TipoCuenta, Titular, EstadoValidacion, Activa)
            VALUES (@idProp, 'Banco Nacional', CONCAT('CR', RIGHT(CONCAT('00000000000000000000', @i), 20)), 'IBAN', @nombreProp, 'Validada', 1);

        SELECT TOP 1 @idCuenta = IdCuentaBancaria FROM dbo.CuentasBancarias WHERE IdUsuario = @idProp ORDER BY IdCuentaBancaria DESC;

        IF NOT EXISTS (SELECT 1 FROM dbo.Fincas WHERE IdPropietario = @idProp)
            INSERT INTO dbo.Fincas (IdPropietario, NombreFinca, Provincia, Canton, Distrito, DireccionExacta, Latitud, Longitud, Hectareas, Vegetacion, TieneRecursosHidricos, TieneRiosOQuebradas, TieneNacientes, CantidadNacientes, UsoSuelo, Pendiente, EstadoFinca)
            VALUES (
                @idProp,
                CONCAT('Finca ', FORMAT(@i,'00')),
                @provincia,
                @canton,
                @distrito,
                CONCAT('Dirección finca ', @i),
                @latitudBase + (@i * 0.001),
                @longitudBase + (@i * 0.001),
                10 + @i,
                CASE WHEN @i % 4 = 0 THEN 'Bosque primario' WHEN @i % 4 = 1 THEN 'Bosque secundario' WHEN @i % 4 = 2 THEN 'Plantación forestal' ELSE 'Pasto' END,
                CASE WHEN @i % 2 = 0 THEN 1 ELSE 0 END,
                CASE WHEN @i % 2 = 0 THEN 1 ELSE 0 END,
                CASE WHEN @i % 3 = 0 THEN 1 ELSE 0 END,
                CASE WHEN @i % 3 = 0 THEN 2 ELSE 0 END,
                CASE WHEN @i % 5 = 0 THEN 'Ganadería' WHEN @i % 5 = 1 THEN 'Conservación' WHEN @i % 5 = 2 THEN 'Producción forestal' WHEN @i % 5 = 3 THEN 'Agroforestal' ELSE 'Mixto' END,
                CASE WHEN @i % 3 = 0 THEN 'Muy inclinada' WHEN @i % 3 = 1 THEN 'Inclinada' ELSE 'Plana' END,
                CASE WHEN @i % 4 = 0 THEN 'Aprobada' WHEN @i % 4 = 1 THEN 'Rechazada' WHEN @i % 4 = 2 THEN 'En proceso' ELSE 'Registrada' END
            );

        SELECT TOP 1 @idFinca = IdFinca FROM dbo.Fincas WHERE IdPropietario = @idProp ORDER BY IdFinca DESC;

        SELECT TOP 1 @idIng = IdUsuario FROM dbo.Usuarios WHERE IdRol = @IdRolIng ORDER BY ABS(CHECKSUM(NEWID()));

        IF NOT EXISTS (SELECT 1 FROM dbo.EvaluacionesTecnicas WHERE IdFinca = @idFinca)
            INSERT INTO dbo.EvaluacionesTecnicas
            (IdFinca, IdIngeniero, EstadoEvaluacion, FechaVisita, Observaciones, DecisionTecnica, HectareasAjustadas, VegetacionAjustada, RecursosHidricosAjustado, UsoSueloAjustado, PendienteAjustada, FechaDecision)
            VALUES
            (
                @idFinca,
                CASE WHEN @i % 4 = 3 THEN NULL ELSE @idIng END,
                CASE WHEN @i % 4 = 0 THEN 'Evaluada – Califica' WHEN @i % 4 = 1 THEN 'Evaluada – No califica' WHEN @i % 4 = 2 THEN 'En proceso' ELSE 'Pendiente' END,
                CASE WHEN @i % 4 IN (0,1,2) THEN DATEADD(DAY, -@i, CAST(GETDATE() AS DATE)) ELSE NULL END,
                CONCAT('Observación semilla finca ', @i),
                CASE WHEN @i % 4 = 0 THEN 'Califica' WHEN @i % 4 = 1 THEN 'No Califica' ELSE NULL END,
                CASE WHEN @i % 4 IN (0,1) THEN 10 + @i ELSE NULL END,
                CASE WHEN @i % 4 IN (0,1) THEN 'Bosque secundario' ELSE NULL END,
                CASE WHEN @i % 4 IN (0,1) THEN CASE WHEN @i % 2 = 0 THEN 1 ELSE 0 END ELSE NULL END,
                CASE WHEN @i % 4 IN (0,1) THEN 'Conservación' ELSE NULL END,
                CASE WHEN @i % 4 IN (0,1) THEN 'Inclinada' ELSE NULL END,
                CASE WHEN @i % 4 IN (0,1) THEN SYSDATETIME() ELSE NULL END
            );

        SELECT TOP 1 @idEval = IdEvaluacion FROM dbo.EvaluacionesTecnicas WHERE IdFinca = @idFinca ORDER BY IdEvaluacion DESC;

        IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionesPago WHERE Version = 1)
            INSERT INTO dbo.ConfiguracionesPago (Version, NombreVersion, PrecioBasePorHectarea, TopePorcentajeAjuste, FechaVigenciaDesde, Activa, CreadoPor)
            VALUES (1, 'Configuración Base', 25000, 30, DATEFROMPARTS(YEAR(GETDATE()),1,1), 1, (SELECT TOP 1 IdUsuario FROM dbo.Usuarios WHERE IdRol = @IdRolAdmin));

        SELECT @idConfig = IdConfiguracionPago FROM dbo.ConfiguracionesPago WHERE Version = 1;

        IF (@i % 2 = 0)
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.PlanesPago WHERE IdFinca = @idFinca AND Anio = YEAR(GETDATE()))
                INSERT INTO dbo.PlanesPago (IdFinca, IdEvaluacion, IdConfiguracionPago, IdCuentaBancaria, Anio, MontoBaseMensual, PorcentajeAjusteTotal, MontoMensualCalculado, EstadoPlan)
                VALUES (@idFinca, @idEval, @idConfig, @idCuenta, YEAR(GETDATE()), 300000, 15, 345000, 'Activo');

            DECLARE @idPlan INT = (SELECT TOP 1 IdPlanPago FROM dbo.PlanesPago WHERE IdFinca = @idFinca ORDER BY IdPlanPago DESC);
            DECLARE @mes INT = 1;
            WHILE @mes <= 12
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM dbo.CuotasPago WHERE IdPlanPago = @idPlan AND Mes = @mes)
                    INSERT INTO dbo.CuotasPago (IdPlanPago, Mes, FechaProgramada, MontoProgramado, MontoPendiente, EstadoCuota, FechaPago)
                    VALUES (@idPlan, @mes, DATEFROMPARTS(YEAR(GETDATE()), @mes, 15), 345000,
                            CASE WHEN @mes <= 4 THEN 0 ELSE 345000 END,
                            CASE WHEN @mes <= 4 THEN 'Pagada' ELSE 'Programada' END,
                            CASE WHEN @mes <= 4 THEN DATEFROMPARTS(YEAR(GETDATE()), @mes, 16) ELSE NULL END);
                SET @mes += 1;
            END
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
