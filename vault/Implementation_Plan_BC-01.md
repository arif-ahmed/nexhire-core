# Implementation Plan — BC-01 Identity Access & User Account Management

> **Based on:** `vault/Handover_Packages/BC-01-IAM-and-UAM.md` + `vault/Handover_Packages/00-Shared-Foundations.md`
> **Target:** Fully build the `IdentityAccess` module against the design spec, with in-memory DB first (swappable to real SQL Server/PostgreSQL).

---

## Migration from Current State

The existing implementation is a skeleton (~5% of the spec). Key differences:

| Aspect | Current | Target (Design) |
|---|---|---|
| `UserAccount` aggregate | `Email` + `FullName` + `CreatedAtUtc` only | `Credential` (email+mobile+passwordHash), `Role`, `Status`, `LockState`, `Mfa`, `BackupCodes`, `Sessions`, `PasswordHistory`, `Permissions`, 21 behaviors |
| Child entities | None | `Session`, `TrustedDevice`, `BackupCode`, `PasswordResetToken` |
| `OtpChallenge` | Not implemented | Separate aggregate with own lifecycle |
| `AdminActionLog` | Not implemented | Append-only log |
| Commands | 1 (`CreateUser`) | 25 commands ($10.1) |
| Queries | 1 (`GetUserById`) | 7 queries ($10.2) |
| Ports | None | 6 port interfaces ($9.2) |
| DB tables | 1 (`user_accounts` with 4 columns) | 8 tables ($11.1) + outbox/inbox |
| API routes | 2 (`POST/GET /api/users`) | 26 routes under `/api/identity` ($12) |
| Contracts layer | Doesn't exist | New project for integration events + public APIs |

---

## Phase 0 — Purge Incompatible Implementation

Remove these files before rebuilding. They conflict with the design's aggregate shape, command set, and routing.

| File | Reason |
|---|---|
| `Domain/Domain/UserAccount.cs` | Wrong shape — no `Credential`, `Role`, `Status`, etc. Has `FullName` which is out-of-scope for IAM |
| `Domain/Domain/ValueObjects/FullName.cs` | Not in IAM scope — belongs to BC-2/BC-3 |
| `Application/Accounts/Commands/CreateUser/*` | Design has `ProvisionCredentialCommand`, not `CreateUserCommand` |
| `Application/Accounts/Queries/GetUserById/*` | Replaced by `GetMyAccountQuery` + `GetUserAsAdminQuery` |
| `Application/DTOs/UserDto.cs` | Replaced by 7 new DTOs |
| `Presentation/Endpoints/UserEndpoints.cs` | Wrong routes (`/api/users` vs `/api/identity`), wrong commands |
| `Presentation/IdentityAccessPresentationModule.cs` | Updated to reference new endpoint class |
| `Infrastructure/Persistence/Configurations/UserAccountConfiguration.cs` | Full schema rewrite — 18 columns instead of 4 |
| `Infrastructure/Persistence/Repositories/UserAccountRepository.cs` | New interface with 10+ methods |
| `Domain/Domain/Repositories/IUserAccountRepository.cs` | New interface |

---

## Phase 1 — Domain Layer: Enums + Value Objects

### 1.1 New Enums

Create `Domain/Domain/` enums:

| Enum | Values | File |
|---|---|---|
| `UserRole` | `JobSeeker`, `Employer`, `ThirdPartyPortal`, `MoLAdministrator` | `UserRole.cs` |
| `AccountStatus` | `PendingActivation`, `Active`, `Suspended`, `Deactivated` | `AccountStatus.cs` |
| `OtpPurpose` | `Activation`, `Mfa`, `PasswordReset` | `OtpPurpose.cs` |
| `OtpStatus` | `Issued`, `Verified`, `Expired`, `Locked` | `OtpStatus.cs` |
| `Channel` | `Web`, `Mobile`, `Api` | `Channel.cs` |
| `AdminActionType` | `ApprovedEmployer`, `RejectedEmployer`, `Suspended`, `Reinstated`, `Deactivated`, `Unlocked`, `PasswordResetIssued`, `RoleAssigned`, `Viewed` | `AdminActionType.cs` |

### 1.2 Strongly-Typed IDs

```csharp
// Domain/Domain/
public record UserAccountId(Guid Value);
public record OtpChallengeId(Guid Value);
public record SessionId(Guid Value);
public record BackupCodeId(Guid Value);
public record TrustedDeviceId(Guid Value);
public record PasswordResetTokenId(Guid Value);
```

> **Note:** Each aggregates/entities use strongly-typed IDs (per $5 shared kernel convention). EF Core maps these via value converters. The bare `Guid` stored in `user_accounts.id` *is* the platform `UserId` that other BCs reference as a plain `Guid`.

### 1.3 Existing VOs (Retain)

These are already correct per the design; keep as-is:

- `EmailAddress` — RFC 5322, lower-cased on store
- `MobileNumber` — E.164, default +880
- `PasswordHash` — argon2id algorithm validation
- `RawPassword` — min 10 chars, >=3 character classes, trivial sequence check
- `LockState` — `IsLocked`, `LockedUntilUtc?`, `FailedLoginCount`, `FailedOtpCount`
- `MfaConfiguration` — `Enabled`, `Method` (`Totp`/`SmsOtp`/`None`), `SecretRef?`
- `AccessTokenSpec` — `Subject`, `Role`, `Permissions`, `Scopes`, `SessionId`, `ExpiresOnUtc` (max 1h TTL)
- `DeviceFingerprint` — non-empty hash string
- `Credential` — composite of `Email` + `Mobile` + `PasswordHash`

**Minor update to `Credential.Create`:** Change nullable params to non-nullable for cleaner design.

### 1.4 Tests

**Existing VO tests** (retain): `EmailAddressTests`, `MobileNumberTests`, `RawPasswordTests`, `PasswordHashTests`, `LockStateTests`, `MfaConfigurationTests`, `AccessTokenSpecTests`, `DeviceFingerprintTests`, `CredentialTests`.

**Add:** Strongly-typed ID tests (equality, construction), enum parsing tests.

---

## Phase 2 — Domain Layer: Domain Services

New directory: `Domain/Domain/Services/`

### `PasswordPolicyService`

```csharp
public static Result Validate(RawPassword candidate, string errorCodePrefix = "E-REG")
```

Wraps `RawPassword.Create` result, maps error codes to `E-REG-INVALID-PASSWORD` or `E-RESET-INVALID-PASSWORD`. Pure logic — breach check and no-reuse are separate (handler concerns).

### `PermissionResolver`

```csharp
public static IReadOnlyList<string> Resolve(UserRole role, IReadOnlyList<string> explicitGrants)
```

| Role | Baseline permissions |
|---|---|
| `JobSeeker` | `profile:self`, `applications:self`, `search:read` |
| `Employer` | `employer:self`, `jobs:write`, `applications:read`, `candidates:read` |
| `ThirdPartyPortal` | `integrations:read`, `jobs:write` (scoped) |
| `MoLAdministrator` | `users:manage`, `jobs:moderate`, `taxonomy:manage`, `reports:read`, plus all of the above |

### `TokenClaimsBuilder`

```csharp
public static Result<AccessTokenSpec> BuildAccessToken(UserAccount account, Guid sessionId, IReadOnlyList<string> scopes, TimeSpan ttl)
```

Assembles claims from account data. Pure — calls `AccessTokenSpec.Create`.

### `AccountStateMachine`

