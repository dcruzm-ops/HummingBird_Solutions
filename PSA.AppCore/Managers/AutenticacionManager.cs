using PSA.AppCore.Servicios;
using PSA.AppCore.Services.Security;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.Usuarios;
using PSA.EntidadesDTO.Entidades;

namespace PSA.AppCore.Managers
{
    public class AutenticacionManager
    {
        private readonly IServicioHashContrasena _servicioHashContrasena;
        private readonly UsuarioDAO _usuarioDAO;
        private readonly AuditoriaLogDAO _auditoriaLogDAO;
        private readonly IPasswordPolicy _passwordPolicy;

        public AutenticacionManager(
            IServicioHashContrasena servicioHashContrasena,
            UsuarioDAO usuarioDAO,
            AuditoriaLogDAO auditoriaLogDAO,
            IPasswordPolicy passwordPolicy)
        {
            _servicioHashContrasena = servicioHashContrasena;
            _usuarioDAO = usuarioDAO;
            _auditoriaLogDAO = auditoriaLogDAO;
            _passwordPolicy = passwordPolicy;
        }

        public async Task<int> RegistrarUsuarioAsync(RegistrarUsuarioDTO dto)
        {
            const int idRolPropietario = 2;

            if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
                throw new Exception("El nombre completo es requerido.");

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new Exception("El correo electrónico es requerido.");

            if (string.IsNullOrWhiteSpace(dto.Contrasena))
                throw new Exception("La contraseña es requerida.");

            if (dto.Contrasena != dto.ConfirmacionContrasena)
                throw new Exception("La contraseña y la confirmación no coinciden.");

            if (!_passwordPolicy.IsValid(dto.Contrasena))
                throw new Exception(_passwordPolicy.RequirementsMessage);

            var rolExiste = await _usuarioDAO.ExisteRolAsync(idRolPropietario);
            if (!rolExiste)
                throw new Exception("No existe el rol por defecto 'Propietario' (IdRol = 2).");

            var usuarioExistente = await _usuarioDAO.ObtenerPorEmailAsync(dto.Email.Trim());

            if (usuarioExistente != null)
                throw new Exception("Ya existe un usuario registrado con ese correo.");

            var usuario = new Usuario
            {
                NombreCompleto = dto.NombreCompleto.Trim(),
                Email = dto.Email.Trim(),
                PasswordHash = _servicioHashContrasena.GenerarHash(dto.Contrasena),
                IdRol = idRolPropietario,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UltimoAcceso = null
            };

            return await _usuarioDAO.CrearUsuarioAsync(usuario);
        }

        public async Task<RespuestaInicioSesionDTO> IniciarSesionAsync(InicioSesionDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new Exception("El correo electrónico es requerido.");

            if (string.IsNullOrWhiteSpace(dto.Contrasena))
                throw new Exception("La contraseña es requerida.");

            var usuario = await _usuarioDAO.ObtenerPorEmailAsync(dto.Email.Trim());

            if (usuario == null)
            {
                await _auditoriaLogDAO.RegistrarEventoAsync(
                    idUsuario: null,
                    modulo: "Autenticacion",
                    tablaAfectada: "Usuarios",
                    accion: "LOGIN_FALLIDO",
                    detalle: $"Intento fallido para correo no registrado: {dto.Email.Trim()}"
                );

                throw new Exception("Credenciales inválidas.");
            }

            var contrasenaValida = _servicioHashContrasena.VerificarHash(
                usuario.PasswordHash,
                dto.Contrasena
            );

            if (!contrasenaValida)
            {
                await _auditoriaLogDAO.RegistrarEventoAsync(
                    idUsuario: usuario.IdUsuario,
                    modulo: "Autenticacion",
                    tablaAfectada: "Usuarios",
                    idRegistroAfectado: usuario.IdUsuario,
                    accion: "LOGIN_FALLIDO",
                    detalle: $"Contraseña inválida para el usuario {usuario.Email}"
                );

                throw new Exception("Credenciales inválidas.");
            }

            var fechaAcceso = DateTime.Now;
            await _usuarioDAO.ActualizarUltimoAccesoAsync(usuario.IdUsuario, fechaAcceso);

            await _auditoriaLogDAO.RegistrarEventoAsync(
                idUsuario: usuario.IdUsuario,
                modulo: "Autenticacion",
                tablaAfectada: "Usuarios",
                idRegistroAfectado: usuario.IdUsuario,
                accion: "LOGIN_EXITOSO",
                detalle: $"Inicio de sesión exitoso para {usuario.Email}"
            );

            return new RespuestaInicioSesionDTO
            {
                IdUsuario = usuario.IdUsuario,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                IdRol = usuario.IdRol,
                UltimoAcceso = fechaAcceso,
                Mensaje = "Inicio de sesión exitoso."
            };
        }

        public async Task AsignarRolAsync(AsignarRolUsuarioDTO dto)
        {
            if (dto.IdUsuario <= 0)
                throw new Exception("El Id del usuario es inválido.");

            if (dto.IdRol <= 0)
                throw new Exception("El Id del rol es inválido.");

            var rolExiste = await _usuarioDAO.ExisteRolAsync(dto.IdRol);
            if (!rolExiste)
                throw new Exception("El rol indicado no existe.");

            await _usuarioDAO.AsignarRolAsync(dto.IdUsuario, dto.IdRol);

            await _auditoriaLogDAO.RegistrarEventoAsync(
                idUsuario: dto.IdUsuario,
                modulo: "Autenticacion",
                tablaAfectada: "Usuarios",
                idRegistroAfectado: dto.IdUsuario,
                accion: "ASIGNACION_ROL",
                detalle: $"Rol {dto.IdRol} asignado al usuario {dto.IdUsuario}."
            );
        }
    }
}
