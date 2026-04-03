/*
  Migración de compatibilidad ISSUE-25 / ISSUE-29
  Objetivo:
  - Permitir flujo de evaluación técnica extendido en app.
  - Evitar ruptura por constraints antiguos.
*/

USE PSA_CostaRica;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /* 1) IdIngeniero nullable para permitir estado Pendiente sin asignación */
    IF COL_LENGTH('dbo.EvaluacionesTecnicas', 'IdIngeniero') IS NOT NULL
    BEGIN
        DECLARE @isNullable BIT;
        SELECT @isNullable = CASE WHEN c.is_nullable = 1 THEN 1 ELSE 0 END
        FROM sys.columns c
        INNER JOIN sys.tables t ON t.object_id = c.object_id
        WHERE t.name = 'EvaluacionesTecnicas' AND c.name = 'IdIngeniero';

        IF (@isNullable = 0)
        BEGIN
            ALTER TABLE dbo.EvaluacionesTecnicas DROP CONSTRAINT FK_Evaluaciones_Usuarios_Ingeniero;
            ALTER TABLE dbo.EvaluacionesTecnicas ALTER COLUMN IdIngeniero INT NULL;
            ALTER TABLE dbo.EvaluacionesTecnicas
                ADD CONSTRAINT FK_Evaluaciones_Usuarios_Ingeniero
                FOREIGN KEY (IdIngeniero) REFERENCES dbo.Usuarios(IdUsuario);
        END
    END

    /* 2) Expandir estados de evaluación para nuevo flujo (manteniendo legado Finalizada) */
    IF EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE name = 'CK_Evaluaciones_Estado'
          AND parent_object_id = OBJECT_ID('dbo.EvaluacionesTecnicas')
    )
    BEGIN
        ALTER TABLE dbo.EvaluacionesTecnicas DROP CONSTRAINT CK_Evaluaciones_Estado;
    END

    ALTER TABLE dbo.EvaluacionesTecnicas
        ADD CONSTRAINT CK_Evaluaciones_Estado CHECK (
            EstadoEvaluacion IN (
                'Pendiente',
                'En Proceso',
                'Finalizada',
                'En proceso',
                'Evaluada – No califica',
                'Evaluada – Califica',
                'Pendiente de cuenta bancaria',
                'Pendiente de aprobación final de pago',
                'Pagos activos'
            )
        );

    /* 3) Expandir estados de finca para reflejar transición en evaluación */
    IF EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE name = 'CK_Fincas_Estado'
          AND parent_object_id = OBJECT_ID('dbo.Fincas')
    )
    BEGIN
        ALTER TABLE dbo.Fincas DROP CONSTRAINT CK_Fincas_Estado;
    END

    ALTER TABLE dbo.Fincas
        ADD CONSTRAINT CK_Fincas_Estado CHECK (
            EstadoFinca IN (
                'Registrada',
                'Pendiente',
                'EnRevision',
                'En proceso',
                'Aprobada',
                'Rechazada',
                'Suspendida',
                'Inactiva'
            )
        );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
