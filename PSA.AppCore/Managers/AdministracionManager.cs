using PSA.AppCore.Servicios;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;
using PSA.EntidadesDTO.Entidades;

namespace PSA.AppCore.Managers
{
    public class AdministracionManager
    {
        private static readonly string[] EstadosUsuarioPermitidos = { "Activo", "Inactivo", "Bloqueado" };
        private static readonly string[] TiposFactorPermitidos = { "Vegetacion", "RecursosHidricos", "Pendiente", "UsoSuelo" };

        private readonly UsuarioDAO _usuarioDAO;
        private readonly RolPermisoDAO _rolPermisoDAO;
        private readonly ConfiguracionPagoDAO _configuracionPagoDAO;
        private readonly CuentaBancariaDAO _cuentaBancariaDAO;
        private readonly AuditoriaLogDAO _auditoriaLogDAO;
        private readonly IServicioHashContrasena _servicioHashContrasena;

        public AdministracionManager(UsuarioDAO usuarioDAO, RolPermisoDAO rolPermisoDAO, ConfiguracionPagoDAO configuracionPagoDAO, CuentaBancariaDAO cuentaBancariaDAO, AuditoriaLogDAO auditoriaLogDAO, IServicioHashContrasena servicioHashContrasena)
        {
            _usuarioDAO = usuarioDAO;
            _rolPermisoDAO = rolPermisoDAO;
            _configuracionPagoDAO = configuracionPagoDAO;
            _cuentaBancariaDAO = cuentaBancariaDAO;
            _auditoriaLogDAO = auditoriaLogDAO;
            _servicioHashContrasena = servicioHashContrasena;
        }

        public Task<List<UsuarioAdminListadoDTO>> ObtenerUsuariosAsync(int? idRol = null) => _usuarioDAO.ObtenerUsuariosAdministracionAsync(idRol);
        public Task<List<RolDTO>> ObtenerRolesAsync() => _usuarioDAO.ObtenerRolesAsync();

        public Task<UsuarioAdminEdicionDTO?> ObtenerUsuarioAsync(int idUsuario)
        {
            if (idUsuario <= 0) throw new InvalidOperationException("El usuario solicitado no es válido.");
            return _usuarioDAO.ObtenerUsuarioEdicionAsync(idUsuario);
        }

        public async Task<int> CrearUsuarioAsync(UsuarioAdminEdicionDTO dto, int idAdministrador, string? ipOrigen)
        {
            await ValidarAdministradorActivoAsync(idAdministrador);
            ValidarUsuario(dto, true);
            if (await _usuarioDAO.ObtenerPorEmailAsync(dto.Email.Trim()) != null) throw new InvalidOperationException("Ya existe un usuario registrado con ese correo.");
            if (!await _usuarioDAO.ExisteRolAsync(dto.IdRol)) throw new InvalidOperationException("El rol indicado no existe o se encuentra inactivo.");

            var usuario = new Usuario
            {
                NombreCompleto = dto.NombreCompleto.Trim(),
                Email = dto.Email.Trim(),
                PasswordHash = _servicioHashContrasena.GenerarHash(dto.Contrasena!),
                IdRol = dto.IdRol,
                Estado = dto.Estado.Trim(),
                FechaCreacion = DateTime.Now
            };

            var idUsuario = await _usuarioDAO.CrearUsuarioAsync(usuario);
            await _auditoriaLogDAO.RegistrarEventoAsync(idAdministrador, "Administracion", "Usuarios", "USUARIO_CREADO_ADMIN", $"Se creó el usuario {usuario.Email} con rol {dto.IdRol}.", idUsuario, ipOrigen);
            return idUsuario;
        }

