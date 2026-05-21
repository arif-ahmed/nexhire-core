---
title: "Handover Package — BC-1 IAM and UAM"
type: handover-package
bc_id: BC-1
bc_name: IAM and UAM
bc_class: generic
stack: "language-neutral — see [[00-Shared-Foundations]] (Target Stack declared there)"
generated: 2026-05-15
related:
  - "[[00-Shared-Foundations]]"
  - "[[BC_Mapping]]"
  - "[[Context_Map]]"
  - "[[Event_Catalog]]"
tags:
  - handover-package
  - bc/identity
  - bc/iam-uam
---

# Handover Package — BC-1 IAM and UAM

> **Audience:** an AI coding agent. This package owns the **domain design** for the `IdentityAccess` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `IdentityAccess`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `identity_access`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

**This is the platform's keystone BC.** Every other BC depends on it for authentication and depends on its identity lifecycle events. It is an **Open Host Service** (OHS) with a **Published Language** (PL): a stable, versioned set of integration events plus two synchronous public contracts (`IdentityProvisioningApi`, `TokenValidationApi`). Treat the public surface as a contract that must not break — see the appendix.

---

## 1. Purpose & scope boundaries

### What this BC is for

IAM and UAM owns **who a user is and what they are allowed to do**: the credential (email/mobile + password hash), the account lifecycle state machine, one-time passwords for activation and reset, access-token issuance and refresh, session tracking, multi-factor authentication, roles and permissions, and the administrator-facing user-account management surface (suspend/ban, reinstate, deactivate, unlock, password-reset assistance). It is a **generic** subdomain — identity is a solved, non-differentiating problem — but it is **load-bearing**: every domain BC authenticates through it and reacts to its lifecycle events.

### In scope

The `IdentityAccess` module is responsible for:

- The **credential**: email and mobile (each unique), password **policy enforcement**, **breach check** (a k-anonymity range check against an external corpus, via a port), **password hashing** (`argon2id`) and verification.
- **Identity provisioning** — the synchronous `IdentityProvisioningApi.ProvisionCredential` entry point called by BC-2 Employer Profile and BC-3 JobSeeker Profile during their registration journeys. Enforces uniqueness, policy, breach check, hashing; creates the `UserAccount` in `PendingActivation`; triggers OTP send; returns the new `UserId`.
- **OTP** generation, delivery orchestration (via a port), validation, expiry (5 min), attempt limits (max 5 for activation; max 3 for MFA / password-reset), and account-lock-after-failed-OTP.
- **Account activation** via OTP — `PendingActivation → Active`.
- **Login** with credentials (`US-3.1.5-01`) — auto-detect email vs. mobile identifier, verify hash (constant-time), enforce account state with **distinct error codes per state**, count failed attempts, **rate-limit per IP** (10 attempts / 1 min → 5-min throttle, prevents enumeration), issue an access token + refresh token and create a `Session` on success, optionally extend the session to 14 days on "remember me".
- **Multi-factor authentication** (`US-3.1.5-02`) — optional per user; enrol a second factor (`Totp` via RFC 6238, or `SmsOtp`) with a verify-before-activate confirmation step; challenge MFA on **every** login before issuing tokens; **8–10 single-use backup codes** issued at setup for recovery; lock the account for 15 min after **3 failed MFA codes within 5 min**; disable MFA requires re-auth.
- **Access-token issuance** (`RS256`, access-token TTL 1 h, refresh-token TTL 30 d — see `US-3.4.3-04`), **refresh**, and **revocation** (a revocation list).
- **Session** records — one per successful login, tracking channel, device, issued/last-seen/expiry; explicit logout and logout-everywhere; **inactivity timeout** (configurable, default 1 h); concurrent sessions allowed.
- **Roles & permissions** — `RoleAssigned`, permission checks, the `TokenValidationApi` used by the host's auth middleware; **RBAC** enforced at the API surface with `E-UNAUTHORIZED-ROLE` / `E-FORBIDDEN` (`US-3.1.5-04`).
- **Administrator user-account management** (`US-3.1.4-01`) — searchable/filterable user list, view-as-admin, approve/reject pending employer, **suspend/ban** (policy-violation lock), **reinstate**, **deactivate** (soft delete), **unlock**, admin-initiated **password reset**, and the **admin action audit trail**.
- **Account recovery** — self-service password reset (`US-3.1.5-03`: request → SMS OTP → verify → set new password), admin-assisted reset/unlock; reinstating a deactivated account on next login + OTP.
- Publishing the integration events in §8 and reacting to the integration events in §9.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong to other BCs and are reached via the contracts in §9, or are simply not this BC's concern:

- **The job-seeker candidate profile** (name-as-candidate, education, experience, skills, resume) → BC-3 JobSeeker Profile. This module holds only the *credential* and *account*, not the profile. It returns the new `UserId`; BC-3 builds the profile around it.
- **The employer company profile** (company name, logo, documents, verification badge) → BC-2 Employer Profile Management. This module provisions the credential; BC-2 owns the company.
- **Government-registry verification of identity or employer** → BC-8 External Job Synchronization. This module only *reacts* to `IdentityVerifiedByGovernment` / `IdentityVerificationFailed` and sets a flag on the user record. It does not call any government API.
- **Sending the actual email / SMS** → BC-9 Notification. This module emits events (`UserRegistered`, `PasswordReset`, etc.) and, for OTP/activation-critical messages, calls a thin `OtpDeliveryPort` adapter; it does not own templates, channels, or delivery tracking.
- **OTP delivery channel internals** (SMS gateway, email transport) → external, reached via `OtpDeliveryPort`.
- **Job postings, applications, search, recommendations, reporting** → respective BCs. This module knows nothing about the job-board domain.
- **Per-BC authorization policy** — this module issues role/permission claims in the access token and validates tokens; each consuming BC decides which of *its* routes need which permission. This module does not own those route-to-permission maps.
- **Platform-wide access/activity logging** — `US-3.1.5-04` calls for per-request access logs (90-day retention) and `US-3.1.5-04`/`US-3.1.4-01` for sensitive-operation audit. This module logs *its own* admin actions (the `admin_action_log`). Cross-cutting per-request access logging and platform activity reporting → BC-10 Reporting (it consumes the events).
- **Rate-limiting infrastructure** — this module *decides* `E-REG-RATE-LIMITED` / `E-LOGIN-RATE-LIMITED` / `E-RESET-RATE-LIMITED` against a counter port; the distributed counter store is infrastructure, behind `RateLimiterPort`.
- **OAuth client registration / consent UI** — `US-3.4.3-04` assumes third-party clients exist; this module issues and validates tokens for them but does not build the client-registration admin surface (a future BC-11 / BC-12 concern).

### Boundary note — provisioning is a synchronous OHS call (teaching point)

The [[Event_Catalog]] lists `UserRegistered` as consumed by BC-2 and BC-3. This package **deliberately models the primary registration path as a synchronous call** — BC-2/BC-3 call `IdentityProvisioningApi.ProvisionCredential` and need the `UserId` back **in the same unit of work** to build their own aggregate. `UserRegistered` is *still* published (to BC-9, BC-10, BC-12 who only need to react, not to get a return value). So both patterns coexist: a sync OHS call for request/response needs, plus a fire-and-forget integration event for pure reactors. This is a good class discussion on **sync OHS call vs. pure event choreography** — see appendix.

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **User Account** | The `UserAccount` aggregate — the root of this BC. One per person or third-party portal. Carries the credential, state, MFA config, and role assignments. |
| **User Id** | `uuid` primary identity. This is the value every other BC stores as a plain `uuid` to refer to a user. Immutable. |
| **Credential** | Email + mobile + password hash. A value object owned by the account. |
| **Account Status** | `PendingActivation`, `Active`, `Suspended`, `Deactivated`. The lifecycle state machine. |
| **Role** | `JobSeeker`, `Employer`, `ThirdPartyPortal`, `MoLAdministrator`. One primary role per account (assigned at provisioning); admins may be additionally granted. (Story `US-3.1.5-04` also names finer employer sub-roles — Owner/Recruiter/Admin; those are modelled as permission grants, not separate top-level roles — see appendix.) |
| **Permission** | A fine-grained capability string (e.g. `users:manage`, `jobs:moderate`). Derived from role; carried as access-token claims. |
| **OTP** | One-time passcode — 6 digits, 5-minute TTL. Max 5 attempts for `Activation`; max 3 for `Mfa` and `PasswordReset`. Used for activation, password reset, and (optionally) as an MFA second factor. |
| **OTP Challenge** | A pending OTP for a given account and purpose (`Activation`, `Mfa`, `PasswordReset`). |
| **MFA** | Multi-factor authentication. Optional per user. A second factor enrolled on the account: `Totp` (authenticator app, RFC 6238) or `SmsOtp`. Challenged on every login. |
| **Backup Code** | One of 8–10 single-use recovery codes issued at MFA setup; usable in place of the second factor when the MFA device is unavailable. Stored hashed. |
| **Session** | A `Session` entity created on successful login. Tracks channel, device, issued/expiry/last-seen, and revoked flag. Has a configurable inactivity timeout (default 1 h). |
| **Access Token** | A short-lived (1 h) `RS256` token carrying `sub` (UserId), `role`, `permissions`/`scopes`, `sessionId`, `exp`. |
| **Refresh Token** | A long-lived (30 d) opaque token that mints a new access token. One-time-use; rotated on each refresh. |
| **Revocation List** | Set of revoked token identifiers / refresh tokens; checked on every validation. |
| **Password Policy** | Min length 10, must mix character classes, not a known-breached password, and not one of the account's previous 3 passwords. Enforced at provisioning, password-change, and password-reset. |
| **Breach Check** | A k-anonymity range query against an external breach corpus; a password whose hash-prefix matches is rejected (`E-REG-PASSWORD-BREACHED` / `E-RESET-PASSWORD-BREACHED`). |
| **Admin Action** | An entry in the `admin_action_log` — admin id, action type, target user, reason, timestamp. Append-only. |
| **Suspend / Ban** | Admin locks an account for a policy violation. `Active → Suspended`. Not self-reversible — only an admin `Reinstate` recovers it. |
| **Deactivate** | A soft delete. `Active → Deactivated`. Reversible by the user on next login + OTP, or by an admin. Data retained 12 months. |
| **Reinstate** | Admin lifts a suspension. `Suspended → Active`. |
| **Lock** | A transient block after too many failed login/OTP/MFA attempts. Distinct from `Suspended` (an account state) — lock is a counter-driven flag with an auto-expiry (15 min) or admin `Unlock`. |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `IdentityAccess`.
- Public surface (`Contracts`): the integration events published in §8 + the two public APIs in §9.3 (`IdentityProvisioningApi`, `TokenValidationApi`). This is the only layer other modules may reference.
- **Module-specific notes — background workers / scheduled jobs:** this module runs (a) an **OTP-expiry sweep** that moves stale `Issued` `OtpChallenge`s to `Expired`; (b) a **session-expiry sweep** that marks sessions past their inactivity/absolute expiry; (c) a **revoked-token / expired-OTP cleanup** job that deletes rows from `revoked_tokens` and `otp_challenges` once past their expiry, keeping those high-churn tables small. Each is a scheduled job in the `Infrastructure` layer.

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has **two aggregates**: `UserAccount` (the root of the BC) and `OtpChallenge`. Sessions, MFA factors, backup codes, trusted devices, role assignments and password-reset tokens are child entities/VOs of `UserAccount`. `AdminActionLog` is a thin append-only log persisted directly (not an aggregate with behaviour). (Notation: see [[00-Shared-Foundations]] §2.)

