using System;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class AuditoriaEventoDTO
    {
        public int IdLog { get; set; }
        public int? IdUsuario { get; set; }
        public string? NombreUsuario { get; set; }
        public string Modulo { get; set; } = string.Empty;
        public string TablaAfectada { get; set; } = string.Empty;
        public int? IdRegistroAfectado { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public string? IpOrigen { get; set; }
        public DateTime FechaAccion { get; set; }
        public string? ValorAnterior { get; set; }
        public string? ValorNuevo { get; set; }
    }
}
