namespace PSA.AppCore.Services.Notifications;

public interface INotificationEmailSender
{
    Task SendHtmlAsync(string destino, string asunto, string cuerpoHtml);
}

public interface INotificationDispatcher
{
    Task NotifyInAppAsync(int idUsuario, string titulo, string mensaje, string tipo, int? idEntidadReferencia = null);
    Task NotifyEmailAsync(string? destino, string asunto, string cuerpoHtml);
}
