namespace PSA.EntidadesDTO.DTOs.Fincas;

public class EstadoRenovacionAnualDTO
{
    public bool PuedeRenovar { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? EstadoPlanActual { get; set; }
    public int CuotasRestantes { get; set; }
    public bool ExisteEvaluacionPendienteActiva { get; set; }
}
