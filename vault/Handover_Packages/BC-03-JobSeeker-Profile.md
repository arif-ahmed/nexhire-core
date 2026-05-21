---
title: "Handover Package — BC-3 JobSeeker Profile"
type: handover-package
bc_id: BC-3
bc_name: JobSeeker Profile
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
  - bc/seeker-profile
---

# Handover Package — BC-3 JobSeeker Profile

> **Audience:** an AI coding agent. This package owns the **domain design** for the `JobSeekerProfile` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `JobSeekerProfile`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `job_seeker_profile`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

---

## 1. Purpose & scope boundaries

### What this BC is for

JobSeeker Profile owns the **job seeker's identity-as-a-candidate**: their profile data across three completeness levels, their uploaded resume and its parsed contents, supplementary documents, profile visibility / public sharing, and profile edit history. It is a **supporting** subdomain — it does not contain the platform's competitive core (that is the Recommendation Engine), but the quality of its data directly fuels matching.

### In scope

The `JobSeekerProfile` module is responsible for:

- The job seeker **registration journey** (orchestration only — see boundary note below) and profile lifecycle (`PendingActivation → Active → Deactivated → Active`).
- **Level 1** profile data (name, email, mobile, gender).
- **Level 2** profile data (education, work experience, skills, job preferences, addresses, optional salary).
- **Resume** upload, virus-scan orchestration, parse orchestration (calls an external parser), the review/confirm workflow, and merging selected parsed fields into the profile.
- **Supplementary documents** (certificates, portfolios, references) — upload, list, delete.
- **Profile completeness** scoring and recommendations.
- **Profile visibility** (private / recruiters-only / public) and **public sharing** (slug, public URL, QR code).
- **Profile edit history** with restore, and 12-month retention.
- Publishing the integration events in §8 and reacting to the integration events in §9.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong to other BCs and are reached via the contracts in §9:

- **Credentials, password hashing, password policy, breach checks, access tokens, sessions, login, MFA** → BC-1 IAM and UAM. This module calls BC-1's provisioning API; it never stores a password or issues a token.
- **One-time-passcode generation/validation, account-lock-after-failed-OTP** → BC-1. This module only reacts to the `UserAccountActivated` event.
- **The resume parsing algorithm / ML model itself** → external parser, reached via the `ResumeParser` port. This module orchestrates the call and stores the result; it does not implement parsing.
- **Virus scanning engine** → external, reached via the `VirusScanner` port.
- **File storage** (the actual file bytes) → external, reached via the `ObjectStorage` port. This module stores only file *references*.
- **The skill/occupation taxonomy** → BC-11 Administrators Configuration. This module references taxonomy codes and validates them via the `TaxonomyApi` port; it does not own the taxonomy.
- **Identity / education verification against government registries** → BC-8 External Job Synchronization. This module only reacts to `IdentityVerifiedByGovernment` and `EducationVerified` events and sets flags.
- **Job recommendations, embeddings, match scores** → BC-7 Recommendation Engine. This module emits `ResumeParsed` / `ProfileSkillsUpdated`; BC-7 reacts.
- **Searching for job seekers** → BC-6 Search & Discovery. This module emits `ProfileVisibilityChanged`; BC-6 maintains the index.
- **Sending notifications / emails / SMS** → BC-9 Notification. This module emits events; BC-9 decides what to send.
- **Blocking job applications when Level 2 is incomplete** → BC-5 Job Application. This module exposes `IsLevel2Complete`; BC-5 enforces the gate.

### Boundary note — the registration split (teaching point)

Registration (`US-3.1.1-01`) and activation (`US-3.1.1-02`) are *owned* by this BC as **journeys**, but credential mechanics belong to BC-1. This package models registration as a **synchronous call** to BC-1's `IdentityProvisioningApi` port (because the handler needs the `UserId` back in the same unit of work), then activation as an **event reaction** to BC-1's `UserAccountActivated`. The [[Event_Catalog]] lists `UserRegistered` as consumed by BC-3; we deliberately use the synchronous-provisioning model instead. Both are defensible — this is a good class discussion on sync-call vs. event-choreography for request/response needs.

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **Job Seeker** | A person seeking employment. Their platform identity (`UserId`) is owned by BC-1; their *candidate profile* is owned here. |
| **Profile** | The `JobSeekerProfile` aggregate — the root of this BC. |
| **Level 1 / L1** | Core registration fields: first name, last name, email, mobile, gender. All 6 required. |
| **Level 2 / L2** | Employability data: education, experience, skills, preferences, addresses, optional salary. "L2 complete" = at least one education **or** one experience entry. |
| **Level 3 / L3** | Supplementary documents. Optional. |
| **Completeness Score** | A 0–100 weighted percentage: L1 = 30%, L2 = 50%, L3 = 10%, resume present = 10%. |
| **Resume** | The uploaded CV document and its parse lifecycle. Separate aggregate. One active resume per profile. |
| **Parsed Field** | A value extracted from a resume by the external parser, carrying a `ConfidenceScore`. |
| **Confidence Score** | 0–100 integer from the parser. Below 70 = "needs verification". |
| **Supplementary Document** | A certificate / portfolio / reference file. Child entity of the profile (metadata only). Max 10. |
| **Profile Visibility** | `Private` (default), `RecruitersOnly`, or `Public`. |
| **Public Sharing** | An opt-in feature producing a public slug, URL `/p/{slug}`, and QR code. |
| **Slug** | `{firstname}-{lastname}-{4-char-hash}`, lowercased, profanity-filtered, unique. |
| **Profile Version** | An immutable snapshot in `ProfileHistory`, retained 12 months, restorable. |
| **Profile Status** | `PendingActivation`, `Active`, `Deactivated`. |
| **Verification Flag** | `IdentityVerified` / `EducationVerified` (set from BC-8 events), `SelfAttested` (set by the seeker). |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `JobSeekerProfile`.
- Public surface (`Contracts`): the integration events published in §8 + the public API in §9.3.
- **Module-specific notes:** none — this module has no background workers or scheduled jobs.

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has **three aggregates**: `JobSeekerProfile` (the root of the BC), `Resume`, and `ProfileHistory`. (Notation: see [[00-Shared-Foundations]] §2.)

### 5.1 Aggregate: JobSeekerProfile

**Aggregate root.** Identity: `JobSeekerProfileId` (strongly-typed id wrapping `uuid`).

