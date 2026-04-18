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

/* =========================================
   Mi perfil - consulta y actualización de usuario
   ========================================= */
CREATE OR ALTER PROCEDURE dbo.SP_Perfil_ObtenerMiPerfil
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        u.IdUsuario,
        u.NombreCompleto,
        u.Email,
        u.IdRol,
        r.Nombre AS RolNombre,
        u.Estado,
        u.FechaCreacion,
        u.UltimoAcceso
    FROM dbo.Usuarios u
    INNER JOIN dbo.Roles r ON r.IdRol = u.IdRol
    WHERE u.IdUsuario = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Perfil_ActualizarMiPerfil
    @IdUsuario INT,
    @NombreCompleto NVARCHAR(150),
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdUsuario <= 0
        THROW 53001, 'El identificador de usuario es inválido.', 1;

    IF LTRIM(RTRIM(ISNULL(@NombreCompleto, ''))) = ''
        THROW 53002, 'El nombre completo es obligatorio.', 1;

    IF LTRIM(RTRIM(ISNULL(@Email, ''))) = ''
        THROW 53003, 'El correo electrónico es obligatorio.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.Usuarios
        WHERE Email = @Email
          AND IdUsuario <> @IdUsuario
    )
        THROW 53004, 'El correo electrónico ya está en uso por otro usuario.', 1;

    UPDATE dbo.Usuarios
    SET NombreCompleto = @NombreCompleto,
        Email = @Email
    WHERE IdUsuario = @IdUsuario;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
GO

/* =========================================
   Pagos - Simulación/generación de plan e historial del dueño
   ========================================= */
