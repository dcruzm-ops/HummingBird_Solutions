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
        private readonly AuditoriaLogDAO _auditoriaLogDAO;

        public RecuperacionContrasenaManager(
            UsuarioDAO usuarioDAO,
            TokenRecuperacionDAO tokenRecuperacionDAO,
            IServicioHashContrasena servicioHash,
            AuditoriaLogDAO auditoriaLogDAO)
        {
            _usuarioDAO = usuarioDAO;
            _tokenRecuperacionDAO = tokenRecuperacionDAO;
            _servicioHash = servicioHash;
            _auditoriaLogDAO = auditoriaLogDAO;
        }

        public async Task<(string Token, string NombreUsuario)> GenerarTokenConNombreAsync(string email)
        {
            var usuario = await _usuarioDAO.ObtenerPorEmailAsync(email);
            if (usuario == null)
            {
                await _auditoriaLogDAO.RegistrarEventoAsync(
                    idUsuario: null,
                    modulo: "Autenticacion",
                    tablaAfectada: "TokensRecuperacion",
                    accion: "TOKEN_RECUPERACION_FALLIDO",
                    detalle: $"Solicitud de recuperación para correo inexistente: {email}"
                );

                throw new InvalidOperationException("No existe una cuenta asociada al correo indicado.");
            }

            await _tokenRecuperacionDAO.InvalidarTokensActivosPorUsuarioAsync(usuario.IdUsuario);

            var token = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            await _tokenRecuperacionDAO.CrearTokenAsync(usuario.IdUsuario, token, DateTime.UtcNow.AddMinutes(3));

            await _auditoriaLogDAO.RegistrarEventoAsync(
                idUsuario: usuario.IdUsuario,
                modulo: "Autenticacion",
                tablaAfectada: "TokensRecuperacion",
                idRegistroAfectado: usuario.IdUsuario,
                accion: "TOKEN_RECUPERACION_GENERADO",
                detalle: "Se generó token de recuperación de contraseña."
            );

            return (token, usuario.NombreCompleto);
        }

        public async Task<bool> TokenEsValidoAsync(string token)
        {
            var registro = await _tokenRecuperacionDAO.ObtenerTokenVigenteAsync(token);

            await _auditoriaLogDAO.RegistrarEventoAsync(
                idUsuario: registro?.IdUsuario,
                modulo: "Autenticacion",
                tablaAfectada: "TokensRecuperacion",
                idRegistroAfectado: registro?.IdToken,
                accion: registro == null ? "TOKEN_RECUPERACION_INVALIDO" : "TOKEN_RECUPERACION_VALIDADO",
                detalle: registro == null
                    ? "Se intentó usar un token inválido o expirado."
                    : "Se validó un token de recuperación."
            );

            return registro != null;
        }

        public async Task<string> GenerarTokenAsync(string email)
        {
            var resultado = await GenerarTokenConNombreAsync(email);
            return resultado.Token;
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
                await _auditoriaLogDAO.RegistrarEventoAsync(
                    idUsuario: null,
                    modulo: "Autenticacion",
                    tablaAfectada: "Usuarios",
                    accion: "CAMBIO_CONTRASENA_FALLIDO",
                    detalle: "Se intentó restablecer contraseña con token inválido o expirado."
                );

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

            await _auditoriaLogDAO.RegistrarEventoAsync(
                idUsuario: usuario.IdUsuario,
                modulo: "Autenticacion",
                tablaAfectada: "Usuarios",
                idRegistroAfectado: usuario.IdUsuario,
                accion: "CAMBIO_CONTRASENA_EXITOSO",
                detalle: "Se restableció la contraseña usando token de recuperación."
            );
        }
    }
}