        public async Task ActualizarUsuarioAsync(UsuarioAdminEdicionDTO dto, int idAdministrador, string? ipOrigen)
        {
            await ValidarAdministradorActivoAsync(idAdministrador);
            if (dto.IdUsuario <= 0) throw new InvalidOperationException("El usuario que desea actualizar no es válido.");
            ValidarUsuario(dto, false);
            var actual = await _usuarioDAO.ObtenerPorIdAsync(dto.IdUsuario);
            if (actual == null) throw new InvalidOperationException("El usuario no existe.");
            var repetido = await _usuarioDAO.ObtenerPorEmailAsync(dto.Email.Trim());
            if (repetido != null && repetido.IdUsuario != dto.IdUsuario) throw new InvalidOperationException("Ya existe otro usuario registrado con ese correo.");
            if (!await _usuarioDAO.ExisteRolAsync(dto.IdRol)) throw new InvalidOperationException("El rol indicado no existe o se encuentra inactivo.");
            var actualizado = await _usuarioDAO.ActualizarUsuarioAsync(new Usuario
            {
                IdUsuario = dto.IdUsuario,
                NombreCompleto = dto.NombreCompleto.Trim(),
                Email = dto.Email.Trim(),
                IdRol = dto.IdRol,
                Estado = dto.Estado.Trim(),
                PasswordHash = string.IsNullOrWhiteSpace(dto.Contrasena) ? null : _servicioHashContrasena.GenerarHash(dto.Contrasena)
            });
            if (!actualizado) throw new InvalidOperationException("No fue posible actualizar el usuario.");
            await _auditoriaLogDAO.RegistrarEventoAsync(idAdministrador, "Administracion", "Usuarios", "USUARIO_ACTUALIZADO_ADMIN", $"Se actualizó el usuario {dto.Email.Trim()}.", dto.IdUsuario, ipOrigen);
        }

        public async Task EliminarUsuarioAsync(int idUsuario, int idAdministrador, string? ipOrigen)
        {
            await ValidarAdministradorActivoAsync(idAdministrador);
            if (idUsuario <= 0) throw new InvalidOperationException("El usuario indicado no es válido.");
            if (idUsuario == idAdministrador) throw new InvalidOperationException("No es posible eliminar el usuario administrador autenticado.");
            var usuario = await _usuarioDAO.ObtenerPorIdAsync(idUsuario);
            if (usuario == null) throw new InvalidOperationException("El usuario no existe.");
            if (await _usuarioDAO.TieneDependenciasAsync(idUsuario)) throw new InvalidOperationException("El usuario tiene dependencias operativas o de auditoría y no se puede eliminar físicamente. Cámbielo a Inactivo si necesita retirarlo.");
            var eliminado = await _usuarioDAO.EliminarUsuarioAsync(idUsuario);
            if (!eliminado) throw new InvalidOperationException("No fue posible eliminar el usuario.");
            await _auditoriaLogDAO.RegistrarEventoAsync(idAdministrador, "Administracion", "Usuarios", "USUARIO_ELIMINADO_ADMIN", $"Se eliminó el usuario {usuario.Email}.", idUsuario, ipOrigen);
        }

        public async Task<int> ReasignarClienteAsync(ReasignacionClienteDTO dto, int idAdministrador, string? ipOrigen)
        {
            await ValidarAdministradorActivoAsync(idAdministrador);
            if (dto.IdPropietario <= 0 || dto.IdIngenieroDestino <= 0) throw new InvalidOperationException("Debe indicar un propietario y un asesor válidos.");
            var propietario = await _usuarioDAO.ObtenerPorIdAsync(dto.IdPropietario);
            if (propietario == null || propietario.IdRol != 2) throw new InvalidOperationException("El cliente seleccionado no corresponde a un propietario válido.");
            var ingeniero = await _usuarioDAO.ObtenerPorIdAsync(dto.IdIngenieroDestino);
            if (ingeniero == null || ingeniero.IdRol != 3) throw new InvalidOperationException("El asesor seleccionado no corresponde a un ingeniero forestal válido.");
            var evaluacionesActualizadas = await _usuarioDAO.ReasignarPropietarioAIngenieroAsync(dto.IdPropietario, dto.IdIngenieroDestino);
            await _auditoriaLogDAO.RegistrarEventoAsync(idAdministrador, "Administracion", "EvaluacionesTecnicas", "CLIENTE_REASIGNADO_A_ASESOR", $"Se reasignó el propietario {propietario.Email} al asesor {ingeniero.Email}. Evaluaciones afectadas: {evaluacionesActualizadas}.", dto.IdPropietario, ipOrigen);
            return evaluacionesActualizadas;
        }

