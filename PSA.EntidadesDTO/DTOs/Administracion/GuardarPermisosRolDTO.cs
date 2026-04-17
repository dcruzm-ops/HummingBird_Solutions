using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class GuardarPermisosRolDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un rol válido.")]
        public int IdRol { get; set; }

        public List<string> CodigosPermiso { get; set; } = new();
    }
}
