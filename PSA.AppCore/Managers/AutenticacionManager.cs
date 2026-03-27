using PSA.AppCore.Servicios;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.Entidades;

namespace PSA.AppCore.Managers
{
    public class AutenticacionManager
    {
        private readonly IServicioHashContrasena _servicioHashContrasena;

        public AutenticacionManager(IServicioHashContrasena servicioHashContrasena)
        {
            _servicioHashContrasena = servicioHashContrasena;
        }

        public Usuario ConstruirUsuarioParaRegistro(RegistrarUsuarioDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new Exception("El nombre es requerido.");

            if (string.IsNullOrWhiteSpace(dto.Apellidos))
                throw new Exception("Los apellidos son requeridos.");

            if (string.IsNullOrWhiteSpace(dto.CorreoElectronico))
                throw new Exception("El correo electrónico es requerido.");

            if (string.IsNullOrWhiteSpace(dto.Contrasena))
                throw new Exception("La contraseña es requerida.");

            if (dto.Contrasena != dto.ConfirmacionContrasena)
                throw new Exception("La contraseña y su confirmación no coinciden.");

            var usuario = new Usuario
            {
                Nombre = dto.Nombre.Trim(),
                Apellidos = dto.Apellidos.Trim(),
                CorreoElectronico = dto.CorreoElectronico.Trim(),
                IdRol = dto.IdRol,
                PasswordHash = _servicioHashContrasena.GenerarHash(dto.Contrasena),
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            return usuario;
        }

        public bool ValidarCredenciales(Usuario? usuario, string contrasenaIngresada)
        {
            if (usuario == null)
                return false;

            if (!usuario.Activo)
                return false;

            return _servicioHashContrasena.VerificarHash(
                usuario.PasswordHash,
                contrasenaIngresada
            );
        }
    }
}