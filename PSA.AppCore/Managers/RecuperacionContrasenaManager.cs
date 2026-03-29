using System.Security.Cryptography;
using PSA.AppCore.Servicios;
using PSA.DataAccess.DAO;

namespace PSA.AppCore.Managers
{
    public class RecuperacionContrasenaManager
    {
        private readonly UsuarioDAO _usuarioDAO;
        private readonly TokenRecuperacionDAO _tokenRecuperacionDAO;
        private readonly IServicioHashContrasena _servicioHash;

        public RecuperacionContrasenaManager(
            UsuarioDAO usuarioDAO,
            TokenRecuperacionDAO tokenRecuperacionDAO,
            IServicioHashContrasena servicioHash)
        {
            _usuarioDAO = usuarioDAO;
            _tokenRecuperacionDAO = tokenRecuperacionDAO;
            _servicioHash = servicioHash;
        }

        public async Task<string> GenerarTokenAsync(string email)
        {
            var usuario = await _usuarioDAO.ObtenerPorEmailAsync(email);
            if (usuario == null)
            {
                throw new InvalidOperationException("No existe una cuenta asociada al correo indicado.");
            }

            await _tokenRecuperacionDAO.InvalidarTokensActivosPorUsuarioAsync(usuario.IdUsuario);

            var token = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            await _tokenRecuperacionDAO.CrearTokenAsync(usuario.IdUsuario, token, DateTime.UtcNow.AddMinutes(3));
            return token;
        }

        public async Task<bool> TokenEsValidoAsync(string token)
        {
            var registro = await _tokenRecuperacionDAO.ObtenerTokenVigenteAsync(token);
            return registro != null;
        }

        // Compatibilidad con llamadas existentes que usen versión sincrónica.
        public bool TokenEsValido(string token)
        {
            return TokenEsValidoAsync(token).GetAwaiter().GetResult();
        }

        public async Task RestablecerContrasenaAsync(string token, string nuevaContrasena)
        {
            var registro = await _tokenRecuperacionDAO.ObtenerTokenVigenteAsync(token);
            if (registro == null)
            {
                throw new InvalidOperationException("El token es inválido o expiró.");
            }

            var usuario = await _usuarioDAO.ObtenerPorIdAsync(registro.IdUsuario);
            if (usuario == null)
            {
                throw new InvalidOperationException("No se encontró el usuario asociado al token.");
            }

            var hash = _servicioHash.GenerarHash(nuevaContrasena);
            await _usuarioDAO.ActualizarPasswordHashPorEmailAsync(usuario.Email, hash);
            await _tokenRecuperacionDAO.MarcarTokenComoUsadoAsync(registro.IdToken);
        }
    }
}
