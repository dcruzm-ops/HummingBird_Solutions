using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace PSA.WebAPI.Services.Security;

public interface IJwtTokenService
{
    string CreateToken(int idUsuario, int idRol, string email, string nombreCompleto, IReadOnlyCollection<string> permisos, string? nombreRol = null);
}

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private readonly IConfiguration _configuration = configuration;

    public string CreateToken(int idUsuario, int idRol, string email, string nombreCompleto, IReadOnlyCollection<string> permisos, string? nombreRol = null)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "PSA.WebAPI";
        var audience = _configuration["Jwt:Audience"] ?? "PSA.WebApp";
        var configuredKey = _configuration["Jwt:Key"];
        var key = string.IsNullOrWhiteSpace(configuredKey) || configuredKey.Contains("set-via", StringComparison.OrdinalIgnoreCase)
            ? "development-placeholder-key-not-for-production"
            : configuredKey;

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var expiresMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var configured)
            ? Math.Clamp(configured, 30, 1440)
            : 480;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, idUsuario.ToString()),
            new(ClaimTypes.NameIdentifier, idUsuario.ToString()),
            new(ClaimTypes.Role, idRol.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, nombreCompleto)
        };

        if (!string.IsNullOrWhiteSpace(nombreRol))
        {
            claims.Add(new Claim(ClaimTypes.Role, nombreRol.Trim()));
        }

        foreach (var permiso in permisos.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("perm", permiso.Trim()));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
