using PSA.AppCore.Servicios;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;
using PSA.EntidadesDTO.Entidades;

namespace PSA.AppCore.Managers;

public class AdministracionManager
{
    private readonly UsuarioDAO _usuarioDao;
    private readonly RolPermisoDAO _rolPermisoDao;
    private readonly ConfiguracionPagoDAO _configuracionPagoDao;
    private readonly CuentaBancariaDAO _cuentaBancariaDao;
    private readonly AuditoriaLogDAO _auditoriaLogDao;
    private readonly IServicioHashContrasena _servicioHashContrasena;

    public AdministracionManager(
        UsuarioDAO usuarioDao,
        RolPermisoDAO rolPermisoDao,
        ConfiguracionPagoDAO configuracionPagoDao,
        CuentaBancariaDAO cuentaBancariaDao,
        AuditoriaLogDAO auditoriaLogDao,
        IServicioHashContrasena servicioHashContrasena)
    {
        _usuarioDao = usuarioDao;
        _rolPermisoDao = rolPermisoDao;
        _configuracionPagoDao = configuracionPagoDao;
        _cuentaBancariaDao = cuentaBancariaDao;
        _auditoriaLogDao = auditoriaLogDao;
        _servicioHashContrasena = servicioHashContrasena;
    }

    public Task<List<UsuarioAdminListadoDTO>> ObtenerUsuariosAsync(int? idRol = null)
        => _usuarioDao.ObtenerUsuariosAdminAsync(idRol);

    public Task<UsuarioAdminEdicionDTO?> ObtenerUsuarioAsync(int idUsuario)
        => _usuarioDao.ObtenerUsuarioAdminPorIdAsync(idUsuario);

    public async Task CrearUsuarioAsync(UsuarioAdminEdicionDTO model, int idAdmin, string? ip)
    {
        ValidarUsuarioAdmin(model, requiereContrasena: true);

        var existente = await _usuarioDao.ObtenerPorEmailAsync(model.Email.Trim());
        if (existente != null)
        {
            throw new InvalidOperationException("Ya existe un usuario con el correo indicado.");
        }

        var nuevo = new Usuario
        {
            NombreCompleto = model.NombreCompleto.Trim(),
            Email = model.Email.Trim(),
            PasswordHash = _servicioHashContrasena.GenerarHash(model.Contrasena!),
            IdRol = model.IdRol,
            Estado = model.Estado,
            FechaCreacion = DateTime.UtcNow,
            UltimoAcceso = null
        };

        var idCreado = await _usuarioDao.CrearUsuarioAsync(nuevo);

        await _auditoriaLogDao.RegistrarEventoAsync(
            idUsuario: idAdmin,
            modulo: "Administracion",
            tablaAfectada: "Usuarios",
            accion: "CREAR_USUARIO",
            detalle: $"Usuario creado: {model.Email}",
            idRegistroAfectado: idCreado,
            ipOrigen: ip);
    }

    public async Task ActualizarUsuarioAsync(UsuarioAdminEdicionDTO model, int idAdmin, string? ip)
    {
        if (model.IdUsuario <= 0)
        {
            throw new InvalidOperationException("El Id del usuario es inválido.");
        }

        ValidarUsuarioAdmin(model, requiereContrasena: false);

        string? nuevoHash = null;
        if (!string.IsNullOrWhiteSpace(model.Contrasena))
        {
            if (model.Contrasena != model.ConfirmacionContrasena)
            {
                throw new InvalidOperationException("La contraseña y su confirmación no coinciden.");
            }

            nuevoHash = _servicioHashContrasena.GenerarHash(model.Contrasena);
        }

        var actualizados = await _usuarioDao.ActualizarUsuarioAdminAsync(model, nuevoHash);
        if (actualizados <= 0)
        {
            throw new InvalidOperationException("No se encontró el usuario a actualizar.");
        }

        await _auditoriaLogDao.RegistrarEventoAsync(
            idUsuario: idAdmin,
            modulo: "Administracion",
            tablaAfectada: "Usuarios",
            accion: "ACTUALIZAR_USUARIO",
            detalle: $"Usuario actualizado: {model.IdUsuario}",
            idRegistroAfectado: model.IdUsuario,
            ipOrigen: ip);
    }

    public async Task EliminarUsuarioAsync(int idUsuario, int idAdmin, string? ip)
    {
        if (idUsuario <= 0)
        {
            throw new InvalidOperationException("El Id del usuario es inválido.");
        }

        var filas = await _usuarioDao.EliminarUsuarioAdminAsync(idUsuario);
        if (filas <= 0)
        {
            throw new InvalidOperationException("No se encontró el usuario a desactivar.");
        }

        await _auditoriaLogDao.RegistrarEventoAsync(
            idUsuario: idAdmin,
            modulo: "Administracion",
            tablaAfectada: "Usuarios",
            accion: "DESACTIVAR_USUARIO",
            detalle: $"Usuario desactivado: {idUsuario}",
            idRegistroAfectado: idUsuario,
            ipOrigen: ip);
    }