        public Task<List<RolPermisoDTO>> ObtenerRolesConPermisosAsync() => _rolPermisoDAO.ObtenerRolesConPermisosAsync();

        public async Task GuardarPermisosRolAsync(GuardarPermisosRolDTO dto, int idAdministrador, string? ipOrigen)
        {
            await ValidarAdministradorActivoAsync(idAdministrador);
            if (dto == null || dto.IdRol <= 0) throw new InvalidOperationException("Debe indicar un rol válido.");
            dto.CodigosPermiso ??= new List<string>();
            if (!await _usuarioDAO.ExisteRolAsync(dto.IdRol)) throw new InvalidOperationException("El rol indicado no existe o se encuentra inactivo.");
            await _rolPermisoDAO.GuardarPermisosRolAsync(dto.IdRol, dto.CodigosPermiso);
            await _auditoriaLogDAO.RegistrarEventoAsync(idAdministrador, "Administracion", "RolPermisos", "ROL_PERMISOS_ACTUALIZADOS", $"Se actualizaron los permisos del rol {dto.IdRol}.", dto.IdRol, ipOrigen, valorNuevo: string.Join(',', dto.CodigosPermiso.OrderBy(x => x)));
        }

        public Task<ConfiguracionPagoAdminDTO?> ObtenerConfiguracionVigenteAsync() => _configuracionPagoDAO.ObtenerVigenteAsync();
        public Task<List<ConfiguracionPagoAdminDTO>> ObtenerHistorialConfiguracionesAsync() => _configuracionPagoDAO.ObtenerHistorialAsync();

        public async Task<int> CrearConfiguracionPagoAsync(ConfiguracionPagoAdminDTO dto, int idAdministrador, string? ipOrigen)
        {
            ValidarConfiguracionPago(dto);
            var admin = await _usuarioDAO.ObtenerPorIdAsync(idAdministrador);
            if (admin == null || admin.IdRol != 1) throw new InvalidOperationException("Solo un administrador activo puede crear configuraciones de pago.");
            dto.CreadoPor = idAdministrador;
            var idConfiguracion = await _configuracionPagoDAO.CrearConfiguracionAsync(dto);
            await _auditoriaLogDAO.RegistrarEventoAsync(idAdministrador, "Administracion", "ConfiguracionesPago", "CONFIGURACION_PAGO_CREADA", $"Se creó la configuración de pago '{dto.NombreVersion}'.", idConfiguracion, ipOrigen);
            return idConfiguracion;
        }

        public Task<List<CuentaBancariaPendienteDTO>> ObtenerCuentasPendientesAsync() => _cuentaBancariaDAO.ObtenerPendientesAsync();

        public async Task ValidarCuentaBancariaAsync(ValidacionCuentaBancariaDTO dto, string? ipOrigen)
        {
            if (dto == null || dto.IdCuentaBancaria <= 0) throw new InvalidOperationException("Debe indicar una cuenta válida.");
            var admin = await _usuarioDAO.ObtenerPorIdAsync(dto.IdAdministrador);
            if (admin == null || admin.IdRol != 1) throw new InvalidOperationException("Solo un administrador activo puede validar cuentas bancarias.");
            var ok = await _cuentaBancariaDAO.ValidarCuentaAsync(dto);
            if (!ok) throw new InvalidOperationException("No fue posible registrar la validación. La cuenta pudo haber sido procesada previamente.");
            await _auditoriaLogDAO.RegistrarEventoAsync(dto.IdAdministrador, "Administracion", "CuentasBancarias", dto.Aprobada ? "CUENTA_BANCARIA_VALIDADA" : "CUENTA_BANCARIA_RECHAZADA", dto.Observaciones, dto.IdCuentaBancaria, ipOrigen);
        }

