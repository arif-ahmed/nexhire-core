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
    private readonly RsaSecurityKey _signingKey;
    private readonly RsaSecurityKey _validationKey;

    public JwtSigner(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        _issuer = jwtSettings["Issuer"] ?? "Nexhire";
        _audience = jwtSettings["Audience"] ?? "Nexhire.Api";

        var privateKeyPem = jwtSettings["RsaPrivateKeyPem"]
            ?? throw new InvalidOperationException(
                "JwtSettings:RsaPrivateKeyPem is required. Add a PKCS8 PEM-encoded RSA private key to appsettings.Development.json.");

        // Import private key for signing
        var rsaPrivate = RSA.Create();
        rsaPrivate.ImportFromPem(privateKeyPem);
        _signingKey = new RsaSecurityKey(rsaPrivate);

        // Import public key only for validation (prevents private key exposure in validation path)
        var rsaPublic = RSA.Create();
        rsaPublic.ImportRSAPublicKey(rsaPrivate.ExportRSAPublicKey(), out _);
        _validationKey = new RsaSecurityKey(rsaPublic);
    }

    public string SignAccessToken(AccessTokenSpec spec)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, spec.Subject.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, spec.Role),
            new("session_id", spec.SessionId.ToString())
        };

        foreach (var permission in spec.Permissions)
            claims.Add(new Claim("permission", permission));

        foreach (var scope in spec.Scopes)
            claims.Add(new Claim("scope", scope));

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = spec.ExpiresOnUtc,
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
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        var token = Convert.ToBase64String(randomBytes);
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
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
                ValidateLifetime = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = _validationKey,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