| Member | Type | Notes |
|---|---|---|
| `Id` | `JobSeekerProfileId` | |
| `UserId` | `uuid` | Identity owned by BC-1. Set at registration. Immutable. |
| `Status` | `ProfileStatus` | enum: `PendingActivation`, `Active`, `Deactivated` |
| `Name` | `PersonName` | VO |
| `Email` | `EmailAddress` | VO |
| `Mobile` | `MobileNumber` | VO |
| `Gender` | `Gender` | VO/enum |
| `Education` | `list<EducationEntry>` | child entities |
| `Experience` | `list<ExperienceEntry>` | child entities |
| `Skills` | `list<ProfileSkill>` | child entities |
| `Preferences` | `JobPreferences?` | VO, nullable until set |
| `CurrentAddress` | `Address?` | VO |
| `PermanentAddress` | `Address?` | VO, optional |
| `RecentSalary` | `Money?` | VO, optional |
| `Documents` | `list<SupplementaryDocument>` | child entities (metadata only), max 10 |
| `Visibility` | `ProfileVisibility` | VO, default `Private` |
| `PublicSharing` | `PublicSharingSettings` | VO |
| `Verification` | `VerificationFlags` | VO |
| `HasActiveResume` | `bool` | set by the resume-merge flow; feeds completeness |
| `Completeness` | `CompletenessScore` | VO, recomputed on every mutation |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

**Child entities** (identity local to the aggregate; only mutated through the root):

- `EducationEntry` — `EducationEntryId`, `Degree`, `Institution`, `Period` (`DateRange`), `Gpa` (`decimal?`).
- `ExperienceEntry` — `ExperienceEntryId`, `Company`, `Role`, `Period` (`DateRange`), `IsCurrent` (`bool`), `Responsibilities` (`string`).
- `ProfileSkill` — `ProfileSkillId`, `CanonicalSkillRef` (VO — taxonomy code from BC-11), `RawLabel` (`string`), `Category` (`SkillCategory`: `Hard`/`Soft`), `Tier` (`SkillTier`: `Primary`/`Secondary`), `Proficiency` (`int` 1–5).
- `SupplementaryDocument` — `SupplementaryDocumentId`, `File` (`FileReference`), `Kind` (`DocumentKind`), `ScanResult` (`VirusScanResult`), `UploadedOnUtc`.

### 5.2 Aggregate: Resume

**Aggregate root.** Identity: `ResumeId`. Kept separate from `JobSeekerProfile` because it has its own multi-step lifecycle (`Uploaded → Scanning → Scanned → Parsing → Parsed | Failed`) that can fail independently, and because its parsed payload is large.

| Member | Type | Notes |
|---|---|---|
| `Id` | `ResumeId` | |
| `JobSeekerProfileId` | `JobSeekerProfileId` | owning profile |
| `File` | `FileReference` | VO |
| `ScanResult` | `VirusScanResult` | VO |
| `ParseStatus` | `ResumeParseStatus` | enum: `Uploaded`, `Scanning`, `Scanned`, `Parsing`, `Parsed`, `Failed` |
| `ParsedData` | `ParsedResumeData?` | VO — extracted education/experience/skills/personal, each with `ConfidenceScore` |
| `ParserName` | `string?` | which parser produced the result |
| `ParsedOnUtc` | `datetime?` | |
| `FailureReason` | `string?` | set when `ParseStatus = Failed` |
| `MergedFieldKeys` | `list<string>` | which parsed fields the seeker confirmed into the profile |
| `UploadedOnUtc` | `datetime` | |

### 5.3 Aggregate: ProfileHistory

**Aggregate root.** Identity: `ProfileHistoryId` (one per profile). Append-only log of `ProfileVersion` entities.

| Member | Type | Notes |
|---|---|---|
| `Id` | `ProfileHistoryId` | |
| `JobSeekerProfileId` | `JobSeekerProfileId` | |
| `Versions` | `list<ProfileVersion>` | append-only |

- `ProfileVersion` — `ProfileVersionId`, `SnapshotJson` (`string` — serialized profile state), `ChangedFields` (`list<string>`), `Action` (`HistoryAction`: `Edited`/`Restored`), `CreatedOnUtc`.

### 5.4 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `PersonName` | `First`, `Last` | both non-empty, ≤ 100 chars, trimmed |
| `EmailAddress` | `Value` | RFC 5322; lower-cased on store |
| `MobileNumber` | `Value` | E.164; default region `+880` (Bangladesh) — region configurable |
| `Gender` | `Value` | enum: `Male`, `Female`, `Other`, `PreferNotToSay` |
| `Address` | `Line1`, `Line2?`, `City`, `District`, `Postcode`, `Country` | `Line1`, `City`, `District`, `Country` required |
| `Money` | `Amount` (`decimal`), `Currency` | `Amount ≥ 0`; default currency `BDT` |
| `SalaryExpectation` | `Min` (`Money`), `Max` (`Money`) | same currency; `Min ≤ Max` |
| `JobPreferences` | `JobTypes`, `Industries`, `Locations`, `WorkArrangements` (set of `OnSite`/`Hybrid`/`Remote`), `SalaryExpectation?` | at least one job type |
| `DateRange` | `Start`, `End?` | `Start ≤ End` when `End` present |
| `CompletenessScore` | `Percentage` (`int` 0–100), `MissingSections` (`list<string>`) | 0 ≤ percentage ≤ 100 |
| `ProfileVisibility` | `Level` | enum: `Private`, `RecruitersOnly`, `Public` |
| `PublicSharingSettings` | `Enabled` (`bool`), `Slug?`, `QrCodeRef?` (`FileReference`) | `Slug` present iff `Enabled` |
| `VerificationFlags` | `IdentityVerified`, `EducationVerified`, `SelfAttested` (all `bool`) | |
| `FileReference` | `StorageKey`, `OriginalFileName`, `MimeType`, `SizeBytes` (`int64`) | size > 0 |
| `VirusScanResult` | `Status` (`Pending`/`Clean`/`Infected`), `ScannedOnUtc?` | |
| `ConfidenceScore` | `Value` (`int` 0–100) | `NeedsVerification` is true when `Value < 70` |
| `CanonicalSkillRef` | `TaxonomyCode`, `DisplayLabel` | code non-empty |
| `ParsedResumeData` | `Personal`, `Education[]`, `Experience[]`, `Skills[]` — each wrapped with `ConfidenceScore` | — |

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior lives on the aggregate roots. Handlers never set properties directly. Method shapes below are neutral specifications (see [[00-Shared-Foundations]] §2).