### 5.1 Aggregate: UserAccount

**Aggregate root.** Identity: `UserAccountId` (strongly-typed id wrapping `uuid` — this `uuid` value *is* the platform-wide `UserId`).

| Member | Type | Notes |
|---|---|---|
| `Id` | `UserAccountId` | The platform `UserId`. Immutable. |
| `Credential` | `Credential` | VO — email, mobile, password hash |
| `Role` | `UserRole` | enum: `JobSeeker`, `Employer`, `ThirdPartyPortal`, `MoLAdministrator` |
| `Status` | `AccountStatus` | enum: `PendingActivation`, `Active`, `Suspended`, `Deactivated` |
| `LockState` | `LockState` | VO — `IsLocked`, `LockedUntilUtc?`, `FailedLoginCount`, `FailedOtpCount` |
| `Mfa` | `MfaConfiguration` | VO — `Enabled`, `Method` (`Totp`/`SmsOtp`/`None`), `SecretRef?` |
| `BackupCodes` | `list<BackupCode>` | child entities — 8–10, each single-use, hashed |
| `TrustedDevices` | `list<TrustedDevice>` | child entities |
| `Sessions` | `list<Session>` | child entities |
| `PasswordHistory` | `list<string>` | last 3 password hashes — checked on change/reset to prevent reuse |
| `Permissions` | `list<string>` | derived from `Role` + admin grants; recomputed on `AssignRole` |
| `IdentityVerified` | `bool` | set from BC-8 `IdentityVerifiedByGovernment`; default false |
| `ActivatedOnUtc` | `datetime?` | |
| `SuspendedReason` | `string?` | set when `Status == Suspended` |
| `DeactivatedOnUtc` | `datetime?` | soft-delete marker; drives 12-month retention |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

**Child entities** (identity local to the aggregate; only mutated through the root):

- `Session` — `SessionId`, `Channel` (`Web`/`Mobile`/`Api`), `DeviceFingerprint` (`string`), `RefreshTokenHash` (`string`), `IssuedOnUtc`, `LastSeenUtc`, `ExpiresOnUtc`, `RevokedOnUtc?`. `LastSeenUtc` drives the inactivity timeout; `ExpiresOnUtc` is extended to 14 days when "remember me" is set.
- `TrustedDevice` — `TrustedDeviceId`, `DeviceFingerprint`, `Label`, `TrustedUntilUtc`.
- `BackupCode` — `BackupCodeId`, `CodeHash` (`string`), `UsedOnUtc?` (`datetime?`). Single-use: a code with `UsedOnUtc` set cannot be redeemed again.
- `PasswordResetToken` — `PasswordResetTokenId`, `TokenHash` (`string`), `IssuedOnUtc`, `ExpiresOnUtc`, `UsedOnUtc?`. (Modelled as a child entity; at most one *unused, unexpired* token per account.)

### 5.2 Aggregate: OtpChallenge

**Aggregate root.** Identity: `OtpChallengeId`. Kept separate from `UserAccount` because it has its own short lifecycle (`Issued → Verified | Expired | Locked`), a high churn rate, and is created during provisioning *before* the account is something the user can act on.

| Member | Type | Notes |
|---|---|---|
| `Id` | `OtpChallengeId` | |
| `UserAccountId` | `UserAccountId` | owning account |
| `Purpose` | `OtpPurpose` | enum: `Activation`, `Mfa`, `PasswordReset` |
| `CodeHash` | `string` | hashed 6-digit code (never store plaintext) |
| `Status` | `OtpStatus` | enum: `Issued`, `Verified`, `Expired`, `Locked` |
| `AttemptCount` | `int` | incremented on each wrong submission |
| `MaxAttempts` | `int` | 5 for `Activation`; 3 for `Mfa` / `PasswordReset` |
| `IssuedOnUtc` | `datetime` | |
| `ExpiresOnUtc` | `datetime` | `IssuedOnUtc + 5 min` |
| `VerifiedOnUtc` | `datetime?` | |

### 5.3 AdminActionLog (append-only log, not a behavioural aggregate)

| Member | Type | Notes |
|---|---|---|
| `Id` | `uuid` | |
| `AdminUserId` | `uuid` | who acted |
| `ActionType` | `AdminActionType` | enum: `ApprovedEmployer`, `RejectedEmployer`, `Suspended`, `Reinstated`, `Deactivated`, `Unlocked`, `PasswordResetIssued`, `RoleAssigned`, `Viewed` |
| `TargetUserId` | `uuid` | |
| `Reason` | `string?` | required for suspend/reject/deactivate |
| `OccurredOnUtc` | `datetime` | |

### 5.4 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `EmailAddress` | `Value` | RFC 5322; lower-cased on store |
| `MobileNumber` | `Value` | E.164; default region `+880` (Bangladesh) — region configurable |
| `PasswordHash` | `Algorithm` (`"argon2id"`), `Value` | non-empty; opaque; produced only by the `PasswordHasher` port |
| `Credential` | `Email` (`EmailAddress`), `Mobile` (`MobileNumber`), `PasswordHash` | all three present |
| `LockState` | `IsLocked`, `LockedUntilUtc?`, `FailedLoginCount`, `FailedOtpCount` | counts ≥ 0; `IsLocked ⇒ LockedUntilUtc` set or admin-cleared |
| `MfaConfiguration` | `Enabled`, `Method`, `SecretRef?` | `Enabled ⇒ Method != None && SecretRef != null` |
| `RawPassword` | `Value` | min length 10; ≥3 character classes; transient, never persisted — used only to feed the hasher and breach check |
| `AccessTokenSpec` | `Subject`, `Role`, `Permissions`, `Scopes`, `SessionId`, `ExpiresOnUtc` | TTL ≤ 1 h |
| `DeviceFingerprint` | `Value` | non-empty hash string |

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior lives on the aggregate roots. Handlers never set properties directly. Method shapes below are neutral specifications (see [[00-Shared-Foundations]] §2).

