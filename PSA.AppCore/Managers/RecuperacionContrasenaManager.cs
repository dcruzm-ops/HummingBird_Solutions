using System.Collections.Concurrent;
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

            var token = Guid.NewGuid().ToString("N");
            TokensActivos[token] = (email, DateTime.UtcNow.AddMinutes(30));
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

        public async Task RestablecerContrasenaAsync(string token, string nuevaContrasena)
        {
            if (!TokenEsValido(token))
            {
                throw new InvalidOperationException("El token es inválido o expiró.");
            }

            var (email, _) = TokensActivos[token];
            var hash = _servicioHash.GenerarHash(nuevaContrasena);
            await _usuarioDAO.ActualizarPasswordHashPorEmailAsync(email, hash);
            TokensActivos.TryRemove(token, out _);
        }
    }
}
