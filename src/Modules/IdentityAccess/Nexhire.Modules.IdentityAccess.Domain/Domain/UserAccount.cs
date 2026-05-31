using System.Security.Cryptography;
using System.Text;
using Nexhire.Modules.IdentityAccess.Contracts.Events;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Events;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Domain.Domain;

public class UserAccount : AggregateRoot<UserAccountId>
{
    public Credential Credential { get; private set; }
    public UserRole Role { get; private set; }
    public AccountStatus Status { get; private set; }
    public LockState LockState { get; private set; }
    public MfaConfiguration Mfa { get; private set; }
    private readonly List<BackupCode> _backupCodes = new();
    public IReadOnlyList<BackupCode> BackupCodes => _backupCodes.AsReadOnly();
    private readonly List<TrustedDevice> _trustedDevices = new();
    public IReadOnlyList<TrustedDevice> TrustedDevices => _trustedDevices.AsReadOnly();
    private readonly List<Session> _sessions = new();
    public IReadOnlyList<Session> Sessions => _sessions.AsReadOnly();
    private readonly List<string> _passwordHistory = new();
    public IReadOnlyList<string> PasswordHistory => _passwordHistory.AsReadOnly();
    private readonly List<string> _permissions = new();
    public IReadOnlyList<string> Permissions => _permissions.AsReadOnly();
    private readonly List<PasswordResetToken> _passwordResetTokens = new();
    public IReadOnlyList<PasswordResetToken> PasswordResetTokens => _passwordResetTokens.AsReadOnly();
    
    public bool IdentityVerified { get; private set; }
    public DateTime? ActivatedOnUtc { get; private set; }
    public string? SuspendedReason { get; private set; }
    public DateTime? DeactivatedOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private UserAccount() 
    { 
        Credential = null!;
        LockState = null!;
        Mfa = null!;
        SuspendedReason = null;
    } // EF Core

    private UserAccount(
        UserAccountId id,
        Credential credential,
        UserRole role,
        IReadOnlyList<string> permissions,
        DateTime createdOnUtc) : base(id)
    {
        Credential = credential;
        Role = role;
        Status = AccountStatus.PendingActivation;
        LockState = LockState.CreateUnlocked();
        Mfa = MfaConfiguration.CreateDisabled();
        
        foreach(var p in permissions) _permissions.Add(p);
        
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = createdOnUtc;
        IdentityVerified = false;
    }

    public static UserAccount Provision(EmailAddress email, MobileNumber mobile, PasswordHash passwordHash, UserRole role, IReadOnlyList<string> permissions)
    {
        var credential = Credential.Create(email, mobile, passwordHash).Value;
        var now = DateTime.UtcNow;
        var account = new UserAccount(new UserAccountId(Guid.NewGuid()), credential, role, permissions, now);
        
        account.RaiseDomainEvent(new UserRegisteredIntegrationEvent(
            Guid.NewGuid(), account.Id.Value, role.ToString(), email.Value, now, now));
            
        return account;
    }

    public Result Activate()
    {
        // Spec §6.1: only from PendingActivation. Idempotent if already Active.
        if (Status == AccountStatus.Active)
            return Result.Success();

        if (Status != AccountStatus.PendingActivation)
            return Result.Failure(new Error("Account.InvalidTransition",
                $"Cannot activate an account in status {Status}."));

        Status = AccountStatus.Active;
        ActivatedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = ActivatedOnUtc.Value;

        RaiseDomainEvent(new UserAccountActivatedIntegrationEvent(
            Guid.NewGuid(), Id.Value, ActivatedOnUtc.Value, ActivatedOnUtc.Value));

        return Result.Success();
    }

    public Result RecordSuccessfulLogin(Channel channel, DeviceFingerprint deviceFingerprint, string refreshTokenHash, DateTime expiresOnUtc)
    {
        if (Status != AccountStatus.Active)
            return Result.Failure(new Error($"E-LOGIN-ACCOUNT-{Status.ToString().ToUpper()}", $"Account is {Status}."));

        if (LockState.IsLocked && !LockState.IsExpired())
            return Result.Failure(new Error("E-LOGIN-ACCOUNT-LOCKED", "Account is locked."));

        var now = DateTime.UtcNow;
        var session = new Session(new SessionId(Guid.NewGuid()), Id, channel, deviceFingerprint, refreshTokenHash, now, expiresOnUtc);
        _sessions.Add(session);

        LockState = LockState.Unlock();
        UpdatedOnUtc = now;

        RaiseDomainEvent(new UserLoggedInIntegrationEvent(
            Guid.NewGuid(), Id.Value, session.Id.Value, channel.ToString(), now, now));

        return Result.Success();
    }

