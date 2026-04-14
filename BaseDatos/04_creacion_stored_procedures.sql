USE PSA_CostaRica;
GO

/* Stored procedures base para el módulo de reportes */

CREATE OR ALTER PROCEDURE dbo.SP_ReporteDueno_MisFincas
    @IdPropietario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        f.IdFinca,
        f.NombreFinca,
        f.Provincia,
        f.Canton,
        f.Distrito,
        f.EstadoFinca,
        ISNULL(e.EstadoEvaluacion, 'Pendiente') AS EstadoEvaluacion,
        e.Observaciones
    FROM dbo.Fincas f
    OUTER APPLY (
        SELECT TOP 1 EstadoEvaluacion, Observaciones
        FROM dbo.EvaluacionesTecnicas
        WHERE IdFinca = f.IdFinca
        ORDER BY IdEvaluacion DESC
    ) e
    WHERE f.IdPropietario = @IdPropietario
    ORDER BY f.FechaRegistro DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_ReporteIngeniero_Pendientes
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.IdEvaluacion, e.IdFinca, f.NombreFinca, f.Provincia, f.Canton, f.Distrito, e.EstadoEvaluacion
    FROM dbo.EvaluacionesTecnicas e
    INNER JOIN dbo.Fincas f ON f.IdFinca = e.IdFinca
    WHERE e.EstadoEvaluacion = 'Pendiente'
    ORDER BY e.IdEvaluacion ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_ReporteAdmin_UsuariosRoles
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.IdUsuario, u.NombreCompleto, u.Email, r.Nombre AS Rol, u.Estado
    FROM dbo.Usuarios u
    INNER JOIN dbo.Roles r ON r.IdRol = u.IdRol
    ORDER BY u.Estado, r.Nombre, u.NombreCompleto;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_ReporteAdmin_FincasPorEstado
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EstadoFinca, COUNT(1) AS Cantidad
    FROM dbo.Fincas
    GROUP BY EstadoFinca
    ORDER BY EstadoFinca;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_ReporteAdmin_AuditoriaCritica
    @TopN INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@TopN)
        a.FechaAccion,
        a.Modulo,
        a.TablaAfectada,
        a.Accion,
        a.Detalle,
        ISNULL(u.NombreCompleto, 'Sistema') AS Usuario
    FROM dbo.AuditoriaLog a
    LEFT JOIN dbo.Usuarios u ON u.IdUsuario = a.IdUsuario
    ORDER BY a.FechaAccion DESC;