        public async Task<List<AuditoriaEventoDTO>> ObtenerEventosAuditoriaAsync(AuditoriaFiltroDTO filtro)
        {
            filtro ??= new AuditoriaFiltroDTO();
            if (filtro.MaximoRegistros <= 0 || filtro.MaximoRegistros > 200) filtro.MaximoRegistros = 50;
            if (filtro.FechaDesde.HasValue && filtro.FechaHasta.HasValue && filtro.FechaHasta.Value.Date < filtro.FechaDesde.Value.Date) throw new InvalidOperationException("La fecha final no puede ser menor que la fecha inicial.");
            return await _auditoriaLogDAO.ObtenerEventosAsync(filtro);
        }


        private async Task ValidarAdministradorActivoAsync(int idAdministrador)
        {
            if (idAdministrador <= 0) throw new InvalidOperationException("El administrador autenticado no es válido.");
            var admin = await _usuarioDAO.ObtenerPorIdAsync(idAdministrador);
            if (admin == null || admin.IdRol != 1 || !string.Equals(admin.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La operación solo puede ser ejecutada por un administrador activo.");
            }
        }

        private static void ValidarUsuario(UsuarioAdminEdicionDTO dto, bool requiereContrasena)
        {
            if (dto == null) throw new InvalidOperationException("Debe enviar la información del usuario.");
            if (string.IsNullOrWhiteSpace(dto.NombreCompleto)) throw new InvalidOperationException("El nombre completo es obligatorio.");
            if (string.IsNullOrWhiteSpace(dto.Email)) throw new InvalidOperationException("El correo es obligatorio.");
            if (!EstadosUsuarioPermitidos.Contains(dto.Estado?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("El estado del usuario no es válido.");
            if (dto.IdRol <= 0) throw new InvalidOperationException("Debe seleccionar un rol válido.");
            if (requiereContrasena && string.IsNullOrWhiteSpace(dto.Contrasena)) throw new InvalidOperationException("La contraseña es obligatoria para crear el usuario.");
            if (!string.IsNullOrWhiteSpace(dto.Contrasena) && dto.Contrasena != dto.ConfirmacionContrasena) throw new InvalidOperationException("La contraseña y la confirmación no coinciden.");
        }

        private static void ValidarConfiguracionPago(ConfiguracionPagoAdminDTO dto)
        {
            if (dto == null) throw new InvalidOperationException("Debe enviar una configuración de pago válida.");
            if (string.IsNullOrWhiteSpace(dto.NombreVersion)) throw new InvalidOperationException("El nombre de la versión es obligatorio.");
            if (dto.PrecioBasePorHectarea < 0) throw new InvalidOperationException("El precio base por hectárea no puede ser negativo.");
            if (dto.TopePorcentajeAjuste < 0 || dto.TopePorcentajeAjuste > 100) throw new InvalidOperationException("El tope de ajuste debe estar entre 0 y 100.");
            if (dto.FechaVigenciaHasta.HasValue && dto.FechaVigenciaHasta.Value.Date < dto.FechaVigenciaDesde.Date) throw new InvalidOperationException("La fecha de vigencia final no puede ser menor que la fecha inicial.");
            if (dto.Ajustes == null) return;
            foreach (var ajuste in dto.Ajustes.Where(x => !string.IsNullOrWhiteSpace(x.TipoFactor) || !string.IsNullOrWhiteSpace(x.ValorFactor)))
            {
                if (string.IsNullOrWhiteSpace(ajuste.TipoFactor) || string.IsNullOrWhiteSpace(ajuste.ValorFactor)) throw new InvalidOperationException("Cada ajuste configurado debe incluir tipo de factor y valor.");
                if (!TiposFactorPermitidos.Contains(ajuste.TipoFactor.Trim(), StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException($"El tipo de factor '{ajuste.TipoFactor}' no es válido.");
            }
        }
    }
}