```csharp
public static Result EnsureTransitionAllowed(AccountStatus from, AccountStatus to)
```

Legal transitions ($6.2 invariant #1):

| From | To |
|---|---|
| `PendingActivation` | `Active` |
| `PendingActivation` | `Suspended` |
| `Active` | `Suspended` |
| `Suspended` | `Active` |
| `Active` | `Deactivated` |
| `Deactivated` | `PendingActivation` |
| `Deactivated` | `Active` |

All other transitions (8 total) are illegal.

### Tests

- **`PasswordPolicyServiceTests`**: 6+ cases (valid, too short, too few classes, trivial sequence, with both error prefixes)
- **`PermissionResolverTests`**: 6+ cases (each role baseline, union with explicit grants, empty grants)
- **`TokenClaimsBuilderTests`**: 4+ cases (valid spec, claims include all required fields, no secrets leaked)
- **`AccountStateMachineTests`**: Exhaustive 4x4=16 transition matrix (8 legal, 8 illegal)

---

## Phase 3 — Domain Layer: Aggregates + Child Entities + Events

### 3.1 `UserAccount` Aggregate (Full Rewrite)

**File:** `Domain/Domain/UserAccount.cs`

```csharp
public class UserAccount : AggregateRoot<UserAccountId>
{
    // Members (§5.1)
    public Credential Credential { get; private set; }
    public UserRole Role { get; private set; }
    public AccountStatus Status { get; private set; }
    public LockState LockState { get; private set; }
    public MfaConfiguration Mfa { get; private set; }
    private readonly List<BackupCode> _backupCodes = new();
    private readonly List<TrustedDevice> _trustedDevices = new();
    private readonly List<Session> _sessions = new();
    private readonly List<string> _passwordHistory = new(); // SHA-256 verifiers for no-reuse check
    private readonly List<string> _permissions = new();
    private readonly List<PasswordResetToken> _passwordResetTokens = new();
    public bool IdentityVerified { get; private set; }
    public DateTime? ActivatedOnUtc { get; private set; }
    public string? SuspendedReason { get; private set; }
    public DateTime? DeactivatedOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    // --- Behaviors (§6.1) ---
    public static UserAccount Provision(EmailAddress email, MobileNumber mobile, PasswordHash passwordHash, UserRole role, IReadOnlyList<string> permissions);
    public Result Activate();
    public Result RecordSuccessfulLogin(Channel channel, DeviceFingerprint deviceFingerprint, string refreshTokenHash, DateTime expiresOnUtc);
    public void RecordFailedLogin();
    public Result EnableMfa(MfaMethod method, string secretRef, IReadOnlyList<string> backupCodeHashes);
    public void DisableMfa();
    public Result RedeemBackupCode(string codeHash);
    public void TrustDevice(DeviceFingerprint fingerprint, string label, DateTime trustedUntilUtc);
    public void RevokeSession(SessionId sessionId);
    public void RevokeAllSessions();
    public Result TouchSession(SessionId sessionId, DateTime nowUtc);
    public void IssuePasswordResetToken(string tokenHash, DateTime expiresOnUtc);
    public Result CompletePasswordReset(string tokenHash, PasswordHash newPasswordHash);
    public Result ChangePassword(PasswordHash newPasswordHash);
    public void AssignRole(UserRole role, IReadOnlyList<string> newPermissions);
    public Result Suspend(string reason);
    public void Reinstate();
    public Result Deactivate();
    public Result ReactivateAfterDeactivation();
    public void Lock(DateTime lockedUntilUtc);
    public void Unlock();
    public void ApplyGovernmentIdentityVerified();
    public void RecordOtpFailure();
}
```

**⚠️ Design Issue — argon2id + `PasswordHistory` no-reuse check:**

Argon2id uses a random salt per hash, so `PasswordHash.Value` for the same raw password differs each time. Comparing hash strings for the no-reuse check (invariant #4) won't work.

**Solution:** Store a deterministic verifier (SHA-256 of the normalized raw password) in `_passwordHistory` instead of the argon2id hash. Before calling `PasswordHasher.Hash()` on the new password, compute the verifier and check it against history. This keeps the domain crypto-aware only of this one deterministic comparison; argon2id hashing remains behind the port.

### 3.2 Child Entities

**`Session`** (`Domain/Domain/Session.cs`):
```csharp
public class Session : Entity<SessionId>
{
    public UserAccountId UserAccountId { get; private set; }
    public Channel Channel { get; private set; }
    public DeviceFingerprint DeviceFingerprint { get; private set; }
    public string RefreshTokenHash { get; private set; }
    public DateTime IssuedOnUtc { get; private set; }
    public DateTime LastSeenUtc { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime? RevokedOnUtc { get; private set; }

    public bool IsRevoked => RevokedOnUtc.HasValue;
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresOnUtc;
    public bool IsInactive(DateTime utcNow, TimeSpan inactivityTimeout)
        => utcNow - LastSeenUtc > inactivityTimeout;

    public void Revoke();
    public void Touch(DateTime utcNow);
}
```

**`TrustedDevice`** (`Domain/Domain/TrustedDevice.cs`): `TrustedDeviceId`, `DeviceFingerprint`, `Label`, `TrustedUntilUtc`.

**`BackupCode`** (`Domain/Domain/BackupCode.cs`):
```csharp
public class BackupCode : Entity<BackupCodeId>
{
    public string CodeHash { get; private set; }
    public DateTime? UsedOnUtc { get; private set; }
    public bool IsUsed => UsedOnUtc.HasValue;
    public Result Redeem(); // Fails if already used
}
```

**`PasswordResetToken`** (`Domain/Domain/PasswordResetToken.cs`):
```csharp
public class PasswordResetToken : Entity<PasswordResetTokenId>
{
    public string TokenHash { get; private set; }
    public DateTime IssuedOnUtc { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime? UsedOnUtc { get; private set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresOnUtc;
    public bool IsUsed => UsedOnUtc.HasValue;
    public Result MarkUsed(); // Fails if already used
}
```

### 3.3 `OtpChallenge` Aggregate (Separate Root)

**File:** `Domain/Domain/OtpChallenge.cs`

```csharp
public class OtpChallenge : AggregateRoot<OtpChallengeId>
{
    public UserAccountId UserAccountId { get; private set; }
    public OtpPurpose Purpose { get; private set; }
    public string CodeHash { get; private set; }
    public OtpStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }  // 5 for Activation, 3 otherwise
    public DateTime IssuedOnUtc { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }  // IssuedOnUtc + 5min
    public DateTime? VerifiedOnUtc { get; private set; }

    // Behaviors ($6.3)
    public static OtpChallenge Issue(UserAccountId userAccountId, OtpPurpose purpose, string codeHash, TimeSpan ttl, int maxAttempts);
    public Result<string> Verify(string submittedCodeHash, DateTime utcNow);
    // Returns Result.Ok() on success; E-OTP-EXPIRED if past 5min; E-OTP-LOCKED if max attempts exceeded
    public void MarkExpired();
}
```

### 3.4 `AdminActionLog` (Append-Only Log)

**File:** `Domain/Domain/AdminActionLog.cs`

Not an aggregate — just a factory + record. Persisted in the same transaction as the aggregate change.

```csharp
public sealed class AdminActionLog
{
    public Guid Id { get; private set; }
    public Guid AdminUserId { get; private set; }
    public AdminActionType ActionType { get; private set; }
    public Guid TargetUserId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }

    public static AdminActionLog Record(Guid adminUserId, AdminActionType actionType, Guid targetUserId, string? reason);
}
```

### 3.5 Domain Events

**Directory:** `Domain/Domain/Events/`

**Internal domain events** ($8.2) — records implementing `IDomainEvent`:
- `MfaEnabledEvent(Guid EventId, UserAccountId AccountId, MfaMethod Method, DateTime OccurredOnUtc)`
- `MfaDisabledEvent(...)`
- `BackupCodeRedeemedEvent(...)`
- `SessionRevokedEvent(...)`
- `AccountUnlockedEvent(...)`
- `IdentityVerificationAppliedEvent(...)`
- `OtpIssuedEvent(...)`
- `OtpVerifiedEvent(...)`

**Integration events** ($8.1) — defined in the Contracts layer (Phase 9), but the aggregate raises them as `IDomainEvent`:
- `UserRegisteredIntegrationEvent`
- `UserAccountActivatedIntegrationEvent`
- `UserAccountSuspendedIntegrationEvent`
- `UserAccountReinstatedIntegrationEvent`
- `AccountDeactivatedIntegrationEvent`
- `UserLoggedInIntegrationEvent`
- `UserLoginFailedIntegrationEvent`
- `PasswordResetIntegrationEvent`
- `RoleAssignedIntegrationEvent`

The aggregate raises integration events via `RaiseDomainEvent()`. The `PublishDomainEventsInterceptor` writes them to the outbox in the same transaction.

### 3.6 Tests

- **`UserAccountBehaviourTests`**: ~100+ test methods covering all 21 behaviors + invariants ($13.1):
  - Status machine: every legal transition succeeds; every illegal one fails
  - `RecordSuccessfulLogin` fails from each non-`Active` status with distinct error codes
  - `Suspend` requires non-empty reason, sets `SuspendedReason`, revokes sessions, raises event
  - `Deactivate` revokes sessions, sets `DeactivatedOnUtc`, raises event
  - `CompletePasswordReset` with wrong/expired/used token fails; valid token → sets new hash + revokes sessions
  - `ChangePassword` / `CompletePasswordReset` reject hash matching any of last 3 in history
  - `EnableMfa` stores 8-10 backup codes; `RedeemBackupCode` single-use
  - `TouchSession` past inactivity revokes session
  - `AssignRole` recomputes permissions
  - `UserId` immutable after `Provision`
- **`OtpChallengeTests`**: 15+ methods ($13.1):
  - `Verify` after expiry → `Expired` + `E-OTP-EXPIRED`
  - Wrong attempts past `MaxAttempts` → `Locked` + `E-OTP-LOCKED`
  - Correct code → `Verified`
  - Already-`Verified`/`Locked` challenge cannot be re-verified
- **`AdminActionLogTests`**: Factory creates correct record, append-only
- **Child entity tests**: `Session.Revoke()` / `Touch()`, `BackupCode.Redeem()` single-use, `PasswordResetToken.MarkUsed()`

---

## Phase 4 — Application Layer: Port Interfaces

**Directory:** `Application/Ports/`

| Interface | Methods | Real impl | Stub for dev |
|---|---|---|---|
| `IPasswordHasher` | `Hash(RawPassword) -> PasswordHash`, `Verify(RawPassword, PasswordHash) -> bool` | Konscious argon2id | Same (real) |
| `IBreachCheckPort` | `IsBreached(RawPassword) -> bool` | HaveIBeenPwned API client | In-memory blocklist |
| `IJwtSigner` | `SignAccessToken(AccessTokenSpec) -> string`, `IssueRefreshToken() -> (string Token, string TokenHash)`, `ValidateSignature(string) -> bool` | Microsoft.IdentityModel RS256 | Same (real with dev key) |
| `IOtpDeliveryPort` | `Send(string destination, string plaintextCode, OtpPurpose purpose) -> Result` | SMS/email gateway adapter | ILogger log |
| `ITotpProvider` | `Enroll(string accountLabel) -> (string SecretRef, string ProvisioningUri)`, `Verify(string secretRef, string submittedCode) -> bool` | Otp.NET RFC 6238 | Same (real) |
| `IRateLimiterPort` | `TryConsume(string key, int maxInWindow, TimeSpan window) -> bool` | Redis-based (distributed) | In-memory ConcurrentDictionary |

The port interfaces stay **exactly** as specified in $9.2 so production adapters drop in later.

---

## Phase 5 — Application Layer: Commands + Handlers

**Directory:** `Application/Accounts/Commands/{CommandName}/`

### Batch A — Registration & Activation

| Command | Handler responsibilities ($10.1) | Validator rules ($10.3) |
|---|---|---|
| `ProvisionCredentialCommand` | Rate-limit → uniqueness → policy → breach → hash → `UserAccount.Provision()` → `OtpChallenge.Issue(Activation)` → persist → react to `OtpIssued` → return `UserId` | Email RFC 5322, Mobile E.164, password >=10 chars, role in enum |
| `ActivateAccountCommand` | Load `OtpChallenge(Activation)` + `UserAccount` → `challenge.Verify()` → `account.Activate()` → persist | UserId present, OTP exactly 6 digits |
| `ResendActivationOtpCommand` | Load account → if `PendingActivation`, expire prior challenge → reissue + resend. Rate-limited. | UserId present |

### Batch B — Login & Sessions

| Command | Handler responsibilities | Validator rules |
|---|---|---|
| `LoginWithCredentialsCommand` | Rate-limit per IP (10/min) → `GetByEmailOrMobile` (auto-detect) → wrong/missing → generic `E-LOGIN-INVALID-CREDENTIALS` → status check → lock check → MFA check → `TokenClaimsBuilder` + `JwtSigner` → `RecordSuccessfulLogin` → persist | Identifier present, password present |
| `RefreshAccessTokenCommand` | Lookup session by token hash → reject if revoked/expired → rotate tokens → persist | Token string present |
| `RevokeTokenCommand` | Add to revocation list; `account.RevokeSession(sessionId)` | Token string present |
| `LogoutCommand` | `account.RevokeSession(currentSessionId)` → persist | — |
| `LogoutAllSessionsCommand` | `account.RevokeAllSessions()` → persist | — |

### Batch C — MFA

| Command | Handler responsibilities | Validator rules |
|---|---|---|
| `EnrollMfaCommand` | Authenticated, `Active` → `TotpProvider.Enroll` or SMS OTP sent | Method in {Totp, SmsOtp} |
| `ConfirmMfaEnrollmentCommand` | Verify test code → generate 8-10 backup codes → `account.EnableMfa()` → persist → return plaintext codes once | — |
| `DisableMfaCommand` | Re-auth → `account.DisableMfa()` → persist | — |
| `VerifyMfaChallengeCommand` | TOTP/SMS-OTP/backup code → on success issue tokens → on failure `RecordOtpFailure` (3 failures → 15min lock) | — |

### Batch D — Password Reset

| Command | Handler responsibilities | Validator rules |
|---|---|---|
| `RequestPasswordResetCommand` | Rate-limit (3/hour) → `GetByEmailOrMobile` → if found, `OtpChallenge.Issue(PasswordReset)` + send SMS → persist. Always returns success (no enumeration) | — |
| `VerifyPasswordResetOtpCommand` | Load `OtpChallenge(PasswordReset)` → `challenge.Verify()` → on success `account.IssuePasswordResetToken()` | OTP exactly 6 digits |
| `CompletePasswordResetCommand` | Validate token → policy + breach + no-reuse → hash → `account.CompletePasswordReset()` → persist | Token present, new password >=10 chars |
| `ChangePasswordCommand` | Authenticated; verify current password → policy + breach + no-reuse → `account.ChangePassword()` → persist | — |

### Batch E — Admin Actions

| Command | Handler responsibilities | Validator rules |
|---|---|---|
| `AdminApproveEmployerCommand` | Load target → `account.Activate()` → append `AdminActionLog(ApprovedEmployer)` → persist | — |
| `AdminRejectEmployerCommand` | `reason` required → `account.Suspend(reason)` → append `AdminActionLog(RejectedEmployer)` → persist | `reason` non-empty |
| `AdminSuspendUserCommand` | `reason` required → `account.Suspend(reason)` → append `AdminActionLog(Suspended)` → persist | `reason` non-empty |
| `AdminReinstateUserCommand` | `account.Reinstate()` → append `AdminActionLog(Reinstated)` → persist | — |
| `AdminDeactivateUserCommand` | `account.Deactivate()` → append `AdminActionLog(Deactivated)` → persist | `reason` non-empty |
| `AdminUnlockAccountCommand` | `account.Unlock()` → append `AdminActionLog(Unlocked)` → persist | — |
| `AdminIssuePasswordResetCommand` | `account.IssuePasswordResetToken()` → append `AdminActionLog(PasswordResetIssued)` → persist → trigger reset notification | — |
| `AssignRoleCommand` | `account.AssignRole(role, newPermissions)` → append `AdminActionLog(RoleAssigned)` → persist | role in enum, UserId present |
| `IssueOAuthTokenCommand` | Authorization-Code (PKCE) / Client-Credentials flow → `TokenClaimsBuilder` + `JwtSigner` → create `Session(Api)` → return tokens | — |

### 5.1 Tests

Per $13.2 — each handler tested with port doubles:

- **`ProvisionCredentialCommandHandlerTests`**: 8+ tests (happy path, duplicate email, duplicate mobile, breached password, rate limited, invalid email format, invalid mobile format)
- **`LoginWithCredentialsCommandHandlerTests`**: 10+ tests (wrong password, not-activated, deactivated, suspended, locked, rate-limited, MFA challenge returned, MFA disabled → tokens, "remember me" expiry)
- **`VerifyMfaChallengeCommandHandlerTests`**: 6+ tests (valid TOTP, valid SMS-OTP, valid backup code, consumed backup code rejected, 3 failures → 15min lock)
- **`ActivateAccountCommandHandlerTests`**: 4+ tests (valid OTP, expired OTP, too many failed OTPs, account state after failure)
- **`CompletePasswordResetCommandHandlerTests`**: 6+ tests (valid, expired token, breached password, reused password, locked account reset, no-enumeration)
- **`RefreshAccessTokenCommandHandlerTests`**: 4 tests (valid token rotated, revoked token rejected, expired token rejected, unknown token rejected)
- **`AdminSuspendUserCommandHandlerTests`**: 3 tests (happy path + admin log written, empty reason rejected by validator, not-active account)
- Validator tests for each command

---

## Phase 6 — Application Layer: Queries + DTOs

**Directory:** `Application/Accounts/Queries/{QueryName}/` + `Application/DTOs/`

| Query | Returns | Notes ($10.2) |
|---|---|---|
| `GetMyAccountQuery` | `AccountDto` | UserId, email/mobile (masked for admin), role, status, MFA enabled, identityVerified |
| `GetMySessionsQuery` | `List<SessionDto>` | Channel, device label, issued, lastSeen, isCurrent? |
| `GetMfaStatusQuery` | `MfaStatusDto` | Enabled, method, lastVerified?, backupCodesRemaining |
| `ListUsersQuery` | `PagedResult<UserListItemDto>` | Searchable (name/email) + filterable (type, status, date, verification) |
| `GetUserAsAdminQuery` | `AdminUserDetailDto` | Full contact, registration info, verification, status, lock state, session count. Logs `AdminActionLog(Viewed)`. |
| `GetAdminActionLogQuery` | `PagedResult<AdminActionDto>` | Filter by admin, target, action type, date range |
| `ValidateTokenQuery` | `ValidatedPrincipal` | UserId, Role, Permissions, SessionId — behind `TokenValidationApi` |

**DTOs** ($10.4):

| DTO | Fields |
|---|---|
| `AccountDto` | `Guid UserId, string Email, string? MobileMasked, string Role, string Status, bool MfaEnabled, bool IdentityVerified` |
| `SessionDto` | `Guid SessionId, string Channel, string? DeviceLabel, DateTime IssuedOnUtc, DateTime LastSeenUtc, bool IsCurrent` |
| `MfaStatusDto` | `bool Enabled, string? Method, DateTime? LastVerifiedUtc, int BackupCodesRemaining` |
| `UserListItemDto` | `Guid UserId, string Email, string Role, string Status, DateTime CreatedOnUtc, bool IdentityVerified` |
| `AdminUserDetailDto` | `Guid UserId, string Email, string Mobile, string Role, string Status, bool IdentityVerified, bool IsLocked, DateTime? LockedUntilUtc, int FailedLoginCount, int FailedOtpCount, int SessionCount` |
| `AdminActionDto` | `Guid Id, Guid AdminUserId, string ActionType, Guid TargetUserId, string? Reason, DateTime OccurredOnUtc` |
| `PagedResult<T>` | `IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize` |
| `ValidatedPrincipal` | `Guid UserId, string Role, List<string> Permissions, Guid SessionId` |

**Never expose** in any DTO: password hash, OTP code, refresh-token hash, MFA secret, backup-code hash. Mobile numbers masked (`+8801******23`) in admin DTOs.

---

## Phase 7 — Application Layer: Validators

14 validators total in `Application/Accounts/Commands/{CommandName}/` alongside each handler. FluentValidation rules from $10.3:

| Validator | Key Rules |
|---|---|
| `ProvisionCredentialCommandValidator` | Email RFC 5322 → `E-REG-INVALID-EMAIL`; Mobile E.164 → `E-REG-INVALID-MOBILE`; password >=10 chars & >=3 classes → `E-REG-INVALID-PASSWORD`; role in enum |
| `ActivateAccountCommandValidator` | UserId present; OTP exactly 6 digits |
| `LoginWithCredentialsCommandValidator` | Identifier present (email or mobile); password present |
| `EnrollMfaCommandValidator` | Method in {Totp, SmsOtp} |
| `VerifyPasswordResetOtpCommandValidator` | OTP exactly 6 digits |
| `CompletePasswordResetCommandValidator` | Token present; new password >=10 chars & >=3 classes |
| `AdminSuspendUserCommandValidator` | `reason` non-empty |
| `AdminRejectEmployerCommandValidator` | `reason` non-empty |
| `AdminDeactivateUserCommandValidator` | `reason` non-empty |
| `AssignRoleCommandValidator` | Role in enum; target UserId present |
| `RefreshAccessTokenCommandValidator` | Token string present |
| `RevokeTokenCommandValidator` | Token string present |

---

## Phase 8 — Infrastructure: EF Core + Repositories + Adapters + Background Services

### 8.1 `IdentityAccessDbContext` (Extend)

Implement `IOutboxInboxDbContext` — add `DbSet<OutboxMessage>` + `DbSet<InboxMessage>`.

```csharp
public class IdentityAccessDbContext : DbContext, IOutboxInboxDbContext
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<AdminActionLog> AdminActionLogs => Set<AdminActionLog>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    // ...
}
```

**`RevokedToken`** is a new simple entity (not an aggregate):
```csharp
public sealed class RevokedToken
{
    public string TokenIdOrRefreshHash { get; private set; }
    public DateTime RevokedOnUtc { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
}
```

### 8.2 EF Configurations

**Schema:** `identity_access` (already set via `HasDefaultSchema`).

**`UserAccountConfiguration`** — the core configuration, 18 columns:

| Column | EF Mapping |
|---|---|
| `Id` | `UserAccountId → Guid` via `HasConversion(id => id.Value, g => new UserAccountId(g))` |
| `email` | Scalar (flattened from `Credential.Email.Value`), unique index, max 255 |
| `mobile` | Scalar (flattened from `Credential.Mobile.Value`), unique index, max 20 |
| `password_algorithm` | Scalar (`Credential.PasswordHash.Algorithm`), max 20 |
| `password_hash` | Scalar (`Credential.PasswordHash.Value`), max 255 |
| `password_history` | `json` column (list of SHA-256 verifier strings) |
| `role` | Stored as string, max 50 |
| `status` | Stored as string, max 50 |
| `permissions` | `json` column |
| `lock_state` | `OwnsOne` → `LockState` VO, stored as json |
| `mfa` | `OwnsOne` → `MfaConfiguration` VO, stored as json |
| `identity_verified` | bool, default false |
| `suspended_reason` | nullable string |
| `activated_on_utc` | nullable datetime |
| `deactivated_on_utc` | nullable datetime |
| `created_on_utc` | datetime |
| `updated_on_utc` | datetime |
| `version` | `IsConcurrencyToken=true` (optimistic concurrency) |

Indices: `email` (unique), `mobile` (unique), `status`, `role`.

**Owned collections:**

```csharp
builder.OwnsMany(u => u.Sessions, s =>
{
    s.ToTable("sessions");
    s.WithOwner().HasForeignKey("user_account_id");
    s.Property(x => x.Id).HasConversion(id => id.Value, g => new SessionId(g));
    // ... scalar columns for each field
});

builder.OwnsMany(u => u.BackupCodes, b => { /* ... */ });
builder.OwnsMany(u => u.TrustedDevices, td => { /* ... */ });
builder.OwnsMany(u => u.PasswordResetTokens, prt => { /* ... */ });
```

**`OtpChallengeConfiguration`** (separate aggregate):
- `Id` → `OtpChallengeId` value converter
- `UserAccountId` → plain `Guid` column with **no FK constraint** ($11.2: no cross-aggregate FK)
- Purpose stored as string
- Status stored as string
- Indices: `(user_account_id, purpose) WHERE status = 'Issued'`

**`AdminActionLogConfiguration`**: Simple table mapping. Indices on `target_user_id`, `admin_user_id`, `occurred_on_utc`.

**`RevokedTokenConfiguration`**: PK on `token_id_or_refresh_hash`.

**Outbox/Inbox configuration** (standard per Shared Foundations).

### 8.3 Repository Implementations

**`UserAccountRepository`**:
```csharp
public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(UserAccountId id, CancellationToken ct = default);
    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<UserAccount?> GetByMobileAsync(string mobile, CancellationToken ct = default);
    Task<UserAccount?> GetByEmailOrMobileAsync(string identifier, CancellationToken ct = default);
    Task<UserAccount?> GetBySessionRefreshTokenHashAsync(string hash, CancellationToken ct = default);
    Task<UserAccount?> GetByPasswordResetTokenHashAsync(string hash, CancellationToken ct = default);
    Task<PagedResult<UserAccount>> SearchAsync(UserSearchCriteria criteria, CancellationToken ct = default);
    Task AddAsync(UserAccount user, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> MobileExistsAsync(string mobile, CancellationToken ct = default);
}
```

Implementation uses `.Include(u => u.Sessions)` etc. where needed, `.AsSplitQuery()` for owned collections.

**`OtpChallengeRepository`**:
```csharp
public interface IOtpChallengeRepository
{
    Task<OtpChallenge?> GetByIdAsync(OtpChallengeId id, CancellationToken ct = default);
    Task<OtpChallenge?> GetActiveByAccountAndPurposeAsync(UserAccountId accountId, OtpPurpose purpose, CancellationToken ct = default);
    Task AddAsync(OtpChallenge challenge, CancellationToken ct = default);
}
```

**`RevokedTokenStore`**:
```csharp
public interface IRevokedTokenStore
{
    Task AddAsync(string tokenIdOrHash, DateTime expiresOnUtc, CancellationToken ct = default);
    Task<bool> IsRevokedAsync(string tokenIdOrHash, CancellationToken ct = default);
}
```

**`AdminActionLogRepository`**:
```csharp
public interface IAdminActionLogRepository
{
    Task AddAsync(AdminActionLog log, CancellationToken ct = default);
    Task<PagedResult<AdminActionLog>> QueryAsync(AdminActionLogQuery criteria, CancellationToken ct = default);
}
```

### 8.4 Port Adapters

| Adapter | File | Implementation |
|---|---|---|
| `PasswordHasher` | `Infrastructure/PortAdapters/PasswordHasher.cs` | Real argon2id via `Konscious.Security.Cryptography.Argon2id`. Hash params: memory=65536, iterations=3, parallelism=4, salt=16 bytes, tag=32 bytes. Verify re-hashes with the stored salt+params. |
| `BreachCheckPort` | `Infrastructure/PortAdapters/BreachCheckPortStub.cs` | Stub: `HashSet<string>` of common breached passwords. For production, replace with a k-anonymity HaveIBeenPwned API client. |
| `JwtSigner` | `Infrastructure/PortAdapters/JwtSigner.cs` | Real RS256 via `Microsoft.IdentityModel.Tokens` + `System.IdentityModel.Tokens.Jwt`. Dev RSA key from `appsettings.Development.json`. Access token: 1h TTL. Refresh token: 30d TTL, opaque, stored as hash. |
| `OtpDeliveryPort` | `Infrastructure/PortAdapters/OtpDeliveryPortStub.cs` | Stub: logs "OTP for {purpose} to {destination}: {code}" via ILogger. |
| `TotpProvider` | `Infrastructure/PortAdapters/TotpProvider.cs` | Real RFC 6238 via `Otp.NET` NuGet. TOTP: 6 digits, 30s step, SHA-1. |
| `RateLimiterPort` | `Infrastructure/PortAdapters/RateLimiterPortStub.cs` | Stub: `ConcurrentDictionary<string, (int Count, DateTime WindowStart)>` in-memory sliding window. For production, replace with Redis-based. |

### 8.5 Background Services

**`OtpExpirySweepBackgroundService`** (30s interval):
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        var challenges = await _dbContext.OtpChallenges
            .Where(o => o.Status == OtpStatus.Issued && o.ExpiresOnUtc <= DateTime.UtcNow)
            .ToListAsync(stoppingToken);
        foreach (var c in challenges) c.MarkExpired();
        await _dbContext.SaveChangesAsync(stoppingToken);
    }
}
```

**`SessionExpirySweepBackgroundService`** (60s interval): Loads `UserAccount`s with `Sessions`, calls `TouchSession` for each session past inactivity window, saves.

**`CleanupBackgroundService`** (1h interval): Deletes from `RevokedTokens` and `OtpChallenges` where `expires_on_utc < UtcNow - 7d`.

### 8.6 DI Registration (`IdentityAccessModule.cs`)

```csharp
public static class IdentityAccessModule
{
    public static IServiceCollection AddIdentityAccessModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        // DbContext — switch UseInMemoryDatabase to UseNpgsql/UseSqlServer for real DB
        services.AddDbContext<IdentityAccessDbContext>(options =>
            options.UseInMemoryDatabase("IdentityAccess"));

