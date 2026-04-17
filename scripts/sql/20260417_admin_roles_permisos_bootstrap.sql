/*
    Script de compatibilidad para módulo de Administración (Roles y permisos)
    Ejecutar antes de iniciar la app en ambientes existentes.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* 1) Stored procedure: catálogo de permisos */
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

/* 2) Stored procedure: guardar permisos por rol (compatible RolPermisos/RolesPermisos) */
CREATE OR ALTER PROCEDURE dbo.usp_Admin_GuardarPermisosRol
    @IdRol INT,
    @CodigosPermisoCsv NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdRol IS NULL OR @IdRol <= 0
    BEGIN
        RAISERROR('Debe indicar un IdRol válido.', 16, 1);
        RETURN;
    END;

    IF OBJECT_ID('dbo.Permisos', 'U') IS NULL
    BEGIN
        RAISERROR('No existe la tabla dbo.Permisos.', 16, 1);
        RETURN;
    END;

    DECLARE @TablaRolPermisos SYSNAME = CASE
        WHEN OBJECT_ID('dbo.RolesPermisos', 'U') IS NOT NULL THEN 'dbo.RolesPermisos'
        WHEN OBJECT_ID('dbo.RolPermisos', 'U') IS NOT NULL THEN 'dbo.RolPermisos'
        ELSE NULL
    END;

    IF @TablaRolPermisos IS NULL
    BEGIN
        RAISERROR('No existe tabla de relación de roles/permisos (RolesPermisos o RolPermisos).', 16, 1);
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @SqlDelete NVARCHAR(MAX) = N'DELETE FROM ' + @TablaRolPermisos + N' WHERE IdRol = @IdRol;';
        EXEC sp_executesql @SqlDelete, N'@IdRol INT', @IdRol = @IdRol;

        IF OBJECT_ID('tempdb..#CodigosPermiso') IS NOT NULL
            DROP TABLE #CodigosPermiso;

        CREATE TABLE #CodigosPermiso (Codigo NVARCHAR(100) NOT NULL PRIMARY KEY);

        IF @CodigosPermisoCsv IS NOT NULL AND LEN(LTRIM(RTRIM(@CodigosPermisoCsv))) > 0
        BEGIN
            INSERT INTO #CodigosPermiso (Codigo)
            SELECT DISTINCT LTRIM(RTRIM(value))
            FROM STRING_SPLIT(@CodigosPermisoCsv, ',')
            WHERE LTRIM(RTRIM(value)) <> '';

            DECLARE @SqlInsert NVARCHAR(MAX) = N'
                INSERT INTO ' + @TablaRolPermisos + N' (IdRol, IdPermiso)
                SELECT @IdRol, p.IdPermiso
                FROM #CodigosPermiso c
                INNER JOIN dbo.Permisos p ON p.Codigo = c.Codigo;';

            EXEC sp_executesql @SqlInsert, N'@IdRol INT', @IdRol = @IdRol;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @MensajeError NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @SeveridadError INT = ERROR_SEVERITY();
        DECLARE @EstadoError INT = ERROR_STATE();
        RAISERROR(@MensajeError, @SeveridadError, @EstadoError);
    END CATCH;
END;
GO
