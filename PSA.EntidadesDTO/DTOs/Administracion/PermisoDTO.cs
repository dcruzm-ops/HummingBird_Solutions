namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class PermisoDTO
    {
        public int IdPermiso { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
}