        // Repositories
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IOtpChallengeRepository, OtpChallengeRepository>();
        services.AddScoped<IRevokedTokenStore, RevokedTokenStore>();
        services.AddScoped<IAdminActionLogRepository, AdminActionLogRepository>();

        // Port adapters
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IBreachCheckPort, BreachCheckPortStub>();
        services.AddScoped<IJwtSigner, JwtSigner>();
        services.AddScoped<IOtpDeliveryPort, OtpDeliveryPortStub>();
        services.AddScoped<ITotpProvider, TotpProvider>();
        services.AddScoped<IRateLimiterPort, RateLimiterPortStub>();

        // Background services
        services.AddHostedService<OtpExpirySweepBackgroundService>();
        services.AddHostedService<SessionExpirySweepBackgroundService>();
        services.AddHostedService<CleanupBackgroundService>();

        return services;
    }
}
```

> **Switching to real DB:** Change `UseInMemoryDatabase("IdentityAccess")` → `UseNpgsql(connectionString)` or `UseSqlServer(connectionString)`. Add EF Core migration (`dotnet ef migrations add Initial --project ...`). All other code stays the same.

---

## Phase 9 — Contracts Layer (New Project)

**Project:** `src/Modules/IdentityAccess/Nexhire.Modules.IdentityAccess.Contracts/`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\Nexhire.Shared.Core\Nexhire.Shared.Core.csproj" />
  </ItemGroup>
</Project>
```

