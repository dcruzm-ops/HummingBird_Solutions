/*
    Fix: robustecer validación de catálogo en SP de fincas para evitar caídas por catálogo no sembrado.
    Fecha: 2026-04-12
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Fincas_Registrar
    @IdPropietario INT,
    @NombreFinca VARCHAR(150),
    @Provincia VARCHAR(100),
    @Canton VARCHAR(100),
    @Distrito VARCHAR(100),
    @DireccionExacta VARCHAR(250) = NULL,
    @Latitud DECIMAL(9,6),
    @Longitud DECIMAL(9,6),
    @Hectareas DECIMAL(12,2),
    @Vegetacion VARCHAR(100),
    @TieneRecursosHidricos BIT,
    @TieneRiosOQuebradas BIT,
    @TieneNacientes BIT,
    @CantidadNacientes INT,
    @UsoSuelo VARCHAR(100),
    @Pendiente VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdPropietario <= 0
        THROW 52001, 'El propietario es inválido.', 1;

    IF @Hectareas <= 0
        THROW 52002, 'El tamaño en hectáreas debe ser mayor a 0.', 1;

    IF @Latitud < -90 OR @Latitud > 90
        THROW 52003, 'La latitud debe estar entre -90 y 90.', 1;

    IF @Longitud < -180 OR @Longitud > 180
        THROW 52004, 'La longitud debe estar entre -180 y 180.', 1;

    IF @TieneNacientes = 0
        SET @CantidadNacientes = 0;

    IF @TieneNacientes = 1 AND @CantidadNacientes <= 0
        THROW 52005, 'Debe indicar una cantidad de nacientes mayor a 0.', 1;

    IF @TieneNacientes = 0 AND @CantidadNacientes <> 0
        THROW 52006, 'La cantidad de nacientes debe ser 0 cuando no posee nacientes.', 1;

    IF @Vegetacion NOT IN ('Bosque primario', 'Bosque secundario', 'Plantación forestal', 'Pasto')
        THROW 52007, 'El tipo de vegetación no es válido.', 1;

    IF @UsoSuelo NOT IN ('Conservación', 'Producción forestal', 'Agroforestal', 'Ganadería', 'Mixto')
        THROW 52008, 'El uso de suelo no es válido.', 1;

    IF @Pendiente NOT IN ('Plana', 'Inclinada', 'Muy inclinada')
        THROW 52009, 'El tipo de superficie (pendiente) no es válido.', 1;

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
        @NombreFinca,
        @Provincia,
        @Canton,
        @Distrito,
        @DireccionExacta,
        @Latitud,
        @Longitud,
        @Hectareas,
        @Vegetacion,
        @TieneRecursosHidricos,
        @TieneRiosOQuebradas,
        @TieneNacientes,
        @CantidadNacientes,
        @UsoSuelo,
        @Pendiente,
        'Registrada'
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS IdFinca;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Fincas_Actualizar
    @IdFinca INT,
    @IdPropietario INT,
    @NombreFinca VARCHAR(150),
    @Provincia VARCHAR(100),
    @Canton VARCHAR(100),
    @Distrito VARCHAR(100),
    @DireccionExacta VARCHAR(250) = NULL,
    @Latitud DECIMAL(9,6),
    @Longitud DECIMAL(9,6),
    @Hectareas DECIMAL(12,2),
    @Vegetacion VARCHAR(100),
    @TieneRecursosHidricos BIT,
    @TieneRiosOQuebradas BIT,
    @TieneNacientes BIT,
    @CantidadNacientes INT,
    @UsoSuelo VARCHAR(100),
    @Pendiente VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdFinca <= 0 OR @IdPropietario <= 0
        THROW 52010, 'Los identificadores de finca y propietario son obligatorios.', 1;

    IF @Hectareas <= 0
        THROW 52011, 'El tamaño en hectáreas debe ser mayor a 0.', 1;

    IF @Latitud < -90 OR @Latitud > 90
        THROW 52012, 'La latitud debe estar entre -90 y 90.', 1;

    IF @Longitud < -180 OR @Longitud > 180
        THROW 52013, 'La longitud debe estar entre -180 y 180.', 1;

    IF @TieneNacientes = 0
        SET @CantidadNacientes = 0;

    IF @TieneNacientes = 1 AND @CantidadNacientes <= 0
        THROW 52014, 'Debe indicar una cantidad de nacientes mayor a 0.', 1;

    IF @TieneNacientes = 0 AND @CantidadNacientes <> 0
        THROW 52015, 'La cantidad de nacientes debe ser 0 cuando no posee nacientes.', 1;

    IF @Vegetacion NOT IN ('Bosque primario', 'Bosque secundario', 'Plantación forestal', 'Pasto')
        THROW 52016, 'El tipo de vegetación no es válido.', 1;

    IF @UsoSuelo NOT IN ('Conservación', 'Producción forestal', 'Agroforestal', 'Ganadería', 'Mixto')
        THROW 52017, 'El uso de suelo no es válido.', 1;

    IF @Pendiente NOT IN ('Plana', 'Inclinada', 'Muy inclinada')
        THROW 52018, 'El tipo de superficie (pendiente) no es válido.', 1;

    UPDATE dbo.Fincas
    SET NombreFinca = @NombreFinca,
        Provincia = @Provincia,
        Canton = @Canton,
        Distrito = @Distrito,
        DireccionExacta = @DireccionExacta,
        Latitud = @Latitud,
        Longitud = @Longitud,
        Hectareas = @Hectareas,
        Vegetacion = @Vegetacion,
        TieneRecursosHidricos = @TieneRecursosHidricos,
        TieneRiosOQuebradas = @TieneRiosOQuebradas,
        TieneNacientes = @TieneNacientes,
        CantidadNacientes = @CantidadNacientes,
        UsoSuelo = @UsoSuelo,
        Pendiente = @Pendiente,
        FechaActualizacion = SYSDATETIME()
    WHERE IdFinca = @IdFinca
      AND IdPropietario = @IdPropietario;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
GO
