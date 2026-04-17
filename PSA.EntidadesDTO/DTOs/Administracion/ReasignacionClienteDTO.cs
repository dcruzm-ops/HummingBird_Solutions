using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class ReasignacionClienteDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un propietario válido.")]
        public int IdPropietario { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un asesor válido.")]
        public int IdIngenieroDestino { get; set; }
    }
}
