using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;
using PSA.WebApp.Models;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "1")]
    public class AdministracionController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdministracionController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GestionUsuarios()
        {
            ConfigurarPagina("Gestión de usuarios", "Administre accesos, estados y roles del sistema.", "Gestión de usuarios");
            var model = new GestionUsuariosViewModel
            {
                Usuarios = await ObtenerDesdeApiAsync<List<UsuarioAdminListadoDTO>>("api/Administracion/usuarios") ?? new List<UsuarioAdminListadoDTO>()
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CrearUsuario()
        {
            ConfigurarPagina("Crear usuario", "Registre usuarios operativos desde el panel administrativo.", "Crear usuario", "Gestión de usuarios", Url.Action(nameof(GestionUsuarios)));
            return View("FormularioUsuario", await CrearModeloFormularioAsync(new UsuarioAdminEdicionDTO { Estado = "Activo" }, "Crear usuario", "Crear usuario"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(FormularioUsuarioAdminViewModel model)
        {
            ConfigurarPagina("Crear usuario", "Registre usuarios operativos desde el panel administrativo.", "Crear usuario", "Gestión de usuarios", Url.Action(nameof(GestionUsuarios)));
            if (!ModelState.IsValid)
            {
                return View("FormularioUsuario", await CrearModeloFormularioAsync(model.Usuario, "Crear usuario", "Crear usuario"));
            }

            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PostAsJsonAsync($"api/Administracion/usuarios?idAdministrador={ObtenerIdUsuarioSesion()}", model.Usuario);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, await LeerMensajeErrorAsync(response));
                return View("FormularioUsuario", await CrearModeloFormularioAsync(model.Usuario, "Crear usuario", "Crear usuario"));
            }

            TempData["MensajeExito"] = "Usuario creado correctamente.";
            return RedirectToAction(nameof(GestionUsuarios));
        }

        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            ConfigurarPagina("Editar usuario", "Actualice datos, estado o rol del usuario seleccionado.", "Editar usuario", "Gestión de usuarios", Url.Action(nameof(GestionUsuarios)));
            var usuario = await ObtenerDesdeApiAsync<UsuarioAdminEdicionDTO>($"api/Administracion/usuarios/{id}");
            if (usuario == null)
            {
                TempData["MensajeError"] = "No fue posible encontrar el usuario solicitado.";
                return RedirectToAction(nameof(GestionUsuarios));
            }

            return View("FormularioUsuario", await CrearModeloFormularioAsync(usuario, "Editar usuario", "Guardar cambios"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUsuario(FormularioUsuarioAdminViewModel model)
        {
            ConfigurarPagina("Editar usuario", "Actualice datos, estado o rol del usuario seleccionado.", "Editar usuario", "Gestión de usuarios", Url.Action(nameof(GestionUsuarios)));
            if (!ModelState.IsValid)
            {
                return View("FormularioUsuario", await CrearModeloFormularioAsync(model.Usuario, "Editar usuario", "Guardar cambios"));
            }

            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PutAsJsonAsync($"api/Administracion/usuarios/{model.Usuario.IdUsuario}?idAdministrador={ObtenerIdUsuarioSesion()}", model.Usuario);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, await LeerMensajeErrorAsync(response));
                return View("FormularioUsuario", await CrearModeloFormularioAsync(model.Usuario, "Editar usuario", "Guardar cambios"));
            }

            TempData["MensajeExito"] = "Usuario actualizado correctamente.";
            return RedirectToAction(nameof(GestionUsuarios));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.DeleteAsync($"api/Administracion/usuarios/{id}?idAdministrador={ObtenerIdUsuarioSesion()}");
            TempData[response.IsSuccessStatusCode ? "MensajeExito" : "MensajeError"] = response.IsSuccessStatusCode ? "Usuario eliminado correctamente." : await LeerMensajeErrorAsync(response);
            return RedirectToAction(nameof(GestionUsuarios));
        }

        [HttpGet]
        public async Task<IActionResult> ReasignarCliente()
        {
            ConfigurarPagina("Reasignar cliente", "Mueva los casos activos de un cliente hacia otro asesor.", "Reasignar cliente", "Gestión de usuarios", Url.Action(nameof(GestionUsuarios)));
            return View(await CrearModeloReasignacionAsync(new ReasignacionClienteDTO()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReasignarCliente(ReasignacionClienteViewModel model)
        {
            ConfigurarPagina("Reasignar cliente", "Mueva los casos activos de un cliente hacia otro asesor.", "Reasignar cliente", "Gestión de usuarios", Url.Action(nameof(GestionUsuarios)));
            if (!ModelState.IsValid)
            {
                return View(await CrearModeloReasignacionAsync(model.Reasignacion));
            }

            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PostAsJsonAsync($"api/Administracion/usuarios/reasignacion-cliente?idAdministrador={ObtenerIdUsuarioSesion()}", model.Reasignacion);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, await LeerMensajeErrorAsync(response));
                return View(await CrearModeloReasignacionAsync(model.Reasignacion));
            }

            TempData["MensajeExito"] = "Cliente reasignado correctamente.";
            return RedirectToAction(nameof(GestionUsuarios));
        }

        [HttpGet]
        public async Task<IActionResult> RolesPermisos()
        {
            ConfigurarPagina("Roles y permisos", "Consulte y ajuste las capacidades habilitadas por rol.", "Roles y permisos");
            var model = new RolesPermisosViewModel
            {
                Roles = await ObtenerDesdeApiAsync<List<RolPermisoDTO>>("api/Administracion/roles-permisos") ?? new List<RolPermisoDTO>()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarPermisosRol(GuardarPermisosRolDTO dto)
        {
            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PutAsJsonAsync($"api/Administracion/roles-permisos/{dto.IdRol}?idAdministrador={ObtenerIdUsuarioSesion()}", dto);
            TempData[response.IsSuccessStatusCode ? "MensajeExito" : "MensajeError"] = response.IsSuccessStatusCode ? "Permisos actualizados correctamente." : await LeerMensajeErrorAsync(response);
            return RedirectToAction(nameof(RolesPermisos));
        }

        [HttpGet]
        public async Task<IActionResult> ParametrosPago()
        {
            ConfigurarPagina("Parámetros de pago", "Defina configuraciones versionadas para el cálculo de pagos.", "Parámetros de pago");
            var historial = await ObtenerDesdeApiAsync<List<ConfiguracionPagoAdminDTO>>("api/Administracion/configuraciones-pago/historial") ?? new List<ConfiguracionPagoAdminDTO>();
            var model = new ParametrosPagoViewModel
            {
                ConfiguracionActual = historial.FirstOrDefault(x => x.Activa),
                Historial = historial,
                NuevaConfiguracion = CrearModeloConfiguracionVacia(historial.FirstOrDefault()?.Version + 1 ?? 1)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ParametrosPago(ParametrosPagoViewModel model)
        {
            ConfigurarPagina("Parámetros de pago", "Defina configuraciones versionadas para el cálculo de pagos.", "Parámetros de pago");
            NormalizarAjustes(model.NuevaConfiguracion);
            if (!ModelState.IsValid)
            {
                var historialInvalido = await ObtenerDesdeApiAsync<List<ConfiguracionPagoAdminDTO>>("api/Administracion/configuraciones-pago/historial") ?? new List<ConfiguracionPagoAdminDTO>();
                model.ConfiguracionActual = historialInvalido.FirstOrDefault(x => x.Activa);
                model.Historial = historialInvalido;
                CompletarAjustesMinimos(model.NuevaConfiguracion);
                return View(model);
            }

            model.NuevaConfiguracion.CreadoPor = ObtenerIdUsuarioSesion();
            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PostAsJsonAsync($"api/Administracion/configuraciones-pago?idAdministrador={ObtenerIdUsuarioSesion()}", model.NuevaConfiguracion);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, await LeerMensajeErrorAsync(response));
                var historial = await ObtenerDesdeApiAsync<List<ConfiguracionPagoAdminDTO>>("api/Administracion/configuraciones-pago/historial") ?? new List<ConfiguracionPagoAdminDTO>();
                model.ConfiguracionActual = historial.FirstOrDefault(x => x.Activa);
                model.Historial = historial;
                CompletarAjustesMinimos(model.NuevaConfiguracion);
                return View(model);
            }

            TempData["MensajeExito"] = "Configuración de pago creada correctamente.";
            return RedirectToAction(nameof(ParametrosPago));
        }

        [HttpGet]
        public async Task<IActionResult> ValidacionCuentasBancarias()
        {
            ConfigurarPagina("Validación de cuentas bancarias", "Revise y apruebe cuentas bancarias vinculadas a propietarios.", "Validación bancaria");
            var model = new ValidacionCuentasBancariasViewModel
            {
                CuentasPendientes = await ObtenerDesdeApiAsync<List<CuentaBancariaPendienteDTO>>("api/Administracion/cuentas-bancarias/pendientes") ?? new List<CuentaBancariaPendienteDTO>()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarRevisionCuenta(int idCuentaBancaria, bool aprobada, string? observaciones)
        {
            var dto = new ValidacionCuentaBancariaDTO
            {
                IdCuentaBancaria = idCuentaBancaria,
                Aprobada = aprobada,
                Observaciones = observaciones,
                IdAdministrador = ObtenerIdUsuarioSesion()
            };
            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PostAsJsonAsync("api/Administracion/cuentas-bancarias/validacion", dto);
            TempData[response.IsSuccessStatusCode ? "MensajeExito" : "MensajeError"] = response.IsSuccessStatusCode ? "Validación registrada correctamente." : await LeerMensajeErrorAsync(response);
            return RedirectToAction(nameof(ValidacionCuentasBancarias));
        }

        [HttpGet]
        public async Task<IActionResult> AuditoriaLogs([FromQuery] AuditoriaFiltroDTO filtro)
        {
            ConfigurarPagina("Auditoría y logs", "Consulte trazabilidad de cambios y acciones críticas del sistema.", "Auditoría y logs");
            var model = new AuditoriaLogsViewModel
            {
                Filtro = filtro ?? new AuditoriaFiltroDTO(),
                Eventos = await ObtenerEventosAuditoriaAsync(filtro ?? new AuditoriaFiltroDTO())
            };
            return View(model);
        }

        private async Task<FormularioUsuarioAdminViewModel> CrearModeloFormularioAsync(UsuarioAdminEdicionDTO usuario, string titulo, string accion)
            => new()
            {
                Usuario = usuario,
                Roles = await ObtenerDesdeApiAsync<List<RolDTO>>("api/Administracion/roles") ?? new List<RolDTO>(),
                TituloFormulario = titulo,
                TextoAccion = accion
            };

        private async Task<ReasignacionClienteViewModel> CrearModeloReasignacionAsync(ReasignacionClienteDTO dto)
            => new()
            {
                Reasignacion = dto,
                Propietarios = await ObtenerDesdeApiAsync<List<UsuarioAdminListadoDTO>>("api/Administracion/usuarios?idRol=2") ?? new List<UsuarioAdminListadoDTO>(),
                Ingenieros = await ObtenerDesdeApiAsync<List<UsuarioAdminListadoDTO>>("api/Administracion/usuarios?idRol=3") ?? new List<UsuarioAdminListadoDTO>()
            };

        private async Task<List<AuditoriaEventoDTO>> ObtenerEventosAuditoriaAsync(AuditoriaFiltroDTO filtro)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(filtro.Modulo)) query.Add($"Modulo={Uri.EscapeDataString(filtro.Modulo)}");
            if (!string.IsNullOrWhiteSpace(filtro.Accion)) query.Add($"Accion={Uri.EscapeDataString(filtro.Accion)}");
            if (filtro.FechaDesde.HasValue) query.Add($"FechaDesde={Uri.EscapeDataString(filtro.FechaDesde.Value.ToString("o"))}");
            if (filtro.FechaHasta.HasValue) query.Add($"FechaHasta={Uri.EscapeDataString(filtro.FechaHasta.Value.ToString("o"))}");
            query.Add($"MaximoRegistros={Math.Clamp(filtro.MaximoRegistros <= 0 ? 50 : filtro.MaximoRegistros, 1, 200)}");
            var suffix = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
            return await ObtenerDesdeApiAsync<List<AuditoriaEventoDTO>>($"api/Administracion/auditoria{suffix}") ?? new List<AuditoriaEventoDTO>();
        }

        private static ConfiguracionPagoAdminDTO CrearModeloConfiguracionVacia(int version)
        {
            var model = new ConfiguracionPagoAdminDTO
            {
                Version = version,
                FechaVigenciaDesde = DateTime.Today,
                Ajustes = new List<ConfiguracionPagoAjusteDTO>()
            };
            CompletarAjustesMinimos(model);
            return model;
        }

        private static void NormalizarAjustes(ConfiguracionPagoAdminDTO configuracion)
        {
            configuracion.Ajustes ??= new List<ConfiguracionPagoAjusteDTO>();
            configuracion.Ajustes = configuracion.Ajustes.Where(x => !string.IsNullOrWhiteSpace(x.TipoFactor) || !string.IsNullOrWhiteSpace(x.ValorFactor) || x.PorcentajeAjuste != 0).ToList();
            CompletarAjustesMinimos(configuracion);
        }

        private static void CompletarAjustesMinimos(ConfiguracionPagoAdminDTO configuracion)
        {
            configuracion.Ajustes ??= new List<ConfiguracionPagoAjusteDTO>();
            while (configuracion.Ajustes.Count < 4) configuracion.Ajustes.Add(new ConfiguracionPagoAjusteDTO());
        }

        private async Task<T?> ObtenerDesdeApiAsync<T>(string ruta)
        {
            try
            {
                return await _httpClientFactory.CreateClient("AuthApi").GetFromJsonAsync<T>(ruta);
            }
            catch
            {
                return default;
            }
        }

        private static async Task<string> LeerMensajeErrorAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) return "No fue posible completar la operación.";
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("Mensaje", out var mensaje)) return mensaje.GetString() ?? "No fue posible completar la operación.";
            }
            catch { }
            return "No fue posible completar la operación.";
        }

        private int ObtenerIdUsuarioSesion() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private void ConfigurarPagina(string titulo, string subtitulo, string breadcrumbActual, string? breadcrumbPadreTexto = null, string? breadcrumbPadreUrl = null)
        {
            ViewBag.ModuloActivo = "administracion";
            ViewBag.RolActivo = "Administrador";
            ViewBag.TituloPagina = titulo;
            ViewBag.SubtituloPagina = subtitulo;
            ViewBag.BreadcrumbActual = breadcrumbActual;
            ViewBag.BreadcrumbPadreTexto = breadcrumbPadreTexto;
            ViewBag.BreadcrumbPadreUrl = breadcrumbPadreUrl;
        }
    }
}
