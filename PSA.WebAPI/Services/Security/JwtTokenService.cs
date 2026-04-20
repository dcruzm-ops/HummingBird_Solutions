using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace PSA.WebAPI.Services.Security;

public interface IJwtTokenService
{
    string CreateToken(int idUsuario, int idRol, string email, string nombreCompleto, IReadOnlyCollection<string> permisos);
}

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private readonly IConfiguration _configuration = configuration;

    public string CreateToken(int idUsuario, int idRol, string email, string nombreCompleto, IReadOnlyCollection<string> permisos)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "PSA.WebAPI";
        var audience = _configuration["Jwt:Audience"] ?? "PSA.WebApp";
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Debe configurar Jwt:Key para emitir tokens.");
        }

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
