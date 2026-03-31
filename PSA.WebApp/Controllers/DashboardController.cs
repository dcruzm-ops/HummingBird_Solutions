using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PSA.DataAccess.DAO;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace PSA.WebApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly FincaDAO _fincaDAO;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public DashboardController(
            FincaDAO fincaDAO,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _fincaDAO = fincaDAO;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> Dueno()
        {
            ViewBag.ModuloActivo = "dashboard";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Dashboard del dueño de finca";
            ViewBag.SubtituloPagina = "Resumen general de fincas, evaluaciones, notificaciones y pagos.";
            ViewBag.BreadcrumbActual = "Dashboard";

            var idUsuario = ObtenerIdUsuarioSesion();
            var fincas = idUsuario > 0
                ? await _fincaDAO.ObtenerPorPropietarioAsync(idUsuario)
                : new List<PSA.EntidadesDTO.DTOs.FincaResumenDTO>();

            ViewBag.TotalFincas = fincas.Count;
            ViewBag.FincasActivas = fincas.Count(f => string.Equals(f.EstadoFinca, "Activa", StringComparison.OrdinalIgnoreCase));
            ViewBag.EvaluacionesPendientes = fincas.Count(f => !string.Equals(f.EstadoEvaluacion, "Aprobada", StringComparison.OrdinalIgnoreCase));
            ViewBag.EnRevision = fincas.Count(f => string.Equals(f.EstadoFinca, "Registrada", StringComparison.OrdinalIgnoreCase) || string.Equals(f.EstadoFinca, "En revision", StringComparison.OrdinalIgnoreCase));
            ViewBag.ActividadReciente = fincas
                .OrderByDescending(f => f.FechaRegistro)
                .Take(3)
                .Select(f => $"Expediente #{f.IdFinca:D3} - {f.NombreFinca} ({f.EstadoEvaluacion})")
                .ToList();

            return View();
        }

        [HttpGet]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Ingeniero()
        {
            ViewBag.ModuloActivo = "dashboard";
            ViewBag.RolActivo = "Ingeniero";
            ViewBag.TituloPagina = "Dashboard del ingeniero forestal";
            ViewBag.SubtituloPagina = "Accesos rápidos a evaluaciones, visitas y fincas pendientes.";
            ViewBag.BreadcrumbActual = "Dashboard";

            var resumen = await ObtenerResumenIngenieroDesdeDbAsync();
            ViewBag.FincasPendientes = resumen.FincasPendientes;
            ViewBag.EvaluacionesEnProceso = resumen.EvaluacionesEnProceso;
            ViewBag.DecisionesMes = resumen.DecisionesMes;
            ViewBag.TopProvincias = resumen.TopProvincias;
            ViewBag.ForecastProvincias = await ObtenerPronosticoProvinciasAsync();

            return View();
        }

        [HttpGet]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> Administrador()
        {
            ViewBag.ModuloActivo = "dashboard";
            ViewBag.RolActivo = "Administrador";
            ViewBag.TituloPagina = "Dashboard del administrador";
            ViewBag.SubtituloPagina = "Monitoreo operativo del sistema, usuarios, pagos y auditoría.";
            ViewBag.BreadcrumbActual = "Dashboard";

            var resumen = await ObtenerResumenAdministradorDesdeDbAsync();
            ViewBag.UsuariosActivos = resumen.UsuariosActivos;
            ViewBag.UsuariosPendientesAprobacion = resumen.UsuariosPendientesAprobacion;
            ViewBag.CuentasPorValidar = resumen.CuentasPorValidar;
            ViewBag.EventosAuditoria24h = resumen.EventosAuditoria24h;
            ViewBag.AlertasAdministrativas = resumen.Alertas;

            return View();
        }

        private int ObtenerIdUsuarioSesion()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var idUsuario) ? idUsuario : 0;
        }

        private async Task<(int FincasPendientes, int EvaluacionesEnProceso, int DecisionesMes, List<string> TopProvincias)> ObtenerResumenIngenieroDesdeDbAsync()
        {
            var connectionString = _configuration.GetConnectionString("PSAConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return (0, 0, 0, new List<string>());
            }

            const string sqlPendientes = @"
SELECT COUNT(1)
FROM Fincas f
OUTER APPLY (
    SELECT TOP 1 e.EstadoEvaluacion
    FROM EvaluacionesTecnicas e
    WHERE e.IdFinca = f.IdFinca
    ORDER BY e.IdEvaluacion DESC
) ev
WHERE ISNULL(ev.EstadoEvaluacion, 'Sin iniciar') IN ('Sin iniciar', 'Pendiente');";

            const string sqlEnProceso = @"
SELECT COUNT(1)
FROM EvaluacionesTecnicas
WHERE EstadoEvaluacion IN ('En Proceso', 'En proceso');";

            const string sqlDecisionesMes = @"
SELECT COUNT(1)
FROM EvaluacionesTecnicas
WHERE EstadoEvaluacion = 'Finalizada'
  AND FechaDecision IS NOT NULL
  AND YEAR(FechaDecision) = YEAR(GETDATE())
  AND MONTH(FechaDecision) = MONTH(GETDATE());";

            const string sqlTopProvincias = @"
SELECT TOP 3 Provincia, COUNT(1) AS Cantidad
FROM Fincas
GROUP BY Provincia
ORDER BY Cantidad DESC, Provincia ASC;";

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var pendientes = await EjecutarEscalarAsync(connection, sqlPendientes);
                var enProceso = await EjecutarEscalarAsync(connection, sqlEnProceso);
                var decisionesMes = await EjecutarEscalarAsync(connection, sqlDecisionesMes);

                var topProvincias = new List<string>();
                using (var cmdProvincias = new SqlCommand(sqlTopProvincias, connection))
                using (var reader = await cmdProvincias.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var provincia = reader["Provincia"]?.ToString() ?? "Sin dato";
                        var cantidad = Convert.ToInt32(reader["Cantidad"]);
                        topProvincias.Add($"{provincia}: {cantidad} fincas");
                    }
                }

                return (pendientes, enProceso, decisionesMes, topProvincias);
            }
            catch
            {
                return (0, 0, 0, new List<string>());
            }
        }

        private static async Task<int> EjecutarEscalarAsync(SqlConnection connection, string sql)
        {
            using var cmd = new SqlCommand(sql, connection);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private async Task<(int UsuariosActivos, int UsuariosPendientesAprobacion, int CuentasPorValidar, int EventosAuditoria24h, List<string> Alertas)> ObtenerResumenAdministradorDesdeDbAsync()
        {
            var connectionString = _configuration.GetConnectionString("PSAConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return (0, 0, 0, 0, new List<string>());
            }

            const string sqlUsuariosActivos = @"
SELECT COUNT(1)
FROM Usuarios
WHERE Estado = 'Activo';";

            const string sqlUsuariosPendientesAprobacion = @"
SELECT COUNT(1)
FROM Usuarios
WHERE Estado IN ('Inactivo', 'Bloqueado');";

            const string sqlCuentasPorValidar = @"
SELECT COUNT(1)
FROM CuentasBancarias
WHERE EstadoValidacion = 'Pendiente';";

            const string sqlEventosAuditoria24h = @"
IF COL_LENGTH('AuditoriaLog', 'FechaAccion') IS NOT NULL
    SELECT COUNT(1)
    FROM AuditoriaLog
    WHERE FechaAccion >= DATEADD(HOUR, -24, GETDATE());
ELSE IF COL_LENGTH('AuditoriaLog', 'FechaEvento') IS NOT NULL
    SELECT COUNT(1)
    FROM AuditoriaLog
    WHERE FechaEvento >= DATEADD(HOUR, -24, GETDATE());
ELSE
    SELECT COUNT(1) FROM AuditoriaLog;";

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var usuariosActivos = await EjecutarEscalarAsync(connection, sqlUsuariosActivos);
                var usuariosPendientes = await EjecutarEscalarAsync(connection, sqlUsuariosPendientesAprobacion);
                var cuentasPorValidar = await EjecutarEscalarAsync(connection, sqlCuentasPorValidar);
                var eventosAuditoria24h = await EjecutarEscalarAsync(connection, sqlEventosAuditoria24h);

                var alertas = new List<string>
                {
                    $"Hay {cuentasPorValidar} cuentas bancarias pendientes de validación administrativa.",
                    $"Se registraron {eventosAuditoria24h} eventos de auditoría en las últimas 24 horas.",
                    $"Existen {usuariosPendientes} usuarios inactivos o bloqueados que requieren revisión de acceso."
                };

                return (usuariosActivos, usuariosPendientes, cuentasPorValidar, eventosAuditoria24h, alertas);
            }
            catch
            {
                return (0, 0, 0, 0, new List<string>());
            }
        }

        private async Task<Dictionary<string, string>> ObtenerPronosticoProvinciasAsync()
        {
            var provincias = new Dictionary<string, (double Lat, double Lon)>
            {
                ["San José"] = (9.9281, -84.0907),
                ["Alajuela"] = (10.0163, -84.2116),
                ["Cartago"] = (9.8644, -83.9194),
                ["Heredia"] = (9.9980, -84.1165),
                ["Guanacaste"] = (10.6346, -85.4407),
                ["Puntarenas"] = (9.9763, -84.8384),
                ["Limón"] = (9.9907, -83.0360)
            };

            var client = _httpClientFactory.CreateClient();
            var salida = new Dictionary<string, string>();

            foreach (var provincia in provincias)
            {
                try
                {
                    var url = $"https://api.open-meteo.com/v1/forecast?latitude={provincia.Value.Lat}&longitude={provincia.Value.Lon}&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weather_code&timezone=auto&forecast_days=1";
                    var respuesta = await client.GetStringAsync(url);
                    using var doc = JsonDocument.Parse(respuesta);
                    var daily = doc.RootElement.GetProperty("daily");
                    var max = daily.GetProperty("temperature_2m_max")[0].GetDecimal();
                    var min = daily.GetProperty("temperature_2m_min")[0].GetDecimal();
                    var lluvia = daily.GetProperty("precipitation_sum")[0].GetDecimal();
                    var codigoTiempo = daily.TryGetProperty("weather_code", out var codigoTiempoPropiedad)
                        ? codigoTiempoPropiedad[0].GetInt32()
                        : -1;

                    var icono = ObtenerIconoClima(codigoTiempo, lluvia);
                    salida[provincia.Key] = $"{icono} Máx {max:0.#}°C / Mín {min:0.#}°C · Lluvia {lluvia:0.#} mm";
                }
                catch
                {
                    salida[provincia.Key] = "❔ Pronóstico no disponible.";
                }
            }

            return salida;
        }

        private static string ObtenerIconoClima(int codigoTiempo, decimal lluvia)
        {
            return codigoTiempo switch
            {
                0 => "☀️",
                1 or 2 => "🌤️",
                3 => "☁️",
                45 or 48 => "🌫️",
                51 or 53 or 55 or 56 or 57 => "🌦️",
                61 or 63 or 65 or 66 or 67 => "🌧️",
                71 or 73 or 75 or 77 => "❄️",
                80 or 81 or 82 => "🌧️",
                85 or 86 => "🌨️",
                95 or 96 or 99 => "⛈️",
                _ => lluvia > 0 ? "🌦️" : "🌤️"
            };
        }

    }
}
