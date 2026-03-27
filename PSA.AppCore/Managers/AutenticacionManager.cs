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

            if (dto.IdRol <= 0)
                throw new Exception("El IdRol debe ser mayor a 0.");

            var rolExiste = await _usuarioDAO.ExisteRolAsync(dto.IdRol);
            if (!rolExiste)
                throw new Exception("El rol indicado no existe. Verifica el IdRol enviado.");

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
    }
}
