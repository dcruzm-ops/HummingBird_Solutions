using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class ValidacionCuentaBancariaDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una cuenta válida.")]
        public int IdCuentaBancaria { get; set; }

        public bool Aprobada { get; set; }
        public string? Observaciones { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar el administrador responsable.")]
        public int IdAdministrador { get; set; }
    }
}
