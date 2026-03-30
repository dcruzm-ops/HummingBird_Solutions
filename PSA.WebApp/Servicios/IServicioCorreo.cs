namespace PSA.WebApp.Servicios
{
    public interface IServicioCorreo
    {
        Task EnviarAsync(string destinatario, string asunto, string cuerpoTextoPlano);
    }
}
