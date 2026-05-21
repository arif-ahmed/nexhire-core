---
title: "Handover Package — BC-2 Employer Profile Management"
type: handover-package
bc_id: BC-2
bc_name: Employer Profile Management
bc_class: supporting
stack: "language-neutral — see [[00-Shared-Foundations]] (Target Stack declared there)"
generated: 2026-05-15
related:
  - "[[00-Shared-Foundations]]"
  - "[[BC_Mapping]]"
  - "[[Context_Map]]"
  - "[[Event_Catalog]]"
tags:
  - handover-package
  - bc/employer-profile
---

# Handover Package — BC-2 Employer Profile Management

> **Audience:** an AI coding agent. This package owns the **domain design** for the `EmployerProfile` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `EmployerProfile`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `employer_profile`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

---

## 1. Purpose & scope boundaries

### What this BC is for

Employer Profile Management owns the **company's identity-as-an-employer**: the employer's stepwise registration journey, the company profile (Level 1 contact data and Level 2 branding/details), uploaded logo/images/supplementary documents, the **verification state machine** (auto-verification against government registries, then manual MoL review), the "Verified Employer" badge, and the employer dashboard read model. It is a **supporting** subdomain — important to the platform but not its competitive core (that is the Recommendation Engine).

### In scope

The `EmployerProfile` module is responsible for:

- The employer **registration journey** (orchestration only — see boundary note below): Level 1 (company name, email, mobile, company ID / registration number) and Level 2 (website, industry, company size, address, description).
- Profile lifecycle: `PendingActivation → PendingVerification → Verified` with `PendingManualVerification` and `Rejected` branches.
- **Logo** upload (PNG/JPG ≤ 5 MB), **company images** gallery (PNG/JPG ≤ 5 MB, max 5), **supplementary documents** (PDF/JPG/PNG ≤ 10 MB, max 10) — upload, virus-scan orchestration, list, download reference, delete.
- The **verification workflow**: triggering the `EmployerVerificationSaga` (a separate package — see §9), reacting to its outcome events, transitioning the verification state, and granting/withholding the "Verified Employer" badge.
- **Profile management** — editing company information after activation, with changes reflected to search/recommendations within 60 seconds (via events).
- The **employer dashboard** read model — job-posting counts by status, application totals, matched-candidate counts, shortlist counts — assembled from integration events emitted by BC-4, BC-5, and BC-7.
- **Talent-pool shortlists** — named collections of candidate `UserId`s the employer saves from the dashboard.
- Publishing the integration events in §8 and reacting to the integration events in §9.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong to other BCs and are reached via the contracts in §9:

- **Credentials, password hashing, password policy, breach checks, access-token issuance, sessions, login, MFA, OTP** → BC-1 IAM and UAM. This module calls BC-1's provisioning API; it never stores a password or issues a token.
- **The OTP activation mechanics** → BC-1. This module only *reacts* to the `UserAccountActivated` event to move the profile forward.
- **Calling the government registry** → BC-8 External Job Synchronization, orchestrated by the `EmployerVerificationSaga`. This module *emits* `EmployerVerificationRequested` and *reacts* to `EmployerVerifiedByGovernment`; it never calls a government API directly.
- **The `EmployerVerificationSaga` orchestration itself** → a separate handover package. This module participates in the saga (emits the trigger, reacts to the outcome) but does not own the saga state machine.
- **Virus scanning engine** → external, reached via the `VirusScanner` port.
- **Object storage** (the actual file bytes) → external, reached via the `ObjectStorage` port. This module stores only file *references*.
- **Job postings** — creating, editing, moderating job postings → BC-4 Job Postings. This module exposes whether an employer is verified (`EmployerProfilePublicApi`); BC-4 enforces the "must be verified to publish" gate.
- **Job applications** → BC-5 Job Application. The dashboard *displays* application counts from BC-5 events; it does not own applications.
- **Candidate matching / match scores** → BC-7 Recommendation Engine. The dashboard *displays* matched-candidate counts from BC-7 events; it does not compute matches. Setting qualification thresholds is also BC-7.
- **The job-seeker (candidate) profile** → BC-3 JobSeeker Profile. The dashboard shows a candidate's *name and match score* (from BC-7's event) and links to BC-3's public profile; it does not store candidate profile data — a shortlist holds only `UserId`s.
- **Sending notifications / emails / SMS** → BC-9 Notification. This module emits events; BC-9 decides what to send.
- **Admin approve/reject of the employer account itself** → BC-1 IAM and UAM (`US-3.1.4-01`). BC-1 owns the *account*-level approve/reject; this module owns the *profile*-level verification state and reacts to BC-1's account events.

### Boundary note — the registration split (teaching point)

Registration (`US-3.1.2-01`) is *owned* by this BC as a **journey**, but credential mechanics belong to BC-1. This package models registration as a **synchronous call** to BC-1's `IdentityProvisioningApi` port (because the handler needs the `UserId` back in the same unit of work to create the `EmployerProfile`), then activation as an **event reaction** to BC-1's `UserAccountActivated`. The [[Event_Catalog]] also lists `UserRegistered` as consumed by BC-2; we deliberately use the synchronous-provisioning model for the primary path instead. This mirrors exactly how the BC-3 JobSeeker Profile package handles its registration — the two profile BCs are intentionally symmetric here. It is a good class discussion on sync-call vs. event-choreography for request/response needs.

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **Employer** | A company that recruits on the platform. Its platform identity (`UserId`) is owned by BC-1; its *company profile* is owned here. |
| **Employer Profile** | The `EmployerProfile` aggregate — the root of this BC. |
| **Employer Owner** | The person who registered the employer account; the primary actor for registration and profile management. |
| **Level 1 / L1** | Core registration fields: company name, email, mobile, company ID / registration number. Required. |
| **Level 2 / L2** | Company details: website, industry, company size, address, company description. Filled after activation. |
| **Company Identifier** | The company ID / government registration number. Unique across all employers. |
| **Profile Status** | `PendingActivation`, `PendingVerification`, `PendingManualVerification`, `Verified`, `Rejected`, `Suspended`, `Deactivated`. |
| **Verification** | The process of confirming the company is real — first automatically (government DB lookup), then manually (MoL review) if auto fails. |
| **Verified Badge** | The "Verified Employer" badge shown next to the company name once `Status == Verified`. |
| **Company Logo** | One image (PNG/JPG ≤ 5 MB) shown on the public profile. |
| **Company Image** | A gallery image (PNG/JPG ≤ 5 MB). Max 5 per profile. |
| **Supplementary Document** | A registration certificate / VAT certificate / other doc (PDF/JPG/PNG ≤ 10 MB). Max 10. Child entity, metadata only. |
| **Dashboard** | The employer's read-model workspace: posting counts, application totals, matched-candidate counts, shortlist counts. |
| **Shortlist / Talent Pool** | A named collection of candidate `UserId`s the employer saves from the dashboard. |
| **Verification Saga** | The `EmployerVerificationSaga` — a separate orchestration package that calls BC-8 (government lookup) and BC-10 (audit). This BC triggers and reacts to it. |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `EmployerProfile`.
- Public surface (`Contracts`): the integration events published in §8 + the public API in §9.3.
- **Module-specific notes:** a background outbox relay runs as described in [[00-Shared-Foundations]] §6.2; this module has no other background workers or scheduled jobs. The verification workflow is driven by an external `EmployerVerificationSaga` package (see §9), not by a worker inside this module.

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has **two aggregates**: `EmployerProfile` (the root of the BC) and `Shortlist`. (Notation: see [[00-Shared-Foundations]] §2.)