### 6.1 UserAccount — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Provision(email, mobile, passwordHash, role)` | Creates account in `PendingActivation`. Email & mobile valid. Permissions derived from `role`. Uniqueness is checked by the *handler* against the repository before calling this (DB unique index is the backstop). | `UserRegistered` *(integration)* |
| `Activate()` | Only from `PendingActivation`. → `Active`. Sets `ActivatedOnUtc`. Idempotent if already `Active`. Fails from `Suspended`/`Deactivated`. | `UserAccountActivated` *(integration)* |
| `RecordSuccessfulLogin(channel, deviceFingerprint, refreshTokenHash, expiresOnUtc)` | Only from `Active`. Creates a `Session` (`expiresOnUtc` carries the default-or-"remember me" TTL). Resets `LockState.FailedLoginCount = 0`. | `UserLoggedIn` *(integration)* |
| `RecordFailedLogin(identifier, reason)` | Increments `FailedLoginCount`. Per-IP throttling (10/min) is the *handler's* job via `RateLimiterPort`; the account counter is for audit/diagnostics, not the primary lockout. | `UserLoginFailed` *(integration)* |
| `EnableMfa(method, secretRef, backupCodeHashes)` | Only from `Active`, and only after a verify-before-activate confirmation step (handler-driven). Sets `MfaConfiguration`; stores 8–10 `BackupCode`s. | `MfaEnabled` *(internal)* |
| `DisableMfa()` | Clears MFA config and backup codes. Requires the caller to have re-authenticated (handler concern). | `MfaDisabled` *(internal)* |
| `RedeemBackupCode(codeHash)` | code must exist and be unused; marks it `UsedOnUtc`. Allows login to proceed in place of the second factor. | `BackupCodeRedeemed` *(internal)* |
| `TrustDevice(fingerprint, label, until)` | adds a `TrustedDevice`; replaces any with the same fingerprint | — |
| `RevokeSession(sessionId)` | marks the `Session.RevokedOnUtc`; that session's refresh token is dead | `SessionRevoked` *(internal)* |
| `RevokeAllSessions()` | revokes every active session (logout-everywhere; also used on password change) | `SessionRevoked` per session *(internal)* |
| `TouchSession(sessionId, nowUtc)` | updates `LastSeenUtc`; if `nowUtc - LastSeenUtc` exceeds the inactivity window, instead revokes the session | `SessionRevoked` *(internal, on timeout)* |
| `IssuePasswordResetToken(tokenHash, expiresOnUtc)` | invalidates any prior unused token; adds a new `PasswordResetToken` | — |
| `CompletePasswordReset(tokenHash, newPasswordHash)` | token must exist, be unused, unexpired, and match; new hash must not equal any of the last 3 in `PasswordHistory`; sets new hash; pushes the old hash into `PasswordHistory` (keep last 3); marks token used; calls `RevokeAllSessions()`; clears `LockState` | `PasswordReset` *(integration)* |
| `ChangePassword(newPasswordHash)` | self-service while authenticated; new hash must not equal any of the last 3; sets new hash; updates `PasswordHistory`; `RevokeAllSessions()` | `PasswordReset` *(integration)* |
| `AssignRole(role, grantedByAdminId)` | recomputes `Permissions`; only an `MoLAdministrator` may be the grantor (handler-checked) | `RoleAssigned` *(integration)* |
| `Suspend(reason, byAdminId)` | Only from `Active` (or `PendingActivation` → employer-reject path). → `Suspended`. `reason` required. Sets `SuspendedReason`. Calls `RevokeAllSessions()`. | `UserAccountSuspended` *(integration)* |
| `Reinstate(byAdminId)` | Only from `Suspended`. → `Active`. Clears `SuspendedReason`. | `UserAccountReinstated` *(integration)* |
| `Deactivate()` | Only from `Active`. → `Deactivated`. Sets `DeactivatedOnUtc`. Calls `RevokeAllSessions()`. Used both by self-deactivation and admin soft-delete. | `AccountDeactivated` *(integration)* |
| `ReactivateAfterDeactivation()` | Only from `Deactivated` **and** within the 12-month retention window. → `PendingActivation` (a fresh OTP is then required) or `Active` if an admin reactivates directly. Idempotent. | `UserAccountReinstated` *(integration)* |
| `Lock(until)` / `Unlock()` | sets / clears `LockState.IsLocked`. `Unlock()` also resets `FailedLoginCount` and `FailedOtpCount`. | `AccountUnlocked` *(internal)* on `Unlock` |
| `ApplyGovernmentIdentityVerified(registry)` | sets `IdentityVerified = true` | `IdentityVerificationApplied` *(internal)* |
| `RecordOtpFailure()` | increments `FailedOtpCount`; at the per-purpose threshold, `Lock(now + 15 min)` | — |

### 6.2 Core invariants (must always hold)

1. **Status transitions** form a fixed machine: `PendingActivation → Active`; `PendingActivation → Suspended` (employer-reject path only); `Active ↔ Suspended` (via `Suspend`/`Reinstate`); `Active → Deactivated`; `Deactivated → PendingActivation | Active` (via `ReactivateAfterDeactivation`). No other transition is legal. In particular `Suspended → Deactivated` and `Deactivated → Suspended` are forbidden.
2. **Email uniqueness** and **mobile uniqueness** are absolute across the whole table — enforced by a DB unique index *and* a pre-check in the provisioning handler (`E-REG-DUPLICATE`).
3. **A password is never stored in plaintext.** `Credential.PasswordHash` is the only persisted form; it is produced exclusively by the `PasswordHasher` port (`argon2id`).
4. **A password must pass policy, breach check, and the no-reuse rule** before a `PasswordHash` is created — at provisioning, password change, and password reset. Policy = min length 10 + ≥3 character classes; no-reuse = not equal to any of the last 3 hashes in `PasswordHistory`. A breached password → `E-REG-PASSWORD-BREACHED` (or `E-RESET-PASSWORD-BREACHED` on the reset path).
5. **OTP**: 6 digits, 5-minute TTL. Max **5** attempts for `Activation`, max **3** for `Mfa` and `PasswordReset`. An attempt past the limit, or an expired challenge, moves it to `Locked`/`Expired`; the *account* `LockState` is updated (15-minute lock).
6. **One active (unused, unexpired) password-reset token** per account at a time — `IssuePasswordResetToken` invalidates any prior one.
7. **Suspended and Deactivated accounts cannot log in.** `RecordSuccessfulLogin` is only legal from `Active`. Login against each non-`Active` state returns its own distinct error code (`E-LOGIN-ACCOUNT-NOT-ACTIVATED`, `E-LOGIN-ACCOUNT-DEACTIVATED`, `E-LOGIN-ACCOUNT-BANNED`).
8. **Suspending, deactivating, changing or resetting a password all revoke every active session.** A revoked session's refresh token must fail validation immediately.
9. **`UserId` (`UserAccountId` value)** is immutable for the life of the account.
10. **Permissions are always a pure function of `Role` (+ explicit admin grants).** Never set independently.
11. **Suspend / Reinstate / Deactivate / Unlock / admin password-reset / role-assign by an admin must each write an `AdminActionLog` row** in the same transaction — see §6.4. `reason` is mandatory for suspend, employer-reject, and deactivate.
12. **Access tokens are `RS256`, TTL ≤ 1 h**; refresh tokens are one-time-use and rotated on every refresh.
13. **MFA, when enabled, is challenged on every login** — there is no "remember this device" skip for the MFA factor in the initial release (trusted-device tracking exists for diagnostics but does not bypass the MFA challenge). **Backup codes are single-use.**
14. **A `Session` expires on inactivity** once `nowUtc - LastSeenUtc` exceeds the configured window (default 1 h); "remember me" sets an extended absolute expiry (14 days) instead.

### 6.3 OtpChallenge — behaviors

| Method | Rules | Domain event raised |
|---|---|---|
| `static Issue(userAccountId, purpose, codeHash, ttl, maxAttempts)` | creates challenge `Issued`, `ExpiresOnUtc = now + ttl` (default 5 min); `maxAttempts` = 5 for `Activation`, 3 otherwise | `OtpIssued` *(internal — handler then calls `OtpDeliveryPort`)* |
| `Verify(submittedCodeHash, nowUtc)` | if `nowUtc > ExpiresOnUtc` → `Expired`, fail with `E-OTP-EXPIRED`. If hash mismatches → increment `AttemptCount`; at `MaxAttempts` → `Locked`, fail with `E-OTP-LOCKED`. If match → `Verified`. Only from `Issued`. | `OtpVerified` *(internal)* |
| `MarkExpired()` | the OTP-expiry sweep job moves stale `Issued` challenges to `Expired` | — |

**OtpChallenge invariants:** strictly ordered status machine; `CodeHash` is a hash, never the plaintext code; `VerifiedOnUtc` non-null iff `Status == Verified`; a `Verified` or `Locked` challenge can never be re-verified; `AttemptCount ≤ MaxAttempts`.

### 6.4 AdminActionLog — behavior

`AdminActionLog` is append-only. There is one factory `AdminActionLog.Record(adminUserId, actionType, targetUserId, reason)` and **no mutators**. The application handler for every admin command appends one row in the same unit of work as the `UserAccount` change. Never updated or deleted.

---

## 7. Domain services

Stateless. Live in `Domain` (the ones that are pure) or are thin wrappers over ports. Used when logic spans entities or needs a small external input.

### 7.1 `PasswordPolicyService`

```
Validate(candidate: RawPassword) -> Result
```

Pure. Enforces: min length 10, at least 3 of {lowercase, uppercase, digit, symbol}, not composed of trivial sequences. Returns `Error("E-REG-INVALID-PASSWORD", ...)` (or `E-RESET-INVALID-PASSWORD` on the reset path) on failure. Breach check and the no-reuse check are *separate* — breach needs a port (§9.2 `BreachCheckPort`); no-reuse is enforced by the `UserAccount` behaviors against `PasswordHistory`.

### 7.2 `PermissionResolver`

```
Resolve(role: UserRole, explicitGrants: list<string>) -> list<string>
```

Pure. Maps a role to its baseline permission set and unions explicit admin grants. The role → permission table:

