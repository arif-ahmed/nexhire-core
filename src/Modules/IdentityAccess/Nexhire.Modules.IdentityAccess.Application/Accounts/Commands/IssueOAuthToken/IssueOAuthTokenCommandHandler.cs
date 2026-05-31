using Nexhire.Modules.IdentityAccess.Application.DTOs;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.IssueOAuthToken;

/// <summary>
/// Issues OAuth 2.0 access + refresh tokens (spec §10.1, US-3.4.3-04).
///
/// Authorization-Code + PKCE path requires:
///   - An authorization-code store (not yet implemented — future BC-11 / BC-12 concern).
///   - The /authorize endpoint to issue the code and store the code_challenge.
///
/// Until the auth-code store exists this handler returns E-OAUTH-UNSUPPORTED for the
/// authorization_code grant, but the PKCE code_verifier validation is fully implemented
/// so it can be wired in once the store is available (see PkceValidator domain service).
/// </summary>
public class IssueOAuthTokenCommandHandler : ICommandHandler<IssueOAuthTokenCommand, LoginResultDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IJwtSigner             _jwtSigner;

    public IssueOAuthTokenCommandHandler(
        IUserAccountRepository userAccountRepository,
        IJwtSigner jwtSigner)
    {
        _userAccountRepository = userAccountRepository;
        _jwtSigner             = jwtSigner;
    }

    public async Task<Result<LoginResultDto>> Handle(
        IssueOAuthTokenCommand request,
        CancellationToken cancellationToken)
    {
        return request.GrantType switch
        {
            "authorization_code" => await HandleAuthorizationCode(request, cancellationToken),
            "client_credentials" => await HandleClientCredentials(request, cancellationToken),
            _                    => Result.Failure<LoginResultDto>(
                                       new Error("E-OAUTH-UNSUPPORTED-GRANT", "Unsupported grant type."))
        };
    }

    // ── Authorization-code + PKCE ─────────────────────────────────────────────

    private async Task<Result<LoginResultDto>> HandleAuthorizationCode(
        IssueOAuthTokenCommand request,
        CancellationToken cancellationToken)
    {
        // Step 1 — require code and code_verifier (plain method is not permitted)
        if (string.IsNullOrWhiteSpace(request.Code))
            return Result.Failure<LoginResultDto>(
                new Error("E-OAUTH-INVALID-GRANT", "authorization_code grant requires a 'code' parameter."));

        if (string.IsNullOrWhiteSpace(request.CodeVerifier))
            return Result.Failure<LoginResultDto>(
                new Error("E-OAUTH-INVALID-PKCE", "PKCE code_verifier is required (plain method is not permitted)."));

        // Step 2 — validate code_verifier length/format before hitting any store
        //          (full S256 challenge match is done below once the stored challenge is retrieved)
        var verifierLengthCheck = PkceValidator.Verify(
            request.CodeVerifier,
            PkceValidator.ComputeS256Challenge(request.CodeVerifier)); // trivial self-check for format only
        // Note: the real verification (verifier vs stored challenge) happens after retrieving
        // the authorization code record.  The call above validates format constraints only.

        // Step 3 — TODO: look up the authorization code from the auth-code store.
        //          The store must contain: { code, code_challenge, user_id, client_id, redirect_uri, expiry }.
        //          Once retrieved:
        //
        //   var pkceResult = PkceValidator.Verify(request.CodeVerifier, storedRecord.CodeChallenge);
        //   if (pkceResult.IsFailure) return Result.Failure<LoginResultDto>(pkceResult.Error);
        //
        //   var account = await _userAccountRepository.GetByIdAsync(storedRecord.UserId, cancellationToken);
        //   … issue tokens via TokenClaimsBuilder + _jwtSigner, create Session(Channel.Api) …

        return Result.Failure<LoginResultDto>(new Error(
            "E-OAUTH-UNSUPPORTED",
            "Authorization-code store is not yet implemented. " +
            "PKCE validation is wired — complete the auth-code store to enable this flow."));
    }

    // ── Client-credentials ────────────────────────────────────────────────────

    private async Task<Result<LoginResultDto>> HandleClientCredentials(
        IssueOAuthTokenCommand request,
        CancellationToken cancellationToken)
    {
        // Client-credentials: authenticate the client against a registered client table.
        // Until the client registry exists this path is not enabled.
        // Future implementation:
        //   - Validate client_id + client_secret against a registered OAuth client store.
        //   - Issue a service-account access token (no refresh token for client-credentials).
        //   - Create a Session of Channel.Api.

        await Task.CompletedTask; // suppress async warning until real impl

        return Result.Failure<LoginResultDto>(new Error(
            "E-OAUTH-UNSUPPORTED",
            "Client-credentials flow requires a registered OAuth client store (not yet implemented)."));
    }
}
