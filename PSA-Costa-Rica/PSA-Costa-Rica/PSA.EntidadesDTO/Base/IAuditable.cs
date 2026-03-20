namespace PSA.EntidadesDTO.Base
{
    public interface IAuditable
    {
        int? CreadoPor { get; set; }
        int? ActualizadoPor { get; set; }
    }
}