### 9.1 Integration Events ($8.1)

```csharp
// Events/UserRegisteredIntegrationEvent.cs
public record UserRegisteredIntegrationEvent(
    Guid EventId,
    Guid UserId,
    string Role,
    string Email,
    DateTime CreatedAt,
    DateTime OccurredOnUtc) : IDomainEvent;

// Events/UserAccountActivatedIntegrationEvent.cs
public record UserAccountActivatedIntegrationEvent(
    Guid EventId,
    Guid UserId,
    DateTime ActivatedAt,
    DateTime OccurredOnUtc) : IDomainEvent;

// Events/UserAccountSuspendedIntegrationEvent.cs
public record UserAccountSuspendedIntegrationEvent(
    Guid EventId,
    Guid UserId,
    string Reason,
    Guid By,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;

// Events/UserAccountReinstatedIntegrationEvent.cs
public record UserAccountReinstatedIntegrationEvent(
    Guid EventId,
    Guid UserId,
    Guid? By,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;

// Events/AccountDeactivatedIntegrationEvent.cs
public record AccountDeactivatedIntegrationEvent(
    Guid EventId,
    Guid UserId,
    DateTime DeactivatedAt,
    DateTime OccurredOnUtc) : IDomainEvent;

// Events/UserLoggedInIntegrationEvent.cs
public record UserLoggedInIntegrationEvent(
    Guid EventId,
    Guid UserId,
    Guid SessionId,
    string Channel,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;

// Events/UserLoginFailedIntegrationEvent.cs
public record UserLoginFailedIntegrationEvent(
    Guid EventId,
    string Identifier,
    string Reason,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;

// Events/PasswordResetIntegrationEvent.cs
public record PasswordResetIntegrationEvent(
    Guid EventId,
    Guid UserId,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;

// Events/RoleAssignedIntegrationEvent.cs
public record RoleAssignedIntegrationEvent(
    Guid EventId,
    Guid UserId,
    string Role,
    Guid By,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;
```