### 5.1 Aggregate: EmployerProfile

**Aggregate root.** Identity: `EmployerProfileId` (strongly-typed id wrapping `uuid`).

| Member | Type | Notes |
|---|---|---|
| `Id` | `EmployerProfileId` | |
| `UserId` | `uuid` | Identity owned by BC-1. Set at registration. Immutable. |
| `Status` | `EmployerProfileStatus` | enum: `PendingActivation`, `PendingVerification`, `PendingManualVerification`, `Verified`, `Rejected`, `Suspended`, `Deactivated` |
| `CompanyName` | `CompanyName` | VO |
| `Email` | `EmailAddress` | VO |
| `Mobile` | `MobileNumber` | VO |
| `CompanyIdentifier` | `CompanyIdentifier` | VO — company ID / registration number, unique |
| `Website` | `WebsiteUrl?` | VO, set at L2 |
| `Industry` | `string?` | L2; canonical industry label |
| `CompanySize` | `CompanySize?` | VO/enum: `Micro`, `Small`, `Medium`, `Large` |
| `Address` | `Address?` | VO, L2 |
| `Description` | `CompanyDescription?` | VO, L2 |
| `Logo` | `FileReference?` | VO — one logo image |
| `Images` | `list<CompanyImage>` | child entities, max 5 |
| `Documents` | `list<SupplementaryDocument>` | child entities (metadata only), max 10 |
| `Verification` | `VerificationState` | VO — `Outcome`, `Method`, `EvidenceRef?`, `RejectionReason?`, `LastAttemptUtc?` |
| `Completeness` | `ProfileCompleteness` | VO — `Level1Complete`, `Level2Complete` |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

**Child entities** (identity local to the aggregate; only mutated through the root):

- `CompanyImage` — `CompanyImageId`, `File` (`FileReference`), `ScanResult` (`VirusScanResult`), `UploadedOnUtc`.
- `SupplementaryDocument` — `SupplementaryDocumentId`, `File` (`FileReference`), `Kind` (`DocumentKind`: `RegistrationCertificate`/`VatCertificate`/`Other`), `ScanResult` (`VirusScanResult`), `UploadedOnUtc`.

### 5.2 Aggregate: Shortlist

**Aggregate root.** Identity: `ShortlistId`. Kept separate from `EmployerProfile` because it has its own lifecycle (create / rename / delete) and an unbounded membership list that would bloat the profile aggregate; it is also a distinct consistency boundary (adding a candidate need not lock the whole profile).

| Member | Type | Notes |
|---|---|---|
| `Id` | `ShortlistId` | |
| `EmployerProfileId` | `EmployerProfileId` | owning employer |
| `Name` | `string` | non-empty, ≤ 100 chars |
| `Members` | `list<ShortlistMember>` | child entities |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

