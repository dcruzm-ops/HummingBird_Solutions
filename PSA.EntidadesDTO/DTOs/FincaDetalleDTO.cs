namespace PSA.EntidadesDTO.DTOs
{
    public class FincaDetalleDTO
    {
        public int IdFinca { get; set; }

        public int IdPropietario { get; set; }

        public string NombreFinca { get; set; } = string.Empty;

        public string PropietarioNombre { get; set; } = string.Empty;

        public string Provincia { get; set; } = string.Empty;

        public string Canton { get; set; } = string.Empty;

        public string Distrito { get; set; } = string.Empty;

        public string? DireccionExacta { get; set; }

        public decimal Latitud { get; set; }

        public decimal Longitud { get; set; }

        public decimal Hectareas { get; set; }

        public string Vegetacion { get; set; } = string.Empty;

        public bool TieneRecursosHidricos { get; set; }

        public string UsoSuelo { get; set; } = string.Empty;

        public string Pendiente { get; set; } = string.Empty;

        public string EstadoFinca { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }

        public DateTime FechaActualizacion { get; set; }

        public string EstadoEvaluacion { get; set; } = "Sin iniciar";

        public int CantidadEvidencias { get; set; }

        public bool TieneCuentaBancaria { get; set; }

        public bool TienePlanPago { get; set; }
    }
}
