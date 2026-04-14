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