- `ShortlistMember` — `ShortlistMemberId`, `CandidateUserId` (`uuid` — BC-1 identity, no FK), `MatchScore` (`int?` — copied from BC-7's event at add-time), `AddedOnUtc`.

### 5.3 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `CompanyName` | `Value` | non-empty, ≤ 200 chars, trimmed |
| `EmailAddress` | `Value` | RFC 5322; lower-cased on store |
| `MobileNumber` | `Value` | E.164; default region `+880` (Bangladesh) — region configurable |
| `CompanyIdentifier` | `Value` | non-empty; alphanumeric; unique across employers (DB index is the backstop) |
| `WebsiteUrl` | `Value` | valid absolute http/https URL |
| `CompanySize` | `Value` | enum: `Micro`, `Small`, `Medium`, `Large` |
| `Address` | `Line1`, `Line2?`, `City`, `District`, `Postcode`, `Country` | `Line1`, `City`, `District`, `Country` required |
| `CompanyDescription` | `Value` | ≤ 5000 chars |
| `FileReference` | `StorageKey`, `OriginalFileName`, `MimeType`, `SizeBytes` (`int64`) | size > 0 |
| `VirusScanResult` | `Status` (`Pending`/`Clean`/`Infected`), `ScannedOnUtc?` | |
| `VerificationState` | `Outcome` (`NotStarted`/`AutoPending`/`AutoPassed`/`AutoFailed`/`ManualPending`/`ManualPassed`/`ManualRejected`), `Method` (`None`/`Automatic`/`Manual`), `EvidenceRef?`, `RejectionReason?`, `LastAttemptUtc?` | `ManualRejected ⇒ RejectionReason` set |
| `ProfileCompleteness` | `Level1Complete` (`bool`), `Level2Complete` (`bool`) | — |

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior lives on the aggregate roots. Handlers never set properties directly. Method shapes below are neutral specifications (see [[00-Shared-Foundations]] §2).

### 6.1 EmployerProfile — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Register(userId, companyName, email, mobile, companyIdentifier)` | Creates profile in `PendingActivation`. All L1 fields valid. `Completeness.Level1Complete = true`. | `EmployerRegistered` *(integration)* |
| `Activate()` | Only from `PendingActivation`. → `PendingVerification`. Idempotent if already past activation. Fails from `Suspended`/`Deactivated`. | `EmployerProfileActivated` *(internal)* |
| `CompleteLevel2(website, industry, companySize, address, description)` | Only when `Status` is `PendingVerification` or later. Sets L2 fields. `Completeness.Level2Complete = true`. | `EmployerProfileUpdated` *(integration)* |
| `BeginAutomaticVerification(registryRef)` | Only when L1 (and per assumption, L2) complete and `Status == PendingVerification`. Sets `Verification.Outcome = AutoPending`, `Method = Automatic`. | `EmployerVerificationRequested` *(integration)* |
| `RecordAutomaticVerificationPassed(evidenceRef)` | from `Verification.Outcome == AutoPending`. → `Status = Verified`, `Verification.Outcome = AutoPassed`. | `EmployerVerified` *(integration)* |
| `RecordAutomaticVerificationFailed()` | from `AutoPending`. → `Status = PendingManualVerification`, `Verification.Outcome = AutoFailed`. MoL is notified (via event consumer). | `EmployerManualVerificationRequired` *(integration)* |
| `ApproveManualVerification(byAdminId, evidenceRef)` | from `Status == PendingManualVerification`. → `Status = Verified`, `Verification.Outcome = ManualPassed`. | `EmployerVerified` *(integration)* |
| `RejectManualVerification(byAdminId, reason)` | from `Status == PendingManualVerification`. → `Status = Rejected`, `Verification.Outcome = ManualRejected`, `RejectionReason = reason`. | `EmployerVerificationFailed` *(integration)* |
| `ResubmitForVerification()` | only from `Status == Rejected`. → `Status = PendingManualVerification`, clears `RejectionReason`. Requires the company info to have been edited since rejection (handler-checked). | `EmployerManualVerificationRequired` *(integration)* |
| `UpdateCompanyInformation(companyName?, website?, industry?, companySize?, address?, description?)` | Only when `Status` is `PendingVerification`, `PendingManualVerification`, `Verified`, or `Rejected`. Applies the provided fields. | `EmployerProfileUpdated` *(integration)* |
| `SetLogo(fileReference, scanResult)` | `scanResult.Status` must be `Clean` (`E-UPLOAD-VIRUS`); `MimeType` ∈ {PNG, JPG} (`E-UPLOAD-INVALID-FORMAT`); `SizeBytes ≤ 5 MB` (`E-UPLOAD-SIZE-EXCEEDED`). Replaces any prior logo. | `EmployerProfileUpdated` *(integration)* |
| `AddCompanyImage(fileReference, scanResult)` | **max 5 images** (`E-UPLOAD-LIMIT-EXCEEDED`); `scanResult.Status == Clean`; format PNG/JPG; size ≤ 5 MB. | `EmployerProfileUpdated` *(integration)* |
| `RemoveCompanyImage(companyImageId)` | image must exist | `EmployerProfileUpdated` *(integration)* |
| `AddSupplementaryDocument(fileReference, kind, scanResult)` | **max 10 documents** (`E-UPLOAD-LIMIT-EXCEEDED`); `scanResult.Status == Clean`; format PDF/JPG/PNG; size ≤ 10 MB. | `SupplementaryDocumentAdded` *(internal)* |
| `RemoveSupplementaryDocument(documentId)` | document must exist | `SupplementaryDocumentRemoved` *(internal)* |
| `Suspend(reason)` | from any active state. → `Status = Suspended`. Reacts to BC-1 `UserAccountSuspended`. | `EmployerProfileSuspended` *(internal)* |
| `Reinstate()` | only from `Suspended`. → restores the pre-suspension status (stored) or `PendingVerification` if unknown. | `EmployerProfileReinstated` *(internal)* |
| `Deactivate()` | from any state. → `Status = Deactivated`. Reacts to BC-1 `AccountDeactivated`. | `EmployerProfileDeactivated` *(internal)* |

### 6.2 Core invariants (must always hold)

1. **Status transitions** form a fixed machine: `PendingActivation → PendingVerification`; `PendingVerification → Verified | PendingManualVerification`; `PendingManualVerification → Verified | Rejected`; `Rejected → PendingManualVerification` (resubmit); any active state `→ Suspended` and `→ Deactivated`; `Suspended → (prior state)`. No other transition is legal. In particular `PendingActivation → Verified` is forbidden — you cannot be verified before you are activated.
2. **L1 completeness**: a profile always has all four L1 fields once `Register` succeeds (they are constructor args).
3. **Company identifier uniqueness** is absolute across all employers — enforced by a DB unique index *and* a pre-check in the registration handler (`E-REG-DUPLICATE`). Likewise email/mobile uniqueness is enforced by **BC-1** at provisioning time; this module additionally keeps its own unique index on `company_identifier`.
4. **Verified badge** is shown **iff** `Status == Verified`. No other status grants the badge.
5. **A "Verified Employer" status is required to publish a public job posting** — this BC does not enforce that gate itself; it *exposes* `IsVerified` via `EmployerProfilePublicApi` and BC-4 enforces it. But the invariant that drives it lives here: only `Verified` profiles answer `true`.
6. **Company images**: at most **5** per profile. **Supplementary documents**: at most **10** per profile. Only `Clean`-scanned files may be added — an `Infected` or `Pending` scan result is rejected.
7. **Logo / image format** is PNG or JPG only; **document format** is PDF, JPG, or PNG only. Unsupported formats → `E-UPLOAD-INVALID-FORMAT`.
8. **`UserId` is immutable** after `Register`.
9. **A rejected profile may only resubmit after its company information has been edited** (`US-3.1.2-02 AC-08`) — the resubmit handler verifies an `UpdateCompanyInformation` happened after `LastAttemptUtc`.
10. **Verification can only begin once the account is activated** — `BeginAutomaticVerification` is illegal from `PendingActivation`.

### 6.3 Shortlist — behaviors

| Method | Rules | Domain event raised |
|---|---|---|
| `static Create(employerProfileId, name)` | `name` non-empty, ≤ 100 chars | — |
| `Rename(newName)` | `newName` non-empty, ≤ 100 chars | — |
| `AddCandidate(candidateUserId, matchScore?)` | no duplicate `CandidateUserId` in this shortlist | `CandidateSavedToTalentPool` *(integration)* |
| `RemoveCandidate(shortlistMemberId)` | member must exist | `CandidateRemovedFromTalentPool` *(internal)* |
| `Delete()` | marks the shortlist deleted (soft) | `ShortlistDeleted` *(internal)* |

**Shortlist invariants:** a candidate appears at most once per shortlist; `Name` is always non-empty; a deleted shortlist accepts no further mutations.

---

## 7. Domain services

Stateless. Live in `Domain`. Used when logic spans entities or needs a small external input.

### 7.1 `EmployerProfileCompletenessService`

```
Evaluate(profile: EmployerProfile) -> ProfileCompleteness
```

Pure. `Level1Complete` = all four L1 fields present (always true post-registration). `Level2Complete` = website, industry, company size, address, and description all present. Recomputed by the aggregate on every L2-affecting mutation. Used by the dashboard and to gate "begin verification" (per the story assumption that verification can run after L2).

### 7.2 `VerificationStateMachine`

```
EnsureTransitionAllowed(from: EmployerProfileStatus, to: EmployerProfileStatus) -> Result
EnsureVerificationOutcomeAllowed(from: VerificationOutcome, to: VerificationOutcome) -> Result
```

Pure. Centralises invariant #1 and the verification-outcome ordering. The `EmployerProfile` behaviors call it before mutating `Status` / `Verification.Outcome`, so the legal-transition tables live in exactly one place and are trivially unit-testable.

### 7.3 `UploadPolicyService`

```
ValidateLogo(file: FileReference, scan: VirusScanResult) -> Result
ValidateCompanyImage(file: FileReference, scan: VirusScanResult, currentImageCount: int) -> Result
ValidateSupplementaryDocument(file: FileReference, scan: VirusScanResult, currentDocCount: int) -> Result
```

Pure. Encapsulates the format/size/count/scan rules from §6.2 invariants 6–7 so the three upload behaviors share one consistent rule set. Returns `E-UPLOAD-INVALID-FORMAT`, `E-UPLOAD-SIZE-EXCEEDED`, `E-UPLOAD-LIMIT-EXCEEDED`, or `E-UPLOAD-VIRUS`.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module. **Integration events** (`IntegrationEvent`) cross the BC boundary — they go through the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract exactly. Other modules depend on these payloads — do not change a field without versioning.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

| Integration event | Raised when | Payload | Primary consumers |
|---|---|---|---|
| `EmployerRegisteredIntegrationEvent` | `Register` succeeds | `EmployerProfileId`, `UserId`, `CompanyName`, `At`, `OccurredOnUtc` | BC-4, BC-9, BC-10 |
| `EmployerProfileUpdatedIntegrationEvent` | profile fields / logo / images changed | `EmployerProfileId`, `ChangedFields` (`list<string>`), `At`, `OccurredOnUtc` | BC-7, BC-10 |
| `EmployerVerificationRequestedIntegrationEvent` | `BeginAutomaticVerification` | `EmployerProfileId`, `RegistryRef`, `At`, `OccurredOnUtc` | BC-8 (consumed by the `EmployerVerificationSaga`) |
| `EmployerVerifiedIntegrationEvent` | `RecordAutomaticVerificationPassed` or `ApproveManualVerification` | `EmployerProfileId`, `VerifiedAt`, `EvidenceRef`, `OccurredOnUtc` | BC-4, BC-9, BC-10 |
| `EmployerVerificationFailedIntegrationEvent` | `RejectManualVerification` | `EmployerProfileId`, `Reason`, `At`, `OccurredOnUtc` | BC-9, BC-10 |
| `EmployerManualVerificationRequiredIntegrationEvent` | `RecordAutomaticVerificationFailed` or `ResubmitForVerification` | `EmployerProfileId`, `Reason` (`"auto-verification-failed"` or `"resubmitted"`), `At`, `OccurredOnUtc` | BC-9 (notify MoL), BC-10 |
| `CandidateSavedToTalentPoolIntegrationEvent` | `Shortlist.AddCandidate` | `EmployerId` (the `UserId`), `JobSeekerId` (`CandidateUserId`), `PoolId` (`ShortlistId`), `At`, `OccurredOnUtc` | BC-3, BC-9, BC-10 |

> The [[Event_Catalog]] lists six BC-2 events: `EmployerRegistered`, `EmployerProfileUpdated`, `EmployerVerificationRequested`, `EmployerVerified`, `EmployerVerificationFailed`, `CandidateSavedToTalentPool`. This package adds one **internal-to-the-verification-flow** integration event, `EmployerManualVerificationRequired`, so the `EmployerVerificationSaga` and BC-9 know auto-verification fell back to manual. If the instructor prefers strict catalog conformance, fold it into `EmployerProfileUpdated` with a `changedFields: ["verificationStatus"]` — documented as a deliberate, defensible extension.

Consumers (for context only — you do not code them): BC-4 lets a verified employer publish and blocks an unverified or suspended one; BC-7 re-ranks candidates when employer config changes; BC-9 sends verification-result emails and notifies MoL; BC-10 consumes everything for the employer-activity dashboard; BC-3 notifies a job seeker they were saved to a talent pool.

### 8.2 Internal domain events (NOT published outside the module)

`EmployerProfileActivated`, `SupplementaryDocumentAdded`, `SupplementaryDocumentRemoved`, `EmployerProfileSuspended`, `EmployerProfileReinstated`, `EmployerProfileDeactivated`, `CandidateRemovedFromTalentPool`, `ShortlistDeleted`. Use these for in-module reactions (e.g. `EmployerProfileSuspended` → also refresh the dashboard read model). They never reach the outbox.

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained for its domain. (Outbox/inbox mechanics: [[00-Shared-Foundations]] §6.2–6.3.)

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent handlers)

