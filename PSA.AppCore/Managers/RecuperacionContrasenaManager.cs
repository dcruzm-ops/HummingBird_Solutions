using PSA.AppCore.Servicios;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;

namespace PSA.AppCore.Managers
{
    public class RecuperacionContrasenaManager
    {
        private readonly RecuperacionContrasenaDAO _dao;
        private readonly IServicioHashContrasena _servicioHashContrasena;

        public RecuperacionContrasenaManager(
            RecuperacionContrasenaDAO dao,
            IServicioHashContrasena servicioHashContrasena)
        {
            _dao = dao;
            _servicioHashContrasena = servicioHashContrasena;
        }

        public RespuestaRecuperacionDTO GenerarToken(RecuperarContrasenaDTO dto, string? baseUrl)
        {
            var respuesta = new RespuestaRecuperacionDTO
            {
                Exito = true,
                Mensaje = "Si el correo existe en el sistema, se envió un enlace de recuperación."
            };

            var usuario = _dao.ObtenerUsuarioActivoPorCorreo(dto.Correo);
            if (usuario == null)
                return respuesta;

            _dao.InvalidarTokensAnteriores(usuario.Value.IdUsuario);

            var token = RecuperacionContrasenaDAO.GenerarTokenSeguro();
            var expiracion = DateTime.Now.AddMinutes(30);
            _dao.GuardarToken(usuario.Value.IdUsuario, token, expiracion);

            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                respuesta.LinkRecuperacion = $"{baseUrl.TrimEnd('/')}/Autenticacion/RestablecerContrasena?token={Uri.EscapeDataString(token)}";
            }

            respuesta.CorreoDestino = usuario.Value.Email;
            respuesta.NombreUsuario = usuario.Value.NombreCompleto;

            return respuesta;
        }

        public RespuestaRecuperacionDTO ValidarToken(ValidarTokenDTO dto)
        {
            var valido = _dao.TokenEsValido(dto.Token);

            return new RespuestaRecuperacionDTO
            {
                Exito = valido,
                Mensaje = valido ? "Token válido." : "El enlace no es válido o ya expiró."
            };
        }

        public RespuestaRecuperacionDTO RestablecerContrasena(RestablecerContrasenaDTO dto)
        {
            if (dto.NuevaContrasena != dto.ConfirmarContrasena)
            {
                return new RespuestaRecuperacionDTO
                {
                    Exito = false,
                    Mensaje = "Las contraseñas no coinciden."
                };
            }

            var idUsuario = _dao.ObtenerIdUsuarioPorToken(dto.Token);
            if (idUsuario == null)
            {
                return new RespuestaRecuperacionDTO
                {
                    Exito = false,
                    Mensaje = "El token no es válido, ya fue usado o expiró."
                };
            }

            var hash = _servicioHashContrasena.GenerarHash(dto.NuevaContrasena);
            _dao.ActualizarPassword(idUsuario.Value, hash);
            _dao.MarcarTokenComoUsado(dto.Token);

            return new RespuestaRecuperacionDTO
            {
                Exito = true,
                Mensaje = "La contraseña fue restablecida correctamente."
            };
        }
    }
}
