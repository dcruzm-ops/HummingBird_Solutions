using PSA.DataAccess.DAO;

namespace PSA.AppCore.Services.Notifications;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly NotificacionDAO _notificacionDao;
    private readonly INotificationEmailSender _emailSender;

    public NotificationDispatcher(NotificacionDAO notificacionDao, INotificationEmailSender emailSender)
    {
        _notificacionDao = notificacionDao;
        _emailSender = emailSender;
    }

    public Task NotifyInAppAsync(int idUsuario, string titulo, string mensaje, string tipo, int? idEntidadReferencia = null)
    {
        if (idUsuario <= 0 || string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(mensaje))
        {
            return Task.CompletedTask;
        }

        return EjecutarSeguroAsync(() => _notificacionDao.CrearAsync(idUsuario, titulo, mensaje, tipo, idEntidadReferencia));
    }

    public Task NotifyEmailAsync(string? destino, string asunto, string cuerpoHtml)
    {
        if (string.IsNullOrWhiteSpace(destino) || string.IsNullOrWhiteSpace(asunto) || string.IsNullOrWhiteSpace(cuerpoHtml))
        {
            return Task.CompletedTask;
        }

        return EjecutarSeguroAsync(() => _emailSender.SendHtmlAsync(destino.Trim(), asunto.Trim(), cuerpoHtml));
    }

    private static async Task EjecutarSeguroAsync(Func<Task> accion)
    {
        try
        {
            await accion();
        }
        catch
        {
            // Las notificaciones no deben bloquear el flujo principal de negocio.
        }
    }
}
