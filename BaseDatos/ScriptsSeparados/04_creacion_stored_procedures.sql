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
