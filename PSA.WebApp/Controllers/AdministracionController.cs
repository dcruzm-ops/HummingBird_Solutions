using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;
using PSA.WebApp.Models;
using PSA.WebApp.Services;

namespace PSA.WebApp.Controllers
{
    public class AdministracionController : Controller
    {
        private readonly HttpClientService _httpClientService;

        public AdministracionController(HttpClientService httpClientService)
        {
            _httpClientService = httpClientService;
        }

        public async Task<IActionResult> GestionUsuarios()
        {
            var usuarios = await _httpClientService.GetAsync<List<UsuarioAdminListadoDTO>>("api/Administracion/usuarios") ?? new();
            var model = new GestionUsuariosViewModel
            {
                Usuarios = usuarios
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CrearUsuario()
        {
            var roles = await _httpClientService.GetAsync<List<RolDTO>>("api/Administracion/roles-basicos") ?? new();
            var model = new FormularioUsuarioAdminViewModel
            {
                Usuario = new UsuarioAdminEdicionDTO { Estado = "Activo" },
                Roles = roles
            };

            return View("FormularioUsuarioSimple", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(FormularioUsuarioAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = await _httpClientService.GetAsync<List<RolDTO>>("api/Administracion/roles-basicos") ?? new();
                return View("FormularioUsuarioSimple", model);
            }

            var respuesta = await _httpClientService.PostAsync<UsuarioAdminEdicionDTO, bool>("api/Administracion/usuarios", model.Usuario);
            if (!respuesta)
            {
                ModelState.AddModelError(string.Empty, "No se pudo crear el usuario.");
                model.Roles = await _httpClientService.GetAsync<List<RolDTO>>("api/Administracion/roles-basicos") ?? new();
                return View("FormularioUsuarioSimple", model);
            }

            TempData["Exito"] = "Usuario creado correctamente.";
            return RedirectToAction(nameof(GestionUsuarios));
        }

        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            var usuario = await _httpClientService.GetAsync<UsuarioAdminEdicionDTO>($"api/Administracion/usuarios/{id}");
            if (usuario == null)
            {
                TempData["Error"] = "No se encontró el usuario solicitado.";
                return RedirectToAction(nameof(GestionUsuarios));
            }

            var roles = await _httpClientService.GetAsync<List<RolDTO>>("api/Administracion/roles-basicos") ?? new();
            var model = new FormularioUsuarioAdminViewModel
            {
                Usuario = usuario,
                Roles = roles
            };

            return View("FormularioUsuarioSimple", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUsuario(FormularioUsuarioAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = await _httpClientService.GetAsync<List<RolDTO>>("api/Administracion/roles-basicos") ?? new();
                return View("FormularioUsuarioSimple", model);
            }

            var respuesta = await _httpClientService.PutAsync<UsuarioAdminEdicionDTO, bool>($"api/Administracion/usuarios/{model.Usuario.IdUsuario}", model.Usuario);
            if (!respuesta)
            {
                ModelState.AddModelError(string.Empty, "No se pudo actualizar el usuario.");
                model.Roles = await _httpClientService.GetAsync<List<RolDTO>>("api/Administracion/roles-basicos") ?? new();
                return View("FormularioUsuarioSimple", model);
            }

            TempData["Exito"] = "Usuario actualizado correctamente.";
            return RedirectToAction(nameof(GestionUsuarios));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var respuesta = await _httpClientService.DeleteAsync<bool>($"api/Administracion/usuarios/{id}");
            TempData[respuesta ? "Exito" : "Error"] = respuesta
                ? "Usuario eliminado correctamente."
                : "No se pudo eliminar el usuario.";

            return RedirectToAction(nameof(GestionUsuarios));
        }

        [HttpGet]
        public async Task<IActionResult> ReasignarCliente()
        {
            ViewBag.Propietarios = await _httpClientService.GetAsync<List<UsuarioAdminListadoDTO>>("api/Administracion/usuarios/propietarios") ?? new();
            ViewBag.Ingenieros = await _httpClientService.GetAsync<List<UsuarioAdminListadoDTO>>("api/Administracion/usuarios/ingenieros") ?? new();
            return View(new ReasignacionClienteDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReasignarCliente(ReasignacionClienteDTO model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Propietarios = await _httpClientService.GetAsync<List<UsuarioAdminListadoDTO>>("api/Administracion/usuarios/propietarios") ?? new();
                ViewBag.Ingenieros = await _httpClientService.GetAsync<List<UsuarioAdminListadoDTO>>("api/Administracion/usuarios/ingenieros") ?? new();
                return View(model);
            }

            var respuesta = await _httpClientService.PostAsync<ReasignacionClienteDTO, bool>("api/Administracion/usuarios/reasignar-cliente", model);
            if (!respuesta)
            {
                ModelState.AddModelError(string.Empty, "No se pudo reasignar el cliente.");
                ViewBag.Propietarios = await _httpClientService.GetAsync<List<UsuarioAdminListadoDTO>>("api/Administracion/usuarios/propietarios") ?? new();
                ViewBag.Ingenieros = await _httpClientService.GetAsync<List<UsuarioAdminListadoDTO>>("api/Administracion/usuarios/ingenieros") ?? new();
                return View(model);
            }

            TempData["Exito"] = "Cliente reasignado correctamente.";
            return RedirectToAction(nameof(GestionUsuarios));
        }

        [HttpGet]
        public async Task<IActionResult> RolesPermisos()
        {
            var roles = await _httpClientService.GetAsync<List<RolPermisoDTO>>("api/Administracion/roles-permisos") ?? new();
            var model = new RolesPermisosViewModel
            {
                Roles = roles,
                PermisosDisponibles = roles.FirstOrDefault()?.PermisosDisponibles ?? new()
            };

            return View("RolesPermisosSimple", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarPermisosRol(GuardarPermisosRolDTO model)
        {
            var respuesta = await _httpClientService.PostAsync<GuardarPermisosRolDTO, bool>("api/Administracion/roles-permisos", model);
            TempData[respuesta ? "Exito" : "Error"] = respuesta
                ? "Permisos actualizados correctamente."
                : "No se pudieron actualizar los permisos.";

            return RedirectToAction(nameof(RolesPermisos));
        }

        [HttpGet]
        public async Task<IActionResult> ParametrosPago()
        {
            var configuracionActual = await _httpClientService.GetAsync<ConfiguracionPagoAdminDTO>("api/Administracion/configuracion-pago/actual");
            var historial = await _httpClientService.GetAsync<List<ConfiguracionPagoAdminDTO>>("api/Administracion/configuracion-pago/historial") ?? new();

            var model = new ParametrosPagoViewModel
            {
                ConfiguracionActual = configuracionActual,
                Historial = historial,
                NuevaConfiguracion = new ConfiguracionPagoAdminDTO
                {
                    FechaVigenciaDesde = DateTime.Today,
                    Ajustes = new List<ConfiguracionPagoAjusteDTO>
                    {
                        new() { TipoFactor = "Vegetacion", ValorFactor = "Bosque primario" },
                        new() { TipoFactor = "Vegetacion", ValorFactor = "Bosque secundario" },
                        new() { TipoFactor = "Vegetacion", ValorFactor = "Plantación" },
                        new() { TipoFactor = "Vegetacion", ValorFactor = "Pasto" },
                        new() { TipoFactor = "RecursosHidricos", ValorFactor = "Con recursos" },
                        new() { TipoFactor = "Pendiente", ValorFactor = "Plana" },
                        new() { TipoFactor = "Pendiente", ValorFactor = "Inclinada" },
                        new() { TipoFactor = "Pendiente", ValorFactor = "Muy inclinada" }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarConfiguracionPago(ConfiguracionPagoAdminDTO model)
        {
            if (!ModelState.IsValid)
            {
                var configuracionActual = await _httpClientService.GetAsync<ConfiguracionPagoAdminDTO>("api/Administracion/configuracion-pago/actual");
                var historial = await _httpClientService.GetAsync<List<ConfiguracionPagoAdminDTO>>("api/Administracion/configuracion-pago/historial") ?? new();

                return View("ParametrosPago", new ParametrosPagoViewModel
                {
                    ConfiguracionActual = configuracionActual,
                    Historial = historial,
                    NuevaConfiguracion = model
                });
            }

            var respuesta = await _httpClientService.PostAsync<ConfiguracionPagoAdminDTO, bool>("api/Administracion/configuracion-pago", model);
            TempData[respuesta ? "Exito" : "Error"] = respuesta
                ? "Configuración de pago guardada correctamente."
                : "No se pudo guardar la configuración de pago.";

            return RedirectToAction(nameof(ParametrosPago));
        }

        [HttpGet]
        public async Task<IActionResult> ValidacionCuentasBancarias()
        {
            var cuentas = await _httpClientService.GetAsync<List<CuentaBancariaPendienteDTO>>("api/Administracion/cuentas-bancarias/pendientes") ?? new();
            var model = new ValidacionCuentasBancariasViewModel
            {
                CuentasPendientes = cuentas
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarCuentaBancaria(ValidacionCuentaBancariaDTO model)
        {
            var respuesta = await _httpClientService.PostAsync<ValidacionCuentaBancariaDTO, bool>("api/Administracion/cuentas-bancarias/validar", model);
            TempData[respuesta ? "Exito" : "Error"] = respuesta
                ? "Validación procesada correctamente."
                : "No se pudo procesar la validación.";

            return RedirectToAction(nameof(ValidacionCuentasBancarias));
        }

        [HttpGet]
        public async Task<IActionResult> AuditoriaLogs(string? modulo, string? accion, DateTime? fechaDesde, DateTime? fechaHasta, int maximoRegistros = 50)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(modulo)) queryParams.Add($"modulo={Uri.EscapeDataString(modulo)}");
            if (!string.IsNullOrWhiteSpace(accion)) queryParams.Add($"accion={Uri.EscapeDataString(accion)}");
            if (fechaDesde.HasValue) queryParams.Add($"fechaDesde={fechaDesde.Value:yyyy-MM-dd}");
            if (fechaHasta.HasValue) queryParams.Add($"fechaHasta={fechaHasta.Value:yyyy-MM-dd}");
            queryParams.Add($"maximoRegistros={maximoRegistros}");

            var endpoint = "api/Administracion/auditoria";
            if (queryParams.Any())
            {
                endpoint += "?" + string.Join("&", queryParams);
            }

            var eventos = await _httpClientService.GetAsync<List<AuditoriaEventoDTO>>(endpoint) ?? new();

            var model = new AuditoriaLogsViewModel
            {
                Filtro = new AuditoriaFiltroDTO
                {
                    Modulo = modulo,
                    Accion = accion,
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    MaximoRegistros = maximoRegistros
                },
                Eventos = eventos
            };

            return View(model);
        }
    }
}
