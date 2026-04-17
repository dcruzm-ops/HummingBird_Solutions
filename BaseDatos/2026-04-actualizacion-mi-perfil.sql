/*
    Script de actualización puntual - Mi perfil
    Fecha: 2026-04-14
    Objetivo:
    - Crear/actualizar stored procedures para consultar y editar datos del perfil de usuario.
*/

USE PSA_CostaRica;
GO

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
