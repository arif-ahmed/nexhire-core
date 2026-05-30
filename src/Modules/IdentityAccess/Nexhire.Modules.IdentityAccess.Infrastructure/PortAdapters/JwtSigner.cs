using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.PortAdapters;

public class JwtSigner : IJwtSigner
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SecurityKey _securityKey;

    public JwtSigner(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        _issuer = jwtSettings["Issuer"] ?? "Nexhire";
        _audience = jwtSettings["Audience"] ?? "Nexhire.Api";
        
        // For development, we'll just use a symmetric key if none provided. 
        // Real implementation should use RSA.
        var secret = jwtSettings["Secret"] ?? "SuperSecretKeyForDevelopmentOnlyDoNotUseInProduction!";
        _securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
    }

    public string SignAccessToken(Nexhire.Modules.IdentityAccess.Domain.ValueObjects.AccessTokenSpec spec)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, spec.Subject.ToString()),
            new Claim(ClaimTypes.Role, spec.Role),
            new Claim("session_id", spec.SessionId.ToString())
        };

        foreach (var permission in spec.Permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        return tokenHandler.WriteToken(token);
    }

    public (string Token, string TokenHash) IssueRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var token = Convert.ToBase64String(randomNumber);
        // Quick hash for token hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        var tokenHash = Convert.ToBase64String(hashBytes);
        return (token, tokenHash);
    }

    public bool ValidateSignature(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = _securityKey
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
