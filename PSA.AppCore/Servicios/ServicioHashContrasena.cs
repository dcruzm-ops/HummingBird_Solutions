using Microsoft.AspNetCore.Identity;
using PSA.EntidadesDTO.Entidades;
using System.Security.Cryptography;
using System.Text;

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
            var resultado = _passwordHasher.VerifyHashedPassword(usuarioTemporal, hashAlmacenado, contrasenaIngresada);

            if (resultado == PasswordVerificationResult.Success || resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return true;
            }

            return EsHashSha256Hex(hashAlmacenado)
                && string.Equals(hashAlmacenado, CalcularSha256Hex(contrasenaIngresada), StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsHashSha256Hex(string valor)
            => valor.Length == 64 && valor.All(Uri.IsHexDigit);

        private static string CalcularSha256Hex(string texto)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
            return Convert.ToHexString(bytes);
        }
    }
}