### 6.1 JobSeekerProfile — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Register(userId, name, email, mobile, gender)` | Creates profile in `PendingActivation`. All L1 fields valid. | `JobSeekerRegistered` |
| `Activate()` | Only from `PendingActivation`. → `Active`. Idempotent if already `Active`. Fails from `Deactivated`. | `ProfileActivated` |
| `Deactivate()` | Only from `Active`. → `Deactivated`. Clears `PublicSharing` (disabled). | `ProfileDeactivated` |
| `Reactivate()` | Only from `Deactivated`. → `Active`. | `ProfileReactivated` |
| `AddEducation(degree, institution, period, gpa)` | `period` valid `DateRange`. Recompute completeness. May flip `IsLevel2Complete`. | `ProfileLevel2Completed` *(only if L2 just became complete)*; always `ProfileCompletenessChanged` |
| `AddExperience(company, role, period, isCurrent, responsibilities)` | If `isCurrent` then `period.End` must be null. Recompute completeness. | as above |
| `AddSkill(canonicalRef, rawLabel, category, tier, proficiency)` | `proficiency` ∈ [1,5]. No duplicate `CanonicalSkillRef`. Recompute completeness. | `ProfileSkillsUpdated`; `ProfileCompletenessChanged` |
| `RemoveSkill(profileSkillId)` | Skill must exist. | `ProfileSkillsUpdated`; `ProfileCompletenessChanged` |
| `UpdateEducation` / `UpdateExperience` / `RemoveEducation` / `RemoveExperience` | entry must exist; same `DateRange` rules | `ProfileCompletenessChanged` |
| `SetPreferences(jobPreferences)` | valid `JobPreferences` | `ProfileCompletenessChanged` |
| `SetAddresses(current, permanent?)` | `current` required | `ProfileCompletenessChanged` |
| `SetRecentSalary(money?)` | optional; `Amount ≥ 0` | — |
| `AddSupplementaryDocument(file, kind, scanResult)` | **max 10 documents** (`E-UPLOAD-LIMIT-EXCEEDED`); `scanResult.Status` must be `Clean`. Recompute completeness (L3). | `SupplementaryDocumentUploaded`; `ProfileCompletenessChanged` |
| `RemoveSupplementaryDocument(documentId)` | must exist | `ProfileCompletenessChanged` |
| `SetVisibility(level)` | any → any. Default was `Private`. | `ProfileVisibilityChanged` |
| `EnablePublicSharing(slug, qrCodeRef)` | **profile must be 100% complete** (`E-SHARE-PROFILE-INCOMPLETE`); status `Active`. | `PublicSharingEnabled` |
| `DisablePublicSharing()` | clears slug + QR | `PublicSharingDisabled` |
| `RegeneratePublicSlug(newSlug, newQrCodeRef)` | sharing must be enabled; old slug invalidated | `PublicSharingSlugRegenerated` |
| `MarkResumeAttached()` / `MarkResumeDetached()` | sets `HasActiveResume`; recompute completeness (resume = 10%) | `ProfileCompletenessChanged` |
| `ApplyIdentityVerified()` | sets `Verification.IdentityVerified = true` | `ProfileVerificationChanged` *(internal)* |
| `ApplyEducationVerified(credentialRef)` | sets `Verification.EducationVerified = true` | `ProfileVerificationChanged` *(internal)* |
| `MarkSelfAttested()` | seeker attests profile accuracy (`US-3.3.1-04 AC-05`) | `ProfileVerificationChanged` *(internal)* |

### 6.2 Core invariants (must always hold)

1. **Status transitions** form a fixed machine: `PendingActivation → Active`; `Active ↔ Deactivated`. No other transition is legal. `PendingActivation → Deactivated` is forbidden.
2. **L1 completeness**: a profile always has all 6 L1 fields once `Register` succeeds (they are constructor arguments).
3. **L2 completeness** = `Education.Count > 0 || Experience.Count > 0`. Exposed as `IsLevel2Complete`.
4. **DateRange**: `Start ≤ End` whenever `End` is set. `ExperienceEntry.IsCurrent == true ⇒ Period.End == null`.
5. **Skill proficiency** ∈ [1, 5]. No two `ProfileSkill`s share a `CanonicalSkillRef`.
6. **Supplementary documents**: at most **10** per profile. Only `Clean`-scanned files may be added.
7. **One active resume per profile** (enforced across aggregates — see §7 `ResumeReplacementService`).
8. **Public sharing** can be enabled only when `Completeness.Percentage == 100` and `Status == Active`. `PublicSharingSettings.Slug` is non-null iff `Enabled`.
9. **Completeness** is recomputed (via the domain service in §7) on **every** mutation that affects it, and `ProfileCompletenessChanged` is raised when the percentage changes.
10. **Deactivated profile** must not be publicly shared — `Deactivate()` forces `PublicSharing.Enabled = false`.
11. `UserId` is immutable after `Register`.

### 6.3 Resume — behaviors

| Method | Rules | Domain event raised |
|---|---|---|
| `static Upload(profileId, file)` | `file.MimeType` ∈ {PDF, DOCX, TXT}; `SizeBytes ≤ 5 MB` (`E-UPLOAD-INVALID-FORMAT` / `E-UPLOAD-SIZE-EXCEEDED`). Status `Uploaded`. | `ResumeUploaded` |
| `RecordScanResult(virusScanResult)` | `Uploaded → Scanned`; if `Infected` → `Failed` with reason, raise quarantine event (`E-UPLOAD-VIRUS`) | `ResumeScanFailed` *(if infected)* |
| `BeginParsing()` | only from `Scanned`. → `Parsing`. | — |
| `RecordParseSuccess(parsedData, parserName)` | `Parsing → Parsed`. | `ResumeParsed` |
| `RecordParseFailure(reason)` | `Parsing → Failed`. Non-blocking — seeker falls back to manual entry. | `ResumeParseFailed` |
| `ConfirmMergedFields(fieldKeys)` | only from `Parsed`; records which fields were merged into the profile | `ResumeFieldsConfirmed` *(internal)* |

**Resume invariants:** parse-status machine is strictly ordered; `ParsedData` is non-null iff `ParseStatus == Parsed`; `FailureReason` non-null iff `ParseStatus == Failed`; parser timeout is 30 s (enforced by the Application handler, not the aggregate).

### 6.4 ProfileHistory — behaviors

| Method | Rules | Event |
|---|---|---|
| `static Start(profileId)` | creates empty history | — |
| `AppendEdit(snapshotJson, changedFields)` | appends a `ProfileVersion` with `Action = Edited` | — |
| `AppendRestore(snapshotJson, restoredFromVersionId)` | appends `ProfileVersion` with `Action = Restored` | `ProfileRestored` *(internal)* |
| `PurgeOlderThan(cutoffUtc)` | removes versions older than 12 months | — |

**Invariant:** `Versions` is append-only and immutable; never updated or selectively deleted except by `PurgeOlderThan`.

---

## 7. Domain services

Stateless. Live in `Domain`. Used when logic spans entities or needs a small external input.

