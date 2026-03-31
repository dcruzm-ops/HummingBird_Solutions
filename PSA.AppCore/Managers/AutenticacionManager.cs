using PSA.AppCore.Servicios;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.Entidades;

namespace PSA.AppCore.Managers
{
    public class AutenticacionManager
    {
        private readonly IServicioHashContrasena _servicioHashContrasena;
        private readonly UsuarioDAO _usuarioDAO;

        public AutenticacionManager(
            IServicioHashContrasena servicioHashContrasena,
            UsuarioDAO usuarioDAO)
        {
            _servicioHashContrasena = servicioHashContrasena;
            _usuarioDAO = usuarioDAO;
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
                throw new Exception("Credenciales inválidas.");

            var contrasenaValida = _servicioHashContrasena.VerificarHash(
                usuario.PasswordHash,
                dto.Contrasena
            );

            if (!contrasenaValida)
                throw new Exception("Credenciales inválidas.");

            var fechaAcceso = DateTime.Now;
            await _usuarioDAO.ActualizarUltimoAccesoAsync(usuario.IdUsuario, fechaAcceso);

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
    }
}
