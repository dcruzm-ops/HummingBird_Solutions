namespace PSA.EntidadesDTO.DTOs
{
    public class ValidarTokenDTO
    {
        public string Token { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // Compatibilidad con clientes que envían "correo"
        public string Correo
        {
            get => Email;
            set => Email = value;
        }
    }
}   
