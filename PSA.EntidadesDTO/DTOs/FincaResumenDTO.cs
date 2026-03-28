namespace PSA.EntidadesDTO.DTOs
{
    public class FincaResumenDTO
    {
        public int IdFinca { get; set; }

        public string NombreFinca { get; set; } = string.Empty;

        public string Provincia { get; set; } = string.Empty;

        public string Canton { get; set; } = string.Empty;

        public string Distrito { get; set; } = string.Empty;

        public decimal Hectareas { get; set; }

        public string EstadoFinca { get; set; } = string.Empty;

        public string EstadoEvaluacion { get; set; } = "Sin iniciar";

        public DateTime FechaRegistro { get; set; }
    }
}