### 7.1 `ProfileCompletenessCalculator`

```
Calculate(profile: JobSeekerProfile) -> CompletenessScore
```

Weighted formula (from `US-3.1.1-05`): **L1 = 30%, L2 = 50%, L3 = 10%, resume present = 10%**.

- L1 (30): all 6 fields present ⇒ full 30; profile always has these once registered.
- L2 (50): proportional across five parts — education present, experience present, ≥1 skill, preferences set, current address set — 10 points each.
- L3 (10): `Documents.Count > 0` ⇒ 10, else 0.
- Resume (10): `HasActiveResume` ⇒ 10, else 0.

Returns the percentage **and** the list of missing section names (for the recommendations UI). Called by the aggregate on every completeness-affecting mutation.

### 7.2 `PublicSlugGenerator`

```
Generate(name: PersonName, isSlugTaken: (string) -> bool) -> Result<string>
```

Produces `{first}-{last}-{4charhash}`, lowercased, non-alphanumeric stripped. Runs a profanity filter. Retries the hash on collision (caller supplies the `isSlugTaken` uniqueness check, backed by the repository). Fails with `E-SLUG-GENERATION` after N retries.

### 7.3 `ResumeToProfileMerger`

```
MergeSelectedFields(profile: JobSeekerProfile, parsed: ParsedResumeData, selectedFieldKeys: list<string>) -> Result
```

Applies only the seeker-selected parsed fields onto the profile by calling the profile's own `AddEducation` / `AddExperience` / `AddSkill` behaviors (so all invariants still run). Skill labels are mapped to `CanonicalSkillRef` — the *raw* skill text is kept in `RawLabel`; the canonical mapping is supplied by the caller via the `TaxonomyApi` port (see §9). Returns aggregated failures without aborting the whole merge (partial merge is allowed — mirrors `US-3.1.1-04 AC-07`).

### 7.4 `ResumeReplacementService`

```
Replace(existing: Resume?, incoming: Resume) -> Result
```

Enforces invariant #7 ("one active resume per profile"): marks the existing resume superseded so a new upload replaces the old. Invoked by the `UploadResume` handler since it spans two `Resume` aggregate instances.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module. **Integration events** (`IntegrationEvent`) cross the BC boundary — they go through the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract exactly. Other modules depend on these payloads — do not change a field without versioning.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

| Integration event | Raised when | Payload |
|---|---|---|
| `JobSeekerRegisteredIntegrationEvent` | `Register` succeeds | `JobSeekerProfileId`, `UserId`, `OccurredOnUtc` |
| `ProfileLevel2CompletedIntegrationEvent` | L2 just became complete | `JobSeekerProfileId`, `CompletenessPercentage`, `OccurredOnUtc` |
| `ResumeUploadedIntegrationEvent` | resume `Upload` succeeds | `JobSeekerProfileId`, `ResumeId`, `MimeType`, `OccurredOnUtc` |
| `ResumeParsedIntegrationEvent` | `RecordParseSuccess` | `JobSeekerProfileId`, `ResumeId`, `Skills` (`list of {taxonomyCode, label, confidence}`), `Education` (`list of {degree, institution, start, end}`), `Experience` (`list of {role, company, start, end}`), `OccurredOnUtc` |
| `ProfileSkillsUpdatedIntegrationEvent` | skills added/removed | `JobSeekerProfileId`, `AddedSkills` (`list<string>` taxonomy codes), `RemovedSkills` (`list<string>`), `OccurredOnUtc` |
| `ProfileVisibilityChangedIntegrationEvent` | `SetVisibility` | `JobSeekerProfileId`, `Visibility` (`string`), `OccurredOnUtc` |
| `SupplementaryDocumentUploadedIntegrationEvent` | `AddSupplementaryDocument` | `JobSeekerProfileId`, `SupplementaryDocumentId`, `Kind` (`string`), `OccurredOnUtc` |
| `ProfileCompletenessChangedIntegrationEvent` | completeness percentage changes | `JobSeekerProfileId`, `Score` (`int`), `OccurredOnUtc` |

Consumers (for context only — you do not code them): BC-7 Recommendation Engine consumes `ResumeParsed`, `ProfileSkillsUpdated`, `ProfileLevel2Completed`; BC-6 Search consumes `ProfileVisibilityChanged`; BC-9 Notification consumes `ProfileCompletenessChanged`; BC-10 Reporting consumes all.

### 8.2 Internal domain events (NOT published outside the module)

`ProfileActivated`, `ProfileDeactivated`, `ProfileReactivated`, `ProfileVerificationChanged`, `ResumeScanFailed`, `ResumeParseFailed`, `ResumeFieldsConfirmed`, `PublicSharingEnabled`, `PublicSharingDisabled`, `PublicSharingSlugRegenerated`, `ProfileRestored`. Use these for in-module reactions (e.g., `ProfileDeactivated` → also append a history entry). They never reach the outbox.

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained for its domain. (Outbox/inbox mechanics: [[00-Shared-Foundations]] §6.2–6.3.)

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent handlers)

| Integration event | From | Payload you receive | Your reaction |
|---|---|---|---|
| `UserAccountActivatedIntegrationEvent` | BC-1 IAM/UAM | `UserId`, `ActivatedOnUtc` | Find profile by `UserId`; call `Activate()`. Idempotent. |
| `AccountDeactivatedIntegrationEvent` | BC-1 | `UserId`, `DeactivatedOnUtc` | Find profile by `UserId`; call `Deactivate()`. |
| `UserAccountSuspendedIntegrationEvent` | BC-1 | `UserId`, `Reason` | Treat like `Deactivate()` (hide from search/recs). |
| `UserAccountReinstatedIntegrationEvent` | BC-1 | `UserId` | Call `Reactivate()`. |
| `IdentityVerifiedByGovernmentIntegrationEvent` | BC-8 External Job Sync | `UserId`, `Registry`, `VerifiedOnUtc` | Find profile by `UserId`; call `ApplyIdentityVerified()`. |
| `EducationVerifiedIntegrationEvent` | BC-8 | `JobSeekerProfileId`, `CredentialRef`, `VerifiedOnUtc` | Call `ApplyEducationVerified(credentialRef)`. |
| `TaxonomyUpdatedIntegrationEvent` | BC-11 Admin Config | `TaxonomyId`, `Version`, `ChangeSummary` | Re-validate stored `CanonicalSkillRef`s; flag any now-deprecated codes for the seeker to review. |

**Idempotency** is mandatory — see [[00-Shared-Foundations]] §6.3 (inbox). Inbound events arrive via the module's integration-event subscription wired in the module composition entry point.

