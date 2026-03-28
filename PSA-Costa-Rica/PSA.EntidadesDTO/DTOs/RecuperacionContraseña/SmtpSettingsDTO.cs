namespace PSA.EntidadesDTO.DTOs.RecuperacionContrasena
{
    public class SmtpSettingsDTO
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string FromName { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}