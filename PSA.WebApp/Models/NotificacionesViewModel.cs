namespace PSA.WebApp.Models;

public class NotificacionesViewModel
{
    public List<NotificacionItemViewModel> Items { get; set; } = new();
}

public class NotificacionItemViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public bool Leida { get; set; }
    public DateTime Fecha { get; set; }
}