### 9.2 Public APIs this module CALLS (port interfaces — define in `Application`, implement adapters in `Infrastructure`)

```
Port: IdentityProvisioningApi   (provided by BC-1 IAM/UAM; called synchronously by the RegisterJobSeeker handler)
  ProvisionCredential(email: string, mobile: string, password: string, role: string)
      -> Result<ProvisionedIdentity>
  ProvisionedIdentity { UserId: uuid }
  // BC-1 enforces email/mobile uniqueness, password policy, breach check, hashing; triggers OTP send.
  // Possible error codes returned (surface verbatim): E-REG-DUPLICATE, E-REG-INVALID-PASSWORD,
  // E-REG-PASSWORD-BREACHED, E-REG-INVALID-MOBILE, E-REG-INVALID-EMAIL, E-REG-RATE-LIMITED.

Port: TaxonomyApi               (provided by BC-11 Admin Config; canonical skill taxonomy lookup)
  MapSkill(rawSkillLabel: string)        -> Result<CanonicalSkillRef>
  IsValidSkillCode(taxonomyCode: string) -> bool

Port: ResumeParser              (external parser; orchestrated, not implemented, by this module)
  Parse(file: FileReference) -> Result<ParsedResumeData>
  // 30-second timeout enforced by the handler; on timeout treat as failure.

Port: VirusScanner              (external)
  Scan(file: FileReference) -> VirusScanResult

Port: ObjectStorage             (external; stores/retrieves file bytes; module keeps only FileReference)
  Store(content: bytes, fileName: string, mimeType: string) -> Result<FileReference>
  Retrieve(storageKey: string)  -> Result<bytes>
  Delete(storageKey: string)    -> void

Port: QrCodeGenerator           (QR code generation for public sharing)
  Generate(publicUrl: string) -> Result<FileReference>
  // PNG, error-correction level M, >= 512x512. Returned reference is stored via ObjectStorage.
```

For the exercise, `Infrastructure` may provide **stub adapters** for `ResumeParser`, `VirusScanner`, `ObjectStorage`, `QrCodeGenerator`, and `IdentityProvisioningApi` / `TaxonomyApi` (in-memory or fake) so the module runs standalone. Keep the port shapes exactly as above so real adapters drop in later.

### 9.3 Public API this module EXPOSES (in the `Contracts` surface)

```
Public API: JobSeekerProfilePublicApi
  IsLevel2Complete(jobSeekerProfileId: uuid) -> bool                       // used by BC-5
  GetSummary(jobSeekerProfileId: uuid)       -> JobSeekerProfileSummaryDto?

JobSeekerProfileSummaryDto {
  JobSeekerProfileId: uuid, UserId: uuid, FullName: string, Status: string,
  CompletenessPercentage: int, Visibility: string, IdentityVerified: bool
}
```

---

## 10. Application layer

Every use case is a **command** or a **query** with exactly one handler. Commands return `Result`/`Result<T>`; queries return DTOs. Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

### 10.1 Commands

| Command | Story | Handler responsibilities |
|---|---|---|
| `RegisterJobSeekerCommand` | US-3.1.1-01 | Validate L1 input → call `IdentityProvisioningApi.ProvisionCredential` → on success `JobSeekerProfile.Register(userId,…)` → `ProfileHistory.Start` → persist. Surface BC-1 error codes verbatim. |
| `AddEducationEntryCommand` | US-3.1.1-03 | Load profile → `AddEducation(...)` → append history → persist. |
| `AddExperienceEntryCommand` | US-3.1.1-03 | Load profile → `AddExperience(...)` → append history → persist. |
| `AddSkillCommand` | US-3.1.1-03 | Map raw label via `TaxonomyApi.MapSkill` → `AddSkill(...)` → persist. |
| `RemoveSkillCommand` | US-3.1.1-03 | Load → `RemoveSkill` → persist. |
| `UpdateEducationEntryCommand` / `UpdateExperienceEntryCommand` / `RemoveEducationEntryCommand` / `RemoveExperienceEntryCommand` | US-3.1.1-03 | Load → mutate via root → persist. |
| `SetJobPreferencesCommand` | US-3.1.1-03 | Load → `SetPreferences` → persist. |
| `SetAddressesCommand` | US-3.1.1-03 | Load → `SetAddresses` → persist. |
| `SetRecentSalaryCommand` | US-3.1.1-03 | Load → `SetRecentSalary` → persist. |
| `UploadResumeCommand` | US-3.1.1-04 | Validate format/size → `ObjectStorage.Store` → `Resume.Upload` → `VirusScanner.Scan` → `RecordScanResult` → if clean `BeginParsing` + `ResumeParser.Parse` (30 s timeout) → `RecordParseSuccess`/`RecordParseFailure` → `ResumeReplacementService.Replace` against any existing resume → persist. |
| `ConfirmParsedResumeFieldsCommand` | US-3.1.1-04 / US-3.3.1-04 | Load resume + profile → `ResumeToProfileMerger.MergeSelectedFields` → `resume.ConfirmMergedFields` → `profile.MarkResumeAttached` → append history → persist. |
| `MarkProfileSelfAttestedCommand` | US-3.3.1-04 | Load → `MarkSelfAttested` → persist. |
| `UploadSupplementaryDocumentCommand` | US-3.1.1-06 | Validate format (PDF/PNG/JPG) + size (≤10 MB) → store → scan → `AddSupplementaryDocument` (enforces max-10) → persist. |
| `DeleteSupplementaryDocumentCommand` | US-3.1.1-06 | Load → `RemoveSupplementaryDocument` → `ObjectStorage.Delete` → persist. |
| `SetProfileVisibilityCommand` | US-3.1.1-07 | Load → `SetVisibility` → persist. |
| `EnablePublicSharingCommand` | US-3.1.1-07 | Check completeness → `PublicSlugGenerator.Generate` → `QrCodeGenerator.Generate` → `EnablePublicSharing` → persist. |
| `DisablePublicSharingCommand` | US-3.1.1-07 | Load → `DisablePublicSharing` → persist. |
| `RegeneratePublicSlugCommand` | US-3.1.1-07 | Generate new slug + QR → `RegeneratePublicSlug` → persist (old slug now 404s). |
| `RestoreProfileVersionCommand` | US-3.1.1-08 | Load history + profile → deserialize chosen `ProfileVersion` snapshot → apply to profile → `history.AppendRestore` → persist. |

### 10.2 Queries