    public void RecordFailedLogin()
    {
        var now = DateTime.UtcNow;
        LockState = LockState.IncrementFailedLogin();
        UpdatedOnUtc = now;
    }

    public Result EnableMfa(MfaMethod method, string secretRef, IReadOnlyList<string> backupCodeHashes)
    {
        if (Mfa.Enabled)
            return Result.Failure(new Error("Mfa.AlreadyEnabled", "MFA is already enabled."));

        Mfa = MfaConfiguration.CreateEnabled(method, secretRef).Value;
        
        foreach (var hash in backupCodeHashes)
            _backupCodes.Add(new BackupCode(new BackupCodeId(Guid.NewGuid()), hash));
            
        var now = DateTime.UtcNow;
        UpdatedOnUtc = now;
        
        RaiseDomainEvent(new MfaEnabledEvent(Guid.NewGuid(), Id, method.ToString(), now));
        return Result.Success();
    }

    public void DisableMfa()
    {
        Mfa = MfaConfiguration.CreateDisabled();
        _backupCodes.Clear();
        var now = DateTime.UtcNow;
        UpdatedOnUtc = now;
        
        RaiseDomainEvent(new MfaDisabledEvent(Guid.NewGuid(), Id, now));
    }

    public Result RedeemBackupCode(string codeHash)
    {
        var code = _backupCodes.FirstOrDefault(c => c.CodeHash == codeHash && !c.IsUsed);
        if (code == null)
            return Result.Failure(new Error("E-MFA-INVALID-CODE", "Invalid or already used backup code."));

        var now = DateTime.UtcNow;
        var result = code.Redeem(now);
        if (result.IsFailure) return result;

        UpdatedOnUtc = now;
        RaiseDomainEvent(new BackupCodeRedeemedEvent(Guid.NewGuid(), Id, now));
        return Result.Success();
    }

    public void TrustDevice(DeviceFingerprint fingerprint, string label, DateTime trustedUntilUtc)
    {
        _trustedDevices.Add(new TrustedDevice(new TrustedDeviceId(Guid.NewGuid()), fingerprint, label, trustedUntilUtc));
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void RevokeSession(SessionId sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
        {
            var now = DateTime.UtcNow;
            session.Revoke(now);
            UpdatedOnUtc = now;
            RaiseDomainEvent(new SessionRevokedEvent(Guid.NewGuid(), Id, sessionId, now));
        }
    }

    public void RevokeAllSessions()
    {
        var now = DateTime.UtcNow;
        foreach (var session in _sessions.Where(s => !s.IsRevoked))
        {
            session.Revoke(now);
            RaiseDomainEvent(new SessionRevokedEvent(Guid.NewGuid(), Id, session.Id, now));
        }
        UpdatedOnUtc = now;
    }

    public Result TouchSession(SessionId sessionId, DateTime nowUtc)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null || session.IsRevoked)
            return Result.Failure(new Error("Session.Invalid", "Session is invalid or revoked."));

        if (session.IsExpired(nowUtc))
        {
            session.Revoke(nowUtc);
            return Result.Failure(new Error("Session.Expired", "Session has expired."));
        }

        if (session.IsInactive(nowUtc, TimeSpan.FromMinutes(30)))
        {
            session.Revoke(nowUtc);
            return Result.Failure(new Error("Session.Inactive", "Session timed out due to inactivity."));
        }