### 9.2 Consumed Integration Events ($9.1)

```csharp
// Events/IdentityVerifiedByGovernmentIntegrationEvent.cs
public record IdentityVerifiedByGovernmentIntegrationEvent(
    Guid EventId,
    Guid UserId,
    string Registry,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;

// Events/IdentityVerificationFailedIntegrationEvent.cs
public record IdentityVerificationFailedIntegrationEvent(
    Guid EventId,
    Guid UserId,
    string Registry,
    string Reason,
    DateTime At,
    DateTime OccurredOnUtc) : IDomainEvent;
```

### 9.3 Public API Interfaces ($9.3)

```csharp
// IIdentityProvisioningApi.cs
public interface IIdentityProvisioningApi
{
    Task<Result<ProvisionedIdentity>> ProvisionCredential(
        string email, string mobile, string password, string role);
}

public record ProvisionedIdentity(Guid UserId);

// ITokenValidationApi.cs
public interface ITokenValidationApi
{
    Task<Result<ValidatedPrincipal>> Validate(string accessToken);
}

public record ValidatedPrincipal(Guid UserId, string Role, IReadOnlyList<string> Permissions, Guid SessionId);
```

> **Contract stability:** These signatures are referenced by BC-2 and BC-3 packages. Operation names, parameter order, return types, and `E-REG-*` error code strings must stay exactly as written.