| Integration event | From | Payload you receive | Your reaction |
|---|---|---|---|
| `UserAccountActivatedIntegrationEvent` | BC-1 IAM/UAM | `UserId`, `ActivatedAt` | Find profile by `UserId`; call `Activate()`. Idempotent. |
| `AccountDeactivatedIntegrationEvent` | BC-1 | `UserId`, `DeactivatedAt` | Find profile by `UserId`; call `Deactivate()`. |
| `UserAccountSuspendedIntegrationEvent` | BC-1 | `UserId`, `Reason` | Find profile by `UserId`; call `Suspend(reason)`. |
| `UserAccountReinstatedIntegrationEvent` | BC-1 | `UserId` | Find profile by `UserId`; call `Reinstate()`. |
| `EmployerVerifiedByGovernmentIntegrationEvent` | BC-8 External Job Sync (via the `EmployerVerificationSaga`) | `EmployerId` (the `EmployerProfileId` or `UserId` — see note), `Registry`, `At` | Find profile; call `RecordAutomaticVerificationPassed(evidenceRef)`. Idempotent. |
| `EmployerVerificationFailedByGovernmentIntegrationEvent` *(saga signal)* | BC-8 / `EmployerVerificationSaga` | `EmployerProfileId`, `Reason`, `At` | Call `RecordAutomaticVerificationFailed()` so the profile moves to `PendingManualVerification`. |
| `JobPostingPublishedIntegrationEvent` | BC-4 Job Postings | `PostingId`, `EmployerId`, `Title`, `At` | Increment the employer's dashboard "published postings" counter (read-model projection). Idempotent on `EventId`. |
| `JobPostingClosedIntegrationEvent` / `JobPostingExpiredIntegrationEvent` / `JobPostingSuspendedIntegrationEvent` | BC-4 | `PostingId`, `At` | Decrement "active postings" / move counters in the dashboard read model. |
| `ApplicationSubmittedIntegrationEvent` | BC-5 Job Application | `ApplicationId`, `JobSeekerId`, `PostingId`, `At` | Increment the employer's dashboard "total applications" counter; surface in the dashboard. |
| `CandidateRecommendationGeneratedIntegrationEvent` | BC-7 Recommendation Engine | `EmployerId`, `PostingId`, `JobSeekerIds` (`list<uuid>`), `At` | Update the dashboard "matched candidates" projection — store the candidate ids + match scores for display. |