| Query | Story | Returns |
|---|---|---|
| `GetMyProfileQuery` | US-3.1.1-03/05 | `JobSeekerProfileDto` (full profile + completeness + missing sections) |
| `GetProfileCompletenessQuery` | US-3.1.1-05 | `CompletenessDto` (percentage, missing sections, contextual recommendations) |
| `GetResumeParseStatusQuery` | US-3.1.1-04 | `ResumeParseStatusDto` (status, parsed fields with confidence scores, low-confidence flags) |
| `GetEditHistoryQuery` | US-3.1.1-08 | `list<ProfileVersionDto>` (timestamp, action, changed fields) |
| `GetPublicProfileQuery` | US-3.1.1-07 | `PublicProfileDto` by slug — **404 if slug unknown, sharing disabled, or profile deactivated; never leak PII** |

### 10.3 Validators — representative rules

- `RegisterJobSeekerCommand`: first/last name non-empty ≤100; email RFC 5322 (`E-REG-INVALID-EMAIL`); mobile E.164 (`E-REG-INVALID-MOBILE`); gender in enum; password presence only (policy/breach are BC-1's job).
- `AddEducationEntryCommand`: degree + institution non-empty; `start ≤ end`.
- `AddExperienceEntryCommand`: company + role non-empty; `isCurrent ⇒ end == null`; else `start ≤ end`.
- `AddSkillCommand`: proficiency ∈ [1,5]; category/tier in enums; raw label non-empty.
- `UploadResumeCommand`: mime ∈ {PDF, DOCX, TXT}; size ≤ 5 MB.
- `UploadSupplementaryDocumentCommand`: mime ∈ {PDF, PNG, JPG}; size ≤ 10 MB.

### 10.4 DTOs

Plain data records in `Application`. Never expose Domain entities/VOs across the API. Map aggregate → DTO in the handler or a mapping helper.

---

## 11. Persistence & data model

Schema/namespace: `job_seeker_profile`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. The module-specific relational model follows.

### 11.1 Reference relational model — schema `job_seeker_profile`

```
TABLE job_seeker_profiles
  id                  uuid        PK
  user_id             uuid        NOT NULL, UNIQUE        -- BC-1 identity, no FK
  status              enum        NOT NULL                -- PendingActivation|Active|Deactivated
  first_name          string      NOT NULL
  last_name           string      NOT NULL
  email               string      NOT NULL                -- lower-cased
  mobile              string      NOT NULL
  gender              enum        NOT NULL
  preferences         json        NULL                    -- JobPreferences VO
  current_address     json        NULL                    -- Address VO
  permanent_address   json        NULL
  recent_salary       json        NULL                    -- Money VO
  visibility          enum        NOT NULL DEFAULT 'Private'
  public_sharing      json        NOT NULL                -- PublicSharingSettings VO
  public_slug         string      NULL, UNIQUE            -- denormalised from public_sharing for fast lookup
  verification        json        NOT NULL                -- VerificationFlags VO
  has_active_resume   bool        NOT NULL DEFAULT false
  completeness        json        NOT NULL                -- CompletenessScore VO
  created_on_utc      datetime    NOT NULL
  updated_on_utc      datetime    NOT NULL
  version_token       (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  INDEX (user_id), INDEX (public_slug), INDEX (status)

TABLE education_entries
  id, profile_id (FK → job_seeker_profiles.id, ON DELETE CASCADE),
  degree string, institution string, period_start date, period_end date NULL, gpa decimal NULL

TABLE experience_entries
  id, profile_id (FK → job_seeker_profiles.id, ON DELETE CASCADE),
  company string, role string, period_start date, period_end date NULL,
  is_current bool NOT NULL, responsibilities string

TABLE profile_skills
  id, profile_id (FK → job_seeker_profiles.id, ON DELETE CASCADE),
  taxonomy_code string NOT NULL, display_label string NOT NULL, raw_label string NOT NULL,
  category enum NOT NULL, tier enum NOT NULL, proficiency int NOT NULL
  UNIQUE (profile_id, taxonomy_code)

TABLE supplementary_documents
  id, profile_id (FK → job_seeker_profiles.id, ON DELETE CASCADE),
  storage_key string, original_file_name string, mime_type string, size_bytes int64,
  kind enum NOT NULL, scan_status enum NOT NULL, scanned_on_utc datetime NULL,
  uploaded_on_utc datetime NOT NULL

TABLE resumes
  id                  uuid        PK
  profile_id          uuid        NOT NULL                -- FK → job_seeker_profiles.id
  storage_key         string      NOT NULL
  original_file_name  string      NOT NULL
  mime_type           string      NOT NULL
  size_bytes          int64       NOT NULL
  scan_status         enum        NOT NULL
  scanned_on_utc      datetime    NULL
  parse_status        enum        NOT NULL                -- Uploaded|Scanning|Scanned|Parsing|Parsed|Failed
  parsed_data         json        NULL                    -- ParsedResumeData VO (with confidence scores)
  parser_name         string      NULL
  parsed_on_utc       datetime    NULL
  failure_reason      string      NULL
  merged_field_keys   json        NOT NULL DEFAULT '[]'
  is_superseded       bool        NOT NULL DEFAULT false   -- supports "one active resume"
  uploaded_on_utc     datetime    NOT NULL
  version_token       (optimistic-concurrency token)
  INDEX (profile_id) WHERE is_superseded = false

TABLE profile_histories
  id                  uuid        PK
  profile_id          uuid        NOT NULL, UNIQUE

TABLE profile_versions
  id, history_id (FK → profile_histories.id, ON DELETE CASCADE),
  snapshot_json json NOT NULL, changed_fields json NOT NULL,
  action enum NOT NULL, created_on_utc datetime NOT NULL
  INDEX (history_id, created_on_utc)

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

### 11.2 Module-specific mapping notes

- Child collections (`education_entries`, `experience_entries`, `profile_skills`, `supplementary_documents`) are **owned** by the `JobSeekerProfile` aggregate and loaded with it.
- `email`, `mobile`, `visibility`, and `public_slug` are flattened to scalar columns (not left inside `json`) because they are queried / uniquely constrained.
- Optimistic-concurrency tokens are required on `job_seeker_profiles` and `resumes` (both can be updated concurrently).

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

`JobSeekerProfileRepository` (`GetById`, `GetByUserId`, `GetBySlug`, `Add`, `Update`, `IsSlugTaken`), `ResumeRepository` (`GetById`, `GetActiveByProfileId`, `Add`, `Update`), `ProfileHistoryRepository` (`GetByProfileId`, `Add`, `Update`), `UnitOfWork` (`SaveChanges`).

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. Route prefix `/api/job-seekers`. All endpoints except the public profile require a valid access token (issued by BC-1); the authenticated `UserId` is taken from the token. Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

| Verb & route | Command/Query | Success | Notable failures |
|---|---|---|---|
| `POST /api/job-seekers/register` | `RegisterJobSeekerCommand` | `201` + `JobSeekerProfileId` | `409 E-REG-DUPLICATE`, `400 E-REG-INVALID-*`, `422 E-REG-PASSWORD-BREACHED`, `429 E-REG-RATE-LIMITED` |
| `GET /api/job-seekers/me` | `GetMyProfileQuery` | `200` + `JobSeekerProfileDto` | `404` |
| `GET /api/job-seekers/me/completeness` | `GetProfileCompletenessQuery` | `200` + `CompletenessDto` | |
| `POST /api/job-seekers/me/education` | `AddEducationEntryCommand` | `201` | `400` invalid date range |
| `PUT /api/job-seekers/me/education/{id}` | `UpdateEducationEntryCommand` | `200` | `404` |
| `DELETE /api/job-seekers/me/education/{id}` | `RemoveEducationEntryCommand` | `204` | `404` |
| `POST /api/job-seekers/me/experience` | `AddExperienceEntryCommand` | `201` | `400` `isCurrent` + end date |
| `PUT` / `DELETE .../experience/{id}` | update/remove experience | `200`/`204` | `404` |
| `POST /api/job-seekers/me/skills` | `AddSkillCommand` | `201` | `400` proficiency range, `409` duplicate skill |
| `DELETE /api/job-seekers/me/skills/{id}` | `RemoveSkillCommand` | `204` | `404` |
| `PUT /api/job-seekers/me/preferences` | `SetJobPreferencesCommand` | `200` | `400` |
| `PUT /api/job-seekers/me/addresses` | `SetAddressesCommand` | `200` | `400` |
| `PUT /api/job-seekers/me/salary` | `SetRecentSalaryCommand` | `200` | |
| `POST /api/job-seekers/me/resume` | `UploadResumeCommand` (file upload) | `202` + `ResumeId` | `400 E-UPLOAD-INVALID-FORMAT`, `413 E-UPLOAD-SIZE-EXCEEDED`, `422 E-UPLOAD-VIRUS` |
| `GET /api/job-seekers/me/resume/status` | `GetResumeParseStatusQuery` | `200` + `ResumeParseStatusDto` | `404` |
| `POST /api/job-seekers/me/resume/confirm` | `ConfirmParsedResumeFieldsCommand` | `200` | `404`, `409` resume not parsed |
| `POST /api/job-seekers/me/resume/attest` | `MarkProfileSelfAttestedCommand` | `200` | |
| `POST /api/job-seekers/me/documents` | `UploadSupplementaryDocumentCommand` (file upload) | `201` + id | `400 E-UPLOAD-INVALID-FORMAT`, `413 E-UPLOAD-SIZE-EXCEEDED`, `409 E-UPLOAD-LIMIT-EXCEEDED`, `422 E-UPLOAD-VIRUS` |
| `DELETE /api/job-seekers/me/documents/{id}` | `DeleteSupplementaryDocumentCommand` | `204` | `404` |
| `PUT /api/job-seekers/me/visibility` | `SetProfileVisibilityCommand` | `200` | `400` |
| `POST /api/job-seekers/me/public-sharing` | `EnablePublicSharingCommand` | `200` + URL + QR ref | `409 E-SHARE-PROFILE-INCOMPLETE` |
| `DELETE /api/job-seekers/me/public-sharing` | `DisablePublicSharingCommand` | `204` | |
| `POST /api/job-seekers/me/public-sharing/regenerate` | `RegeneratePublicSlugCommand` | `200` + new URL | `409` sharing disabled |
| `GET /api/job-seekers/me/history` | `GetEditHistoryQuery` | `200` + versions | |
| `POST /api/job-seekers/me/history/{versionId}/restore` | `RestoreProfileVersionCommand` | `200` | `404` |
| `GET /p/{slug}` *(anonymous)* | `GetPublicProfileQuery` | `200` + `PublicProfileDto` | `404` for unknown/disabled/deactivated — generic page, no PII |

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** every VO factory — valid input succeeds; each invalid case returns the right `Error`. Specifically: `EmailAddress` (RFC 5322), `MobileNumber` (E.164 / +880), `DateRange` (`start > end` fails; open-ended ok), `Money` (negative fails), `SalaryExpectation` (`min > max` fails, currency mismatch fails), `CompletenessScore` (out-of-range fails), `ConfidenceScore` (`NeedsVerification` boundary at 70).
- **JobSeekerProfile aggregate:**
  - Status machine: every legal transition succeeds; every illegal one (`PendingActivation → Deactivated`, `Deactivated → PendingActivation`, etc.) returns failure.
  - `AddSkill` proficiency 0 and 6 fail; 1 and 5 pass; duplicate `CanonicalSkillRef` fails.
  - `AddExperience` with `isCurrent = true` and a non-null end date fails.
  - L2 completeness flips to true exactly when the first education **or** experience is added — assert `ProfileLevel2CompletedIntegrationEvent` raised once, not twice.
  - 11th supplementary document fails with `E-UPLOAD-LIMIT-EXCEEDED`; infected scan result is rejected.
  - `EnablePublicSharing` fails at 99% completeness, succeeds at 100%.
  - `Deactivate()` forces `PublicSharing.Enabled = false`.
  - Every completeness-affecting mutation raises `ProfileCompletenessChanged` only when the percentage actually changes.
- **Resume aggregate:** parse-status machine ordering; `RecordParseSuccess` only from `Parsing`; `ParsedData` non-null iff `Parsed`; infected scan → `Failed`.
- **ProfileHistory:** append-only; `PurgeOlderThan` removes only versions past the cutoff.
- **Domain services:** `ProfileCompletenessCalculator` — table-driven cases proving the 30/50/10/10 weighting (empty profile = 30; +education = 40; full L2 = 80; +docs = 90; +resume = 100). `PublicSlugGenerator` — format, lowercasing, profanity rejection, collision retry. `ResumeToProfileMerger` — only selected fields merged; partial-merge aggregates errors without aborting.

### 13.2 Unit tests — Application (handlers, ports replaced with test doubles)

- `RegisterJobSeekerCommand`: happy path provisions identity then creates profile + history; when `IdentityProvisioningApi` returns `E-REG-DUPLICATE`, the handler returns that error and **no profile is persisted**.
- `UploadResumeCommand`: clean file → stored, scanned, parsed, `ResumeParsed` queued to outbox; parser timeout (30 s) → `RecordParseFailure`, flow not blocked, seeker can still proceed; infected file → `E-UPLOAD-VIRUS`, quarantined, no `Resume` left in `Parsed`.
- `ConfirmParsedResumeFieldsCommand`: only selected field keys merged; `MarkResumeAttached` called; completeness rises by the resume weight.
- `EnablePublicSharingCommand`: incomplete profile → `E-SHARE-PROFILE-INCOMPLETE`; complete → slug + QR generated.
- `RestoreProfileVersionCommand`: profile state matches the chosen snapshot; a new `Restored` history entry is appended.
- Validation step: each validator rejects the documented bad inputs before the handler runs.

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip each aggregate including child collections and `json` VOs; optimistic-concurrency conflict is detected; `IsSlugTaken` / `GetBySlug` / `GetByUserId` work.
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `job_seeker_profile`.
- **Outbox:** a profile mutation writes both the row change and the outbox message in one transaction; rolling back the transaction leaves neither.
- **Inbox / idempotency:** delivering `UserAccountActivatedIntegrationEvent` twice activates the profile once and is a no-op the second time.
- **API:** host-level tests for the register → add-education → upload-resume → enable-sharing happy path; `GET /p/{slug}` returns `404` (no PII) for a deactivated profile and for an unknown slug.
- **Consumed events:** `AccountDeactivatedIntegrationEvent` transitions the profile to `Deactivated` and disables public sharing; `TaxonomyUpdatedIntegrationEvent` flags deprecated skill codes.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "Add an education entry"

End-to-end, to pattern-match every other command against:

1. **API.** `POST /api/job-seekers/me/education` with body `{ degree, institution, startDate, endDate, gpa }`. The endpoint reads `UserId` from the access token, builds `AddEducationEntryCommand { UserId, Degree, Institution, Start, End, Gpa }`, dispatches it through the mediator.
2. **Validation step.** `AddEducationEntryCommand`'s validator runs: degree/institution non-empty, `start ≤ end`. On failure → `Result` with `Error`, mapped to `400`.
3. **Handler.** `AddEducationEntryCommandHandler`:
   a. `JobSeekerProfileRepository.GetByUserId(userId)` → `JobSeekerProfile` (with child collections). `404` if missing.
   b. Build `DateRange.Create(start, end)` → `Result<DateRange>`; propagate failure.
   c. `profile.AddEducation(degree, institution, dateRange, gpa)` — the aggregate enforces invariants, recomputes completeness via `ProfileCompletenessCalculator`, raises `ProfileCompletenessChanged`, and — if L2 just became complete — `ProfileLevel2Completed`.
   d. `profileHistory.AppendEdit(snapshotJson, changedFields: ["education"])`.
   e. `repository.Update(profile)`; `unitOfWork.SaveChanges()`.
4. **Domain-event / outbox step.** After the unit of work is saved, the mediator pipeline dispatches internal domain events in-process and writes `ProfileCompletenessChangedIntegrationEvent` (and `ProfileLevel2CompletedIntegrationEvent` if applicable) into the outbox — same transaction. ([[00-Shared-Foundations]] §6.1–6.2.)
5. **Relay.** The background outbox relay publishes the integration events; BC-7 and BC-10 receive them.
6. **Response.** Handler returns `Result` success; the endpoint returns `201`.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.1.1-01 Self-register | Profile created in `PendingActivation`; duplicate email/mobile → `E-REG-DUPLICATE` (from BC-1); invalid email/mobile/password surfaced; rate-limit + breach errors surfaced verbatim from BC-1. |
| US-3.1.1-02 Activate via OTP | On `UserAccountActivatedIntegrationEvent`, profile → `Active`. (OTP mechanics are BC-1's; this module only reacts.) |
| US-3.1.1-03 Complete L2 | Education/experience/skill/preferences/address add+edit+remove; `DateRange` and `isCurrent` rules; proficiency 1–5; `IsLevel2Complete` exposed to BC-5. |
| US-3.1.1-04 Upload & parse resume | Format/size limits; virus scan → quarantine on infection; parser 30 s timeout → graceful non-blocking failure; review screen data; selective field confirm; resume stored as `kind=resume`, one active per profile. |
| US-3.1.1-05 Profile completeness | 0–100 score with 30/50/10/10 weighting; recomputed on every change; missing-section recommendations; milestone events at 50/75/100 (via `ProfileCompletenessChanged`). |
| US-3.1.1-06 Supplementary docs | PDF/PNG/JPG ≤10 MB; virus scan; **max 10**; list with filename/size/date; delete with confirmation. |
| US-3.1.1-07 Visibility & sharing | Private default; visibility levels; slug `{first}-{last}-{4hash}`; public URL `/p/{slug}`; QR PNG ≥512×512 ECC-M; regenerate slug → old URL 404s; disable → 404; deactivate hides profile. |
| US-3.1.1-08 Edit history | Chronological versions with changed fields; 12-month retention + purge; restore previous version → creates a new `Restored` history entry. |
| US-3.3.1-03 Parse & extract | Parser returns structured education/experience/skills/personal; each parsed field carries a `ConfidenceScore`; skills mapped to canonical taxonomy via `TaxonomyApi`. |
| US-3.3.1-04 Review & correct | Low-confidence (<70) fields flagged; editing parsed fields re-raises `ProfileSkillsUpdated`/`ResumeParsed` for BC-7; `MarkSelfAttested` records the attestation. |

---

## Appendix — teaching notes & open questions

- **Two-file brief.** This package + `00-Shared-Foundations.md` together are the complete brief. The shared file carries everything stack-related and identical across all 12 BCs; this package carries only the domain design. Discuss with the class: which parts of a design are genuinely reusable across modules, and what is the cost of the one shared dependency (the packages are no longer 100% standalone)?
- **Aggregate boundary debate.** `SupplementaryDocument` is modelled as a *child entity* of `JobSeekerProfile` (so the "max 10" invariant is enforced naturally by the root) but stores only file *metadata*. `Resume` is a *separate* aggregate (its own lifecycle, large payload). Ask the class: when does a child entity deserve to be its own aggregate?
- **Sync vs. async registration.** §1's boundary note: this package calls BC-1 synchronously for provisioning rather than reacting to `UserRegistered`. Discuss the trade-off (immediate `UserId` vs. looser coupling).
- **`ResumeParsed` ownership.** This BC owns resume parse *orchestration*; BC-7 owns *matching*. The event payload is the contract. Discuss whether parsing should instead live in BC-7.
- **Cross-aggregate invariant.** "One active resume per profile" spans two `Resume` instances, handled by `ResumeReplacementService` + an `is_superseded` flag rather than a single aggregate. Discuss eventual vs. transactional consistency here.
- **Localization.** Story assumptions specify Bangladesh `+880` / `BDT`; `US-3.3.1-03` mentions Arabic/English content. Defaults here follow the `US-3.1.1` stories and are configurable.