    public async Task ReasignarClienteAsync(ReasignacionClienteDTO model, int idAdmin, string? ip)
    {
        if (model.IdPropietario <= 0 || model.IdIngenieroDestino <= 0)
        {
            throw new InvalidOperationException("Debe indicar propietario e ingeniero destino válidos.");
        }

        await _usuarioDao.ReasignarClientesAIngenieroAsync(model.IdPropietario, model.IdIngenieroDestino);

        await _auditoriaLogDao.RegistrarEventoAsync(
            idUsuario: idAdmin,
            modulo: "Administracion",
            tablaAfectada: "EvaluacionesTecnicas",
            accion: "REASIGNAR_CLIENTE",
            detalle: $"Propietario {model.IdPropietario} reasignado al ingeniero {model.IdIngenieroDestino}",
            idRegistroAfectado: model.IdPropietario,
            ipOrigen: ip);
    }

    public Task<List<RolDTO>> ObtenerRolesAsync()
        => _rolPermisoDao.ObtenerRolesAsync();

    public Task<int> CrearRolAsync(CrearRolDTO dto)
        => _rolPermisoDao.CrearRolAsync(dto);

    public Task<List<RolPermisoDTO>> ObtenerRolesConPermisosAsync()
        => _rolPermisoDao.ObtenerRolesConPermisosAsync();

    public Task<List<PermisoDTO>> ObtenerPermisosAsync()
        => _rolPermisoDao.ObtenerPermisosAsync();

    public async Task GuardarPermisosRolAsync(GuardarPermisosRolDTO model, int idAdmin, string? ip)
    {
        await _rolPermisoDao.GuardarPermisosRolAsync(model);

        await _auditoriaLogDao.RegistrarEventoAsync(
            idUsuario: idAdmin,
            modulo: "Administracion",
            tablaAfectada: "RolesPermisos",
            accion: "GUARDAR_PERMISOS_ROL",
            detalle: $"Permisos actualizados para rol {model.IdRol}",
            idRegistroAfectado: model.IdRol,
            ipOrigen: ip);
    }

    public Task<ConfiguracionPagoAdminDTO?> ObtenerConfiguracionVigenteAsync()
        => _configuracionPagoDao.ObtenerConfiguracionVigenteAsync();

    public Task<List<ConfiguracionPagoAdminDTO>> ObtenerHistorialConfiguracionesAsync()
        => _configuracionPagoDao.ObtenerHistorialAsync();

    public Task<ConfiguracionPagoAdminDTO?> ObtenerConfiguracionDetalleAsync(int idConfiguracionPago)
        => _configuracionPagoDao.ObtenerConfiguracionDetalleAsync(idConfiguracionPago);

    public async Task CrearConfiguracionPagoAsync(ConfiguracionPagoAdminDTO model, int idAdmin, string? ip)
    {
        model.CreadoPor = idAdmin;
        var idConfiguracion = await _configuracionPagoDao.CrearConfiguracionAsync(model);

        await _auditoriaLogDao.RegistrarEventoAsync(
            idUsuario: idAdmin,
            modulo: "Administracion",
            tablaAfectada: "ConfiguracionesPago",
            accion: "CREAR_CONFIGURACION_PAGO",
            detalle: $"Nueva configuración de pago creada: {model.NombreVersion}",
            idRegistroAfectado: idConfiguracion,
            ipOrigen: ip);
    }

    public Task<List<CuentaBancariaPendienteDTO>> ObtenerCuentasPendientesAsync()
        => _cuentaBancariaDao.ObtenerPendientesValidacionAsync();

    public async Task ValidarCuentaBancariaAsync(ValidacionCuentaBancariaDTO model, string? ip)
    {
        await _cuentaBancariaDao.ValidarCuentaAsync(model);

        await _auditoriaLogDao.RegistrarEventoAsync(
            idUsuario: model.IdAdministrador,
            modulo: "Administracion",
            tablaAfectada: "CuentasBancarias",
            accion: "VALIDAR_CUENTA_BANCARIA",
            detalle: $"Cuenta {model.IdCuentaBancaria} validada con resultado: {(model.Aprobada ? "Validada" : "Rechazada")}",
            idRegistroAfectado: model.IdCuentaBancaria,
            ipOrigen: ip);
    }

    public Task<List<AuditoriaEventoDTO>> ObtenerEventosAuditoriaAsync(AuditoriaFiltroDTO filtro)
        => _auditoriaLogDao.ObtenerEventosAsync(filtro);

    public Task<AuditoriaOpcionesFiltroDTO> ObtenerOpcionesFiltroAuditoriaAsync(string? modulo = null)
        => _auditoriaLogDao.ObtenerOpcionesFiltroAsync(modulo);

    private static void ValidarUsuarioAdmin(UsuarioAdminEdicionDTO model, bool requiereContrasena)
    {
        if (string.IsNullOrWhiteSpace(model.NombreCompleto))
        {
            throw new InvalidOperationException("El nombre completo es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(model.Email))
        {
            throw new InvalidOperationException("El correo es obligatorio.");
        }

        if (model.IdRol <= 0)
        {
            throw new InvalidOperationException("Debe indicar un rol válido.");
        }

        if (string.IsNullOrWhiteSpace(model.Estado))
        {
            throw new InvalidOperationException("Debe indicar un estado válido.");
        }

        if (requiereContrasena && string.IsNullOrWhiteSpace(model.Contrasena))
        {
            throw new InvalidOperationException("La contraseña es obligatoria para crear usuarios.");
        }
    }
}