---

## Phase 10 — Presentation Layer: API Endpoints

**File:** `Presentation/Endpoints/IdentityEndpoints.cs`

Full rewrite per $12. Route group: `/api/identity`.

```csharp
public static class IdentityEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/identity").WithTags("Identity Access");

        // Anonymous endpoints
        group.MapPost("login", ...);                  // LoginWithCredentialsCommand
        group.MapPost("activate", ...);               // ActivateAccountCommand
        group.MapPost("activate/resend", ...);        // ResendActivationOtpCommand
        group.MapPost("token/refresh", ...);          // RefreshAccessTokenCommand
        group.MapPost("token/revoke", ...);           // RevokeTokenCommand
        group.MapPost("password/reset-request", ...); // RequestPasswordResetCommand
        group.MapPost("password/reset-verify", ...);  // VerifyPasswordResetOtpCommand
        group.MapPost("password/reset", ...);         // CompletePasswordResetCommand
        group.MapPost("oauth/token", ...);            // IssueOAuthTokenCommand

        // Authenticated endpoints (require valid access token)
        group.MapGet("me", ...);                      // GetMyAccountQuery
        group.MapGet("me/sessions", ...);             // GetMySessionsQuery
        group.MapGet("me/mfa", ...);                  // GetMfaStatusQuery
        group.MapPost("mfa/enroll", ...);             // EnrollMfaCommand
        group.MapPost("mfa/enroll/confirm", ...);     // ConfirmMfaEnrollmentCommand
        group.MapDelete("mfa", ...);                  // DisableMfaCommand
        group.MapPost("mfa/verify", ...);             // VerifyMfaChallengeCommand
        group.MapPost("logout", ...);                 // LogoutCommand
        group.MapPost("logout-all", ...);             // LogoutAllSessionsCommand
        group.MapPost("password/change", ...);        // ChangePasswordCommand

        // Admin endpoints (require valid token + "users:manage" permission)
        group.MapGet("admin/users", ...).RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        group.MapGet("admin/users/{id}", ...).RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        group.MapPost("admin/users/{id}/approve", ...).RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        group.MapPost("admin/users/{id}/reject", ...).RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        group.MapPost("admin/users/{id}/suspend", ...).RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        group.MapPost("admin/users/{id}/reinstate", ...).RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        group.MapPost("admin/users/{id}/deactivate", ...).RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        group.MapPost("admin/users/{id}/unlock", ...).RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        group.MapPost("admin/users/{id}/password-reset", ...).RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        group.MapPost("admin/users/{id}/role", ...).RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
        group.MapGet("admin/audit", ...).RequireAuthorization(p => p.RequireClaim("permissions", "users:manage"));
    }
}
```

