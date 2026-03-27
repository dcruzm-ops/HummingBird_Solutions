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
            if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
                throw new Exception("El nombre completo es requerido.");

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new Exception("El correo electrónico es requerido.");

            if (string.IsNullOrWhiteSpace(dto.Contrasena))
                throw new Exception("La contraseña es requerida.");

            if (dto.Contrasena != dto.ConfirmacionContrasena)
                throw new Exception("La contraseña y la confirmación no coinciden.");

            var usuarioExistente = await _usuarioDAO.ObtenerPorEmailAsync(dto.Email.Trim());

            if (usuarioExistente != null)
                throw new Exception("Ya existe un usuario registrado con ese correo.");

            var usuario = new Usuario
            {
                NombreCompleto = dto.NombreCompleto.Trim(),
                Email = dto.Email.Trim(),
                PasswordHash = _servicioHashContrasena.GenerarHash(dto.Contrasena),
                IdRol = dto.IdRol,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UltimoAcceso = null
            };

            return await _usuarioDAO.CrearUsuarioAsync(usuario);
        }
    }
}