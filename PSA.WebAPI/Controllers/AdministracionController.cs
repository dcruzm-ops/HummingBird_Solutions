using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdministracionController : ControllerBase
    {
        private readonly AdministracionManager _administracionManager;

        public AdministracionController(AdministracionManager administracionManager)
        {
            _administracionManager = administracionManager;
        }

        [HttpGet("usuarios")]
        public async Task<ActionResult<List<UsuarioAdminListadoDTO>>> ObtenerUsuariosAsync([FromQuery] int? idRol = null)
            => Ok(await _administracionManager.ObtenerUsuariosAsync(idRol));

        [HttpGet("usuarios/{idUsuario:int}")]
        public async Task<ActionResult<UsuarioAdminEdicionDTO>> ObtenerUsuarioAsync(int idUsuario)
        {
            try
            {
                var usuario = await _administracionManager.ObtenerUsuarioAsync(idUsuario);
                return usuario == null ? NotFound(new { Mensaje = "No se encontró el usuario solicitado." }) : Ok(usuario);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
        }

        [HttpPost("usuarios")]
        public async Task<IActionResult> CrearUsuarioAsync([FromBody] UsuarioAdminEdicionDTO dto, [FromQuery] int idAdministrador)
        {
            try
            {
                var idUsuario = await _administracionManager.CrearUsuarioAsync(dto, idAdministrador, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(new { IdUsuario = idUsuario, Mensaje = "Usuario creado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
        }

        [HttpPut("usuarios/{idUsuario:int}")]
        public async Task<IActionResult> ActualizarUsuarioAsync(int idUsuario, [FromBody] UsuarioAdminEdicionDTO dto, [FromQuery] int idAdministrador)
        {
            try
            {
                dto.IdUsuario = idUsuario;
                await _administracionManager.ActualizarUsuarioAsync(dto, idAdministrador, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(new { Mensaje = "Usuario actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
        }

        [HttpDelete("usuarios/{idUsuario:int}")]
        public async Task<IActionResult> EliminarUsuarioAsync(int idUsuario, [FromQuery] int idAdministrador)
        {
            try
            {
                await _administracionManager.EliminarUsuarioAsync(idUsuario, idAdministrador, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(new { Mensaje = "Usuario eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
        }

        [HttpPost("usuarios/reasignacion-cliente")]
        public async Task<IActionResult> ReasignarClienteAsync([FromBody] ReasignacionClienteDTO dto, [FromQuery] int idAdministrador)
        {
            try
            {
                var evaluacionesActualizadas = await _administracionManager.ReasignarClienteAsync(dto, idAdministrador, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(new { EvaluacionesActualizadas = evaluacionesActualizadas, Mensaje = "Cliente reasignado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
        }

        [HttpGet("roles")]
        public async Task<ActionResult<List<RolDTO>>> ObtenerRolesAsync() => Ok(await _administracionManager.ObtenerRolesAsync());

        [HttpGet("roles-permisos")]
        public async Task<ActionResult<List<RolPermisoDTO>>> ObtenerRolesConPermisosAsync() => Ok(await _administracionManager.ObtenerRolesConPermisosAsync());

        [HttpPut("roles-permisos/{idRol:int}")]
        public async Task<IActionResult> GuardarPermisosRolAsync(int idRol, [FromBody] GuardarPermisosRolDTO dto, [FromQuery] int idAdministrador)
        {
            try
            {
                dto.IdRol = idRol;
                await _administracionManager.GuardarPermisosRolAsync(dto, idAdministrador, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(new { Mensaje = "Permisos actualizados correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
        }

        [HttpGet("configuraciones-pago/vigente")]
        public async Task<ActionResult<ConfiguracionPagoAdminDTO>> ObtenerConfiguracionVigenteAsync()
        {
            var configuracion = await _administracionManager.ObtenerConfiguracionVigenteAsync();
            return configuracion == null ? NotFound(new { Mensaje = "No existe una configuración de pago vigente." }) : Ok(configuracion);
        }

        [HttpGet("configuraciones-pago/historial")]
        public async Task<ActionResult<List<ConfiguracionPagoAdminDTO>>> ObtenerHistorialConfiguracionesAsync()
            => Ok(await _administracionManager.ObtenerHistorialConfiguracionesAsync());

        [HttpPost("configuraciones-pago")]
        public async Task<IActionResult> CrearConfiguracionPagoAsync([FromBody] ConfiguracionPagoAdminDTO dto, [FromQuery] int idAdministrador)
        {
            try
            {
                var idConfiguracion = await _administracionManager.CrearConfiguracionPagoAsync(dto, idAdministrador, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(new { IdConfiguracionPago = idConfiguracion, Mensaje = "Configuración de pago creada correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
        }

        [HttpGet("cuentas-bancarias/pendientes")]
        public async Task<ActionResult<List<CuentaBancariaPendienteDTO>>> ObtenerCuentasPendientesAsync() => Ok(await _administracionManager.ObtenerCuentasPendientesAsync());

        [HttpPost("cuentas-bancarias/validacion")]
        public async Task<IActionResult> ValidarCuentaAsync([FromBody] ValidacionCuentaBancariaDTO dto)
        {
            try
            {
                await _administracionManager.ValidarCuentaBancariaAsync(dto, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(new { Mensaje = "Validación registrada correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
        }

        [HttpGet("auditoria")]
        public async Task<ActionResult<List<AuditoriaEventoDTO>>> ObtenerAuditoriaAsync([FromQuery] AuditoriaFiltroDTO filtro)
        {
            try
            {
                return Ok(await _administracionManager.ObtenerEventosAuditoriaAsync(filtro));
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
        }
    }
}