### Error-to-HTTP Mapping

```csharp
private static IResult ToHttpResult<T>(Result<T> result)
{
    if (result.IsSuccess)
        return Results.Ok(result.Value);

    return result.Error.Code switch
    {
        string c when c.Contains("INVALID") || c.Contains("MISSING") || c.Contains("EMPTY") => Results.BadRequest(result.Error),
        string c when c.Contains("UNAUTHORIZED") => Results.Unauthorized(),
        string c when c.Contains("FORBIDDEN") || c.Contains("BANNED") => Results.Json(result.Error, statusCode: 403),
        string c when c.Contains("NOT-FOUND") => Results.NotFound(result.Error),
        string c when c.Contains("EXPIRED") => Results.Json(result.Error, statusCode: 410),
        string c when c.Contains("CONFLICT") || c.Contains("DUPLICATE") || c.Contains("ALREADY") => Results.Json(result.Error, statusCode: 409),
        string c when c.Contains("LOCKED") => Results.Json(result.Error, statusCode: 423),
        string c when c.Contains("RATE-LIMITED") => Results.Json(result.Error, statusCode: 429),
        _ => Results.BadRequest(result.Error)
    };
}
```

Update `IdentityAccessPresentationModule.cs` to reference `IdentityEndpoints`.

---

## Phase 11 — Host Integration

### 11.1 `Program.cs` Updates

```csharp
// Add Contracts assembly for MediatR/integration event scanning
var moduleAssemblies = new[]
{
    typeof(Nexhire.Modules.IdentityAccess.Domain.UserAccount).Assembly,
    typeof(Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.ProvisionCredential.ProvisionCredentialCommand).Assembly,
    typeof(IdentityAccessModule).Assembly,
    typeof(Nexhire.Modules.IdentityAccess.Contracts.Events.UserRegisteredIntegrationEvent).Assembly, // NEW
    // ... other modules ...
};

// ...

builder.Services.AddIdentityAccessModule(builder.Configuration);

// ...

var app = builder.Build();

// Seed data
await app.Services.SeedIdentityAccessDataAsync();

// ...

app.MapIdentityAccessEndpoints();
```

### 11.2 Public API Wiring

Wire the Contracts interfaces to their implementations:

```csharp
// In Program.cs or IdentityAccessModule.cs
services.AddScoped<IIdentityProvisioningApi>(sp =>
{
    var sender = sp.GetRequiredService<ISender>();
    return new IdentityProvisioningApiAdapter(sender);
});

services.AddScoped<ITokenValidationApi>(sp =>
{
    var sender = sp.GetRequiredService<ISender>();
    return new TokenValidationApiAdapter(sender);
});
```

Create adapter classes in the Host project (or Infrastructure) that translate `IIdentityProvisioningApi.ProvisionCredential` → `sender.Send(new ProvisionCredentialCommand(...))`.

### 11.3 Auth Middleware Integration

The host's authentication middleware should:

1. Extract `accessToken` from the `Authorization: Bearer {token}` header
2. Call `ITokenValidationApi.Validate(accessToken)`
3. On success, build a `ClaimsPrincipal` from the `ValidatedPrincipal` (UserId, Role, Permissions, SessionId)
4. On failure, return `401`

---

## Phase 12 — Data Seeding

**File:** `Infrastructure/Persistence/IdentityAccessSeedData.cs`

```csharp
public static class IdentityAccessSeedData
{
    public static async Task SeedAsync(IdentityAccessDbContext context, IPasswordHasher passwordHasher)
    {
        if (await context.UserAccounts.AnyAsync()) return;

        // Admin
        var adminAccount = CreateAccount(
            "admin@nexhire.gov.bd", "+8801711111111", "Admin@12345!",
            UserRole.MoLAdministrator, passwordHasher);
        adminAccount.Activate(); // Promote past PendingActivation
        context.UserAccounts.Add(adminAccount);

        // Test Employer
        var employerAccount = CreateAccount(
            "employer@nexhire.com", "+8801711111112", "Employer@12345!",
            UserRole.Employer, passwordHasher);
        employerAccount.Activate();
        context.UserAccounts.Add(employerAccount);

        // Test JobSeeker
        var seekerAccount = CreateAccount(
            "seeker@nexhire.com", "+8801711111113", "Seeker@12345!",
            UserRole.JobSeeker, passwordHasher);
        seekerAccount.Activate();
        context.UserAccounts.Add(seekerAccount);

        // Pending Employer (PendingActivation — login will get E-LOGIN-ACCOUNT-NOT-ACTIVATED)
        var pendingAccount = CreateAccount(
            "pending@nexhire.com", "+8801711111114", "Pending@12345!",
            UserRole.Employer, passwordHasher);
        // NOT activated — stays PendingActivation
        context.UserAccounts.Add(pendingAccount);

        // Suspended User
        var suspendedAccount = CreateAccount(
            "suspended@nexhire.com", "+8801711111115", "Suspended@12345!",
            UserRole.JobSeeker, passwordHasher);
        suspendedAccount.Activate();
        suspendedAccount.Suspend("Policy violation — multiple failed login attempts");
        context.UserAccounts.Add(suspendedAccount);

        // Deactivated User
        var deactivatedAccount = CreateAccount(
            "deactivated@nexhire.com", "+8801711111116", "Deactivated@12345!",
            UserRole.JobSeeker, passwordHasher);
        deactivatedAccount.Activate();
        deactivatedAccount.Deactivate();
        context.UserAccounts.Add(deactivatedAccount);

        // Third-Party Portal
        var portalAccount = CreateAccount(
            "portal@external.com", "+8801711111117", "Portal@12345!",
            UserRole.ThirdPartyPortal, passwordHasher);
        portalAccount.Activate();
        context.UserAccounts.Add(portalAccount);

        await context.SaveChangesAsync();
    }

    private static UserAccount CreateAccount(
        string email, string mobile, string rawPassword,
        UserRole role, IPasswordHasher hasher)
    {
        var emailVO = EmailAddress.Create(email).Value;
        var mobileVO = MobileNumber.Create(mobile).Value;
        var rawVO = RawPassword.Create(rawPassword).Value;
        var passwordHash = hasher.Hash(rawVO);
        var permissions = PermissionResolver.Resolve(role, Array.Empty<string>());

        return UserAccount.Provision(emailVO, mobileVO, passwordHash, role, permissions);
    }
}

public static class IdentityAccessSeedExtensions
{
    public static async Task SeedIdentityAccessDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await IdentityAccessSeedData.SeedAsync(context, hasher);
    }
}
```

Called from `Program.cs`:
```csharp
var app = builder.Build();
await app.Services.SeedIdentityAccessDataAsync();
app.Run();
```

---

## Phase 13 — Consumed Integration Events (Inbound)

### 13.1 Inbound Event Handlers ($9.1)

**`IdentityVerifiedByGovernmentIntegrationEvent` handler:**
```csharp
public class IdentityVerifiedByGovernmentEventHandler : INotificationHandler<IdentityVerifiedByGovernmentIntegrationEvent>
{
    public async Task Handle(IdentityVerifiedByGovernmentIntegrationEvent notification, CancellationToken ct)
    {
        // Check inbox idempotency
        // Load UserAccount by UserId
        // Call account.ApplyGovernmentIdentityVerified(notification.Registry)
        // Save
    }
}
```

