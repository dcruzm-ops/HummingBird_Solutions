namespace PSA.AppCore.Services.Security;

public interface IPasswordRecoveryPolicy
{
    TimeSpan TokenLifetime { get; }
}

public interface IPasswordRecoveryEmailSender
{
    Task SendRecoveryEmailAsync(string destino, string nombreUsuario, string token, DateTime fechaExpiracion);
}

public enum EstadoTokenRecuperacion
{
    Invalido = 0,
    Expirado = 1,
    Utilizado = 2,
    Vigente = 3
}

public sealed class TokenRecuperacionValidationResult
{
    public EstadoTokenRecuperacion Estado { get; init; }
    public bool EsValido => Estado == EstadoTokenRecuperacion.Vigente;
}
