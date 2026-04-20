using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;
using PSA.WebAPI.Extensions;

namespace PSA.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "1")]
    public class AdministracionController : ControllerBase
    {
        private readonly AdministracionManager _administracionManager;

        public AdministracionController(AdministracionManager administracionManager)
        {
            _administracionManager = administracionManager;
        }

        [HttpGet("usuarios")]
        public async Task<ActionResult<List<UsuarioAdminListadoDTO>>> ObtenerUsuarios()
        {
            var usuarios = await _administracionManager.ObtenerUsuariosAsync();
            return Ok(usuarios);
        }

        [HttpGet("usuarios/{id:int}")]
        public async Task<ActionResult<UsuarioAdminEdicionDTO>> ObtenerUsuario(int id)
        {
            var usuario = await _administracionManager.ObtenerUsuarioAsync(id);
            return usuario == null ? NotFound() : Ok(usuario);
        }

        [HttpPost("usuarios")]
        public async Task<ActionResult<bool>> CrearUsuario([FromBody] UsuarioAdminEdicionDTO model)
        {
            await _administracionManager.CrearUsuarioAsync(model, GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
        }

        [HttpPut("usuarios/{id:int}")]
        public async Task<ActionResult<bool>> ActualizarUsuario(int id, [FromBody] UsuarioAdminEdicionDTO model)
        {
            model.IdUsuario = id;
            await _administracionManager.ActualizarUsuarioAsync(model, GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
        }

        [HttpDelete("usuarios/{id:int}")]
        public async Task<ActionResult<bool>> EliminarUsuario(int id)
        {
            await _administracionManager.EliminarUsuarioAsync(id, GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
        }

        [HttpGet("usuarios/propietarios")]
        public async Task<ActionResult<List<UsuarioAdminListadoDTO>>> ObtenerPropietarios()
        {
            var usuarios = await _administracionManager.ObtenerUsuariosAsync(2);
            return Ok(usuarios);
        }

        [HttpGet("usuarios/ingenieros")]
        public async Task<ActionResult<List<UsuarioAdminListadoDTO>>> ObtenerIngenieros()
        {
            var usuarios = await _administracionManager.ObtenerUsuariosAsync(3);
            return Ok(usuarios);
        }

        [HttpPost("usuarios/reasignar-cliente")]
        [HttpPost("reasignacion-cliente")]
        public async Task<ActionResult<bool>> ReasignarCliente([FromBody] ReasignacionClienteDTO model)
        {
            await _administracionManager.ReasignarClienteAsync(model, GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
        }

        [HttpGet("roles-basicos")]
        public async Task<ActionResult<List<RolDTO>>> ObtenerRolesBasicos()
        {
            try
            {
                var roles = await _administracionManager.ObtenerRolesAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    mensaje = "No fue posible obtener los roles básicos.",
                    detalle = ex.Message
                });
            }
        }

        [HttpPost("roles")]
        public async Task<ActionResult<int>> CrearRol([FromBody] CrearRolDTO model)
        {
            var idRol = await _administracionManager.CrearRolAsync(model);
            return Ok(idRol);
        }

        [HttpGet("roles-permisos")]
        public async Task<ActionResult<List<RolPermisoDTO>>> ObtenerRolesPermisos()
        {
            try
            {
                var roles = await _administracionManager.ObtenerRolesConPermisosAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    mensaje = "No fue posible obtener roles y permisos.",
                    detalle = ex.Message
                });
            }
        }

        [HttpGet("permisos")]
        public async Task<ActionResult<List<PermisoDTO>>> ObtenerPermisos()
        {
            try
            {
                var permisos = await _administracionManager.ObtenerPermisosAsync();
                return Ok(permisos);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    mensaje = "No fue posible obtener el catálogo de permisos.",
                    detalle = ex.Message
                });
            }
        }

        [HttpPost("roles-permisos")]
        public async Task<ActionResult<bool>> GuardarPermisosRolPost([FromBody] GuardarPermisosRolDTO model)
        {
            await _administracionManager.GuardarPermisosRolAsync(model, GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
        }

        [HttpPut("roles-permisos/{idRol:int}")]
        public async Task<ActionResult<bool>> GuardarPermisosRol(int idRol, [FromBody] GuardarPermisosRolDTO model)
        {
            model.IdRol = idRol;
            await _administracionManager.GuardarPermisosRolAsync(model, GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
        }

        [HttpGet("configuracion-pago/actual")]
        [HttpGet("configuracion-pago/vigente")]
        public async Task<ActionResult<ConfiguracionPagoAdminDTO>> ObtenerConfiguracionPagoActual()
        {
            var configuracion = await _administracionManager.ObtenerConfiguracionVigenteAsync();
            return configuracion == null ? NotFound() : Ok(configuracion);
        }

        [HttpGet("configuracion-pago/historial")]
        public async Task<ActionResult<List<ConfiguracionPagoAdminDTO>>> ObtenerHistorialConfiguracionPago()
        {
            var historial = await _administracionManager.ObtenerHistorialConfiguracionesAsync();
            return Ok(historial);
        }

        [HttpGet("configuracion-pago/{idConfiguracionPago:int}")]
        public async Task<ActionResult<ConfiguracionPagoAdminDTO>> ObtenerDetalleConfiguracionPago(int idConfiguracionPago)
        {
            var detalle = await _administracionManager.ObtenerConfiguracionDetalleAsync(idConfiguracionPago);
            return detalle == null ? NotFound() : Ok(detalle);
        }

        [HttpPost("configuracion-pago")]
        public async Task<ActionResult<bool>> CrearConfiguracionPago([FromBody] ConfiguracionPagoAdminDTO model)
        {
            await _administracionManager.CrearConfiguracionPagoAsync(model, GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
        }

        [HttpGet("cuentas-bancarias/pendientes")]
        public async Task<ActionResult<List<CuentaBancariaPendienteDTO>>> ObtenerCuentasPendientes()
        {
            try
            {
                var cuentas = await _administracionManager.ObtenerCuentasPendientesAsync();
                return Ok(cuentas);
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "No se pudieron obtener las cuentas bancarias pendientes.",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("cuentas-bancarias/validar")]
        [HttpPost("cuentas-bancarias/validacion")]
        public async Task<ActionResult<bool>> ValidarCuentaBancaria([FromBody] ValidarCuentaBancariaRequestDTO request)
        {
            try
            {
                var model = new ValidacionCuentaBancariaDTO
                {
                    IdCuentaBancaria = request.IdCuentaBancaria,
                    Aprobada = request.Aprobada,
                    Observaciones = request.Observaciones,
                    IdAdministrador = GetUserId()
                };

                await _administracionManager.ValidarCuentaBancariaAsync(model, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(true);
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "No se pudo validar la cuenta bancaria.",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("auditoria")]
        public async Task<ActionResult<List<AuditoriaEventoDTO>>> ObtenerAuditoria([FromQuery] string? modulo, [FromQuery] string? accion, [FromQuery] DateTime? fechaDesde, [FromQuery] DateTime? fechaHasta, [FromQuery] int maximoRegistros = 50)
        {
            var eventos = await _administracionManager.ObtenerEventosAuditoriaAsync(new AuditoriaFiltroDTO
            {
                Modulo = modulo,
                Accion = accion,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                MaximoRegistros = maximoRegistros
            });
            return Ok(eventos);
        }

        [HttpGet("auditoria/opciones-filtro")]
        public async Task<ActionResult<AuditoriaOpcionesFiltroDTO>> ObtenerOpcionesFiltroAuditoria([FromQuery] string? modulo)
        {
            var opciones = await _administracionManager.ObtenerOpcionesFiltroAuditoriaAsync(modulo);
            return Ok(opciones);
        }

        public class ValidarCuentaBancariaRequestDTO
        {
            public int IdCuentaBancaria { get; set; }
            public bool Aprobada { get; set; }
            public string? Observaciones { get; set; }
        }
    }
}
