using Microsoft.AspNetCore.Identity;
using PSA.EntidadesDTO.Entidades;

namespace PSA.AppCore.Servicios
{
    public class ServicioHashContrasena : IServicioHashContrasena
    {
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public ServicioHashContrasena()
        {
            _passwordHasher = new PasswordHasher<Usuario>();
        }

        public string GenerarHash(string contrasena)
        {
            var usuarioTemporal = new Usuario();
            return _passwordHasher.HashPassword(usuarioTemporal, contrasena);
        }

        public bool VerificarHash(string? hashAlmacenado, string contrasenaIngresada)
        {
            if (string.IsNullOrWhiteSpace(hashAlmacenado))
            {
                return false;
            }

            var usuarioTemporal = new Usuario();

            var resultado = _passwordHasher.VerifyHashedPassword(
                usuarioTemporal,
                hashAlmacenado,
                contrasenaIngresada
            );

            return resultado == PasswordVerificationResult.Success
                || resultado == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}