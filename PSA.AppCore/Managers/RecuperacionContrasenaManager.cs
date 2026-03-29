using System.Collections.Concurrent;
using System.Security.Cryptography;
using PSA.AppCore.Servicios;
using PSA.DataAccess.DAO;

namespace PSA.AppCore.Managers
{
    public class RecuperacionContrasenaManager
    {
        private static readonly ConcurrentDictionary<string, (string Email, DateTime ExpiraEn)> TokensActivos = new();
        private readonly UsuarioDAO _usuarioDAO;
        private readonly IServicioHashContrasena _servicioHash;

        public RecuperacionContrasenaManager(UsuarioDAO usuarioDAO, IServicioHashContrasena servicioHash)
        {
            _usuarioDAO = usuarioDAO;
            _servicioHash = servicioHash;
        }

        public async Task<string> GenerarTokenAsync(string email)
        {
            var usuario = await _usuarioDAO.ObtenerPorEmailAsync(email);
            if (usuario == null)
            {
                throw new InvalidOperationException("No existe una cuenta asociada al correo indicado.");
            }

            // Invalida tokens previos del mismo correo para dejar uno vigente.
            foreach (var item in TokensActivos.Where(t => t.Value.Email.Equals(email, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                TokensActivos.TryRemove(item.Key, out _);
            }

            var token = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            TokensActivos[token] = (email, DateTime.UtcNow.AddMinutes(3));
            return token;
        }

        public bool TokenEsValido(string token)
        {
            if (!TokensActivos.TryGetValue(token, out var valor))
            {
                return false;
            }

            if (valor.ExpiraEn < DateTime.UtcNow)
            {
                TokensActivos.TryRemove(token, out _);
                return false;
            }

            return true;
        }

        public string ObtenerEmailPorToken(string token)
        {
            if (!TokenEsValido(token))
            {
                throw new InvalidOperationException("El token es inválido o expiró.");
            }

            return TokensActivos[token].Email;
        }

        public async Task RestablecerContrasenaAsync(string token, string nuevaContrasena)
        {
            var email = ObtenerEmailPorToken(token);
            var hash = _servicioHash.GenerarHash(nuevaContrasena);
            await _usuarioDAO.ActualizarPasswordHashPorEmailAsync(email, hash);
            TokensActivos.TryRemove(token, out _);
        }
    }
}
