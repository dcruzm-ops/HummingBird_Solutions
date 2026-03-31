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
WHERE Estado = 'Activo';";

            const string sqlUsuariosPendientes = @"
SELECT COUNT(1)
FROM dbo.Usuarios
WHERE Estado IN ('Inactivo', 'Bloqueado');";

            const string sqlCuentasPendientes = @"
SELECT COUNT(1)
FROM dbo.CuentasBancarias
WHERE EstadoValidacion = 'Pendiente';";

            const string sqlAuditoria24h = @"
SELECT COUNT(1)
FROM dbo.AuditoriaLog
WHERE FechaAccion >= DATEADD(HOUR, -24, GETDATE());";

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var usuariosActivos = await EjecutarEscalarAsync(connection, sqlUsuariosActivos);
            var usuariosPendientes = await EjecutarEscalarAsync(connection, sqlUsuariosPendientes);
            var cuentasPendientes = await EjecutarEscalarAsync(connection, sqlCuentasPendientes);
            var eventosAuditoria = await EjecutarEscalarAsync(connection, sqlAuditoria24h);

            return Ok(new ResumenDashboardAdministradorDTO
            {
                UsuariosActivos = usuariosActivos,
                UsuariosPendientesAprobacion = usuariosPendientes,
                CuentasPorValidar = cuentasPendientes,
                EventosAuditoria24h = eventosAuditoria,
                Alertas = new List<string>
                {
                    $"Hay {cuentasPendientes} cuentas bancarias pendientes de validación administrativa.",
                    $"Se registraron {eventosAuditoria} eventos de auditoría en las últimas 24 horas.",
                    $"Existen {usuariosPendientes} usuarios inactivos o bloqueados que requieren revisión de acceso."
                }
            });
        }

        private static async Task<int> EjecutarEscalarAsync(SqlConnection connection, string sql)
        {
            using var cmd = new SqlCommand(sql, connection);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }
    }
}
