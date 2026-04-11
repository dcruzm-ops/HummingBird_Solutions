USE PSA_CostaRica;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Ubicaciones TABLE
    (
        IdOrden INT PRIMARY KEY,
        Provincia VARCHAR(100),
        Canton VARCHAR(100),
        Distrito VARCHAR(100),
        Latitud DECIMAL(10,7),
        Longitud DECIMAL(10,7)
    );

    INSERT INTO @Ubicaciones (IdOrden, Provincia, Canton, Distrito, Latitud, Longitud)
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
    INNER JOIN @Ubicaciones u ON u.IdOrden = ((fs.NumeroFinca - 1) % 8) + 1;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.Fincas')
          AND name = 'CK_Fincas_Ubicacion_NoGenerica'
    )
    BEGIN
        ALTER TABLE dbo.Fincas
        ADD CONSTRAINT CK_Fincas_Ubicacion_NoGenerica
        CHECK (Canton NOT LIKE 'Canton %' AND Distrito NOT LIKE 'Distrito %');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