END;
GO
/*
    Migración: Encapsula lógica de registro y mantenimiento de propiedades (fincas) en Stored Procedures.
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

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.CatalogoFincaValores
        WHERE TipoCatalogo = 'Vegetacion'
          AND Valor = @Vegetacion
          AND Activo = 1
    )
        THROW 52007, 'El tipo de vegetación no es válido.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.CatalogoFincaValores
        WHERE TipoCatalogo = 'UsoSuelo'
          AND Valor = @UsoSuelo
          AND Activo = 1
    )
        THROW 52008, 'El uso de suelo no es válido.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.CatalogoFincaValores
        WHERE TipoCatalogo = 'Pendiente'
          AND Valor = @Pendiente
          AND Activo = 1
    )
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

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.CatalogoFincaValores
        WHERE TipoCatalogo = 'Vegetacion'
          AND Valor = @Vegetacion
          AND Activo = 1
    )
        THROW 52016, 'El tipo de vegetación no es válido.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.CatalogoFincaValores
        WHERE TipoCatalogo = 'UsoSuelo'
          AND Valor = @UsoSuelo
          AND Activo = 1
    )
        THROW 52017, 'El uso de suelo no es válido.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.CatalogoFincaValores
        WHERE TipoCatalogo = 'Pendiente'
          AND Valor = @Pendiente
          AND Activo = 1
    )
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

CREATE OR ALTER PROCEDURE dbo.SP_Fincas_Eliminar
    @IdFinca INT,
    @IdPropietario INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Fincas
    WHERE IdFinca = @IdFinca
      AND IdPropietario = @IdPropietario;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Fincas_CatalogoValores
    @TipoCatalogo VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Valor
    FROM dbo.CatalogoFincaValores
    WHERE TipoCatalogo = @TipoCatalogo
      AND Activo = 1
    ORDER BY OrdenVisual, Valor;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_FincaEvidencias_Crear
    @IdFinca INT,
    @NombreArchivo VARCHAR(200),
    @RutaArchivo VARCHAR(500),
    @TipoArchivo VARCHAR(50),
    @CargadoPor INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.FincaEvidencias
    (
        IdFinca,
        NombreArchivo,
        RutaArchivo,
        TipoArchivo,
        FechaCarga,
        CargadoPor
    )
    VALUES
    (
        @IdFinca,
        @NombreArchivo,
        @RutaArchivo,
        @TipoArchivo,
        SYSDATETIME(),
        @CargadoPor
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS IdEvidencia;
END;
GO
/*
    Migración: Centraliza lógica crítica de autenticación y landing pages en Stored Procedures.
    Fecha: 2026-04-12
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Auth_RegistrarUsuario
    @NombreCompleto NVARCHAR(150),
    @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(300),
    @IdRol INT,
    @Estado VARCHAR(20),
    @FechaCreacion DATETIME2,
    @UltimoAcceso DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Usuarios
    (
        NombreCompleto,
        Email,
        PasswordHash,
        IdRol,
        Estado,
        FechaCreacion,
        UltimoAcceso
    )
    VALUES
    (
        @NombreCompleto,
        @Email,
        @PasswordHash,
        @IdRol,
        @Estado,
        @FechaCreacion,
        @UltimoAcceso
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Auth_ObtenerUsuarioPorEmail
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        IdUsuario,
        NombreCompleto,
        Email,
        PasswordHash,
        IdRol,
        Estado,
        FechaCreacion,
        UltimoAcceso
    FROM dbo.Usuarios
    WHERE Email = @Email;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Auth_ObtenerUsuarioPorId
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        IdUsuario,
        NombreCompleto,
        Email,
        PasswordHash,
        IdRol,
        Estado,
        FechaCreacion,
        UltimoAcceso
    FROM dbo.Usuarios
    WHERE IdUsuario = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Auth_ExisteRol
    @IdRol INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 1 AS Existe
    FROM dbo.Roles
    WHERE IdRol = @IdRol;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Auth_ActualizarPasswordHashPorEmail
    @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuarios
    SET PasswordHash = @PasswordHash
    WHERE Email = @Email;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Auth_ActualizarUltimoAcceso
    @IdUsuario INT,
    @UltimoAcceso DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuarios
    SET UltimoAcceso = @UltimoAcceso
    WHERE IdUsuario = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Usuarios_AsignarRol
    @IdUsuario INT,
    @IdRol INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuarios
    SET IdRol = @IdRol
    WHERE IdUsuario = @IdUsuario;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Auth_InvalidarTokensActivos
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TokensRecuperacion
    SET Usado = 1,
        FechaUso = SYSDATETIME()
    WHERE IdUsuario = @IdUsuario
      AND Usado = 0
      AND FechaExpiracion > SYSDATETIME();
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Auth_CrearTokenRecuperacion
    @IdUsuario INT,
    @Token NVARCHAR(255),
    @FechaExpiracion DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.TokensRecuperacion (IdUsuario, Token, FechaExpiracion, Usado)
    VALUES (@IdUsuario, @Token, @FechaExpiracion, 0);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS IdToken;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Auth_ObtenerTokenVigente
    @Token NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        IdToken,
        IdUsuario,
        Token,
        FechaCreacion,
        FechaExpiracion,
        Usado,
        FechaUso
    FROM dbo.TokensRecuperacion
    WHERE Token = @Token
      AND Usado = 0
      AND FechaExpiracion > SYSDATETIME();
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Auth_MarcarTokenComoUsado
    @IdToken INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TokensRecuperacion
    SET Usado = 1,
        FechaUso = SYSDATETIME()
    WHERE IdToken = @IdToken;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Landing_ObtenerEquipo
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CAST(N'Equipo PSA Costa Rica' AS NVARCHAR(200)) AS Titulo,
        CAST(N'Aplicamos arquitectura por capas, SQL Server y seguridad de datos orientada a procesos forestales.' AS NVARCHAR(500)) AS Descripcion;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Landing_ObtenerProducto
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CAST(N'Producto PSA' AS NVARCHAR(200)) AS Titulo,
        CAST(N'Plataforma para gestión de fincas, evaluaciones técnicas, pagos y trazabilidad con auditoría.' AS NVARCHAR(500)) AS Descripcion;
END;
GO
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