| Role | Baseline permissions |
|---|---|
| `JobSeeker` | `profile:self`, `applications:self`, `search:read` |
| `Employer` | `employer:self`, `jobs:write`, `applications:read`, `candidates:read` |
| `ThirdPartyPortal` | `integrations:read`, `jobs:write` (scoped) |
| `MoLAdministrator` | `users:manage`, `jobs:moderate`, `taxonomy:manage`, `reports:read`, plus all of the above |

Employer sub-roles (Owner/Recruiter/Admin from `US-3.1.5-04`) are expressed as additional explicit grants on top of the `Employer` baseline rather than separate top-level roles.

### 7.3 `TokenClaimsBuilder`

```
BuildAccessToken(account: UserAccount, sessionId: SessionId, scopes: list<string>, ttl: duration) -> AccessTokenSpec
```

Pure. Assembles the claim set (`sub`, `role`, `permissions`, `scopes`, `sessionId`, `exp`) from the account. The actual **signing** is done by the `JwtSigner` port (§9.2) in `Infrastructure` — this service only builds the *spec*, keeping the Domain crypto-ignorant.

### 7.4 `AccountStateMachine`

```
EnsureTransitionAllowed(from: AccountStatus, to: AccountStatus) -> Result
```

Pure. Centralises invariant #1. The `UserAccount` behaviors call it before mutating `Status`, so the legal-transition table lives in exactly one place and is trivially unit-testable.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module. **Integration events** (`IntegrationEvent`) cross the BC boundary — they go through the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract exactly. Other modules depend on these payloads — do not change a field without versioning.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

| Integration event | Raised when | Payload | Primary consumers |
|---|---|---|---|
| `UserRegisteredIntegrationEvent` | `Provision` succeeds | `UserId`, `Role` (`string`), `Email`, `CreatedAt`, `OccurredOnUtc` | BC-2, BC-3, BC-9, BC-10, BC-12 |
| `UserAccountActivatedIntegrationEvent` | `Activate` succeeds | `UserId`, `ActivatedAt`, `OccurredOnUtc` | BC-2, BC-3, BC-9, BC-10 |
| `UserAccountSuspendedIntegrationEvent` | `Suspend` succeeds | `UserId`, `Reason`, `By` (admin `UserId`), `At`, `OccurredOnUtc` | BC-2, BC-3, BC-4, BC-5, BC-9, BC-10 |
| `UserAccountReinstatedIntegrationEvent` | `Reinstate` or `ReactivateAfterDeactivation` succeeds | `UserId`, `By` (admin `UserId`, nullable for self), `At`, `OccurredOnUtc` | BC-2, BC-3, BC-9, BC-10 |
| `AccountDeactivatedIntegrationEvent` | `Deactivate` succeeds | `UserId`, `DeactivatedAt`, `OccurredOnUtc` | BC-2, BC-3, BC-4, BC-5, BC-7, BC-9, BC-10 |
| `UserLoggedInIntegrationEvent` | `RecordSuccessfulLogin` | `UserId`, `SessionId`, `Channel` (`string`), `At`, `OccurredOnUtc` | BC-10 |
| `UserLoginFailedIntegrationEvent` | `RecordFailedLogin` | `Identifier` (email or mobile, **not** a `UserId` — login may fail before identification), `Reason`, `At`, `OccurredOnUtc` | BC-10 |
| `PasswordResetIntegrationEvent` | `CompletePasswordReset` or `ChangePassword` succeeds | `UserId`, `At`, `OccurredOnUtc` | BC-9, BC-10 |
| `RoleAssignedIntegrationEvent` | `AssignRole` succeeds | `UserId`, `Role` (`string`), `By` (admin `UserId`), `At`, `OccurredOnUtc` | BC-10 |

Consumers (for context only — you do not code them): BC-2/BC-3 create their profile shells from `UserRegistered` (for the reactor path) and react to `UserAccountActivated`/`Suspended`/`Reinstated`/`AccountDeactivated` for lifecycle sync; BC-4 auto-closes postings on employer `AccountDeactivated`/`UserAccountSuspended`; BC-5 withdraws open applications; BC-9 sends welcome/activation/reset SMS/email; BC-10 consumes everything for audit and the user-activity dashboard.

### 8.2 Internal domain events (NOT published outside the module)

`MfaEnabled`, `MfaDisabled`, `BackupCodeRedeemed`, `SessionRevoked`, `AccountUnlocked`, `IdentityVerificationApplied`, `OtpIssued`, `OtpVerified`. Use these for in-module reactions (e.g. `OtpIssued` → the handler calls `OtpDeliveryPort`; `SessionRevoked` → ensure the refresh token hash is dropped). They never reach the outbox.

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained for its domain. (Outbox/inbox mechanics: [[00-Shared-Foundations]] §6.2–6.3.)

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent handlers)

| Integration event | From | Payload you receive | Your reaction |
|---|---|---|---|
| `IdentityVerifiedByGovernmentIntegrationEvent` | BC-8 External Job Sync | `UserId`, `Registry`, `At` | Find account by `UserId`; call `ApplyGovernmentIdentityVerified(registry)`. Idempotent. |
| `IdentityVerificationFailedIntegrationEvent` | BC-8 | `UserId`, `Registry`, `Reason`, `At` | Record the failure for audit (an internal `IdentityVerificationFailed` log row). Do **not** change account status — verification failure is informational, not a lock. |

**Idempotency** is mandatory — see [[00-Shared-Foundations]] §6.3 (inbox). Inbound events arrive via the module's integration-event subscription wired in the module composition entry point.

### 9.2 Ports this module CALLS (port interfaces — define in `Application`, implement adapters in `Infrastructure`)

```
Port: PasswordHasher            (argon2id hashing — Domain stays crypto-ignorant)
  Hash(raw: RawPassword)                       -> PasswordHash      // argon2id, sensible params
  Verify(raw: RawPassword, stored: PasswordHash) -> bool            // constant-time

Port: BreachCheckPort           (k-anonymity range query against an external breach corpus)
  IsBreached(raw: RawPassword) -> bool          // true if the password appears in a known breach corpus

Port: JwtSigner                 (access-token signing / refresh-token minting; RS256, keys from config / key vault)
  SignAccessToken(spec: AccessTokenSpec) -> string                       // the signed token string
  IssueRefreshToken()                    -> { RefreshToken: string, RefreshTokenHash: string }
  ValidateSignature(token: string)       -> bool                         // signature + expiry only

Port: OtpDeliveryPort           (SMS / email transport for activation, MFA, and reset codes)
  Send(destination: string, plaintextCode: string, purpose: OtpPurpose) -> Result
  // channel implied by purpose — SMS for mobile-OTP and password-reset, email/SMS for activation

Port: TotpProvider              (TOTP secret generation / verification for authenticator-app MFA; RFC 6238)
  Enroll(accountLabel: string)                       -> { SecretRef: string, ProvisioningUri: string }
  Verify(secretRef: string, submittedCode: string)   -> bool

Port: RateLimiterPort           (distributed rate-limiter — registration, login, reset throttling)
  TryConsume(key: string, maxInWindow: int, window: duration) -> bool
  // returns false when the caller has exceeded the window; key e.g. "register:{ip}", "login:{ip}", "reset:{identifier}"
```

For the exercise, `Infrastructure` may provide **stub adapters** for `BreachCheckPort` (a small in-memory blocklist), `OtpDeliveryPort` (logs the code), `RateLimiterPort` (in-memory counter), and `TotpProvider`. `PasswordHasher` and `JwtSigner` should be **real** (a proper `argon2id` library and `RS256` signing with a dev key) since they are the security core. Keep the port shapes exactly as above so production adapters drop in later.

### 9.3 Public API this module EXPOSES (in the `Contracts` surface)

This is the **Open Host Service**. Two synchronous contracts plus the event records in §8.1.

```
Public API: IdentityProvisioningApi
  // Called SYNCHRONOUSLY by BC-2 Employer Profile and BC-3 JobSeeker Profile during their
  // registration journeys. Enforces email/mobile uniqueness, password policy, breach check,
  // argon2id hashing; creates the account in PendingActivation; issues + sends an Activation OTP;
  // publishes UserRegistered. Returns the new UserId so the caller can build its own aggregate
  // in the same logical unit of work.
  ProvisionCredential(email: string, mobile: string, password: string, role: string)
      -> Result<ProvisionedIdentity>
  ProvisionedIdentity { UserId: uuid }
  // Error codes returned (callers surface these verbatim):
  //   E-REG-DUPLICATE          email or mobile already in use
  //   E-REG-INVALID-EMAIL      email fails RFC 5322
  //   E-REG-INVALID-MOBILE     mobile fails E.164
  //   E-REG-INVALID-PASSWORD   password fails policy (length / character classes)
  //   E-REG-PASSWORD-BREACHED  password found in breach corpus
  //   E-REG-RATE-LIMITED       too many registrations from this origin in the window

Public API: TokenValidationApi
  // Used by the HOST's authentication middleware (and by any BC that must check a token
  // out-of-band). Validates signature, expiry, and the revocation list; returns the principal.
  Validate(accessToken: string) -> Result<ValidatedPrincipal>
  ValidatedPrincipal { UserId: uuid, Role: string, Permissions: list<string>, SessionId: uuid }
```

