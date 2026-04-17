using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;

namespace PSA.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdministracionController : ControllerBase
    {
        private readonly AdministracionManager _administracionManager;
        private const int AdminSistemaId = 1;

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
            await _administracionManager.CrearUsuarioAsync(model, AdminSistemaId, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
        }

        [HttpPut("usuarios/{id:int}")]
        public async Task<ActionResult<bool>> ActualizarUsuario(int id, [FromBody] UsuarioAdminEdicionDTO model)
        {
            model.IdUsuario = id;
            await _administracionManager.ActualizarUsuarioAsync(model, AdminSistemaId, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
        }

        [HttpDelete("usuarios/{id:int}")]
        public async Task<ActionResult<bool>> EliminarUsuario(int id)
        {
            await _administracionManager.EliminarUsuarioAsync(id, AdminSistemaId, HttpContext.Connection.RemoteIpAddress?.ToString());
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
            await _administracionManager.ReasignarClienteAsync(model, AdminSistemaId, HttpContext.Connection.RemoteIpAddress?.ToString());
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
            await _administracionManager.GuardarPermisosRolAsync(model, AdminSistemaId, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
        }

        [HttpPut("roles-permisos/{idRol:int}")]
        public async Task<ActionResult<bool>> GuardarPermisosRol(int idRol, [FromBody] GuardarPermisosRolDTO model)
        {
            model.IdRol = idRol;
            await _administracionManager.GuardarPermisosRolAsync(model, AdminSistemaId, HttpContext.Connection.RemoteIpAddress?.ToString());
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

        [HttpPost("configuracion-pago")]
        public async Task<ActionResult<bool>> CrearConfiguracionPago([FromBody] ConfiguracionPagoAdminDTO model)
        {
            await _administracionManager.CrearConfiguracionPagoAsync(model, AdminSistemaId, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
        }

        [HttpGet("cuentas-bancarias/pendientes")]
        public async Task<ActionResult<List<CuentaBancariaPendienteDTO>>> ObtenerCuentasPendientes()
        {
            var cuentas = await _administracionManager.ObtenerCuentasPendientesAsync();
            return Ok(cuentas);
        }

        [HttpPost("cuentas-bancarias/validar")]
        [HttpPost("cuentas-bancarias/validacion")]
        public async Task<ActionResult<bool>> ValidarCuentaBancaria([FromBody] ValidacionCuentaBancariaDTO model)
        {
            model.IdAdministrador = AdminSistemaId;
            await _administracionManager.ValidarCuentaBancariaAsync(model, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(true);
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
    }
}