> **Payload-key note.** The [[Event_Catalog]] payloads name the employer key inconsistently (`employerId` in BC-2's own events, also `employerId` in BC-4/BC-7 events). Within this module, treat `EmployerId` consistently as the **BC-1 `UserId`** on *inbound* events (BC-4/BC-5/BC-7 know the employer by their `UserId`), and resolve it to the local `EmployerProfileId` via `GetByUserId`. Outbound `CandidateSavedToTalentPool` follows the catalog and emits `EmployerId = UserId`.

**Idempotency** is mandatory — see [[00-Shared-Foundations]] §6.3 (inbox). Dashboard counters should be projected from an event log, not blindly incremented. Inbound events arrive via the module's integration-event subscription wired in the module composition entry point.

### 9.2 Public APIs this module CALLS (port interfaces — define in `Application`, implement adapters in `Infrastructure`)

```
Port: IdentityProvisioningApi   (provided by BC-1 IAM/UAM; called synchronously by the RegisterEmployer handler)
  ProvisionCredential(email: string, mobile: string, password: string, role: string)
      -> Result<ProvisionedIdentity>
  ProvisionedIdentity { UserId: uuid }
  // BC-1 enforces email/mobile uniqueness, password policy, breach check, hashing;
  // creates the account in PendingActivation; issues + sends an activation OTP.
  // Possible error codes returned (surface verbatim): E-REG-DUPLICATE, E-REG-INVALID-EMAIL,
  // E-REG-INVALID-MOBILE, E-REG-INVALID-PASSWORD, E-REG-PASSWORD-BREACHED, E-REG-RATE-LIMITED.

Port: VirusScanner              (external)
  Scan(file: FileReference) -> VirusScanResult

Port: ObjectStorage             (external; stores/retrieves file bytes; module keeps only FileReference)
  Store(content: bytes, fileName: string, mimeType: string) -> Result<FileReference>
  Retrieve(storageKey: string)  -> Result<bytes>
  Delete(storageKey: string)    -> void
```

> **Note on the `EmployerVerificationSaga`.** Verification is *orchestrated* by a separate `EmployerVerificationSaga` package (it calls BC-8 for the government lookup and BC-10 for audit). This module **does not call BC-8 directly** and does not hold a port for it. The interaction is purely event-based: this module emits `EmployerVerificationRequested`, the saga drives BC-8, and this module reacts to `EmployerVerifiedByGovernment` / `EmployerVerificationFailedByGovernment` (§9.1). If the saga package is not yet built, `Infrastructure` may provide a **stub saga adapter** that, on `EmployerVerificationRequested`, immediately emits a configurable pass/fail outcome event so this module runs standalone.

For the exercise, `Infrastructure` may provide **stub adapters** for `VirusScanner`, `ObjectStorage`, and `IdentityProvisioningApi` (in-memory or fake) so the module runs standalone. Keep the port shapes exactly as above so real adapters drop in later.

### 9.3 Public API this module EXPOSES (in the `Contracts` surface)

```
Public API: EmployerProfilePublicApi
  IsVerified(employerUserId: uuid)  -> bool                          // used by BC-4 to enforce the publish gate
  GetSummary(employerUserId: uuid)  -> EmployerProfileSummaryDto?

EmployerProfileSummaryDto {
  EmployerProfileId: uuid, UserId: uuid, CompanyName: string, Status: string,
  IsVerified: bool, LogoStorageKey: string?, Industry: string?
}
```

---

## 10. Application layer

Every use case is a **command** or a **query** with exactly one handler. Commands return `Result`/`Result<T>`; queries return DTOs. Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

### 10.1 Commands

| Command | Story | Handler responsibilities |
|---|---|---|
| `RegisterEmployerCommand` | US-3.1.2-01 | Validate L1 input → pre-check `company_identifier` uniqueness (`E-REG-DUPLICATE`) → call `IdentityProvisioningApi.ProvisionCredential(email, mobile, password, "Employer")` → on success `EmployerProfile.Register(userId, …)` → persist. Surface BC-1 error codes verbatim. |
| `CompleteEmployerLevel2Command` | US-3.1.2-01 AC-07 | Load profile by `UserId` → `CompleteLevel2(...)` → persist. |
| `RequestEmployerVerificationCommand` | US-3.1.2-02 AC-01 | Load profile → `BeginAutomaticVerification(registryRef)` → persist; emits `EmployerVerificationRequested`, which the `EmployerVerificationSaga` picks up. |
| `ApproveEmployerVerificationCommand` | US-3.1.2-02 AC-04 | Admin-only (MoL) → load profile → `ApproveManualVerification(adminId, evidenceRef)` → persist. |
| `RejectEmployerVerificationCommand` | US-3.1.2-02 AC-04 | Admin-only (MoL) → `reason` required → `RejectManualVerification(adminId, reason)` → persist. |
| `ResubmitEmployerVerificationCommand` | US-3.1.2-02 AC-08 | Load profile → verify company info was edited since `LastAttemptUtc` (else `E-VERIFY-NO-CHANGES`) → `ResubmitForVerification()` → persist. |
| `UpdateEmployerProfileCommand` | US-3.1.2-03 AC-08 | Load profile → `UpdateCompanyInformation(...)` → persist. |
| `UploadEmployerLogoCommand` | US-3.1.2-03 AC-01 | Validate format/size → `ObjectStorage.Store` → `VirusScanner.Scan` → `SetLogo(fileRef, scanResult)` → persist. On infection, delete the stored object and return `E-UPLOAD-VIRUS`. |
| `UploadCompanyImageCommand` | US-3.1.2-03 AC-02 | Validate format/size → store → scan → `AddCompanyImage(...)` (enforces max-5) → persist. |
| `RemoveCompanyImageCommand` | US-3.1.2-03 | Load → `RemoveCompanyImage(id)` → `ObjectStorage.Delete` → persist. |
| `UploadEmployerDocumentCommand` | US-3.1.2-03 AC-06 | Validate format (PDF/JPG/PNG) + size (≤10 MB) → store → scan → `AddSupplementaryDocument(...)` (enforces max-10) → persist. |
| `RemoveEmployerDocumentCommand` | US-3.1.2-03 AC-07 | Load → `RemoveSupplementaryDocument(id)` → `ObjectStorage.Delete` → persist. |
| `CreateShortlistCommand` | US-3.1.2-04 AC-04 | `Shortlist.Create(employerProfileId, name)` → persist. |
| `RenameShortlistCommand` | US-3.1.2-04 AC-05 | Load shortlist → `Rename(newName)` → persist. |
| `DeleteShortlistCommand` | US-3.1.2-04 AC-05 | Load shortlist → `Delete()` → persist. |
| `AddCandidateToShortlistCommand` | US-3.1.2-04 AC-04 | Load shortlist → `AddCandidate(candidateUserId, matchScore?)` → persist; emits `CandidateSavedToTalentPool`. |
| `RemoveCandidateFromShortlistCommand` | US-3.1.2-04 | Load shortlist → `RemoveCandidate(id)` → persist. |

### 10.2 Queries

| Query | Story | Returns |
|---|---|---|
| `GetMyEmployerProfileQuery` | US-3.1.2-03 | `EmployerProfileDto` (full profile + completeness + verification status + logo/images/documents) |
| `GetEmployerVerificationStatusQuery` | US-3.1.2-02 AC-07 | `VerificationStatusDto` (current status, outcome, rejection reason, next steps text) |
| `GetPublicEmployerProfileQuery` | US-3.1.2-03 AC-05 | `PublicEmployerProfileDto` by `EmployerProfileId` — company name, logo, description, images, verified badge; **404 if `Suspended`/`Deactivated`/`Rejected`** |
| `GetEmployerDashboardQuery` | US-3.1.2-04 AC-01/07 | `EmployerDashboardDto` — posting counts by status, total applications, matched-candidate count, shortlist count, all from the read-model projection |
| `GetEmployerJobPostingsQuery` | US-3.1.2-04 AC-02 | `list<DashboardPostingDto>` — projected from BC-4 events (posting id, title, status) |
| `GetMatchedCandidatesQuery` | US-3.1.2-04 AC-03 | `list<MatchedCandidateDto>` — projected from BC-7 `CandidateRecommendationGenerated` (candidate `UserId`, name placeholder, match score) |
| `GetShortlistsQuery` | US-3.1.2-04 AC-05 | `list<ShortlistDto>` (id, name, member count) |
| `GetShortlistQuery` | US-3.1.2-04 | `ShortlistDetailDto` (members with candidate id + match score + added date) |

### 10.3 Validators — representative rules

- `RegisterEmployerCommand`: company name non-empty ≤200; email RFC 5322 (`E-REG-INVALID-EMAIL`); mobile E.164 (`E-REG-INVALID-MOBILE`); company identifier non-empty alphanumeric; password presence only (policy/breach are BC-1's job).
- `CompleteEmployerLevel2Command`: website valid http/https URL when present; company size in enum; address required fields present; description ≤5000 chars.
- `UploadEmployerLogoCommand`: mime ∈ {image/png, image/jpeg}; size ≤ 5 MB.
- `UploadCompanyImageCommand`: mime ∈ {image/png, image/jpeg}; size ≤ 5 MB.
- `UploadEmployerDocumentCommand`: mime ∈ {application/pdf, image/png, image/jpeg}; size ≤ 10 MB.
- `RejectEmployerVerificationCommand`: `reason` non-empty.
- `CreateShortlistCommand` / `RenameShortlistCommand`: `name` non-empty ≤100.

### 10.4 DTOs

Plain data records in `Application`. Never expose Domain entities/VOs across the API. Map aggregate → DTO in the handler or a mapping helper. The dashboard DTOs are projected from the **read-model tables** (§11.1 `dashboard_*`), not by reaching into other modules.

---

## 11. Persistence & data model

Schema/namespace: `employer_profile`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. The module-specific relational model follows.

### 11.1 Reference relational model — schema `employer_profile`

```
TABLE employer_profiles
  id                    uuid          PK
  user_id               uuid          NOT NULL UNIQUE          -- BC-1 identity, no FK
  status                enum          NOT NULL                 -- PendingActivation|PendingVerification|PendingManualVerification|Verified|Rejected|Suspended|Deactivated
  company_name          string        NOT NULL
  email                 string        NOT NULL                 -- lower-cased
  mobile                string        NOT NULL
  company_identifier    string        NOT NULL UNIQUE          -- company ID / registration number
  website               string        NULL
  industry              string        NULL
  company_size          enum          NULL                     -- Micro|Small|Medium|Large
  address               json          NULL                     -- Address VO
  description           string        NULL
  logo                  json          NULL                     -- FileReference VO
  verification          json          NOT NULL                 -- VerificationState VO
  completeness          json          NOT NULL                 -- ProfileCompleteness VO
  status_before_suspend string        NULL                     -- to restore on Reinstate
  created_on_utc        datetime      NOT NULL
  updated_on_utc        datetime      NOT NULL
  version_token         (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  INDEX (user_id), INDEX (company_identifier), INDEX (status)

TABLE company_images
  id                    uuid          PK
  employer_profile_id   uuid          NOT NULL  FK -> employer_profiles.id ON DELETE CASCADE
  storage_key           string        NOT NULL
  original_file_name    string        NOT NULL
  mime_type             string        NOT NULL
  size_bytes            int64         NOT NULL
  scan_status           enum          NOT NULL
  scanned_on_utc        datetime      NULL
  uploaded_on_utc       datetime      NOT NULL

TABLE supplementary_documents
  id                    uuid          PK
  employer_profile_id   uuid          NOT NULL  FK -> employer_profiles.id ON DELETE CASCADE
  storage_key           string        NOT NULL
  original_file_name    string        NOT NULL
  mime_type             string        NOT NULL
  size_bytes            int64         NOT NULL
  kind                  enum          NOT NULL                 -- RegistrationCertificate|VatCertificate|Other
  scan_status           enum          NOT NULL
  scanned_on_utc        datetime      NULL
  uploaded_on_utc       datetime      NOT NULL

TABLE shortlists
  id                    uuid          PK
  employer_profile_id   uuid          NOT NULL  FK -> employer_profiles.id ON DELETE CASCADE
  name                  string        NOT NULL
  is_deleted            bool          NOT NULL DEFAULT false
  created_on_utc        datetime      NOT NULL
  updated_on_utc        datetime      NOT NULL
  INDEX (employer_profile_id) WHERE is_deleted = false

TABLE shortlist_members
  id                    uuid          PK
  shortlist_id          uuid          NOT NULL  FK -> shortlists.id ON DELETE CASCADE
  candidate_user_id     uuid          NOT NULL                 -- BC-1 identity, no FK
  match_score           int           NULL
  added_on_utc          datetime      NOT NULL
  UNIQUE (shortlist_id, candidate_user_id)

-- Dashboard read-model projections, fed by consumed integration events (§9.1). Idempotent on event_id.
TABLE dashboard_postings
  posting_id            uuid          PK                       -- BC-4 posting id
  employer_user_id      uuid          NOT NULL
  title                 string        NOT NULL
  status                string        NOT NULL                 -- mirrors BC-4 posting status
  last_event_on_utc     datetime      NOT NULL
  INDEX (employer_user_id)

TABLE dashboard_applications
  application_id        uuid          PK                       -- BC-5 application id
  employer_user_id      uuid          NOT NULL
  posting_id            uuid          NOT NULL
  job_seeker_id         uuid          NOT NULL
  submitted_on_utc      datetime      NOT NULL
  INDEX (employer_user_id)

TABLE dashboard_matched_candidates
  id                    uuid          PK
  employer_user_id      uuid          NOT NULL
  posting_id            uuid          NOT NULL
  candidate_user_id     uuid          NOT NULL
  match_score           int           NOT NULL
  generated_on_utc      datetime      NOT NULL
  UNIQUE (employer_user_id, posting_id, candidate_user_id)
  INDEX (employer_user_id)

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

### 11.2 Module-specific mapping notes

- Follows the persistence and mapping conventions in [[00-Shared-Foundations]] §3 and §6.
- Child collections (`company_images`, `supplementary_documents`) are **owned** by the `EmployerProfile` aggregate and loaded with it; `shortlist_members` are owned by `Shortlist`.
- Value objects map to `json` columns, **except** ones that need querying/uniqueness: `email`, `mobile`, `company_identifier`, `status`, `website`, `industry`, `company_size` are flattened to scalar columns.
- Optimistic-concurrency tokens are required on `employer_profiles` and `shortlists` (both can be updated concurrently).
- The `dashboard_*` tables are **read-model projections** — they are written *only* by integration-event consumers, never by command handlers, and are never the source of truth for anything outside this module's own dashboard queries.

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

`EmployerProfileRepository` (`GetById`, `GetByUserId`, `CompanyIdentifierExists`, `Add`, `Update`), `ShortlistRepository` (`GetById`, `GetByEmployerProfileId`, `Add`, `Update`), `DashboardProjectionStore` (`UpsertPosting`, `RemovePosting`, `AddApplication`, `UpsertMatchedCandidates`, `GetDashboard`, `GetPostings`, `GetMatchedCandidates`), `UnitOfWork` (`SaveChanges`).

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. Route prefix `/api/employers`. All endpoints except the public profile require a valid access token (issued by BC-1); the authenticated `UserId` is taken from the token. Verification approve/reject endpoints additionally require the `users:manage` (MoL Administrator) permission. Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

| Verb & route | Command/Query | Success | Notable failures |
|---|---|---|---|
| `POST /api/employers/register` | `RegisterEmployerCommand` | `201` + `EmployerProfileId` | `409 E-REG-DUPLICATE`, `400 E-REG-INVALID-*`, `422 E-REG-PASSWORD-BREACHED`, `429 E-REG-RATE-LIMITED` |
| `PUT /api/employers/me/level2` | `CompleteEmployerLevel2Command` | `200` | `400` invalid fields, `409` not yet activated |
| `GET /api/employers/me` | `GetMyEmployerProfileQuery` | `200` + `EmployerProfileDto` | `404` |
| `PUT /api/employers/me` | `UpdateEmployerProfileCommand` | `200` | `400`, `409` wrong status |
| `GET /api/employers/me/verification` | `GetEmployerVerificationStatusQuery` | `200` + `VerificationStatusDto` | `404` |
| `POST /api/employers/me/verification/request` | `RequestEmployerVerificationCommand` | `202` | `409` wrong status / L2 incomplete |
| `POST /api/employers/me/verification/resubmit` | `ResubmitEmployerVerificationCommand` | `202` | `409 E-VERIFY-NO-CHANGES`, `409` not rejected |
| `POST /api/employers/{id}/verification/approve` | `ApproveEmployerVerificationCommand` | `200` | `403`, `409` not pending manual |
| `POST /api/employers/{id}/verification/reject` | `RejectEmployerVerificationCommand` | `200` | `400` reason missing, `403`, `409` |
| `POST /api/employers/me/logo` | `UploadEmployerLogoCommand` (file upload) | `200` + logo ref | `400 E-UPLOAD-INVALID-FORMAT`, `413 E-UPLOAD-SIZE-EXCEEDED`, `422 E-UPLOAD-VIRUS` |
| `POST /api/employers/me/images` | `UploadCompanyImageCommand` (file upload) | `201` + image id | `400 E-UPLOAD-INVALID-FORMAT`, `413 E-UPLOAD-SIZE-EXCEEDED`, `409 E-UPLOAD-LIMIT-EXCEEDED`, `422 E-UPLOAD-VIRUS` |
| `DELETE /api/employers/me/images/{id}` | `RemoveCompanyImageCommand` | `204` | `404` |
| `POST /api/employers/me/documents` | `UploadEmployerDocumentCommand` (file upload) | `201` + document id | `400 E-UPLOAD-INVALID-FORMAT`, `413 E-UPLOAD-SIZE-EXCEEDED`, `409 E-UPLOAD-LIMIT-EXCEEDED`, `422 E-UPLOAD-VIRUS` |
| `DELETE /api/employers/me/documents/{id}` | `RemoveEmployerDocumentCommand` | `204` | `404` |
| `GET /api/employers/me/dashboard` | `GetEmployerDashboardQuery` | `200` + `EmployerDashboardDto` | `404` |
| `GET /api/employers/me/dashboard/postings` | `GetEmployerJobPostingsQuery` | `200` + postings | |
| `GET /api/employers/me/dashboard/matched-candidates` | `GetMatchedCandidatesQuery` | `200` + matched candidates | |
| `GET /api/employers/me/shortlists` | `GetShortlistsQuery` | `200` + shortlists | |
| `POST /api/employers/me/shortlists` | `CreateShortlistCommand` | `201` + `ShortlistId` | `400` name invalid |
| `GET /api/employers/me/shortlists/{id}` | `GetShortlistQuery` | `200` + `ShortlistDetailDto` | `404` |
| `PUT /api/employers/me/shortlists/{id}` | `RenameShortlistCommand` | `200` | `400`, `404` |
| `DELETE /api/employers/me/shortlists/{id}` | `DeleteShortlistCommand` | `204` | `404` |
| `POST /api/employers/me/shortlists/{id}/candidates` | `AddCandidateToShortlistCommand` | `201` | `404`, `409` duplicate candidate |
| `DELETE /api/employers/me/shortlists/{id}/candidates/{memberId}` | `RemoveCandidateFromShortlistCommand` | `204` | `404` |
| `GET /api/employers/{id}/public` *(anonymous)* | `GetPublicEmployerProfileQuery` | `200` + `PublicEmployerProfileDto` | `404` for `Suspended`/`Deactivated`/`Rejected` |

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** every VO factory — valid input succeeds; each invalid case returns the right `Error`. Specifically: `EmailAddress` (RFC 5322), `MobileNumber` (E.164 / +880), `CompanyIdentifier` (empty fails), `WebsiteUrl` (non-http fails), `CompanySize` (out-of-enum fails), `VerificationState` (`ManualRejected` without reason fails).
- **EmployerProfile aggregate:**
  - Status machine via `VerificationStateMachine`: every legal transition succeeds; every illegal one (`PendingActivation → Verified`, `PendingVerification → Rejected`, `Verified → PendingManualVerification`, etc.) returns failure.
  - `BeginAutomaticVerification` fails from `PendingActivation`; succeeds from `PendingVerification`.
  - `RecordAutomaticVerificationPassed` → `Status = Verified`; `RecordAutomaticVerificationFailed` → `Status = PendingManualVerification` and raises `EmployerManualVerificationRequired`.
  - `ApproveManualVerification` only from `PendingManualVerification`; `RejectManualVerification` requires a reason and sets it.
  - `ResubmitForVerification` only from `Rejected`.
  - Verified badge: a profile answers `IsVerified == true` iff `Status == Verified` (test each status).
  - 6th company image fails with `E-UPLOAD-LIMIT-EXCEEDED`; 11th document fails the same way; an `Infected` or `Pending` scan result is rejected with `E-UPLOAD-VIRUS`; a BMP/GIF logo fails with `E-UPLOAD-INVALID-FORMAT`; a 6 MB logo fails with `E-UPLOAD-SIZE-EXCEEDED`.
  - `UserId` is immutable after `Register`.
- **Shortlist aggregate:** adding the same `CandidateUserId` twice fails; rename rejects empty/over-long names; a deleted shortlist rejects further mutations.
- **Domain services:** `EmployerProfileCompletenessService` — table-driven cases proving `Level2Complete` flips only when all five L2 fields are present. `VerificationStateMachine` — exhaustive legal/illegal transition matrix. `UploadPolicyService` — format/size/count/scan rule matrix for logo, image, and document.

### 13.2 Unit tests — Application (handlers, ports replaced with test doubles)

- `RegisterEmployerCommand`: happy path pre-checks `company_identifier`, provisions identity via `IdentityProvisioningApi`, then creates the profile; when `IdentityProvisioningApi` returns `E-REG-DUPLICATE`, the handler returns that error and **no profile is persisted**; a duplicate `company_identifier` returns `E-REG-DUPLICATE` *before* BC-1 is called.
- `RequestEmployerVerificationCommand`: emits `EmployerVerificationRequested` to the outbox; fails with `409` if L2 incomplete or status wrong.
- `UploadEmployerLogoCommand`: clean PNG → stored + `SetLogo`; infected file → stored object deleted, `E-UPLOAD-VIRUS`, no logo set; oversized → `E-UPLOAD-SIZE-EXCEEDED`.
- `ResubmitEmployerVerificationCommand`: with no edits since `LastAttemptUtc` → `E-VERIFY-NO-CHANGES`; with edits → moves to `PendingManualVerification`.
- `AddCandidateToShortlistCommand`: emits `CandidateSavedToTalentPool` with `EmployerId = UserId`; duplicate candidate → `409`.
- Validation step: each validator rejects the documented bad inputs before the handler runs.

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip `EmployerProfile` with its owned `company_images` / `supplementary_documents` and `json` VOs; round-trip `Shortlist` with members; `CompanyIdentifierExists` / `GetByUserId` work; optimistic-concurrency conflict is detected.
- **Unique indexes:** a second profile with the same `company_identifier` fails at the DB level; a second profile with the same `user_id` fails.
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `employer_profile`.
- **Outbox:** a profile mutation writes both the row change and the outbox message in one transaction; rolling back leaves neither.
- **Inbox / idempotency:** delivering `UserAccountActivatedIntegrationEvent` twice activates the profile once; delivering `JobPostingPublishedIntegrationEvent` twice updates the `dashboard_postings` projection once.
- **Read-model projection:** consuming `JobPostingPublished` → `dashboard_postings` row appears; `JobPostingClosed` → status updated; `ApplicationSubmitted` → `dashboard_applications` row; `CandidateRecommendationGenerated` → `dashboard_matched_candidates` rows; `GetEmployerDashboardQuery` reflects the projected counts.
- **API:** host-level tests for the register → activate (via consumed event) → complete-L2 → request-verification → (stub saga emits pass) → `Verified` happy path; `GET /api/employers/{id}/public` returns `404` for a `Suspended` profile; a non-admin calling `verification/approve` gets `403`.
- **Consumed events:** `UserAccountSuspendedIntegrationEvent` moves the profile to `Suspended`; `EmployerVerifiedByGovernmentIntegrationEvent` moves it to `Verified` and emits `EmployerVerified`.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "Register an employer account"

End-to-end, to pattern-match every other command against. This mirrors the BC-3 JobSeeker Profile registration slice — the two profile BCs are deliberately symmetric.

1. **API.** `POST /api/employers/register` with body `{ companyName, email, mobile, companyIdentifier, password }`. The endpoint builds `RegisterEmployerCommand` and dispatches it through the mediator.
2. **Validation step.** `RegisterEmployerCommand`'s validator runs: company name non-empty ≤200, email RFC 5322, mobile E.164, company identifier non-empty alphanumeric, password present. On failure → `Result` with the matching `E-REG-INVALID-*` error, mapped to `400`.
3. **Handler.** `RegisterEmployerCommandHandler`:
   a. `EmployerProfileRepository.CompanyIdentifierExists(companyIdentifier)` → if true, return `E-REG-DUPLICATE` **before** calling BC-1.
   b. `IdentityProvisioningApi.ProvisionCredential(email, mobile, password, "Employer")` — BC-1 enforces email/mobile uniqueness, password policy, breach check, hashing, and triggers the activation OTP. On failure (`E-REG-DUPLICATE`, `E-REG-PASSWORD-BREACHED`, …) the handler returns that error verbatim and **persists nothing**.
   c. On success, BC-1 returns `ProvisionedIdentity { UserId }`.
   d. `EmployerProfile.Register(userId, companyName, email, mobile, companyIdentifier)` — creates the profile in `PendingActivation`, `Level1Complete = true`, raises `EmployerRegistered` (integration).
   e. `repository.Add(profile)`; `unitOfWork.SaveChanges()`.
4. **Domain-event / outbox step.** After the unit of work is saved, the mediator pipeline writes `EmployerRegisteredIntegrationEvent` into the outbox — same transaction. ([[00-Shared-Foundations]] §6.1–6.2.)
5. **Relay.** The background outbox relay publishes `EmployerRegistered`; BC-9 sends a welcome email, BC-10 records the registration, BC-4 notes the employer exists (but cannot publish until verified).
6. **Activation (separate flow).** The employer submits the OTP to BC-1. BC-1 activates the account and publishes `UserAccountActivated`. This module's consumer finds the profile by `UserId` and calls `Activate()`, moving it to `PendingVerification`.
7. **Response.** The handler returns `Result` success carrying the new `EmployerProfileId`; the endpoint returns `201`.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.1.2-01 Register employer account | L1 registration creates the profile in `PendingActivation` and triggers an SMS OTP (via BC-1) within 10 s; duplicate company ID/registration number → `E-REG-DUPLICATE`; duplicate email/mobile → `E-REG-DUPLICATE` (from BC-1); invalid mobile/email surfaced verbatim; on OTP success the account moves to activated and L2 becomes available; L2 saves company details; L2 supplementary documents (PDF/JPG/PNG ≤ 10 MB) scanned and stored; 6 registrations/IP/hour → `E-REG-RATE-LIMITED` (from BC-1). |
| US-3.1.2-02 Verify employer account | After registration, automatic verification is attempted (emits `EmployerVerificationRequested` → `EmployerVerificationSaga` → BC-8 government lookup); auto-pass → `Verified` + badge; auto-fail → `PendingManualVerification` + MoL notified; MoL can approve (`Verified`) or reject (`Rejected`); verified badge shows next to the company name iff `Verified`; unverified/rejected employers cannot post jobs or appear in search (BC-4 enforces, this BC exposes `IsVerified`); the employer sees their current verification status and next steps on the dashboard; a rejected employer can edit company info and resubmit. |
| US-3.1.2-03 Manage employer profile | Logo upload (PNG/JPG ≤ 5 MB), scanned and shown on the public profile; company images (PNG/JPG ≤ 5 MB, max 5) in a gallery; unsupported format → `E-UPLOAD-INVALID-FORMAT`; infected file → `E-UPLOAD-VIRUS`; public profile shows name/logo/description/openings/images; supplementary documents (PDF/JPG/PNG ≤ 10 MB, max 10) — upload, list, download, delete; editing company info is saved and reflected to search/recommendations within 60 s (via `EmployerProfileUpdated`). |
| US-3.1.2-04 Employer dashboard | Dashboard main view with key metrics and shortcuts; job-postings list with status (projected from BC-4 events); matched-candidates view with match score and name (projected from BC-7 events); create/select candidates → add to a named shortlist; view/rename/delete shortlists; click a matched candidate → their BC-3 public profile (if visibility allows); key metrics show posting count, total applications, matched-candidate count, shortlist count; metrics refresh within 10 s (read-model projection is near real-time); the dashboard shows only the logged-in employer's data. |

---

## Appendix — teaching notes & open questions

- **Two-file brief.** This package + `00-Shared-Foundations.md` together are the complete brief. The shared file carries everything stack-related and identical across all 12 BCs; this package carries only the domain design. Discuss with the class: which parts of a design are genuinely reusable across modules, and what is the cost of the one shared dependency?
- **The registration split.** §1's boundary note: this package calls BC-1 *synchronously* for provisioning (it needs the `UserId` back to build the `EmployerProfile`) and also lets BC-1's `UserRegistered` event flow to pure reactors. It is intentionally symmetric with the BC-3 JobSeeker Profile package — comparing the two side by side is a good lecture exercise on consistent cross-BC patterns.
- **Verification is a saga, not a method.** Verification spans BC-2, BC-8 (government lookup), and BC-10 (audit) — three contexts, so it is a *saga candidate* (`EmployerVerificationSaga`) and lives in its own package. This module only emits the trigger and reacts to the outcome. Discuss: why is this orchestrated (a saga) rather than pure choreography? (Answer: it needs explicit failure handling — auto-fail must deterministically fall back to manual review.)
- **Where does account-level approve/reject live?** BC-1's `US-3.1.4-01` also has "approve/reject pending employer." This package draws the line: BC-1 owns the *account* lifecycle (`PendingActivation → Active/Suspended`), BC-2 owns the *profile/verification* lifecycle (`PendingVerification → Verified/Rejected`). Two state machines, two BCs, linked by events. Students often want to merge them — discuss the cost.
- **Dashboard as a read model.** The dashboard reads from `dashboard_*` projection tables fed purely by integration events from BC-4/BC-5/BC-7. This module never queries those BCs synchronously. This is CQRS in microcosm — and a good place to ask: what is the staleness budget, and what happens if an event is lost? (Answer: the inbox + outbox patterns plus idempotent projections bound the risk.)
- **`EmployerManualVerificationRequired` — a catalog extension.** The [[Event_Catalog]] lists six BC-2 events; this package adds a seventh internal-to-the-verification-flow event so the saga and BC-9 know auto-verification fell back to manual. It is flagged in §8.1 as a deliberate, defensible extension — a good example of when a handover package legitimately refines the strategic artifacts, and when it should instead go back and update them.
- **Shortlist as its own aggregate.** A shortlist has an unbounded membership list and its own create/rename/delete lifecycle; folding it into `EmployerProfile` would bloat that aggregate and widen its consistency boundary. Contrast with `CompanyImage`, which *is* a child entity (bounded at 5, no independent lifecycle). Good discussion: what is the test for "deserves its own aggregate"?
