namespace PSA.AppCore.Servicios
{
    public interface IServicioHashContrasena
    {
        string GenerarHash(string contrasena);
        bool VerificarHash(string? hashAlmacenado, string contrasenaIngresada);
    }
}