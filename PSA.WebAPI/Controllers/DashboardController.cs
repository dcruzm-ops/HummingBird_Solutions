using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Dashboard;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("administrador-resumen")]
        public async Task<ActionResult<ResumenDashboardAdministradorDTO>> ObtenerResumenAdministradorAsync()
        {
            var connectionString = _configuration.GetConnectionString("PSAConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return Ok(new ResumenDashboardAdministradorDTO());
            }

            const string sqlUsuariosActivos = @"
SELECT COUNT(1)
FROM dbo.Usuarios
WHERE UPPER(LTRIM(RTRIM(ISNULL(Estado, '')))) = 'ACTIVO';";

            const string sqlUsuariosPendientes = @"
SELECT COUNT(1)
FROM dbo.Usuarios
WHERE UPPER(LTRIM(RTRIM(ISNULL(Estado, '')))) IN ('INACTIVO', 'BLOQUEADO');";

            const string sqlUsuariosNuevosHoy = @"
SELECT COUNT(1)
FROM dbo.Usuarios
WHERE CONVERT(date, FechaCreacion) = CONVERT(date, GETDATE());";

            const string sqlCuentasPendientes = @"
SELECT COUNT(1)
FROM dbo.CuentasBancarias
WHERE UPPER(LTRIM(RTRIM(ISNULL(EstadoValidacion, '')))) = 'PENDIENTE';";

            const string sqlAuditoria24h = @"
SELECT COUNT(1)
FROM dbo.AuditoriaLog
WHERE FechaAccion >= DATEADD(HOUR, -24, GETDATE());";

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var usuariosActivos = await EjecutarEscalarAsync(connection, sqlUsuariosActivos);
            var usuariosPendientes = await EjecutarEscalarAsync(connection, sqlUsuariosPendientes);
            var usuariosNuevosHoy = await EjecutarEscalarAsync(connection, sqlUsuariosNuevosHoy);
            var cuentasPendientes = await EjecutarEscalarAsync(connection, sqlCuentasPendientes);
            var eventosAuditoria = await EjecutarEscalarAsync(connection, sqlAuditoria24h);
            var actividadReciente = await ObtenerActividadAuditoriaAsync(connection);

            return Ok(new ResumenDashboardAdministradorDTO
            {
                UsuariosActivos = usuariosActivos,
                UsuariosNuevosHoy = usuariosNuevosHoy,
                UsuariosPendientesAprobacion = usuariosPendientes,
                CuentasPorValidar = cuentasPendientes,
                EventosAuditoria24h = eventosAuditoria,
                Alertas = new List<string>
                {
                    $"Hay {cuentasPendientes} cuentas bancarias pendientes de validación administrativa.",
                    $"Se registraron {eventosAuditoria} eventos de auditoría en las últimas 24 horas.",
                    $"Existen {usuariosPendientes} usuarios inactivos o bloqueados que requieren revisión de acceso."
                },
                ActividadAuditoria = actividadReciente
            });
        }

        private static async Task<int> EjecutarEscalarAsync(SqlConnection connection, string sql)
        {
            using var cmd = new SqlCommand(sql, connection);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private static async Task<List<ActividadAuditoriaDTO>> ObtenerActividadAuditoriaAsync(SqlConnection connection)
        {
            const string sqlActividad = @"
SELECT TOP 10
    ISNULL(Modulo, 'General') AS Modulo,
    ISNULL(Accion, 'Cambio') AS Accion,
    ISNULL(Detalle, CONCAT(TablaAfectada, ' #', ISNULL(CONVERT(varchar(20), IdRegistroAfectado), 's/d'))) AS Detalle,
    FechaAccion
FROM dbo.AuditoriaLog
ORDER BY FechaAccion DESC;";

            var actividad = new List<ActividadAuditoriaDTO>();
            using var cmd = new SqlCommand(sqlActividad, connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                actividad.Add(new ActividadAuditoriaDTO
                {
                    Modulo = reader["Modulo"]?.ToString() ?? "General",
                    Accion = reader["Accion"]?.ToString() ?? "Cambio",
                    Detalle = reader["Detalle"]?.ToString() ?? "Sin detalle",
                    FechaAccion = reader.GetDateTime(reader.GetOrdinal("FechaAccion"))
                });
            }

            return actividad;
        }
    }
}