> **Contract stability.** `IdentityProvisioningApi` and `TokenValidationApi` are referenced *by signature* in the BC-2 and BC-3 handover packages (BC-3's §9.2 reproduces `ProvisionCredential` returning a `UserId` with the `E-REG-*` error codes). Keep the operation names, parameter order, return types, and the `E-REG-*` error-code strings **exactly** as written here — changing them breaks those packages.

---

## 10. Application layer

Every use case is a **command** or a **query** with exactly one handler. Commands return `Result`/`Result<T>`; queries return DTOs. Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

### 10.1 Commands

| Command | Story | Handler responsibilities |
|---|---|---|
| `ProvisionCredentialCommand` | US-3.1.1-01 / US-3.1.2-01 (callers) | The implementation behind `IdentityProvisioningApi`. Rate-limit check (`RateLimiterPort`) → uniqueness pre-check (`GetByEmail`/`GetByMobile`) → `PasswordPolicyService.Validate` → `BreachCheckPort.IsBreached` → `PasswordHasher.Hash` → `UserAccount.Provision(...)` → `OtpChallenge.Issue(Activation)` → persist → handler reacts to `OtpIssued` by calling `OtpDeliveryPort`. Returns `Result<ProvisionedIdentity>`. |
| `ActivateAccountCommand` | US-3.1.1-02 | Load `OtpChallenge` (purpose `Activation`) + `UserAccount` → `challenge.Verify(...)` → on success `account.Activate()` → persist. On OTP failure, `account.RecordOtpFailure()`. |
| `ResendActivationOtpCommand` | US-3.1.1-02 | Load account; if `PendingActivation`, expire prior challenge, `OtpChallenge.Issue` again, send. Rate-limited. |
| `LoginWithCredentialsCommand` | US-3.1.5-01 | Rate-limit per IP (`login:{ip}`, 10/min → 5-min throttle, `E-LOGIN-RATE-LIMITED`) → `GetByEmailOrMobile` (identifier auto-detected) → if missing or wrong hash (`PasswordHasher.Verify`, constant-time) → `account?.RecordFailedLogin(...)`, return generic `E-LOGIN-INVALID-CREDENTIALS` (no user enumeration) → reject by status with the matching code (`E-LOGIN-ACCOUNT-NOT-ACTIVATED` / `E-LOGIN-ACCOUNT-DEACTIVATED` / `E-LOGIN-ACCOUNT-BANNED`) or `LockState.IsLocked` (`E-LOGIN-ACCOUNT-LOCKED`) → if MFA enabled, return an MFA challenge handle instead of tokens → else `TokenClaimsBuilder` + `JwtSigner` → `account.RecordSuccessfulLogin(...)` (extended TTL when "remember me" set) → persist → return access + refresh token (+ session cookie at the API layer). |
| `VerifyMfaChallengeCommand` | US-3.1.5-02 | Validate the second factor (`TotpProvider.Verify`, an `OtpChallenge` of purpose `Mfa`, or a backup code via `account.RedeemBackupCode`) → on success issue tokens + `RecordSuccessfulLogin`. On failure `RecordOtpFailure`; after 3 failures within 5 min the account is locked for 15 min. |
| `EnrollMfaCommand` | US-3.1.5-02 | Requires an authenticated `Active` account → `TotpProvider.Enroll` (returns QR/provisioning URI) or set up `SmsOtp` (test OTP sent). The verify step (`ConfirmMfaEnrollmentCommand`) must succeed before MFA is active. |
| `ConfirmMfaEnrollmentCommand` | US-3.1.5-02 | Verify the test code → generate 8–10 backup codes (hash them) → `account.EnableMfa(method, secretRef, backupCodeHashes)` → persist; return the plaintext backup codes once for the user to store. |
| `DisableMfaCommand` | US-3.1.5-02 | Requires re-auth → `account.DisableMfa()` → persist. |
| `RefreshAccessTokenCommand` | US-3.4.3-04 AC-04 / US-3.1.5-04 AC-11 | Look up the `Session` by refresh-token hash → reject if revoked/expired/not found (`E-TOKEN-INVALID`) → rotate: issue a new refresh token, update the session, sign a new access token → persist. |
| `RevokeTokenCommand` | US-3.4.3-04 AC-06 | Add the token identifier / refresh token to the revocation list; `account.RevokeSession(sessionId)`. |
| `LogoutCommand` | US-3.1.5-04 AC-06 | `account.RevokeSession(currentSessionId)` → persist (API layer clears the session cookie). |
| `LogoutAllSessionsCommand` | US-3.1.5-04 | `account.RevokeAllSessions()` → persist. |
| `RequestPasswordResetCommand` | US-3.1.5-03 AC-01/09 | Rate-limit (`reset:{identifier}`, 3/hour, `E-RESET-RATE-LIMITED`) → `GetByEmailOrMobile` → if found, `OtpChallenge.Issue(PasswordReset)` and send the SMS OTP via `OtpDeliveryPort` → persist. Always returns success (no user enumeration). |
| `VerifyPasswordResetOtpCommand` | US-3.1.5-03 AC-02/03/04 | Load the `PasswordReset` `OtpChallenge` → `challenge.Verify(...)` (`E-OTP-EXPIRED` past 5 min; `Locked` after 3 wrong attempts → 15-min lock) → on success issue a short-lived reset token (`account.IssuePasswordResetToken`). |
| `CompletePasswordResetCommand` | US-3.1.5-03 AC-05/06/07/08 | Validate the reset token → `PasswordPolicyService` + `BreachCheckPort` + no-reuse check on the new password (`E-RESET-INVALID-PASSWORD` / `E-RESET-PASSWORD-BREACHED`) → `PasswordHasher.Hash` → `account.CompletePasswordReset(tokenHash, newHash)` (revokes all sessions) → persist. |
| `ChangePasswordCommand` | US-3.1.5-03 | Authenticated; verify current password → policy + breach + no-reuse check → `account.ChangePassword(newHash)` → persist. |
| `AdminApproveEmployerCommand` | US-3.1.4-01 AC-05 | Admin-only. Load target → `account.Activate()` (employer verified) → append `AdminActionLog(ApprovedEmployer)` → persist. |
| `AdminRejectEmployerCommand` | US-3.1.4-01 AC-06 | Admin-only. `reason` required → `account.Suspend(reason, adminId)` (reject path) → append `AdminActionLog(RejectedEmployer)` → persist. |
| `AdminSuspendUserCommand` | US-3.1.4-01 AC-08 | Admin-only. `reason` required → `account.Suspend(reason, adminId)` → append `AdminActionLog(Suspended)` → persist. |
| `AdminReinstateUserCommand` | US-3.1.4-01 | Admin-only → `account.Reinstate(adminId)` → append `AdminActionLog(Reinstated)` → persist. |
| `AdminDeactivateUserCommand` | US-3.1.4-01 AC-07 | Admin-only → `account.Deactivate()` → append `AdminActionLog(Deactivated)` → persist. |
| `AdminUnlockAccountCommand` | US-3.1.4-01 AC-10 | Admin-only → `account.Unlock()` → append `AdminActionLog(Unlocked)` → persist. |
| `AdminIssuePasswordResetCommand` | US-3.1.4-01 AC-09 | Admin-only → `account.IssuePasswordResetToken(...)` → append `AdminActionLog(PasswordResetIssued)` → persist → send a reset link to the user's registered email. |
| `AssignRoleCommand` | US-3.1.4-01 / US-3.1.5-04 | Admin-only → `account.AssignRole(role, adminId)` → append `AdminActionLog(RoleAssigned)` → persist. |
| `IssueOAuthTokenCommand` | US-3.4.3-04 AC-01/05 | Authorization-Code (PKCE-capable) / Client-Credentials flow → on a validated authorization → `TokenClaimsBuilder` with the requested **scopes** → `JwtSigner` → create a `Session` of channel `Api` → return access + refresh token. |

### 10.2 Queries

| Query | Story | Returns |
|---|---|---|
| `GetMyAccountQuery` | US-3.1.5-04 | `AccountDto` (UserId, email, mobile masked, role, status, MFA enabled, identityVerified) |
| `GetMySessionsQuery` | US-3.1.5-04 AC-07 | `list<SessionDto>` (channel, device label, issued, lastSeen, current?) |
| `GetMfaStatusQuery` | US-3.1.5-02 AC-10 | `MfaStatusDto` (enabled, method, last verified date, backup-codes-remaining) |
| `ListUsersQuery` | US-3.1.4-01 AC-01/02/03 | `PagedResult<UserListItemDto>` — searchable (name/email) + filterable (type, status, registration date, verification status) |
| `GetUserAsAdminQuery` | US-3.1.4-01 AC-04 | `AdminUserDetailDto` (contact, registration info, verification status, account status, lock state, session count). Logs an `AdminActionLog(Viewed)` row. |
| `GetAdminActionLogQuery` | US-3.1.4-01 AC-11 | `PagedResult<AdminActionDto>` — filter by admin, target user, action type, date range |
| `ValidateTokenQuery` | US-3.4.3-04 AC-02/03 | `ValidatedPrincipal` — the implementation behind `TokenValidationApi` |

### 10.3 Validators — representative rules

- `ProvisionCredentialCommand`: email RFC 5322 (`E-REG-INVALID-EMAIL`); mobile E.164 (`E-REG-INVALID-MOBILE`); password present and ≥10 chars (`E-REG-INVALID-PASSWORD`); role ∈ enum. (Uniqueness, breach, no-reuse, and rate-limit are *handler* responsibilities — they need ports/repos.)
- `ActivateAccountCommand`: `UserId` present; OTP is exactly 6 digits.
- `LoginWithCredentialsCommand`: identifier (email or mobile) present; password present.
- `EnrollMfaCommand`: method ∈ {`Totp`, `SmsOtp`}.
- `VerifyPasswordResetOtpCommand`: OTP is exactly 6 digits.
- `CompletePasswordResetCommand`: token present; new password ≥10 chars and ≥3 character classes.
- `AdminSuspendUserCommand` / `AdminRejectEmployerCommand` / `AdminDeactivateUserCommand`: `reason` non-empty (invariant #11).
- `AssignRoleCommand`: role ∈ enum; target `UserId` present.
- `RefreshAccessTokenCommand` / `RevokeTokenCommand`: token string present.

### 10.4 DTOs

Plain data records in `Application`. Never expose Domain entities/VOs across the API. Map aggregate → DTO in the handler or a mapping helper. **Never** put a password hash, OTP code, refresh-token hash, MFA secret, or backup-code hash in a DTO. Mobile numbers are masked (`+8801******23`) in admin/list DTOs.

---

## 11. Persistence & data model

Schema/namespace: `identity_access`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. The module-specific relational model follows.

### 11.1 Reference relational model — schema `identity_access`

```
TABLE user_accounts
  id                    uuid          PK                       -- this IS the platform UserId
  email                 string        NOT NULL UNIQUE          -- lower-cased
  mobile                string        NOT NULL UNIQUE          -- E.164
  password_algorithm    string        NOT NULL                 -- 'argon2id'
  password_hash         string        NOT NULL
  password_history      json          NOT NULL DEFAULT '[]'    -- last 3 password hashes (no-reuse rule)
  role                  enum          NOT NULL                 -- JobSeeker|Employer|ThirdPartyPortal|MoLAdministrator
  status                enum          NOT NULL                 -- PendingActivation|Active|Suspended|Deactivated
  permissions           json          NOT NULL DEFAULT '[]'    -- resolved permission strings
  lock_state            json          NOT NULL                 -- LockState VO
  mfa                   json          NOT NULL                 -- MfaConfiguration VO
  identity_verified     bool          NOT NULL DEFAULT false
  suspended_reason      string        NULL
  activated_on_utc      datetime      NULL
  deactivated_on_utc    datetime      NULL
  created_on_utc        datetime      NOT NULL
  updated_on_utc        datetime      NOT NULL
  version_token         (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  INDEX (email), INDEX (mobile), INDEX (status), INDEX (role)

TABLE sessions
  id                    uuid          PK
  user_account_id       uuid          NOT NULL  FK -> user_accounts.id ON DELETE CASCADE
  channel               enum          NOT NULL                 -- Web|Mobile|Api
  device_fingerprint    string        NOT NULL
  refresh_token_hash    string        NOT NULL
  issued_on_utc         datetime      NOT NULL
  last_seen_utc         datetime      NOT NULL
  expires_on_utc        datetime      NOT NULL
  revoked_on_utc        datetime      NULL
  INDEX (user_account_id), INDEX (refresh_token_hash),
  INDEX (user_account_id) WHERE revoked_on_utc IS NULL

TABLE trusted_devices
  id                    uuid          PK
  user_account_id       uuid          NOT NULL  FK -> user_accounts.id ON DELETE CASCADE
  device_fingerprint    string        NOT NULL
  label                 string        NOT NULL
  trusted_until_utc     datetime      NOT NULL
  UNIQUE (user_account_id, device_fingerprint)

TABLE backup_codes
  id                    uuid          PK
  user_account_id       uuid          NOT NULL  FK -> user_accounts.id ON DELETE CASCADE
  code_hash             string        NOT NULL
  used_on_utc           datetime      NULL
  INDEX (user_account_id) WHERE used_on_utc IS NULL

TABLE password_reset_tokens
  id                    uuid          PK
  user_account_id       uuid          NOT NULL  FK -> user_accounts.id ON DELETE CASCADE
  token_hash            string        NOT NULL
  issued_on_utc         datetime      NOT NULL
  expires_on_utc        datetime      NOT NULL
  used_on_utc           datetime      NULL
  INDEX (token_hash), INDEX (user_account_id) WHERE used_on_utc IS NULL

TABLE otp_challenges
  id                    uuid          PK
  user_account_id       uuid          NOT NULL                 -- references user_accounts.id, no cross-aggregate FK enforced
  purpose               enum          NOT NULL                 -- Activation|Mfa|PasswordReset
  code_hash             string        NOT NULL
  status                enum          NOT NULL                 -- Issued|Verified|Expired|Locked
  attempt_count         int           NOT NULL DEFAULT 0
  max_attempts          int           NOT NULL                 -- 5 for Activation, 3 otherwise
  issued_on_utc         datetime      NOT NULL
  expires_on_utc        datetime      NOT NULL
  verified_on_utc       datetime      NULL
  INDEX (user_account_id, purpose) WHERE status = 'Issued'

TABLE revoked_tokens
  token_id_or_refresh_hash  string    PK                       -- token identifier or refresh-token hash
  revoked_on_utc            datetime  NOT NULL
  expires_on_utc            datetime  NOT NULL                 -- when it can be swept from the list

TABLE admin_action_log
  id                    uuid          PK
  admin_user_id         uuid          NOT NULL
  action_type           enum          NOT NULL
  target_user_id        uuid          NOT NULL
  reason                string        NULL
  occurred_on_utc       datetime      NOT NULL
  INDEX (target_user_id), INDEX (admin_user_id), INDEX (occurred_on_utc)

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

### 11.2 Module-specific mapping notes

- `Session`, `TrustedDevice`, `BackupCode`, `PasswordResetToken` are **owned** child collections of the `UserAccount` aggregate and loaded with it.
- `email`, `mobile`, `role`, `status` are flattened to scalar columns (not left inside `json`) because they are queried / uniquely constrained; `Credential` is split into `email` / `mobile` / `password_algorithm` / `password_hash` scalar columns. `password_history`, `permissions`, `lock_state`, `mfa` are stored as `json`.
- `OtpChallenge` is a **separate aggregate**: `otp_challenges.user_account_id` is a plain `uuid` reference with **no FK constraint** to `user_accounts` (the no-cross-aggregate-FK rule, [[00-Shared-Foundations]] §6.4), even though both tables live in the same module schema.
- Optimistic-concurrency tokens are required on `user_accounts` and `otp_challenges` (both can be updated concurrently).
- `revoked_tokens` and `otp_challenges` are high-churn — the cleanup job in §3 keeps them small.
- General persistence conventions (one persistence context per module, outbox/inbox wiring, value-object mapping) follow [[00-Shared-Foundations]] §3 and §6.

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

`UserAccountRepository` (`GetById`, `GetByEmail`, `GetByMobile`, `GetByEmailOrMobile`, `GetBySessionRefreshTokenHash`, `GetByPasswordResetTokenHash`, `Search`, `Add`, `Update`, `EmailExists`, `MobileExists`), `OtpChallengeRepository` (`GetById`, `GetActiveByAccountAndPurpose`, `Add`, `Update`), `RevokedTokenStore` (`Add`, `IsRevoked`), `AdminActionLogRepository` (`Add`, `Query`), `UnitOfWork` (`SaveChanges`).

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. Route prefix `/api/identity`. Public endpoints (register, login, refresh, password-reset request/verify/complete, activate) are anonymous; everything else requires a valid access token. Admin endpoints additionally require the `users:manage` permission; RBAC violations return `403` with `E-UNAUTHORIZED-ROLE` / `E-FORBIDDEN`. Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

| Verb & route | Command/Query | Success | Notable failures |
|---|---|---|---|
| `POST /api/identity/login` | `LoginWithCredentialsCommand` | `200` + tokens + session cookie **or** `200` + MFA challenge handle | `401 E-LOGIN-INVALID-CREDENTIALS`, `403 E-LOGIN-ACCOUNT-NOT-ACTIVATED`, `403 E-LOGIN-ACCOUNT-DEACTIVATED`, `403 E-LOGIN-ACCOUNT-BANNED`, `423 E-LOGIN-ACCOUNT-LOCKED`, `429 E-LOGIN-RATE-LIMITED` |
| `POST /api/identity/mfa/verify` | `VerifyMfaChallengeCommand` | `200` + tokens | `401 E-MFA-INVALID-CODE`, `423 E-OTP-LOCKED` |
| `POST /api/identity/mfa/enroll` | `EnrollMfaCommand` | `200` + provisioning URI / QR | `409 E-MFA-ALREADY-ENABLED` |
| `POST /api/identity/mfa/enroll/confirm` | `ConfirmMfaEnrollmentCommand` | `200` + backup codes (shown once) | `401 E-MFA-INVALID-CODE` |
| `DELETE /api/identity/mfa` | `DisableMfaCommand` | `204` | `401` re-auth required |
| `GET /api/identity/me/mfa` | `GetMfaStatusQuery` | `200` + `MfaStatusDto` | `401` |
| `POST /api/identity/activate` | `ActivateAccountCommand` | `200` | `400 E-OTP-INVALID`, `410 E-OTP-EXPIRED`, `423 E-OTP-LOCKED` |
| `POST /api/identity/activate/resend` | `ResendActivationOtpCommand` | `202` | `409` already active, `429 E-REG-RATE-LIMITED` |
| `POST /api/identity/token/refresh` | `RefreshAccessTokenCommand` | `200` + new tokens | `401 E-TOKEN-INVALID` |
| `POST /api/identity/token/revoke` | `RevokeTokenCommand` | `204` | `401` |
| `POST /api/identity/oauth/token` | `IssueOAuthTokenCommand` | `200` + access + refresh token (OAuth2 token-response shape) | `400 E-OAUTH-INVALID-GRANT`, `401 E-OAUTH-INVALID-CLIENT` |
| `POST /api/identity/logout` | `LogoutCommand` | `204` (session cookie cleared) | `401` |
| `POST /api/identity/logout-all` | `LogoutAllSessionsCommand` | `204` | `401` |
| `POST /api/identity/password/reset-request` | `RequestPasswordResetCommand` | `200` (always — no enumeration) | `429 E-RESET-RATE-LIMITED` |
| `POST /api/identity/password/reset-verify` | `VerifyPasswordResetOtpCommand` | `200` + reset token | `400 E-OTP-INVALID`, `410 E-OTP-EXPIRED`, `423 E-OTP-LOCKED` |
| `POST /api/identity/password/reset` | `CompletePasswordResetCommand` | `200` | `400 E-RESET-TOKEN-INVALID`, `400 E-RESET-INVALID-PASSWORD`, `410 E-RESET-TOKEN-EXPIRED`, `422 E-RESET-PASSWORD-BREACHED` |
| `POST /api/identity/password/change` | `ChangePasswordCommand` | `200` | `401` wrong current password, `422 E-RESET-PASSWORD-BREACHED` |
| `GET /api/identity/me` | `GetMyAccountQuery` | `200` + `AccountDto` | `401` |
| `GET /api/identity/me/sessions` | `GetMySessionsQuery` | `200` + sessions | `401` |
| `GET /api/identity/admin/users` | `ListUsersQuery` | `200` + `PagedResult<UserListItemDto>` | `403 E-FORBIDDEN` |
| `GET /api/identity/admin/users/{id}` | `GetUserAsAdminQuery` | `200` + `AdminUserDetailDto` | `403 E-FORBIDDEN`, `404` |
| `POST /api/identity/admin/users/{id}/approve` | `AdminApproveEmployerCommand` | `200` | `403`, `409` not pending |
| `POST /api/identity/admin/users/{id}/reject` | `AdminRejectEmployerCommand` | `200` | `400` reason missing, `403`, `409` |
| `POST /api/identity/admin/users/{id}/suspend` | `AdminSuspendUserCommand` | `200` | `400` reason missing, `403`, `409` not active |
| `POST /api/identity/admin/users/{id}/reinstate` | `AdminReinstateUserCommand` | `200` | `403`, `409` not suspended |
| `POST /api/identity/admin/users/{id}/deactivate` | `AdminDeactivateUserCommand` | `200` | `403`, `409` not active |
| `POST /api/identity/admin/users/{id}/unlock` | `AdminUnlockAccountCommand` | `200` | `403` |
| `POST /api/identity/admin/users/{id}/password-reset` | `AdminIssuePasswordResetCommand` | `202` | `403`, `404` |
| `POST /api/identity/admin/users/{id}/role` | `AssignRoleCommand` | `200` | `400` invalid role, `403` |
| `GET /api/identity/admin/audit` | `GetAdminActionLogQuery` | `200` + `PagedResult<AdminActionDto>` | `403` |

The `TokenValidationApi` is **not** an HTTP route — it is an in-process contract the host's authentication middleware calls directly.

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** every VO factory — valid input succeeds; each invalid case returns the right `Error`. Specifically: `EmailAddress` (RFC 5322), `MobileNumber` (E.164 / +880), `RawPassword` (under 10 chars fails; <3 character classes fails), `MfaConfiguration` (`Enabled` without method/secret fails), `LockState` (negative count fails).
- **UserAccount aggregate:**
  - Status machine via `AccountStateMachine`: every legal transition succeeds; every illegal one (`PendingActivation → Deactivated`, `Suspended → Deactivated`, `Deactivated → Suspended`, `Deactivated → Active` only via `ReactivateAfterDeactivation`, etc.) returns failure.
  - Login state checks: `RecordSuccessfulLogin` fails from every non-`Active` status; each non-`Active` status yields its own distinct login error code.
  - `Suspend` requires a non-empty reason; sets `SuspendedReason`; revokes all sessions; raises `UserAccountSuspended`.
  - `Deactivate` revokes all sessions and sets `DeactivatedOnUtc`; raises `AccountDeactivated`.
  - `CompletePasswordReset` with a wrong/expired/used token fails; with a valid token sets the new hash, marks the token used, and revokes all sessions.
  - `ChangePassword` and `CompletePasswordReset` both revoke all sessions (invariant #8), and both reject a new hash that matches any of the last 3 in `PasswordHistory` (no-reuse).
  - `EnableMfa` stores 8–10 backup codes; `RedeemBackupCode` consumes a code once and rejects a re-use.
  - `TouchSession` past the inactivity window revokes the session.
  - `AssignRole` recomputes `Permissions` via `PermissionResolver`.
  - `UserId` is immutable after `Provision`.
- **OtpChallenge aggregate:** `Verify` after expiry → `Expired` + `E-OTP-EXPIRED`; wrong attempts past `MaxAttempts` (5 for `Activation`, 3 for `Mfa`/`PasswordReset`) → `Locked` + `E-OTP-LOCKED`; a correct code → `Verified`; a `Verified`/`Locked` challenge cannot be re-verified.
- **Domain services:** `PasswordPolicyService` — table-driven valid/invalid cases. `PermissionResolver` — each role maps to the documented baseline set; explicit grants are unioned. `AccountStateMachine` — exhaustive legal/illegal transition matrix. `TokenClaimsBuilder` — claims include `sub`, `role`, `permissions`, `scopes`, `sessionId`, `exp` and nothing secret.

### 13.2 Unit tests — Application (handlers, ports replaced with test doubles)

- `ProvisionCredentialCommand`: happy path checks rate limit → uniqueness → policy → breach → hashes → creates account + OTP, returns `UserId`; duplicate email → `E-REG-DUPLICATE`, **no account persisted**; breached password → `E-REG-PASSWORD-BREACHED`, no account; rate-limited → `E-REG-RATE-LIMITED`, no account; `PasswordHasher` is called exactly once on the happy path; the plaintext password never reaches the repository.
- `LoginWithCredentialsCommand`: wrong password → `RecordFailedLogin` called, generic `E-LOGIN-INVALID-CREDENTIALS` returned (no user enumeration); `PendingActivation` / `Deactivated` / `Suspended` accounts → the matching distinct error code; locked account → `E-LOGIN-ACCOUNT-LOCKED`; 11th attempt from one IP within a minute → `E-LOGIN-RATE-LIMITED`; MFA-enabled account → returns an MFA challenge, **no tokens yet**; "remember me" → session `ExpiresOnUtc` set 14 days out; happy path → tokens issued, `Session` created, `UserLoggedIn` queued to outbox.
- `VerifyMfaChallengeCommand`: valid TOTP / SMS-OTP / backup code → tokens issued; 3 wrong codes within 5 min → account locked 15 min; a consumed backup code cannot be reused.
- `ActivateAccountCommand`: valid OTP → account `Active`, `UserAccountActivated` queued; 5 wrong OTPs → challenge `Locked` and account `LockState` updated.
- `RequestPasswordResetCommand` / `VerifyPasswordResetOtpCommand` / `CompletePasswordResetCommand`: request is rate-limited (4th/hour → `E-RESET-RATE-LIMITED`) and never enumerates users; expired OTP → `E-OTP-EXPIRED`; 3 wrong OTPs → 15-min lock; breached or reused new password rejected; valid → hash updated, all sessions revoked, `PasswordReset` queued.
- `RefreshAccessTokenCommand`: revoked/expired/unknown refresh token → `E-TOKEN-INVALID`; valid → token rotated, old refresh hash no longer valid.
- Admin commands: `AdminSuspendUserCommand` with empty reason → validator rejects; happy path suspends, writes an `AdminActionLog` row in the **same** unit of work, queues `UserAccountSuspended`; `AdminDeactivateUserCommand` writes the log and queues `AccountDeactivated`; `AssignRoleCommand` queues `RoleAssigned` and writes the log.
- Validation step: each validator rejects the documented bad inputs before the handler runs.

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip `UserAccount` with its owned `Session` / `TrustedDevice` / `BackupCode` / `PasswordResetToken` collections and `json` VOs; `EmailExists` / `MobileExists` / `GetByEmailOrMobile` / `GetBySessionRefreshTokenHash` work; optimistic-concurrency conflict is detected.
- **Unique indexes:** inserting a second account with the same email or mobile fails at the DB level (the backstop behind the handler pre-check).
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `identity_access`.
- **Outbox:** a provisioning writes both the `user_accounts` row and the `UserRegistered` outbox message in one transaction; rolling back leaves neither.
- **Inbox / idempotency:** delivering `IdentityVerifiedByGovernmentIntegrationEvent` twice sets the flag once and is a no-op the second time.
- **API:** host-level tests for the register → activate → login → refresh → logout-all happy path; login with a wrong password returns `401` with a generic message (no enumeration); a suspended account cannot log in (`E-LOGIN-ACCOUNT-BANNED`); an admin can suspend a user and the audit log shows the row; a non-admin calling an `/admin/*` route gets `403 E-FORBIDDEN`.
- **Token lifecycle:** an access token validates via `TokenValidationApi`; after `RevokeTokenCommand` the same token is rejected; an expired token is rejected.
- **MFA & reset flows:** enrol → confirm → backup codes issued; login challenges MFA; the SMS-OTP password-reset flow completes and revokes all sessions.
- **Consumed events:** `IdentityVerificationFailedIntegrationEvent` records the failure without changing account status.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "Provision a credential" (the OHS entry point)

End-to-end, to pattern-match every other command against. This is the path BC-3 JobSeeker Profile and BC-2 Employer Profile take during registration.

1. **Caller.** BC-3's `RegisterJobSeekerCommandHandler` resolves `IdentityProvisioningApi` (from this module's `Contracts` surface) and calls `ProvisionCredential(email, mobile, password, "JobSeeker")`.
2. **Entry.** That call is wired to `ProvisionCredentialCommand` (the implementation lives in this module's `Application`). The in-process mediator sends the command.
3. **Validation step.** `ProvisionCredentialCommand`'s validator runs: email RFC 5322, mobile E.164, password present + ≥10 chars, role in enum. On failure → `Result` with the matching `E-REG-INVALID-*` error, returned to the caller.
4. **Handler.** `ProvisionCredentialCommandHandler`:
   a. `RateLimiterPort.TryConsume("register:{origin}", 5, 1h)` → on exceed, return `E-REG-RATE-LIMITED`.
   b. `UserAccountRepository.EmailExists` / `MobileExists` → if either, return `E-REG-DUPLICATE` (no account created).
   c. `PasswordPolicyService.Validate(RawPassword)` → on failure `E-REG-INVALID-PASSWORD`.
   d. `BreachCheckPort.IsBreached(RawPassword)` → if breached, `E-REG-PASSWORD-BREACHED`.
   e. `PasswordHasher.Hash(RawPassword)` → `PasswordHash` (`argon2id`). The plaintext is now discarded.
   f. `UserAccount.Provision(email, mobile, passwordHash, role)` — creates the account in `PendingActivation`, resolves permissions via `PermissionResolver`, raises `UserRegistered` (integration).
   g. `OtpChallenge.Issue(account.Id, OtpPurpose.Activation, codeHash, 5min, maxAttempts: 5)` — raises the internal `OtpIssued`.
   h. `repository.Add(account)`; `otpRepository.Add(challenge)`; `unitOfWork.SaveChanges()`.
5. **Domain-event / outbox step.** After the unit of work is saved, the mediator pipeline dispatches internal domain events in-process (`OtpIssued` → a handler calls `OtpDeliveryPort.Send` with the plaintext code) and writes `UserRegisteredIntegrationEvent` into the outbox — same transaction. ([[00-Shared-Foundations]] §6.1–6.2.)
6. **Relay.** The background outbox relay publishes `UserRegistered`; BC-9 sends the welcome message, BC-10 records the registration.
7. **Response.** The handler returns `Result<ProvisionedIdentity>` carrying the new `UserId`. BC-3's handler receives it synchronously and builds its `JobSeekerProfile` aggregate around that `UserId` in the same logical flow.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.1.4-01 Manage user accounts | Searchable/filterable user list (type, status, registration date, verification status); search by name/email; admin view of a user's full details; approve pending employer → `Active` + notify (event); reject with reason → `Suspended` + notify; deactivate (soft delete) → `Deactivated`, retained 12 months; suspend/ban → locked, sessions revoked, reason logged, not self-reversible; admin password-reset → reset link to registered email; unlock a locked account; **every** admin action written to `admin_action_log` with admin id, action type, timestamp, target, reason. |
| US-3.1.5-01 Login with credentials | Identifier auto-detected (email or mobile); verify `argon2id` hash constant-time; generic `E-LOGIN-INVALID-CREDENTIALS` for both wrong password and non-existent user (no enumeration); distinct codes for not-activated / deactivated / banned; per-IP rate limit (10/min → 5-min throttle, `E-LOGIN-RATE-LIMITED`); on success issue a 1-hour `RS256` access token + refresh token, create a `Session`, set an HTTP-only secure session cookie; "remember me" extends the session to 14 days; failed attempts logged; emit `UserLoggedIn` / `UserLoginFailed`. |
| US-3.1.5-02 Multi-factor authentication | MFA optional per user; enrol `Totp` (RFC 6238, QR/provisioning URI) or `SmsOtp` with a verify-before-activate step; 8–10 single-use backup codes issued at setup; MFA challenged on **every** login before tokens are issued; 3 failed MFA codes within 5 min → 15-min account lock; backup code consumed on use; disable MFA requires re-auth; MFA status view shows enabled/method/last-verified. |
| US-3.1.5-03 Password reset | Self-service request → SMS OTP to the registered mobile; OTP 5-min TTL, 3 attempts then 15-min lock (`E-OTP-EXPIRED` on expiry); new password enforces policy + breach check + no-reuse of the last 3 (`E-RESET-INVALID-PASSWORD` / `E-RESET-PASSWORD-BREACHED`); rate limit 3 requests / email-mobile / hour (`E-RESET-RATE-LIMITED`); on completion all sessions invalidated; no user enumeration; emit `PasswordReset`. |
| US-3.1.5-04 Session and access control | RBAC enforced at the API surface — `E-UNAUTHORIZED-ROLE` / `E-FORBIDDEN` (`403`) for insufficient role; a `Session` per login with channel/device/issued/expiry, stored server-side, HTTP-only secure cookie; configurable inactivity timeout (default 1 h); explicit logout (revoke one) and logout-all (revoke all); concurrent sessions allowed; revoked sessions' refresh tokens fail immediately; token-refresh mechanism issues a new access token; permissions carried as token claims and validated via `TokenValidationApi`. |
| US-3.4.3-04 Implement OAuth 2.0 authentication | Authorization-Code (PKCE-capable) and Client-Credentials flows issue a JWT access token + refresh token; validate signature, expiry, and **scopes** before processing; reject expired tokens with `401`; refresh-token flow mints a new access token; scope-based authorization (`jobs:read`, `applications:write`, …); token revocation list rejects compromised tokens immediately. |

---

## Appendix — teaching notes & open questions

- **Two-file brief.** This package + `00-Shared-Foundations.md` together are the complete brief. The shared file carries everything stack-related and identical across all 12 BCs; this package carries only the domain design. Discuss with the class: which parts of a design are genuinely reusable across modules, and what is the cost of the one shared dependency (the packages are no longer 100% standalone)?
- **Sync OHS call vs. pure event choreography.** This package keeps *both*: `IdentityProvisioningApi` is a synchronous Open Host Service call (BC-2/BC-3 need the `UserId` back in the same unit of work), while `UserRegistered` is still published for pure reactors (BC-9, BC-10, BC-12). Ask the class: when does request/response justify a synchronous coupling between BCs, and what does it cost in availability and deployment independence?
- **`OtpChallenge` as a separate aggregate.** OTP has its own short, high-churn lifecycle and is created *during* provisioning before the account is usable. It is modelled as its own aggregate rather than a child of `UserAccount` — note that even within one module schema, no FK is drawn between the two aggregate tables. Contrast with `Session`, which *is* a child entity. Good discussion: what makes a lifecycle "deserve" its own aggregate?
- **Lock vs. Suspend vs. Deactivate — three different "off" states.** `Lock` is a transient counter-driven flag (auto-expires after 15 min or admin `Unlock`). `Suspend` is an admin policy action (`Active → Suspended`, only an admin reverses it). `Deactivate` is a soft delete (`Active → Deactivated`, user-reversible, 12-month retention). Students routinely collapse these into one boolean — the cost of that mistake is a good exercise. The three login error codes (`E-LOGIN-ACCOUNT-NOT-ACTIVATED` / `-DEACTIVATED` / `-BANNED`) plus the lock code make the distinction visible right at the API.
- **The "generic subdomain that is load-bearing" tension.** IAM is *generic* (you would buy it, not build it) yet *every* BC depends on it. Discuss: does "generic" mean "unimportant"? When would you actually buy an off-the-shelf identity provider instead of building this module, and what would the OHS contract look like then?
- **Employer sub-roles as permission grants, not roles.** `US-3.1.5-04` names Employer Owner / Recruiter / Admin. This package keeps four top-level `UserRole`s and expresses the employer sub-roles as additional permission grants on the `Employer` baseline. Discuss the trade-off: a flat role enum kept simple vs. a richer role hierarchy — and where employer-team-membership *actually* belongs (arguably BC-2, not here).
- **No user-enumeration.** Login and password-reset deliberately return generic responses so an attacker cannot probe which emails exist. This is a security requirement that *shapes the API contract* — a good example of a non-functional requirement driving domain/handler design.
- **Where does `AccountDeactivationCascade` live?** This BC emits `AccountDeactivated`; BC-2/BC-3/BC-4/BC-5 all react. The [[Context_Map]] flags that no BC explicitly owns the cascade saga. Discuss whether IAM should *orchestrate* the cascade or simply *emit the fact* and let choreography handle it (this package chooses: emit the fact).
