using System.Security.Cryptography;
using System.Text;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain.Services;

/// <summary>
/// Validates an OAuth 2.0 PKCE code_verifier against a stored code_challenge (RFC 7636).
/// Only the S256 method is accepted — plain is not permitted (spec §10.1 + RFC 7636 §4.2).
/// </summary>
public static class PkceValidator
{
    private const int MinVerifierLength = 43;
    private const int MaxVerifierLength = 128;

    /// <summary>
    /// Verifies that SHA-256(BASE64URL(code_verifier)) == code_challenge.
    /// </summary>
    /// <param name="codeVerifier">Value sent in the token request.</param>
    /// <param name="storedCodeChallenge">Value stored when the authorization code was issued.</param>
    /// <returns>Success when the verifier matches the challenge; failure with E-OAUTH-INVALID-PKCE otherwise.</returns>
    public static Result Verify(string codeVerifier, string storedCodeChallenge)
    {
        if (string.IsNullOrWhiteSpace(codeVerifier))
            return Result.Failure(new Error("E-OAUTH-INVALID-PKCE", "code_verifier is required."));

        if (codeVerifier.Length < MinVerifierLength || codeVerifier.Length > MaxVerifierLength)
            return Result.Failure(new Error("E-OAUTH-INVALID-PKCE",
                $"code_verifier must be between {MinVerifierLength} and {MaxVerifierLength} characters (RFC 7636 §4.1)."));

        var computed = ComputeS256Challenge(codeVerifier);

        if (!string.Equals(computed, storedCodeChallenge, StringComparison.Ordinal))
            return Result.Failure(new Error("E-OAUTH-INVALID-PKCE", "code_verifier does not match the stored code_challenge."));

        return Result.Success();
    }

    /// <summary>
    /// Computes the S256 code_challenge from a raw code_verifier.
    /// Use this when generating the challenge at the /authorize step to store alongside the authorization code.
    /// </summary>
    public static string ComputeS256Challenge(string codeVerifier)
    {
        var bytes = Encoding.ASCII.GetBytes(codeVerifier);
        var hash = SHA256.HashData(bytes);
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
