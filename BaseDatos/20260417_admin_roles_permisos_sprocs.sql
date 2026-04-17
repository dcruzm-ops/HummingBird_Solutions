/*
    Script: 20260417_admin_roles_permisos_sprocs.sql
    Objetivo:
    - Mejorar eficiencia de lectura/escritura de permisos de roles.
    - Mantener compatibilidad con la implementación actual del DAO.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Admin_ObtenerPermisos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.IdPermiso,
        p.Codigo,
        p.Nombre,
        p.Descripcion
    FROM dbo.Permisos p
    ORDER BY p.Codigo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Admin_GuardarPermisosRol
    @IdRol int,
    @CodigosPermisoCsv nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;

    IF (@IdRol IS NULL OR @IdRol <= 0)
    BEGIN
        THROW 50001, 'El IdRol debe ser mayor a cero.', 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.RolPermisos
        WHERE IdRol = @IdRol;

        ;WITH Codigos AS
        (
            SELECT DISTINCT LTRIM(RTRIM(value)) AS Codigo
            FROM STRING_SPLIT(COALESCE(@CodigosPermisoCsv, ''), ',')
            WHERE LTRIM(RTRIM(value)) <> ''
        )
        INSERT INTO dbo.RolPermisos (IdRol, IdPermiso)
        SELECT @IdRol, p.IdPermiso
        FROM Codigos c
        INNER JOIN dbo.Permisos p ON p.Codigo = c.Codigo;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO
