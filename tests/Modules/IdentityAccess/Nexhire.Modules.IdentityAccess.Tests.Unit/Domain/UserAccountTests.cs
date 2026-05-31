using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Modules.IdentityAccess.Contracts.Events;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Events;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain;

public class UserAccountTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static UserAccount MakeAccount(UserRole role = UserRole.JobSeeker)
    {
        var permissions = PermissionResolver.Resolve(role, []);
        return UserAccount.Provision(
            EmailAddress.Create("test@example.com").Value,
            MobileNumber.Create("+8801700000001").Value,
            PasswordHash.Create("$argon2id$hashed-password").Value,
            role,
            permissions);
    }

    private static UserAccount ActiveAccount(UserRole role = UserRole.JobSeeker)
    {
        var account = MakeAccount(role);
        account.Activate();
        account.ClearDomainEvents();
        return account;
    }

    private static Session AddSession(UserAccount account)
    {
        var fp = DeviceFingerprint.Create("fingerprint-abc").Value;
        account.RecordSuccessfulLogin(Channel.Web, fp, "refresh-hash", DateTime.UtcNow.AddDays(1));
        return account.Sessions.Last();
    }

    // ── Provision ────────────────────────────────────────────────────────────

    public class Provision
    {
        [Fact]
        public void Should_Create_Account_In_PendingActivation()
        {
            var account = MakeAccount();

            account.Status.Should().Be(AccountStatus.PendingActivation);
        }

        [Fact]
        public void Should_Set_Role_And_Permissions()
        {
            var account = MakeAccount(UserRole.Employer);

            account.Role.Should().Be(UserRole.Employer);
            account.Permissions.Should().Contain("jobs:write");
        }

        [Fact]
        public void Should_Start_With_Identity_Not_Verified()
        {
            var account = MakeAccount();

            account.IdentityVerified.Should().BeFalse();
        }

        [Fact]
        public void Should_Start_Unlocked()
        {
            var account = MakeAccount();

            account.LockState.IsLocked.Should().BeFalse();
            account.LockState.FailedLoginCount.Should().Be(0);
            account.LockState.FailedOtpCount.Should().Be(0);
        }

        [Fact]
        public void Should_Raise_UserRegisteredIntegrationEvent()
        {
            var account = MakeAccount();

            account.DomainEvents.Should().ContainSingle(e => e is UserRegisteredIntegrationEvent);
            var evt = (UserRegisteredIntegrationEvent)account.DomainEvents.First(e => e is UserRegisteredIntegrationEvent);
            evt.Email.Should().Be("test@example.com");
            evt.Role.Should().Be("JobSeeker");
        }

        [Fact]
        public void Should_Assign_Immutable_Id()
        {
            var account = MakeAccount();
            var originalId = account.Id.Value;

            account.Activate();
            account.Suspend("reason");
            account.Reinstate();

            account.Id.Value.Should().Be(originalId, "UserId must be immutable");
        }
    }

    // ── Activate ─────────────────────────────────────────────────────────────

    public class Activate
    {
        [Fact]
        public void Should_Transition_To_Active()
        {
            var account = MakeAccount();

            var result = account.Activate();

            result.IsSuccess.Should().BeTrue();
            account.Status.Should().Be(AccountStatus.Active);
        }

        [Fact]
        public void Should_Set_ActivatedOnUtc()
        {
            var account = MakeAccount();
            var before = DateTime.UtcNow;

            account.Activate();

            account.ActivatedOnUtc.Should().NotBeNull();
            account.ActivatedOnUtc.Should().BeAfter(before.AddSeconds(-1));
        }

        [Fact]
        public void Should_Raise_UserAccountActivatedIntegrationEvent()
        {
            var account = MakeAccount();

            account.Activate();

            account.DomainEvents.Should().Contain(e => e is UserAccountActivatedIntegrationEvent);
        }

        [Fact]
        public void Should_Fail_From_Suspended()
        {
            var account = ActiveAccount();
            account.Suspend("policy violation");

            var result = account.Activate();

            result.IsFailure.Should().BeTrue();
        }

        [Fact]
        public void Should_Fail_From_Deactivated()
        {
            var account = ActiveAccount();
            account.Deactivate();

            var result = account.Activate();

            result.IsFailure.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Idempotent_When_Already_Active()
        {
            // Spec §6.1: "Idempotent if already Active" — second call must succeed silently
            var account = MakeAccount();
            account.Activate();

            var result = account.Activate();

            result.IsSuccess.Should().BeTrue(because: "Activate is idempotent when already Active (spec §6.1)");
        }
    }

    // ── RecordSuccessfulLogin ─────────────────────────────────────────────────

    public class RecordSuccessfulLogin
    {
        [Fact]
        public void Should_Create_Session_When_Active()
        {
            var account = ActiveAccount();
            var fp = DeviceFingerprint.Create("fp-1").Value;

            var result = account.RecordSuccessfulLogin(Channel.Web, fp, "hash", DateTime.UtcNow.AddDays(1));

            result.IsSuccess.Should().BeTrue();
            account.Sessions.Should().HaveCount(1);
        }

        [Fact]
        public void Should_Reset_FailedLoginCount()
        {
            var account = ActiveAccount();
            account.RecordFailedLogin();
            account.RecordFailedLogin();

            var fp = DeviceFingerprint.Create("fp-1").Value;
            account.RecordSuccessfulLogin(Channel.Web, fp, "hash", DateTime.UtcNow.AddDays(1));

            account.LockState.FailedLoginCount.Should().Be(0);
        }

        [Fact]
        public void Should_Raise_UserLoggedInIntegrationEvent()
        {
            var account = ActiveAccount();
            var fp = DeviceFingerprint.Create("fp-1").Value;

            account.RecordSuccessfulLogin(Channel.Web, fp, "hash", DateTime.UtcNow.AddDays(1));

            account.DomainEvents.Should().Contain(e => e is UserLoggedInIntegrationEvent);
        }

        [Theory]
        [InlineData(AccountStatus.PendingActivation, "E-LOGIN-ACCOUNT-PENDINGACTIVATION")]
        public void Should_Fail_With_Distinct_Code_For_PendingActivation(AccountStatus _, string errorCode)
        {
            var account = MakeAccount(); // starts PendingActivation
            var fp = DeviceFingerprint.Create("fp-1").Value;

            var result = account.RecordSuccessfulLogin(Channel.Web, fp, "hash", DateTime.UtcNow.AddDays(1));

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(errorCode);
        }

        [Fact]
        public void Should_Fail_With_Distinct_Code_For_Suspended()
        {
            var account = ActiveAccount();
            account.Suspend("reason");
            var fp = DeviceFingerprint.Create("fp-1").Value;

            var result = account.RecordSuccessfulLogin(Channel.Web, fp, "hash", DateTime.UtcNow.AddDays(1));

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-LOGIN-ACCOUNT-SUSPENDED");
        }

        [Fact]
        public void Should_Fail_With_Distinct_Code_For_Deactivated()
        {
            var account = ActiveAccount();
            account.Deactivate();
            var fp = DeviceFingerprint.Create("fp-1").Value;

            var result = account.RecordSuccessfulLogin(Channel.Web, fp, "hash", DateTime.UtcNow.AddDays(1));

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-LOGIN-ACCOUNT-DEACTIVATED");
        }

        [Fact]
        public void Should_Fail_When_Locked()
        {
            var account = ActiveAccount();
            account.Lock(DateTime.UtcNow.AddMinutes(15));
            var fp = DeviceFingerprint.Create("fp-1").Value;

            var result = account.RecordSuccessfulLogin(Channel.Web, fp, "hash", DateTime.UtcNow.AddDays(1));

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-LOGIN-ACCOUNT-LOCKED");
        }
    }

    // ── RecordFailedLogin ─────────────────────────────────────────────────────

    public class RecordFailedLogin
    {
        [Fact]
        public void Should_Increment_FailedLoginCount()
        {
            var account = ActiveAccount();

            account.RecordFailedLogin();
            account.RecordFailedLogin();

            account.LockState.FailedLoginCount.Should().Be(2);
        }
    }

    // ── EnableMfa ─────────────────────────────────────────────────────────────

    public class EnableMfa
    {
        [Fact]
        public void Should_Set_Mfa_Enabled_With_BackupCodes()
        {
            var account = ActiveAccount();
            var backupHashes = Enumerable.Range(0, 8).Select(i => $"code-hash-{i}").ToList();

            var result = account.EnableMfa(MfaMethod.Totp, "secret-ref", backupHashes);

            result.IsSuccess.Should().BeTrue();
            account.Mfa.Enabled.Should().BeTrue();
            account.Mfa.Method.Should().Be(MfaMethod.Totp);
            account.BackupCodes.Should().HaveCount(8);
        }

        [Fact]
        public void Should_Raise_MfaEnabledEvent()
        {
            var account = ActiveAccount();
            var hashes = Enumerable.Range(0, 8).Select(i => $"h{i}").ToList();

            account.EnableMfa(MfaMethod.Totp, "s", hashes);

            account.DomainEvents.Should().Contain(e => e is MfaEnabledEvent);
        }

        [Fact]
        public void Should_Fail_When_Mfa_Already_Enabled()
        {
            var account = ActiveAccount();
            var hashes = Enumerable.Range(0, 8).Select(i => $"h{i}").ToList();
            account.EnableMfa(MfaMethod.Totp, "s", hashes);

            var result = account.EnableMfa(MfaMethod.Totp, "s2", hashes);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Mfa.AlreadyEnabled");
        }

        [Fact]
        public void Should_Accept_10_Backup_Codes()
        {
            var account = ActiveAccount();
            var hashes = Enumerable.Range(0, 10).Select(i => $"h{i}").ToList();

            var result = account.EnableMfa(MfaMethod.SmsOtp, "s", hashes);

            result.IsSuccess.Should().BeTrue();
            account.BackupCodes.Should().HaveCount(10);
        }
    }

    // ── DisableMfa ────────────────────────────────────────────────────────────

    public class DisableMfa
    {
        [Fact]
        public void Should_Clear_Mfa_And_BackupCodes()
        {
            var account = ActiveAccount();
            var hashes = Enumerable.Range(0, 8).Select(i => $"h{i}").ToList();
            account.EnableMfa(MfaMethod.Totp, "s", hashes);

            account.DisableMfa();

            account.Mfa.Enabled.Should().BeFalse();
            account.BackupCodes.Should().BeEmpty();
        }

        [Fact]
        public void Should_Raise_MfaDisabledEvent()
        {
            var account = ActiveAccount();
            account.EnableMfa(MfaMethod.Totp, "s", Enumerable.Range(0, 8).Select(i => $"h{i}").ToList());
            account.ClearDomainEvents();

            account.DisableMfa();

            account.DomainEvents.Should().Contain(e => e is MfaDisabledEvent);
        }
    }

    // ── RedeemBackupCode ──────────────────────────────────────────────────────

    public class RedeemBackupCode
    {
        [Fact]
        public void Should_Succeed_For_Valid_Unused_Code()
        {
            var account = ActiveAccount();
            account.EnableMfa(MfaMethod.Totp, "s", ["code-hash", "h1", "h2", "h3", "h4", "h5", "h6", "h7"]);

            var result = account.RedeemBackupCode("code-hash");

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Mark_Code_As_Used()
        {
            var account = ActiveAccount();
            account.EnableMfa(MfaMethod.Totp, "s", ["code-hash", "h1", "h2", "h3", "h4", "h5", "h6", "h7"]);

            account.RedeemBackupCode("code-hash");

            account.BackupCodes.First(c => c.CodeHash == "code-hash").IsUsed.Should().BeTrue();
        }

        [Fact]
        public void Should_Fail_On_Reuse_Of_Same_Code()
        {
            var account = ActiveAccount();
            account.EnableMfa(MfaMethod.Totp, "s", ["code-hash", "h1", "h2", "h3", "h4", "h5", "h6", "h7"]);
            account.RedeemBackupCode("code-hash");

            var result = account.RedeemBackupCode("code-hash");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-MFA-INVALID-CODE");
        }

        [Fact]
        public void Should_Fail_For_Unknown_Code()
        {
            var account = ActiveAccount();
            account.EnableMfa(MfaMethod.Totp, "s", ["h1", "h2", "h3", "h4", "h5", "h6", "h7", "h8"]);

            var result = account.RedeemBackupCode("not-a-code");

            result.IsFailure.Should().BeTrue();
        }

        [Fact]
        public void Should_Raise_BackupCodeRedeemedEvent()
        {
            var account = ActiveAccount();
            account.EnableMfa(MfaMethod.Totp, "s", ["code-hash", "h1", "h2", "h3", "h4", "h5", "h6", "h7"]);
            account.ClearDomainEvents();

            account.RedeemBackupCode("code-hash");

            account.DomainEvents.Should().Contain(e => e is BackupCodeRedeemedEvent);
        }
    }

    // ── RevokeSession / RevokeAllSessions ─────────────────────────────────────

    public class Sessions
    {
        [Fact]
        public void RevokeSession_Should_Mark_Session_Revoked()
        {
            var account = ActiveAccount();
            var fp = DeviceFingerprint.Create("fp-1").Value;
            account.RecordSuccessfulLogin(Channel.Web, fp, "h", DateTime.UtcNow.AddDays(1));
            var session = account.Sessions.First();

            account.RevokeSession(session.Id);

            account.Sessions.First().IsRevoked.Should().BeTrue();
        }

        [Fact]
        public void RevokeSession_Should_Raise_SessionRevokedEvent()
        {
            var account = ActiveAccount();
            var fp = DeviceFingerprint.Create("fp-1").Value;
            account.RecordSuccessfulLogin(Channel.Web, fp, "h", DateTime.UtcNow.AddDays(1));
            var session = account.Sessions.First();
            account.ClearDomainEvents();

            account.RevokeSession(session.Id);

            account.DomainEvents.Should().Contain(e => e is SessionRevokedEvent);
        }

        [Fact]
        public void RevokeAllSessions_Should_Revoke_All_Active_Sessions()
        {
            var account = ActiveAccount();
            var fp = DeviceFingerprint.Create("fp-1").Value;
            account.RecordSuccessfulLogin(Channel.Web, fp, "h1", DateTime.UtcNow.AddDays(1));
            account.RecordSuccessfulLogin(Channel.Mobile, fp, "h2", DateTime.UtcNow.AddDays(1));

            account.RevokeAllSessions();

            account.Sessions.Should().AllSatisfy(s => s.IsRevoked.Should().BeTrue());
        }

        [Fact]
        public void RevokeAllSessions_Should_Not_Double_Revoke_Already_Revoked_Sessions()
        {
            var account = ActiveAccount();
            var fp = DeviceFingerprint.Create("fp-1").Value;
            account.RecordSuccessfulLogin(Channel.Web, fp, "h1", DateTime.UtcNow.AddDays(1));
            var session = account.Sessions.First();
            account.RevokeSession(session.Id);
            account.ClearDomainEvents();

            account.RevokeAllSessions();

            // No new SessionRevoked event raised for already-revoked session
            account.DomainEvents.Should().BeEmpty();
        }
    }

    // ── TouchSession ──────────────────────────────────────────────────────────

    public class TouchSession
    {
        [Fact]
        public void Should_Succeed_For_Active_Session_Within_Inactivity_Window()
        {
            var account = ActiveAccount();
            var fp = DeviceFingerprint.Create("fp-1").Value;
            account.RecordSuccessfulLogin(Channel.Web, fp, "h", DateTime.UtcNow.AddDays(1));
            var session = account.Sessions.First();

            var result = account.TouchSession(session.Id, DateTime.UtcNow.AddSeconds(30));

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Revoke_Session_Past_Inactivity_Window()
        {
            var account = ActiveAccount();
            var fp = DeviceFingerprint.Create("fp-1").Value;
            account.RecordSuccessfulLogin(Channel.Web, fp, "h", DateTime.UtcNow.AddDays(1));
            var session = account.Sessions.First();

            // Touch far past the 30-minute inactivity window
            var result = account.TouchSession(session.Id, DateTime.UtcNow.AddHours(2));

            result.IsFailure.Should().BeTrue();
            account.Sessions.First().IsRevoked.Should().BeTrue();
        }

        [Fact]
        public void Should_Fail_For_Revoked_Session()
        {
            var account = ActiveAccount();
            var fp = DeviceFingerprint.Create("fp-1").Value;
            account.RecordSuccessfulLogin(Channel.Web, fp, "h", DateTime.UtcNow.AddDays(1));
            var session = account.Sessions.First();
            account.RevokeSession(session.Id);

            var result = account.TouchSession(session.Id, DateTime.UtcNow.AddSeconds(10));

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Session.Invalid");
        }
    }

    // ── CompletePasswordReset ─────────────────────────────────────────────────

    public class CompletePasswordReset
    {
        [Fact]
        public void Should_Set_New_Password_And_Mark_Token_Used()
        {
            var account = ActiveAccount();
            account.IssuePasswordResetToken("token-hash", DateTime.UtcNow.AddHours(1));
            var newHash = PasswordHash.Create("$argon2id$new-hash").Value;

            var result = account.CompletePasswordReset("token-hash", newHash);

            result.IsSuccess.Should().BeTrue();
            account.Credential.PasswordHash.Value.Should().Be("$argon2id$new-hash");
            account.PasswordResetTokens.First().IsUsed.Should().BeTrue();
        }

        [Fact]
        public void Should_Revoke_All_Sessions_On_Success()
        {
            var account = ActiveAccount();
            AddSession(account);
            account.IssuePasswordResetToken("token-hash", DateTime.UtcNow.AddHours(1));
            var newHash = PasswordHash.Create("$argon2id$new-hash").Value;

            account.CompletePasswordReset("token-hash", newHash);

            account.Sessions.Should().AllSatisfy(s => s.IsRevoked.Should().BeTrue());
        }

        [Fact]
        public void Should_Fail_For_Wrong_Token()
        {
            var account = ActiveAccount();
            account.IssuePasswordResetToken("token-hash", DateTime.UtcNow.AddHours(1));
            var newHash = PasswordHash.Create("$argon2id$new-hash").Value;

            var result = account.CompletePasswordReset("wrong-token", newHash);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-RESET-INVALID-TOKEN");
        }

        [Fact]
        public void Should_Fail_For_Expired_Token()
        {
            var account = ActiveAccount();
            account.IssuePasswordResetToken("token-hash", DateTime.UtcNow.AddHours(-1)); // already expired
            var newHash = PasswordHash.Create("$argon2id$new-hash").Value;

            var result = account.CompletePasswordReset("token-hash", newHash);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-RESET-INVALID-TOKEN");
        }

        [Fact]
        public void Should_Fail_For_Already_Used_Token()
        {
            var account = ActiveAccount();
            account.IssuePasswordResetToken("token-hash", DateTime.UtcNow.AddHours(1));
            var hash1 = PasswordHash.Create("$argon2id$hash-one").Value;
            var hash2 = PasswordHash.Create("$argon2id$hash-two").Value;
            account.CompletePasswordReset("token-hash", hash1);

            var result = account.CompletePasswordReset("token-hash", hash2);

            result.IsFailure.Should().BeTrue();
        }
    }

    // ── ChangePassword ────────────────────────────────────────────────────────

    public class ChangePassword
    {
        [Fact]
        public void Should_Update_Credential_And_Revoke_Sessions()
        {
            var account = ActiveAccount();
            AddSession(account);
            var newHash = PasswordHash.Create("$argon2id$new-hash").Value;

            var result = account.ChangePassword(newHash);

            result.IsSuccess.Should().BeTrue();
            account.Credential.PasswordHash.Value.Should().Be("$argon2id$new-hash");
            account.Sessions.Should().AllSatisfy(s => s.IsRevoked.Should().BeTrue());
        }
    }

    // ── PasswordHistory / no-reuse ────────────────────────────────────────────

    public class PasswordHistory
    {
        [Fact]
        public void IsPasswordReused_Should_Detect_Same_Raw_Password()
        {
            var account = ActiveAccount();
            account.AddToPasswordHistory("MyP@ss1234");

            account.IsPasswordReused("MyP@ss1234").Should().BeTrue();
        }

        [Fact]
        public void IsPasswordReused_Should_Return_False_For_Different_Password()
        {
            var account = ActiveAccount();
            account.AddToPasswordHistory("MyP@ss1234");

            account.IsPasswordReused("OtherP@ss9876").Should().BeFalse();
        }

        [Fact]
        public void AddToPasswordHistory_Should_Keep_Last_3_Entries()
        {
            var account = ActiveAccount();
            account.AddToPasswordHistory("P@ss0000001");
            account.AddToPasswordHistory("P@ss0000002");
            account.AddToPasswordHistory("P@ss0000003");
            account.AddToPasswordHistory("P@ss0000004"); // oldest should be evicted

            account.PasswordHistory.Should().HaveCount(3);
            account.IsPasswordReused("P@ss0000001").Should().BeFalse("oldest entry evicted after cap");
            account.IsPasswordReused("P@ss0000004").Should().BeTrue("newest entry retained");
        }

        [Fact]
        public void IsPasswordReused_Is_Case_Sensitive()
        {
            var account = ActiveAccount();
            account.AddToPasswordHistory("MyP@ss1234");

            account.IsPasswordReused("myp@ss1234").Should().BeFalse("SHA-256 is case-sensitive");
        }
    }

    // ── AssignRole ────────────────────────────────────────────────────────────

    public class AssignRole
    {
        [Fact]
        public void Should_Recompute_Permissions_For_New_Role()
        {
            var account = ActiveAccount(UserRole.JobSeeker);
            var newPermissions = PermissionResolver.Resolve(UserRole.Employer, []);

            account.AssignRole(UserRole.Employer, newPermissions);

            account.Role.Should().Be(UserRole.Employer);
            account.Permissions.Should().Contain("jobs:write");
            account.Permissions.Should().NotContain("profile:self",
                because: "permissions are recomputed from the new role, not accumulated");
        }

        [Fact]
        public void Should_Raise_RoleAssignedIntegrationEvent()
        {
            var account = ActiveAccount();
            var perms = PermissionResolver.Resolve(UserRole.Employer, []);
            account.ClearDomainEvents();

            account.AssignRole(UserRole.Employer, perms);

            account.DomainEvents.Should().Contain(e => e is RoleAssignedIntegrationEvent);
        }
    }

    // ── Suspend ───────────────────────────────────────────────────────────────

    public class Suspend
    {
        [Fact]
        public void Should_Transition_To_Suspended_With_Reason()
        {
            var account = ActiveAccount();

            var result = account.Suspend("Policy violation");

            result.IsSuccess.Should().BeTrue();
            account.Status.Should().Be(AccountStatus.Suspended);
            account.SuspendedReason.Should().Be("Policy violation");
        }

        [Fact]
        public void Should_Revoke_All_Sessions()
        {
            var account = ActiveAccount();
            AddSession(account);

            account.Suspend("reason");

            account.Sessions.Should().AllSatisfy(s => s.IsRevoked.Should().BeTrue());
        }

        [Fact]
        public void Should_Raise_UserAccountSuspendedIntegrationEvent()
        {
            var account = ActiveAccount();
            account.ClearDomainEvents();

            account.Suspend("reason");

            account.DomainEvents.Should().Contain(e => e is UserAccountSuspendedIntegrationEvent);
        }

        [Fact]
        public void Should_Fail_When_Reason_Is_Empty()
        {
            var account = ActiveAccount();

            var result = account.Suspend("");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Account.SuspendReasonRequired");
        }

        [Fact]
        public void Should_Fail_When_Reason_Is_Whitespace()
        {
            var account = ActiveAccount();

            var result = account.Suspend("   ");

            result.IsFailure.Should().BeTrue();
        }

        [Fact]
        public void Should_Fail_From_Deactivated()
        {
            var account = ActiveAccount();
            account.Deactivate();

            var result = account.Suspend("reason");

            result.IsFailure.Should().BeTrue();
        }
    }

    // ── Reinstate ─────────────────────────────────────────────────────────────

    public class Reinstate
    {
        [Fact]
        public void Should_Return_To_Active_From_Suspended()
        {
            var account = ActiveAccount();
            account.Suspend("reason");

            account.Reinstate();

            account.Status.Should().Be(AccountStatus.Active);
            account.SuspendedReason.Should().BeNull();
        }

        [Fact]
        public void Should_Raise_UserAccountReinstatedIntegrationEvent()
        {
            var account = ActiveAccount();
            account.Suspend("reason");
            account.ClearDomainEvents();

            account.Reinstate();

            account.DomainEvents.Should().Contain(e => e is UserAccountReinstatedIntegrationEvent);
        }

        [Fact]
        public void Should_Not_Transition_From_Active()
        {
            var account = ActiveAccount();

            // Reinstate from Active is a no-op (state machine prevents it silently)
            account.Reinstate();

            // Status unchanged — illegal transition is silently ignored in Reinstate()
            account.Status.Should().Be(AccountStatus.Active);
        }
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    public class Deactivate
    {
        [Fact]
        public void Should_Transition_To_Deactivated()
        {
            var account = ActiveAccount();

            var result = account.Deactivate();

            result.IsSuccess.Should().BeTrue();
            account.Status.Should().Be(AccountStatus.Deactivated);
            account.DeactivatedOnUtc.Should().NotBeNull();
        }

        [Fact]
        public void Should_Revoke_All_Sessions()
        {
            var account = ActiveAccount();
            AddSession(account);

            account.Deactivate();

            account.Sessions.Should().AllSatisfy(s => s.IsRevoked.Should().BeTrue());
        }

        [Fact]
        public void Should_Raise_AccountDeactivatedIntegrationEvent()
        {
            var account = ActiveAccount();
            account.ClearDomainEvents();

            account.Deactivate();

            account.DomainEvents.Should().Contain(e => e is AccountDeactivatedIntegrationEvent);
        }

        [Fact]
        public void Should_Fail_From_Suspended()
        {
            var account = ActiveAccount();
            account.Suspend("reason");

            var result = account.Deactivate();

            result.IsFailure.Should().BeTrue();
        }
    }

    // ── ReactivateAfterDeactivation ───────────────────────────────────────────

    public class ReactivateAfterDeactivation
    {
        [Fact]
        public void Should_Return_To_Active_From_Deactivated()
        {
            var account = ActiveAccount();
            account.Deactivate();

            var result = account.ReactivateAfterDeactivation();

            result.IsSuccess.Should().BeTrue();
            account.Status.Should().Be(AccountStatus.Active);
            account.DeactivatedOnUtc.Should().BeNull();
        }

        [Fact]
        public void Should_Fail_From_Active()
        {
            var account = ActiveAccount();

            var result = account.ReactivateAfterDeactivation();

            result.IsFailure.Should().BeTrue();
        }
    }

    // ── Lock / Unlock ─────────────────────────────────────────────────────────

    public class LockUnlock
    {
        [Fact]
        public void Lock_Should_Set_IsLocked_True()
        {
            var account = ActiveAccount();

            account.Lock(DateTime.UtcNow.AddMinutes(15));

            account.LockState.IsLocked.Should().BeTrue();
        }

        [Fact]
        public void Unlock_Should_Clear_Lock_And_Reset_Counts()
        {
            var account = ActiveAccount();
            account.Lock(DateTime.UtcNow.AddMinutes(15));
            account.RecordOtpFailure();

            account.Unlock();

            account.LockState.IsLocked.Should().BeFalse();
            account.LockState.FailedLoginCount.Should().Be(0);
            account.LockState.FailedOtpCount.Should().Be(0);
        }

        [Fact]
        public void Unlock_Should_Raise_AccountUnlockedEvent()
        {
            var account = ActiveAccount();
            account.Lock(DateTime.UtcNow.AddMinutes(15));
            account.ClearDomainEvents();

            account.Unlock();

            account.DomainEvents.Should().Contain(e => e is AccountUnlockedEvent);
        }
    }

    // ── RecordOtpFailure ──────────────────────────────────────────────────────

    public class RecordOtpFailure
    {
        [Fact]
        public void Should_Increment_FailedOtpCount()
        {
            var account = ActiveAccount();

            account.RecordOtpFailure();
            account.RecordOtpFailure();

            account.LockState.FailedOtpCount.Should().Be(2);
        }
    }

    // ── ApplyGovernmentIdentityVerified ───────────────────────────────────────

    public class ApplyGovernmentIdentityVerified
    {
        [Fact]
        public void Should_Set_IdentityVerified_True()
        {
            var account = ActiveAccount();

            account.ApplyGovernmentIdentityVerified();

            account.IdentityVerified.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Idempotent()
        {
            var account = ActiveAccount();

            account.ApplyGovernmentIdentityVerified();
            account.ApplyGovernmentIdentityVerified();

            account.IdentityVerified.Should().BeTrue();
        }

        [Fact]
        public void Should_Raise_IdentityVerificationAppliedEvent()
        {
            var account = ActiveAccount();
            account.ClearDomainEvents();

            account.ApplyGovernmentIdentityVerified();

            account.DomainEvents.Should().Contain(e => e is IdentityVerificationAppliedEvent);
        }
    }

    // ── Cross-cutting invariants ──────────────────────────────────────────────

    public class Invariants
    {
        [Fact]
        public void Suspended_Account_Cannot_Login()
        {
            var account = ActiveAccount();
            account.Suspend("reason");
            var fp = DeviceFingerprint.Create("fp-1").Value;

            var result = account.RecordSuccessfulLogin(Channel.Web, fp, "h", DateTime.UtcNow.AddDays(1));

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().NotBe("E-LOGIN-ACCOUNT-DEACTIVATED",
                because: "suspended returns its own distinct error code");
        }

        [Fact]
        public void Deactivated_Account_Cannot_Login()
        {
            var account = ActiveAccount();
            account.Deactivate();
            var fp = DeviceFingerprint.Create("fp-1").Value;

            var result = account.RecordSuccessfulLogin(Channel.Web, fp, "h", DateTime.UtcNow.AddDays(1));

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("E-LOGIN-ACCOUNT-DEACTIVATED");
        }

        [Fact]
        public void Suspended_Deactivated_Codes_Are_Distinct_From_Each_Other()
        {
            var suspended = ActiveAccount();
            suspended.Suspend("reason");

            var deactivated = ActiveAccount();
            deactivated.Deactivate();

            var fp = DeviceFingerprint.Create("fp-1").Value;
            var suspendedResult = suspended.RecordSuccessfulLogin(Channel.Web, fp, "h", DateTime.UtcNow.AddDays(1));
            var deactivatedResult = deactivated.RecordSuccessfulLogin(Channel.Web, fp, "h", DateTime.UtcNow.AddDays(1));

            suspendedResult.Error.Code.Should().NotBe(deactivatedResult.Error.Code,
                because: "each inactive status must return a distinct error code (invariant #7)");
        }

        [Fact]
        public void Suspend_Must_Revoke_All_Sessions_Invariant8()
        {
            var account = ActiveAccount();
            var fp = DeviceFingerprint.Create("fp-1").Value;
            account.RecordSuccessfulLogin(Channel.Web, fp, "h", DateTime.UtcNow.AddDays(1));

            account.Suspend("reason");

            account.Sessions.Should().AllSatisfy(s => s.IsRevoked.Should().BeTrue(),
                because: "invariant #8: suspending must revoke all sessions");
        }

        [Fact]
        public void Deactivate_Must_Revoke_All_Sessions_Invariant8()
        {
            var account = ActiveAccount();
            AddSession(account);

            account.Deactivate();

            account.Sessions.Should().AllSatisfy(s => s.IsRevoked.Should().BeTrue(),
                because: "invariant #8: deactivating must revoke all sessions");
        }

        [Fact]
        public void ChangePassword_Must_Revoke_All_Sessions_Invariant8()
        {
            var account = ActiveAccount();
            AddSession(account);
            var newHash = PasswordHash.Create("$argon2id$new-hash").Value;

            account.ChangePassword(newHash);

            account.Sessions.Should().AllSatisfy(s => s.IsRevoked.Should().BeTrue(),
                because: "invariant #8: password change must revoke all sessions");
        }

        [Fact]
        public void CompletePasswordReset_Must_Revoke_All_Sessions_Invariant8()
        {
            var account = ActiveAccount();
            AddSession(account);
            account.IssuePasswordResetToken("t", DateTime.UtcNow.AddHours(1));
            var newHash = PasswordHash.Create("$argon2id$new-hash").Value;

            account.CompletePasswordReset("t", newHash);

            account.Sessions.Should().AllSatisfy(s => s.IsRevoked.Should().BeTrue(),
                because: "invariant #8: password reset must revoke all sessions");
        }

        [Fact]
        public void UserId_Is_Immutable_After_Provision_Invariant9()
        {
            var account = MakeAccount();
            var originalId = account.Id.Value;

            account.Activate();
            account.Suspend("r");
            account.Reinstate();
            account.Deactivate();

            account.Id.Value.Should().Be(originalId, "UserId must be immutable (invariant #9)");
        }

        [Fact]
        public void Permissions_Are_Replaced_On_AssignRole_Invariant10()
        {
            var account = ActiveAccount(UserRole.JobSeeker);
            // JobSeeker has profile:self; Employer does NOT
            var employerPerms = PermissionResolver.Resolve(UserRole.Employer, []);

            account.AssignRole(UserRole.Employer, employerPerms);

            account.Permissions.Should().NotContain("profile:self",
                because: "permissions are a pure function of the new role (invariant #10)");
            account.Permissions.Should().Contain("jobs:write");
        }
    }
}
