using System.Security.Cryptography;
using PSA.AppCore.Services.Security;
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
        private readonly IPasswordRecoveryPolicy _passwordRecoveryPolicy;
        private readonly IPasswordRecoveryEmailSender _passwordRecoveryEmailSender;

        public RecuperacionContrasenaManager(
            UsuarioDAO usuarioDAO,
            TokenRecuperacionDAO tokenRecuperacionDAO,
            IServicioHashContrasena servicioHash,
            AuditoriaLogDAO auditoriaLogDAO,
            IPasswordRecoveryPolicy passwordRecoveryPolicy,
            IPasswordRecoveryEmailSender passwordRecoveryEmailSender)
        {
            _usuarioDAO = usuarioDAO ?? throw new ArgumentNullException(nameof(usuarioDAO));
            _tokenRecuperacionDAO = tokenRecuperacionDAO ?? throw new ArgumentNullException(nameof(tokenRecuperacionDAO));
            _servicioHash = servicioHash ?? throw new ArgumentNullException(nameof(servicioHash));
            _auditoriaLogDAO = auditoriaLogDAO ?? throw new ArgumentNullException(nameof(auditoriaLogDAO));
            _passwordRecoveryPolicy = passwordRecoveryPolicy ?? throw new ArgumentNullException(nameof(passwordRecoveryPolicy));
            _passwordRecoveryEmailSender = passwordRecoveryEmailSender ?? throw new ArgumentNullException(nameof(passwordRecoveryEmailSender));
        }

        public async Task SolicitarRecuperacionAsync(string email)
        {
            var (token, nombreUsuario, fechaExpiracion) = await GenerarTokenConNombreAsync(email);

            await _passwordRecoveryEmailSender.SendRecoveryEmailAsync(
                destino: email.Trim(),
                nombreUsuario: nombreUsuario,
                token: token,
                fechaExpiracion: fechaExpiracion);
        }

        public async Task<(string Token, string NombreUsuario, DateTime FechaExpiracion)> GenerarTokenConNombreAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("El correo es obligatorio.");
            }

            email = email.Trim();
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
            var fechaExpiracion = DateTime.Now.Add(_passwordRecoveryPolicy.TokenLifetime);
            await _tokenRecuperacionDAO.CrearTokenAsync(usuario.IdUsuario, token, fechaExpiracion);

            await _auditoriaLogDAO.RegistrarEventoAsync(
                idUsuario: usuario.IdUsuario,
                modulo: "Autenticacion",
                tablaAfectada: "TokensRecuperacion",
                idRegistroAfectado: usuario.IdUsuario,
                accion: "TOKEN_RECUPERACION_GENERADO",
                detalle: "Se generó token de recuperación de contraseña."
            );

            return (token, usuario.NombreCompleto, fechaExpiracion);
        }

        public async Task<TokenRecuperacionValidationResult> ValidarTokenAsync(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                return new TokenRecuperacionValidationResult { Estado = EstadoTokenRecuperacion.Invalido };
            }

            var usuario = await _usuarioDAO.ObtenerPorEmailAsync(email.Trim());
            if (usuario == null)
            {
                return new TokenRecuperacionValidationResult { Estado = EstadoTokenRecuperacion.Invalido };
            }

            var registro = await _tokenRecuperacionDAO.ObtenerTokenPorValorAsync(token.Trim());
            var estado = DeterminarEstado(registro);
            if (registro == null || registro.IdUsuario != usuario.IdUsuario)
            {
                estado = EstadoTokenRecuperacion.Invalido;
            }

            await _auditoriaLogDAO.RegistrarEventoAsync(
                idUsuario: registro?.IdUsuario,
                modulo: "Autenticacion",
                tablaAfectada: "TokensRecuperacion",
                idRegistroAfectado: registro?.IdToken,
                accion: estado == EstadoTokenRecuperacion.Vigente ? "TOKEN_RECUPERACION_VALIDADO" : "TOKEN_RECUPERACION_INVALIDO",
                detalle: estado == EstadoTokenRecuperacion.Vigente
                    ? "Se validó un token de recuperación."
                    : $"Se intentó usar un token inválido: {estado}."
            );

            return new TokenRecuperacionValidationResult { Estado = estado };
        }

        public async Task<bool> TokenEsValidoAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var registro = await _tokenRecuperacionDAO.ObtenerTokenPorValorAsync(token.Trim());
            return DeterminarEstado(registro) == EstadoTokenRecuperacion.Vigente;
        }

        public async Task<bool> TokenEsValidoAsync(string token, string email)
        {
            var resultado = await ValidarTokenAsync(token, email);
            return resultado.EsValido;
        }

        public async Task<string> GenerarTokenAsync(string email)
        {
            var resultado = await GenerarTokenConNombreAsync(email);
            return resultado.Token;
        }

        public bool TokenEsValido(string token)
        {
            return TokenEsValidoAsync(token).GetAwaiter().GetResult();
        }

        public async Task RestablecerContrasenaAsync(string token, string email, string nuevaContrasena)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("El token es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("El correo es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena.Trim().Length < 8)
            {
                throw new InvalidOperationException("La nueva contraseña debe contener al menos 8 caracteres.");
            }

            var usuario = await _usuarioDAO.ObtenerPorEmailAsync(email.Trim());
            if (usuario == null)
            {
                throw new InvalidOperationException("token inválido");
            }

            var registro = await _tokenRecuperacionDAO.ObtenerTokenPorValorAsync(token.Trim());
            var estado = DeterminarEstado(registro);
            if (registro == null || registro.IdUsuario != usuario.IdUsuario)
            {
                estado = EstadoTokenRecuperacion.Invalido;
            }

            if (estado != EstadoTokenRecuperacion.Vigente || registro == null)
            {
                await _auditoriaLogDAO.RegistrarEventoAsync(
                    idUsuario: registro?.IdUsuario,
                    modulo: "Autenticacion",
                    tablaAfectada: "Usuarios",
                    accion: "CAMBIO_CONTRASENA_FALLIDO",
                    detalle: $"Se intentó restablecer contraseña con token en estado: {estado}."
                );

                throw new InvalidOperationException(ObtenerMensajePorEstado(estado));
            }

            var usuarioConToken = await _usuarioDAO.ObtenerPorIdAsync(registro.IdUsuario);
            if (usuarioConToken == null)
            {
                throw new InvalidOperationException("No se encontró el usuario asociado al token.");
            }

            var hash = _servicioHash.GenerarHash(nuevaContrasena.Trim());
            await _usuarioDAO.ActualizarPasswordHashPorEmailAsync(usuarioConToken.Email, hash);
            await _tokenRecuperacionDAO.MarcarTokenComoUsadoAsync(registro.IdToken);

            await _auditoriaLogDAO.RegistrarEventoAsync(
                idUsuario: usuarioConToken.IdUsuario,
                modulo: "Autenticacion",
                tablaAfectada: "Usuarios",
                idRegistroAfectado: usuarioConToken.IdUsuario,
                accion: "CAMBIO_CONTRASENA_EXITOSO",
                detalle: "Se restableció la contraseña usando token de recuperación."
            );
        }

        private static EstadoTokenRecuperacion DeterminarEstado(PSA.EntidadesDTO.Entidades.TokenRecuperacion? registro)
        {
            if (registro == null)
            {
                return EstadoTokenRecuperacion.Invalido;
            }

            if (registro.Usado)
            {
                return EstadoTokenRecuperacion.Utilizado;
            }

            return registro.FechaExpiracion <= DateTime.Now
                ? EstadoTokenRecuperacion.Expirado
                : EstadoTokenRecuperacion.Vigente;
        }

        public static string ObtenerMensajePorEstado(EstadoTokenRecuperacion estado)
        {
            return estado switch
            {
                EstadoTokenRecuperacion.Expirado => "token expirado",
                EstadoTokenRecuperacion.Utilizado => "token ya utilizado",
                _ => "token inválido"
            };
        }
    }
}