        session.Touch(nowUtc);
        UpdatedOnUtc = nowUtc;
        return Result.Success();
    }

    public void IssuePasswordResetToken(string tokenHash, DateTime expiresOnUtc)
    {
        var now = DateTime.UtcNow;
        var token = new PasswordResetToken(new PasswordResetTokenId(Guid.NewGuid()), tokenHash, now, expiresOnUtc);
        _passwordResetTokens.Add(token);
        UpdatedOnUtc = now;
        
        RaiseDomainEvent(new PasswordResetIntegrationEvent(Guid.NewGuid(), Id.Value, now, now));
    }

    public Result CompletePasswordReset(string tokenHash, PasswordHash newPasswordHash)
    {
        var now = DateTime.UtcNow;
        var token = _passwordResetTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        if (token == null)
            return Result.Failure(new Error("E-RESET-INVALID-TOKEN", "Invalid token."));
            
        if (token.IsExpired(now) || token.IsUsed)
            return Result.Failure(new Error("E-RESET-INVALID-TOKEN", "Token is expired or already used."));

        var result = token.MarkUsed(now);
        if (result.IsFailure) return result;

        return InternalChangePassword(newPasswordHash, now);
    }

    public Result ChangePassword(PasswordHash newPasswordHash)
    {
        return InternalChangePassword(newPasswordHash, DateTime.UtcNow);
    }

    private Result InternalChangePassword(PasswordHash newPasswordHash, DateTime now)
    {
        Credential = Credential.Create(Credential.Email, Credential.Mobile, newPasswordHash).Value;
        RevokeAllSessions();
        UpdatedOnUtc = now;
        return Result.Success();
    }

    public void AssignRole(UserRole role, IReadOnlyList<string> newPermissions)
    {
        Role = role;
        _permissions.Clear();
        foreach(var p in newPermissions) _permissions.Add(p);
        
        var now = DateTime.UtcNow;
        UpdatedOnUtc = now;
        
        RaiseDomainEvent(new RoleAssignedIntegrationEvent(Guid.NewGuid(), Id.Value, role.ToString(), Guid.Empty, now, now)); // Note: 'By' is missing here, usually handled in application layer. For domain event, we'll leave it empty or map it differently. Let's just use Guid.Empty.
    }

    public Result Suspend(string reason)
    {
        var transitionResult = AccountStateMachine.EnsureTransitionAllowed(Status, AccountStatus.Suspended);
        if (transitionResult.IsFailure) return transitionResult;

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(new Error("Account.SuspendReasonRequired", "Reason is required to suspend."));

        Status = AccountStatus.Suspended;
        SuspendedReason = reason;
        
        var now = DateTime.UtcNow;
        UpdatedOnUtc = now;
        RevokeAllSessions();

        RaiseDomainEvent(new UserAccountSuspendedIntegrationEvent(
            Guid.NewGuid(), Id.Value, reason, Guid.Empty, now, now));

        return Result.Success();
    }

    public void Reinstate()
    {
        var transitionResult = AccountStateMachine.EnsureTransitionAllowed(Status, AccountStatus.Active);
        if (transitionResult.IsFailure) return;

        Status = AccountStatus.Active;
        SuspendedReason = null;
        
        var now = DateTime.UtcNow;
        UpdatedOnUtc = now;

        RaiseDomainEvent(new UserAccountReinstatedIntegrationEvent(
            Guid.NewGuid(), Id.Value, Guid.Empty, now, now));
    }

    public Result Deactivate()
    {
        var transitionResult = AccountStateMachine.EnsureTransitionAllowed(Status, AccountStatus.Deactivated);
        if (transitionResult.IsFailure) return transitionResult;

        Status = AccountStatus.Deactivated;
        DeactivatedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DeactivatedOnUtc.Value;
        RevokeAllSessions();

        RaiseDomainEvent(new AccountDeactivatedIntegrationEvent(
            Guid.NewGuid(), Id.Value, DeactivatedOnUtc.Value, DeactivatedOnUtc.Value));

        return Result.Success();
    }

    public Result ReactivateAfterDeactivation()
    {
        var transitionResult = AccountStateMachine.EnsureTransitionAllowed(Status, AccountStatus.Active);
        if (transitionResult.IsFailure) return transitionResult;

        Status = AccountStatus.Active;
        DeactivatedOnUtc = null;
        UpdatedOnUtc = DateTime.UtcNow;
        
        return Result.Success();
    }

    public void Lock(DateTime lockedUntilUtc)
    {
        LockState = LockState.Lock(lockedUntilUtc - DateTime.UtcNow);
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void Unlock()
    {
        LockState = LockState.Unlock();
        var now = DateTime.UtcNow;
        UpdatedOnUtc = now;
        RaiseDomainEvent(new AccountUnlockedEvent(Guid.NewGuid(), Id, now));
    }

    public void ApplyGovernmentIdentityVerified()
    {
        IdentityVerified = true;
        var now = DateTime.UtcNow;
        UpdatedOnUtc = now;
        RaiseDomainEvent(new IdentityVerificationAppliedEvent(Guid.NewGuid(), Id, now));
    }

    public void RecordOtpFailure()
    {
        var now = DateTime.UtcNow;
        LockState = LockState.IncrementFailedOtp();
        UpdatedOnUtc = now;
    }

    public bool IsPasswordReused(string rawPassword)
    {
        var verifier = ComputeSha256(rawPassword);
        return _passwordHistory.Any(h => h == verifier);
    }

    public void AddToPasswordHistory(string rawPassword)
    {
        var verifier = ComputeSha256(rawPassword);
        _passwordHistory.Add(verifier);
        if (_passwordHistory.Count > 3)
            _passwordHistory.RemoveAt(0); 
    }

    private static string ComputeSha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