CREATE OR ALTER PROCEDURE dbo.SP_Pagos_GenerarPlanPago
    @IdFinca INT,
    @Anio INT,
    @Simular BIT = 0,
    @Silencioso BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdFinca <= 0
    BEGIN
        IF @Silencioso = 1 RETURN;
        THROW 57001, 'La finca es obligatoria para generar el plan.', 1;
    END

    IF @Anio < YEAR(SYSDATETIME())
    BEGIN
        IF @Silencioso = 1 RETURN;
        THROW 57002, 'El año del plan debe ser actual o futuro.', 1;
    END

    DECLARE
        @IdEvaluacion INT,
        @IdPropietario INT,
        @IdConfiguracionPago INT,
        @IdCuentaBancaria INT,
        @HectareasAprobadas DECIMAL(12,2),
        @VegetacionFinal VARCHAR(100),
        @TieneRecursosHidricosFinal BIT,
        @CantidadNacientesFinal INT,
        @PendienteFinal VARCHAR(50),
        @PrecioBasePorHectarea DECIMAL(10,2),
        @TopePorcentajeAjuste DECIMAL(5,2),
        @PorcentajeVegetacion DECIMAL(5,2) = 0,
        @PorcentajeHidrico DECIMAL(5,2) = 0,
        @PorcentajeNacientes DECIMAL(5,2) = 0,
        @PorcentajePendiente DECIMAL(5,2) = 0,
        @PorcentajePorNaciente DECIMAL(5,2) = 0,
        @PorcentajeTotalAntesTope DECIMAL(8,2) = 0,
        @PorcentajeTotalAplicado DECIMAL(8,2) = 0,
        @MontoBaseMensual DECIMAL(12,2) = 0,
        @MontoAjusteMensual DECIMAL(12,2) = 0,
        @MontoFinalMensual DECIMAL(12,2) = 0,
        @IdPlanPago INT = NULL,
        @Mes INT = 1;

    SELECT TOP 1
        @IdPropietario = f.IdPropietario,
        @IdEvaluacion = e.IdEvaluacion,
        @HectareasAprobadas = COALESCE(e.HectareasAjustadas, f.Hectareas),
        @VegetacionFinal = COALESCE(NULLIF(e.VegetacionAjustada, ''), f.Vegetacion),
        @TieneRecursosHidricosFinal = COALESCE(e.RecursosHidricosAjustado, f.TieneRecursosHidricos),
        @CantidadNacientesFinal = COALESCE(f.CantidadNacientes, 0),
        @PendienteFinal = COALESCE(NULLIF(e.PendienteAjustada, ''), f.Pendiente)
    FROM dbo.Fincas f
    INNER JOIN dbo.EvaluacionesTecnicas e ON e.IdFinca = f.IdFinca
    WHERE f.IdFinca = @IdFinca
      AND f.EstadoFinca = 'Aprobada'
      AND e.DecisionTecnica = 'Califica'
      AND e.EstadoEvaluacion = 'Evaluada – Califica'
    ORDER BY e.IdEvaluacion DESC;

    IF @IdEvaluacion IS NULL
    BEGIN
        IF @Silencioso = 1 RETURN;
        THROW 57003, 'La finca debe estar aprobada y calificada para generar plan de pago.', 1;
    END

    SELECT TOP 1
        @IdCuentaBancaria = cb.IdCuentaBancaria
    FROM dbo.CuentasBancarias cb
    WHERE cb.IdUsuario = @IdPropietario
      AND cb.EstadoValidacion = 'Validada'
      AND cb.Activa = 1
    ORDER BY cb.FechaRegistro DESC, cb.IdCuentaBancaria DESC;

    SELECT TOP 1
        @IdConfiguracionPago = cp.IdConfiguracionPago,
        @PrecioBasePorHectarea = cp.PrecioBasePorHectarea,
        @TopePorcentajeAjuste = cp.TopePorcentajeAjuste
    FROM dbo.ConfiguracionesPago cp
    WHERE cp.Activa = 1
      AND cp.FechaVigenciaDesde <= DATEFROMPARTS(@Anio, 1, 1)
      AND (cp.FechaVigenciaHasta IS NULL OR cp.FechaVigenciaHasta >= DATEFROMPARTS(@Anio, 1, 1))
    ORDER BY cp.FechaVigenciaDesde DESC, cp.IdConfiguracionPago DESC;

    IF @IdConfiguracionPago IS NULL
    BEGIN
        IF @Silencioso = 1 RETURN;
        THROW 57005, 'No existe configuración de pago activa para el año solicitado.', 1;
    END

    SELECT @PorcentajeVegetacion = COALESCE(d.PorcentajeAjuste, 0)
    FROM dbo.ConfiguracionPagoDetalle d
    WHERE d.IdConfiguracionPago = @IdConfiguracionPago
      AND d.TipoFactor = 'Vegetacion'
      AND d.ValorFactor = @VegetacionFinal;

    SELECT @PorcentajeHidrico = CASE WHEN @TieneRecursosHidricosFinal = 1 THEN COALESCE(d.PorcentajeAjuste, 0) ELSE 0 END
    FROM dbo.ConfiguracionPagoDetalle d
    WHERE d.IdConfiguracionPago = @IdConfiguracionPago
      AND d.TipoFactor = 'RecursosHidricos'
      AND d.ValorFactor = 'Si';

    SELECT @PorcentajePorNaciente = COALESCE(d.PorcentajeAjuste, 0)
    FROM dbo.ConfiguracionPagoDetalle d
    WHERE d.IdConfiguracionPago = @IdConfiguracionPago
      AND d.TipoFactor = 'RecursosHidricos'
      AND d.ValorFactor = 'Naciente';

    SELECT @PorcentajePendiente = COALESCE(d.PorcentajeAjuste, 0)
    FROM dbo.ConfiguracionPagoDetalle d
    WHERE d.IdConfiguracionPago = @IdConfiguracionPago
      AND d.TipoFactor = 'Pendiente'
      AND d.ValorFactor = @PendienteFinal;

    SET @PorcentajeNacientes = COALESCE(@CantidadNacientesFinal, 0) * COALESCE(@PorcentajePorNaciente, 0);
    SET @PorcentajeTotalAntesTope = COALESCE(@PorcentajeVegetacion, 0) + COALESCE(@PorcentajeHidrico, 0) + COALESCE(@PorcentajeNacientes, 0) + COALESCE(@PorcentajePendiente, 0);
    SET @PorcentajeTotalAplicado = CASE WHEN @PorcentajeTotalAntesTope > @TopePorcentajeAjuste THEN @TopePorcentajeAjuste ELSE @PorcentajeTotalAntesTope END;

    SET @MontoBaseMensual = ROUND(COALESCE(@HectareasAprobadas, 0) * COALESCE(@PrecioBasePorHectarea, 0), 2);
    SET @MontoAjusteMensual = ROUND(@MontoBaseMensual * (@PorcentajeTotalAplicado / 100.0), 2);
    SET @MontoFinalMensual = ROUND(@MontoBaseMensual + @MontoAjusteMensual, 2);

    IF @Simular = 0
    BEGIN
        SELECT TOP 1
            @IdPlanPago = p.IdPlanPago
        FROM dbo.PlanesPago p
        WHERE p.IdFinca = @IdFinca
          AND p.Anio = @Anio
          AND p.EstadoPlan = 'Activo'
        ORDER BY p.IdPlanPago DESC;

        IF @IdPlanPago IS NULL
        BEGIN
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
                @IdFinca,
                @IdEvaluacion,
                @IdConfiguracionPago,
                @IdCuentaBancaria,
                @Anio,
                @MontoBaseMensual,
                @PorcentajeTotalAplicado,
                @MontoFinalMensual,
                'Activo'
            );

            SET @IdPlanPago = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.PlanesPago
            SET IdEvaluacion = @IdEvaluacion,
                IdConfiguracionPago = @IdConfiguracionPago,
                IdCuentaBancaria = @IdCuentaBancaria,
                MontoBaseMensual = @MontoBaseMensual,
                PorcentajeAjusteTotal = @PorcentajeTotalAplicado,
                MontoMensualCalculado = @MontoFinalMensual
            WHERE IdPlanPago = @IdPlanPago;
        END

        IF EXISTS (SELECT 1 FROM dbo.PlanesPagoDetalleCalculo WHERE IdPlanPago = @IdPlanPago)
        BEGIN
            UPDATE dbo.PlanesPagoDetalleCalculo
            SET HectareasAprobadas = @HectareasAprobadas,
                PrecioBasePorHectarea = @PrecioBasePorHectarea,
                PorcentajeVegetacion = @PorcentajeVegetacion,
                PorcentajeHidrico = @PorcentajeHidrico,
                PorcentajeNacientes = @PorcentajeNacientes,
                PorcentajePendiente = @PorcentajePendiente,
                PorcentajeTotalAntesTope = @PorcentajeTotalAntesTope,
                PorcentajeTopeAplicado = @TopePorcentajeAjuste,
                PorcentajeTotalAplicado = @PorcentajeTotalAplicado,
                MontoBaseMensual = @MontoBaseMensual,
                MontoAjusteMensual = @MontoAjusteMensual,
                MontoFinalMensual = @MontoFinalMensual,
                VegetacionFinal = @VegetacionFinal,
                TieneRecursosHidricosFinal = @TieneRecursosHidricosFinal,
                CantidadNacientesFinal = @CantidadNacientesFinal,
                PendienteFinal = @PendienteFinal
            WHERE IdPlanPago = @IdPlanPago;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.PlanesPagoDetalleCalculo
            (
                IdPlanPago,
                HectareasAprobadas,
                PrecioBasePorHectarea,
                PorcentajeVegetacion,
                PorcentajeHidrico,
                PorcentajeNacientes,
                PorcentajePendiente,
                PorcentajeTotalAntesTope,
                PorcentajeTopeAplicado,
                PorcentajeTotalAplicado,
                MontoBaseMensual,
                MontoAjusteMensual,
                MontoFinalMensual,
                VegetacionFinal,
                TieneRecursosHidricosFinal,
                CantidadNacientesFinal,
                PendienteFinal
            )
            VALUES
            (
                @IdPlanPago,
                @HectareasAprobadas,
                @PrecioBasePorHectarea,
                @PorcentajeVegetacion,
                @PorcentajeHidrico,
                @PorcentajeNacientes,
                @PorcentajePendiente,
                @PorcentajeTotalAntesTope,
                @TopePorcentajeAjuste,
                @PorcentajeTotalAplicado,
                @MontoBaseMensual,
                @MontoAjusteMensual,
                @MontoFinalMensual,
                @VegetacionFinal,
                @TieneRecursosHidricosFinal,
                @CantidadNacientesFinal,
                @PendienteFinal
            );
        END

        DELETE FROM dbo.CuotasPago WHERE IdPlanPago = @IdPlanPago;

        WHILE @Mes <= 12
        BEGIN
            INSERT INTO dbo.CuotasPago
            (
                IdPlanPago,
                Mes,
                FechaProgramada,
                MontoProgramado,
                MontoPendiente,
                EstadoCuota
            )
            VALUES
            (
                @IdPlanPago,
                @Mes,
                DATEFROMPARTS(@Anio, @Mes, 1),
                @MontoFinalMensual,
                @MontoFinalMensual,
                'Programada'
            );

            SET @Mes = @Mes + 1;
        END
    END

    SELECT
        ISNULL(@IdPlanPago, 0) AS IdPlanPago,
        @IdFinca AS IdFinca,
        f.NombreFinca,
        @Anio AS Anio,
        @IdConfiguracionPago AS IdConfiguracionPago,
        @IdCuentaBancaria AS IdCuentaBancaria,
        @MontoBaseMensual AS MontoBaseMensual,
        @PorcentajeTotalAplicado AS PorcentajeAjusteTotal,
        @MontoFinalMensual AS MontoMensualCalculado,
        CAST(CASE WHEN @Simular = 1 THEN 'Simulado' ELSE 'Activo' END AS VARCHAR(20)) AS EstadoPlan,
        SYSDATETIME() AS FechaGeneracion,
        @HectareasAprobadas AS HectareasAprobadas,
        @PrecioBasePorHectarea AS PrecioBasePorHectarea,
        @PorcentajeVegetacion AS PorcentajeVegetacion,
        @PorcentajeHidrico AS PorcentajeHidrico,
        @PorcentajeNacientes AS PorcentajeNacientes,
        @PorcentajePendiente AS PorcentajePendiente,
        @PorcentajeTotalAntesTope AS PorcentajeTotalAntesTope,
        @TopePorcentajeAjuste AS PorcentajeTopeAplicado,
        @PorcentajeTotalAplicado AS PorcentajeTotalAplicado,
        @MontoAjusteMensual AS MontoAjusteMensual,
        @MontoFinalMensual AS MontoFinalMensual,
        @VegetacionFinal AS VegetacionFinal,
        @TieneRecursosHidricosFinal AS TieneRecursosHidricosFinal,
        @CantidadNacientesFinal AS CantidadNacientesFinal,
        @PendienteFinal AS PendienteFinal
    FROM dbo.Fincas f
    WHERE f.IdFinca = @IdFinca;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Pagos_ObtenerHistorialDueno
    @IdPropietario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pp.IdPlanPago,
        cp.IdCuotaPago,
        pp.IdFinca,
        f.NombreFinca,
        pp.Anio,
        cp.Mes,
        cp.FechaProgramada,
        cp.MontoProgramado,
        cp.MontoPendiente,
        cp.EstadoCuota,
        cp.FechaPago
    FROM dbo.PlanesPago pp
    INNER JOIN dbo.CuotasPago cp ON cp.IdPlanPago = pp.IdPlanPago
    INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
    WHERE f.IdPropietario = @IdPropietario
    ORDER BY pp.Anio DESC, cp.Mes DESC, cp.IdCuotaPago DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Pagos_ObtenerPlanesDueno
    @IdPropietario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pp.IdPlanPago,
        pp.IdFinca,
        f.NombreFinca,
        pp.Anio,
        pp.MontoMensualCalculado,
        CAST(pp.MontoMensualCalculado * 12 AS DECIMAL(12,2)) AS MontoAnualEstimado,
        pp.EstadoPlan,
        pp.IdCuentaBancaria
    FROM dbo.PlanesPago pp
    INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
    WHERE f.IdPropietario = @IdPropietario
    ORDER BY pp.Anio DESC, pp.IdPlanPago DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Pagos_ObtenerCuentasBancariasDueno
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cb.IdCuentaBancaria,
        cb.Banco,
        cb.NumeroCuenta,
        cb.TipoCuenta,
        cb.Titular,
        cb.EstadoValidacion,
        cb.Activa,
        cb.FechaRegistro
    FROM dbo.CuentasBancarias cb
    WHERE cb.IdUsuario = @IdUsuario
    ORDER BY cb.FechaRegistro DESC, cb.IdCuentaBancaria DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Pagos_RegistrarCuentaBancariaDueno
    @IdUsuario INT,
    @Banco VARCHAR(100),
    @NumeroCuenta VARCHAR(50),
    @TipoCuenta VARCHAR(30),
    @Titular VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdUsuario <= 0
        THROW 57006, 'Debe indicar un usuario válido.', 1;

    IF LTRIM(RTRIM(ISNULL(@Banco, ''))) = ''
        THROW 57007, 'Debe indicar el banco.', 1;

    IF LTRIM(RTRIM(ISNULL(@NumeroCuenta, ''))) = ''
        THROW 57008, 'Debe indicar el número de cuenta.', 1;

    IF LTRIM(RTRIM(ISNULL(@TipoCuenta, ''))) = ''
        THROW 57009, 'Debe indicar el tipo de cuenta.', 1;

    IF LTRIM(RTRIM(ISNULL(@Titular, ''))) = ''
        THROW 57010, 'Debe indicar el titular de la cuenta.', 1;

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
        @IdUsuario,
        @Banco,
        @NumeroCuenta,
        @TipoCuenta,
        @Titular,
        'Pendiente',
        0
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS IdCuentaBancaria;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Pagos_AsociarCuentaPlan
    @IdPlanPago INT,
    @IdUsuario INT,
    @IdCuentaBancaria INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdPlanPago <= 0
        THROW 57011, 'Debe indicar un plan de pago válido.', 1;

    IF @IdUsuario <= 0
        THROW 57012, 'Debe indicar un usuario válido.', 1;

    IF @IdCuentaBancaria <= 0
        THROW 57013, 'Debe indicar una cuenta bancaria válida.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.CuentasBancarias cb
        WHERE cb.IdCuentaBancaria = @IdCuentaBancaria
          AND cb.IdUsuario = @IdUsuario
          AND cb.EstadoValidacion = 'Validada'
          AND cb.Activa = 1
    )
        THROW 57014, 'La cuenta bancaria no pertenece al dueño o no está validada/activa.', 1;

    UPDATE pp
    SET pp.IdCuentaBancaria = @IdCuentaBancaria
    FROM dbo.PlanesPago pp
    INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
    WHERE pp.IdPlanPago = @IdPlanPago
      AND pp.EstadoPlan = 'Activo'
      AND f.IdPropietario = @IdUsuario;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
GO
