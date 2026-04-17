using System;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class AuditoriaFiltroDTO
    {
        public string? Modulo { get; set; }
        public string? Accion { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int MaximoRegistros { get; set; } = 50;
    }
}
