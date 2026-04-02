using Microsoft.Data.SqlClient;

namespace PSA.DataAccess.DAO
{
    public class DashboardDAO
    {
        private readonly string _connectionString;

        public DashboardDAO(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<(int FincasRegistradas, int EvaluacionesPendientes, int CuotasPorConfirmar, List<(string Mensaje, int? IdEntidad)> Actividad)> ObtenerResumenDuenoAsync(int idPropietario)
        {
            const string sql = @"
SELECT
    (SELECT COUNT(1) FROM Fincas WHERE IdPropietario = @IdPropietario) AS FincasRegistradas,
    (SELECT COUNT(1)
     FROM EvaluacionesTecnicas e
     INNER JOIN Fincas f ON f.IdFinca = e.IdFinca
     WHERE f.IdPropietario = @IdPropietario
       AND e.EstadoEvaluacion IN ('Pendiente', 'En Proceso')) AS EvaluacionesPendientes,
    (SELECT COUNT(1)
     FROM CuotasPago c
     INNER JOIN PlanesPago p ON p.IdPlanPago = c.IdPlanPago
     INNER JOIN Fincas f ON f.IdFinca = p.IdFinca
     WHERE f.IdPropietario = @IdPropietario
       AND c.EstadoCuota IN ('Programada', 'Atrasada', 'Acumulada')) AS CuotasPorConfirmar;";

            const string sqlActividad = @"
SELECT TOP 3 Mensaje, IdEntidadReferencia
FROM Notificaciones
WHERE IdUsuario = @IdPropietario
ORDER BY FechaCreacion DESC;";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            int fincas = 0;
            int evaluaciones = 0;
            int cuotas = 0;

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@IdPropietario", idPropietario);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    fincas = reader.GetInt32(reader.GetOrdinal("FincasRegistradas"));
                    evaluaciones = reader.GetInt32(reader.GetOrdinal("EvaluacionesPendientes"));
                    cuotas = reader.GetInt32(reader.GetOrdinal("CuotasPorConfirmar"));
                }
            }

            var actividad = new List<(string Mensaje, int? IdEntidad)>();
            using (var commandActividad = new SqlCommand(sqlActividad, connection))
            {
                commandActividad.Parameters.AddWithValue("@IdPropietario", idPropietario);
                using var readerActividad = await commandActividad.ExecuteReaderAsync();
                while (await readerActividad.ReadAsync())
                {
                    var mensaje = readerActividad["Mensaje"]?.ToString() ?? string.Empty;
                    var idEntidad = readerActividad["IdEntidadReferencia"] == DBNull.Value
                        ? (int?)null
                        : Convert.ToInt32(readerActividad["IdEntidadReferencia"]);
                    actividad.Add((mensaje, idEntidad));
                }
            }

            return (fincas, evaluaciones, cuotas, actividad);
        }

        public async Task<(int FincasPendientes, int EvaluacionesAbiertas, int DecisionesMesActual, List<(int IdFinca, string NombreFinca)> ProximasAcciones)> ObtenerResumenIngenieroAsync(int idIngeniero)
        {
            const string sql = @"
SELECT
    (SELECT COUNT(1) FROM EvaluacionesTecnicas WHERE EstadoEvaluacion = 'Pendiente') AS FincasPendientes,
    (SELECT COUNT(1) FROM EvaluacionesTecnicas WHERE IdIngeniero = @IdIngeniero AND EstadoEvaluacion IN ('Pendiente', 'En Proceso')) AS EvaluacionesAbiertas,
    (SELECT COUNT(1)
     FROM EvaluacionesTecnicas
     WHERE IdIngeniero = @IdIngeniero
       AND DecisionTecnica IS NOT NULL
       AND YEAR(FechaDecision) = YEAR(SYSDATETIME())
       AND MONTH(FechaDecision) = MONTH(SYSDATETIME())) AS DecisionesMesActual;";

            const string sqlAcciones = @"
SELECT TOP 3 f.IdFinca, f.NombreFinca
FROM EvaluacionesTecnicas e
INNER JOIN Fincas f ON f.IdFinca = e.IdFinca
WHERE e.IdIngeniero = @IdIngeniero
  AND e.EstadoEvaluacion IN ('Pendiente', 'En Proceso')
ORDER BY ISNULL(e.FechaVisita, CAST(GETDATE() AS date)) ASC, e.IdEvaluacion DESC;";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            int pendientes = 0;
            int abiertas = 0;
            int decisiones = 0;

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@IdIngeniero", idIngeniero);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    pendientes = reader.GetInt32(reader.GetOrdinal("FincasPendientes"));
                    abiertas = reader.GetInt32(reader.GetOrdinal("EvaluacionesAbiertas"));
                    decisiones = reader.GetInt32(reader.GetOrdinal("DecisionesMesActual"));
                }
            }

            var acciones = new List<(int IdFinca, string NombreFinca)>();
            using (var commandAcciones = new SqlCommand(sqlAcciones, connection))
            {
                commandAcciones.Parameters.AddWithValue("@IdIngeniero", idIngeniero);
                using var readerAcciones = await commandAcciones.ExecuteReaderAsync();
                while (await readerAcciones.ReadAsync())
                {
                    acciones.Add((
                        readerAcciones.GetInt32(readerAcciones.GetOrdinal("IdFinca")),
                        readerAcciones["NombreFinca"]?.ToString() ?? string.Empty
                    ));
                }
            }

            return (pendientes, abiertas, decisiones, acciones);
        }

        public async Task<(int UsuariosActivos, int CuentasPorValidar, int EventosAuditoria24h, List<string> Alertas)> ObtenerResumenAdministradorAsync()
        {
            const string sql = @"
SELECT
    (SELECT COUNT(1) FROM Usuarios WHERE Estado = 'Activo') AS UsuariosActivos,
    (SELECT COUNT(1) FROM CuentasBancarias WHERE EstadoValidacion = 'Pendiente') AS CuentasPorValidar,
    (SELECT COUNT(1) FROM AuditoriaLog WHERE FechaAccion >= DATEADD(HOUR, -24, SYSDATETIME())) AS EventosAuditoria24h;";

            const string sqlAlertas = @"
SELECT TOP 3 Detalle
FROM AuditoriaLog
WHERE Detalle IS NOT NULL AND LTRIM(RTRIM(Detalle)) <> ''
ORDER BY FechaAccion DESC;";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            int usuarios = 0;
            int cuentas = 0;
            int auditoria = 0;

            using (var command = new SqlCommand(sql, connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    usuarios = reader.GetInt32(reader.GetOrdinal("UsuariosActivos"));
                    cuentas = reader.GetInt32(reader.GetOrdinal("CuentasPorValidar"));
                    auditoria = reader.GetInt32(reader.GetOrdinal("EventosAuditoria24h"));
                }
            }

            var alertas = new List<string>();
            using (var commandAlertas = new SqlCommand(sqlAlertas, connection))
            {
                using var readerAlertas = await commandAlertas.ExecuteReaderAsync();
                while (await readerAlertas.ReadAsync())
                {
                    var detalle = readerAlertas["Detalle"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(detalle))
                    {
                        alertas.Add(detalle.Trim());
                    }
                }
            }

            return (usuarios, cuentas, auditoria, alertas);
        }
    }
}
