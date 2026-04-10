/*
  Ajuste ArregloArqui2
  Objetivo:
  - Garantizar que IdIngeniero permita NULL en EvaluacionesTecnicas
    para crear evaluaciones pendientes sin ingeniero asignado.
*/

USE PSA_CostaRica;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'IdIngeniero') IS NOT NULL
    BEGIN
        DECLARE @isNullable BIT;
        DECLARE @fkName SYSNAME;

        SELECT @isNullable = c.is_nullable
        FROM sys.columns c
        INNER JOIN sys.tables t ON t.object_id = c.object_id
        WHERE t.name = 'EvaluacionesTecnicas'
          AND c.name = 'IdIngeniero';

        IF (@isNullable = 0)
        BEGIN
            SELECT TOP 1 @fkName = fk.name
            FROM sys.foreign_keys fk
            WHERE fk.parent_object_id = OBJECT_ID('dbo.EvaluacionesTecnicas')
              AND fk.referenced_object_id = OBJECT_ID('dbo.Usuarios');

            IF @fkName IS NOT NULL
            BEGIN
                EXEC ('ALTER TABLE dbo.EvaluacionesTecnicas DROP CONSTRAINT ' + QUOTENAME(@fkName));
            END

            ALTER TABLE dbo.EvaluacionesTecnicas
                ALTER COLUMN IdIngeniero INT NULL;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.foreign_keys
                WHERE name = 'FK_Evaluaciones_Usuarios_Ingeniero'
                  AND parent_object_id = OBJECT_ID('dbo.EvaluacionesTecnicas')
            )
            BEGIN
                ALTER TABLE dbo.EvaluacionesTecnicas
                    ADD CONSTRAINT FK_Evaluaciones_Usuarios_Ingeniero
                    FOREIGN KEY (IdIngeniero) REFERENCES dbo.Usuarios(IdUsuario);
            END
        END
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
