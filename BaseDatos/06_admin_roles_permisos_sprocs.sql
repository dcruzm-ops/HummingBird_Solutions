/*
    Script: 20260417_admin_roles_permisos_sprocs.sql
    Objetivo:
    - Mejorar eficiencia de lectura/escritura de permisos de roles.
    - Mantener compatibilidad con la implementación actual del DAO.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/*
    Bootstrap incremental:
    En algunos ambientes la BD fue creada sin catálogo de permisos.
    Este bloque evita que falle el script 06 y deja la base lista para script 07.
*/
IF OBJECT_ID('dbo.Permisos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permisos
    (
        IdPermiso     INT IDENTITY(1,1) NOT NULL,
        Codigo        NVARCHAR(100) NOT NULL,
        Nombre        NVARCHAR(150) NOT NULL,
        Descripcion   NVARCHAR(300) NULL,
        Activo        BIT NOT NULL CONSTRAINT DF_Permisos_Activo DEFAULT (1),
        CONSTRAINT PK_Permisos PRIMARY KEY (IdPermiso),
        CONSTRAINT UQ_Permisos_Codigo UNIQUE (Codigo)
    );
END
GO

IF OBJECT_ID('dbo.RolesPermisos', 'U') IS NULL
   AND OBJECT_ID('dbo.RolPermisos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolesPermisos
    (
        IdRol       INT NOT NULL,
        IdPermiso   INT NOT NULL,
        CONSTRAINT PK_RolesPermisos PRIMARY KEY (IdRol, IdPermiso),
        CONSTRAINT FK_RolesPermisos_Roles FOREIGN KEY (IdRol) REFERENCES dbo.Roles(IdRol),
        CONSTRAINT FK_RolesPermisos_Permisos FOREIGN KEY (IdPermiso) REFERENCES dbo.Permisos(IdPermiso)
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Admin_ObtenerPermisos
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID('dbo.Permisos', 'U') IS NULL
    BEGIN
        SELECT
            CAST(NULL AS INT) AS IdPermiso,
            CAST(NULL AS NVARCHAR(100)) AS Codigo,
            CAST(NULL AS NVARCHAR(150)) AS Nombre,
            CAST(NULL AS NVARCHAR(300)) AS Descripcion
        WHERE 1 = 0;
        RETURN;
    END

    EXEC sys.sp_executesql N'
        SELECT
            p.IdPermiso,
            p.Codigo,
            p.Nombre,
            p.Descripcion
        FROM dbo.Permisos p
        ORDER BY p.Codigo;';
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
        RAISERROR('El IdRol debe ser mayor a cero.', 16, 1);
        RETURN;
    END

    DECLARE @TablaRolPermisos SYSNAME = CASE
        WHEN OBJECT_ID('dbo.RolesPermisos', 'U') IS NOT NULL THEN 'dbo.RolesPermisos'
        WHEN OBJECT_ID('dbo.RolPermisos', 'U') IS NOT NULL THEN 'dbo.RolPermisos'
        ELSE NULL
    END;

    IF @TablaRolPermisos IS NULL
    BEGIN
        RAISERROR('No existe tabla de relación de roles/permisos (RolesPermisos o RolPermisos).', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @SqlDelete nvarchar(max) = N'DELETE FROM ' + @TablaRolPermisos + N' WHERE IdRol = @IdRol;';
        EXEC sp_executesql @SqlDelete, N'@IdRol int', @IdRol = @IdRol;

        ;WITH Codigos AS
        (
            SELECT DISTINCT LTRIM(RTRIM(value)) AS Codigo
            FROM STRING_SPLIT(COALESCE(@CodigosPermisoCsv, ''), ',')
            WHERE LTRIM(RTRIM(value)) <> ''
        )
        SELECT c.Codigo
        INTO #CodigosPermiso
        FROM Codigos c;

        DECLARE @SqlInsert nvarchar(max) = N'
            INSERT INTO ' + @TablaRolPermisos + N' (IdRol, IdPermiso)
            SELECT @IdRol, p.IdPermiso
            FROM #CodigosPermiso c
            INNER JOIN dbo.Permisos p ON p.Codigo = c.Codigo;';
        EXEC sp_executesql @SqlInsert, N'@IdRol int', @IdRol = @IdRol;

        DROP TABLE #CodigosPermiso;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @MensajeError NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @SeveridadError INT = ERROR_SEVERITY();
        DECLARE @EstadoError INT = ERROR_STATE();
        RAISERROR(@MensajeError, @SeveridadError, @EstadoError);
    END CATCH
END;
GO