**`IdentityVerificationFailedIntegrationEvent` handler:**
```csharp
public class IdentityVerificationFailedEventHandler : INotificationHandler<IdentityVerificationFailedIntegrationEvent>
{
    public async Task Handle(...)
    {
        // Record failure for audit (no status change)
    }
}
```

Both handlers check the `inbox_messages` table for idempotency before processing.

### 13.2 Outbox Relay

The shared infrastructure should provide a generic `OutboxRelayBackgroundService` that:
1. Polls `outbox_messages` where `processed_on_utc IS NULL`
2. Deserializes each message
3. Publishes via `IPublisher.Publish()`
4. Marks `processed_on_utc`

> **Note:** If the shared infrastructure doesn't already have this, add it to `Nexhire.Shared.Infrastructure` as a reusable background service that all modules can use.

---

## Phase 14 — Tests: Full Coverage

### 14.1 Domain Unit Tests (Expand Existing)

**Add to `tests/Modules/IdentityAccess/Nexhire.Modules.IdentityAccess.Tests.Unit/`:**

| Test File | Test Count | Coverage |
|---|---|---|
| `UserAccountBehaviourTests.cs` | ~100 | All 21 behaviors, all invariants |
| `OtpChallengeTests.cs` | ~15 | Issue, Verify (4 states), MarkExpired |
| `SessionTests.cs` | ~10 | Create, Revoke, Touch, expiry, inactivity |
| `BackupCodeTests.cs` | ~6 | Create, Redeem (single-use), re-redeem rejected |
| `PasswordResetTokenTests.cs` | ~8 | Create, IsExpired, IsUsed, MarkUsed |
| `AccountStateMachineTests.cs` | ~16 | Exhaustive 4x4 transition matrix |
| `PasswordPolicyServiceTests.cs` | ~6 | Table-driven valid/invalid cases |
| `PermissionResolverTests.cs` | ~6 | Each role baseline, union with grants |
| `TokenClaimsBuilderTests.cs` | ~4 | Claims structure, no secrets |
| `AdminActionLogTests.cs` | ~4 | Record creates correct entry, no mutators |

### 14.2 Application Unit Tests (New)

**Add to test project:**

| Test File | Test Count | Key Scenarios |
|---|---|---|
| `ProvisionCredentialCommandHandlerTests.cs` | ~8 | Happy path, duplicate email, breach, rate-limit, invalid input |
| `LoginWithCredentialsCommandHandlerTests.cs` | ~10 | Wrong password, 4 status codes, lock, rate-limit, MFA, "remember me" |
| `ActivateAccountCommandHandlerTests.cs` | ~4 | Valid OTP, expired, locked, failure |
| `VerifyMfaChallengeCommandHandlerTests.cs` | ~6 | TOTP, SMS-OTP, backup-code, re-use, 3-failures→lock |
| `CompletePasswordResetCommandHandlerTests.cs` | ~6 | Valid, expired token, breach, reuse, no-enumeration |
| `RefreshAccessTokenCommandHandlerTests.cs` | ~4 | Valid, revoked, expired, unknown |
| `AdminSuspendUserCommandHandlerTests.cs` | ~4 | Happy + log written, empty reason rejected, not-active |

### 14.3 Integration Tests (Populate Existing Empty Project)

**Add to `tests/Modules/IdentityAccess/Nexhire.Modules.IdentityAccess.Tests.Integration/`:**

| Test File | What It Tests |
|---|---|
| `UserAccountRepositoryTests.cs` | Round-trip with owned collections (Session, BackupCode, TrustedDevice, PasswordResetToken); json VOs; EmailExists, MobileExists; concurrency conflict |
| `OtpChallengeRepositoryTests.cs` | CRUD, GetActiveByAccountAndPurpose query |
| `UniqueIndexTests.cs` | Duplicate email/mobile rejected at DB level |
| `OutboxTests.cs` | Provisioning writes both `user_accounts` row + `UserRegistered` outbox message in one transaction |
| `PasswordHasherTests.cs` | argon2id: Hash + Verify success, wrong password fails |
| `JwtSignerTests.cs` | RS256: Sign + Validate, expired token rejected, tampered token rejected |
| `ApiLifecycleTests.cs` | Register→activate→login→refresh→logout-all happy path; wrong password returns 401 generic; suspended account blocked; admin routes 403 without permission |
| `PasswordResetFlowTests.cs` | Request OTP → verify OTP → complete reset → old password fails, new password works |
| `MfaFlowTests.cs` | Enroll→confirm→backup codes issued; login challenges MFA; TOTP/backup code works; disable MFA |

### 14.4 Acceptance Criteria Coverage ($14.2)

Every AC in the §14.2 table must map to at least one test:

| Story | ACs | Test Coverage |
|---|---|---|
| US-3.1.4-01 | 11 ACs | Admin command handler tests + integration admin API tests |
| US-3.1.5-01 | 9 ACs | Login handler tests + API lifecycle integration |
| US-3.1.5-02 | 8 ACs | MFA handler tests + MFA API integration |
| US-3.1.5-03 | 8 ACs | Password reset handler tests + reset flow integration |
| US-3.1.5-04 | 8 ACs | Session/refresh/logout handler tests + API middleware tests |
| US-3.4.3-04 | 5 ACs | OAuth token handler tests + token validation integration |

---

## Implementation Order (Build Sequence)

```
Phase 0  — Purge incompatible files
Phase 1  — Enums + VOs (Domain)
Phase 2  — Domain Services (Domain)
Phase 3  — Aggregates + Events (Domain)
Phase 4  — Port Interfaces (Application)
Phase 5  — Commands + Handlers (Application)
Phase 6  — Queries + DTOs (Application)
Phase 7  — Validators (Application)
Phase 8  — EF Config + Repos + Adapters + Bg Services (Infrastructure)
Phase 9  — Contracts Layer (new project)
Phase 10 — API Endpoints (Presentation)
Phase 11 — Host Integration
Phase 12 — Data Seeding
Phase 13 — Consumed Events + Outbox Relay
Phase 14 — Tests (full coverage)
```

Each phase produces compilable code with passing tests before proceeding to the next.

---

## Appendix: Design Issue — PasswordHistory + Argon2id

**Problem:** Argon2id uses a random salt per hash, so `PasswordHash("P@ssw0rd1")` produces a different output each time. Comparing `PasswordHash.Value` strings for the no-reuse check (invariant #4) will never match, even for identical passwords.

**Solution in the Domain:** Store a **deterministic verifier** (SHA-256 of the normalized raw password) in `_passwordHistory` instead of the argon2id hash:

```csharp
// In UserAccount aggregate
private readonly List<string> _passwordHistory = new(); // SHA-256 verifiers

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
        _passwordHistory.RemoveAt(0); // Keep last 3
}

private static string ComputeSha256(string value)
    => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
```

> **Rationale:** The SHA-256 verifier is **only** used for the no-reuse check. It is derived from the *raw* password (before salting/hashing), so it is deterministic. The verifier is not usable as a password equivalent because the actual login verification goes through argon2id (which is salted). This is the pragmatic trade-off: a small, acceptable exposure (SHA-256 verifier exists) in exchange for enforcing the no-reuse invariant. In a production audit context, these verifiers should be documented as a conscious design decision.
