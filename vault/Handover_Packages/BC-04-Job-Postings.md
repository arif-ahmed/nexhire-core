---
title: "Handover Package — BC-4 Job Postings"
type: handover-package
bc_id: BC-4
bc_name: Job Postings
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
  - bc/job-posting
---

# Handover Package — BC-4 Job Postings

> **Audience:** an AI coding agent. This package owns the **domain design** for the `JobPostings` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `JobPostings`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `job_postings`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

---

## 1. Purpose & scope boundaries

### What this BC is for

Job Postings owns the **job posting as a published artifact**: its structured content, its Schema.org-standardised representation, its visibility, its lifecycle status machine, and the immutable audit trail of every status change and edit. It is a **supporting** subdomain — a job board needs postings, but the platform's competitive edge is the matching engine (BC-7), not the posting CRUD.

### In scope

The `JobPostings` module is responsible for:

- **Creating** a job posting from a structured form (title, summary, required skills, contract type, education level, application deadline, work format, location, required languages, optional job link).
- **Standardising** posting data to the **Schema.org JobPosting** vocabulary, and rejecting non-compliant data.
- **Editing** posting details and **extending** application deadlines.
- **Visibility** management — `Public`, `Private`, `Targeted` (with targeting criteria).
- The posting **status lifecycle**: `Draft → Active ⇄ Paused → Expired → Archived`, plus admin-driven `Suspended`/`Reinstated` and terminal `Removed`.
- **Automatic expiration** when the application deadline passes.
- **Renewal** of expired postings (creates a *new* posting aggregate; the original is archived).
- An **immutable audit trail** of every status change and field edit — who, when, from→to — retained indefinitely and exportable (CSV/PDF).
- **Admin moderation** (MoL Administrator) — suspend, remove, restore postings with logged reasons; admin listing/filtering/search and per-posting metrics view.
- Publishing the integration events in §8 and reacting to the integration events in §9.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong to other BCs and are reached via the contracts in §9:

- **The skill / occupation / industry taxonomy** → BC-11 Administrators Configuration. This module references taxonomy codes and validates/canonicalises raw skill labels via the `TaxonomyApi` port; it does not own the taxonomy.
- **Employer accounts, employer verification, employer profiles** → BC-2 Employer Profile Management. This module keeps a small local `EmployerStanding` projection (fed by BC-2/BC-1 events) so it can decide whether an employer may publish — it never stores the employer profile itself.
- **Credentials, access tokens, sessions, the recruiter's identity** → BC-1 IAM and UAM. This module stores `PostedByUserId` / `EmployerId` as plain `uuid` references.
- **Searching and indexing postings** → BC-6 Search & Discovery. This module emits posting events; BC-6 maintains the search index. This module never serves the public job-search query.
- **The AI matching / recommendation of postings to seekers** → BC-7 Recommendation Engine. This module emits `JobPostingPublished` / `JobPostingUpdated`; BC-7 computes embeddings and matches.
- **Job applications, application counts, bookmark counts** → BC-5 Job Application. This module does **not** count applications; the "application count" shown on the admin metrics view is read from a local projection fed by BC-5 events (or shown as "via Reporting").
- **Ingesting postings from external job portals** → BC-8 External Job Synchronization. This module *consumes* `ExternalJobIngested` and creates/updates a mirrored posting, but it does not talk to external portals.
- **Sending notifications** (expiration reminders, suspension notices) → BC-9 Notification. This module emits events; BC-9 decides what to send.
- **Analytics, dashboards, employment statistics** → BC-10 Reporting. This module emits events; BC-10 projects them.

### Boundary note — admin moderation is not a separate BC (teaching point)

`US-3.1.4-03` (MoL Administrator moderates postings) is *owned* by this BC, not by a back-office BC. Suspend / remove / restore are simply **admin-initiated status transitions** that run through the same status machine as employer actions — the difference is only the **actor** (`AuditActor.Kind = Admin`) and a mandatory **reason**. There is no separate `Moderation` aggregate. Discuss in class: when does an "admin" capability deserve its own BC versus being a privileged path into an existing aggregate?

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **Job Posting** | The `JobPosting` aggregate — a single advertised job opportunity. The root of this BC. |
| **Recruiter** | The employer user who creates/edits a posting. Identity (`PostedByUserId`) owned by BC-1; employer (`EmployerId`) owned by BC-2. |
| **Draft** | A created-but-not-published posting. Not searchable. |
| **Active** | A published, searchable posting accepting applications. |
| **Paused** | An employer-held posting — temporarily not searchable, not deleted, resumable. |
| **Expired** | A posting whose application deadline has passed. Set **automatically only**. |
| **Suspended** | A posting hidden by **admin** moderation; applications blocked; reversible via Reinstate. |
| **Removed** | A posting permanently ended by **admin** moderation. Terminal. |
| **Archived** | A posting soft-deleted by the **employer** (from Expired). Terminal. Data retained. |
| **Renewal** | Re-publishing an expired posting — creates a **new** `JobPosting` (new id); the original moves to Archived. |
| **Required Skill** | A skill a posting demands, mapped to a `CanonicalSkillRef` from BC-11's taxonomy. |
| **Schema.org JobPosting** | The normative standardised representation the posting is validated/mapped against. |
| **Visibility** | `Public`, `Private`, or `Targeted` (the latter carries `TargetingCriteria`). |
| **Posting Audit Trail** | The append-only, immutable log of every status change and field edit for one posting. Separate aggregate. |
| **Audit Entry** | One immutable record in the audit trail: actor, timestamp, kind (`StatusChange`/`FieldEdit`), details. |
| **Audit Actor** | Who caused a change: `Employer`, `Admin`, or `System` (automatic expiration). |
| **Employer Standing** | A local read-model projection: is this employer verified and active, i.e. allowed to publish? |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `JobPostings`.
- Public surface (`Contracts`): the integration events published in §8 + the public API in §9.3.
- **Module-specific notes:** this module runs a **posting-expiration background service** (`PostingExpirationBackgroundService`) on an interval (e.g. every 5 min). It scans for postings whose deadline has passed and dispatches `ProcessExpiredPostingsCommand` (§10). It is registered by the module composition entry point alongside the handlers, repositories, and port adapters.

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has **two aggregates**: `JobPosting` (the root of the BC) and `PostingAuditTrail` (one per posting, append-only). It also maintains one **read-model projection**, `EmployerStanding`, which is not an aggregate. (Notation: see [[00-Shared-Foundations]] §2.)

### 5.1 Aggregate: JobPosting

**Aggregate root.** Identity: `JobPostingId` (strongly-typed id wrapping `uuid`).

| Member | Type | Notes |
|---|---|---|
| `Id` | `JobPostingId` | |
| `EmployerId` | `uuid` | owned by BC-2. Set at creation. Immutable. |
| `PostedByUserId` | `uuid` | the recruiter; owned by BC-1. Immutable. |
| `Status` | `PostingStatus` | enum: `Draft`, `Active`, `Paused`, `Expired`, `Suspended`, `Reinstated`*, `Removed`, `Archived` (see §6.2 — `Reinstated` is transient, normalised to `Active`) |
| `Title` | `JobTitle` | VO |
| `Summary` | `JobSummary` | VO |
| `ContractType` | `ContractType` | VO/enum |
| `EducationLevel` | `EducationLevel` | VO/enum |
| `WorkFormat` | `WorkFormat` | VO/enum: `Physical`, `Online`, `Hybrid` |
| `Location` | `EmploymentLocation?` | VO — **required iff** `WorkFormat == Physical` |
| `RequiredSkills` | `list<RequiredSkill>` | VO collection |
| `RequiredLanguages` | `list<LanguageRequirement>` | VO collection |
| `SalaryRange` | `SalaryRange?` | VO, optional |
| `Deadline` | `ApplicationDeadline` | VO — date + `AutoCloseEnabled` |
| `JobLink` | `JobPostingLink?` | VO, optional external URL |
| `Visibility` | `PostingVisibility` | VO — level + optional `TargetingCriteria` |
| `SchemaOrg` | `SchemaOrgJobPosting?` | VO — the standardised representation, populated on publish |
| `RenewedFromPostingId` | `JobPostingId?` | set when this posting was created via renewal |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |
| `PublishedOnUtc` | `datetime?` | |

There are no child *entities* — `RequiredSkill` and `LanguageRequirement` are value objects (replaced wholesale on edit; no independent identity or lifecycle).

### 5.2 Aggregate: PostingAuditTrail

**Aggregate root.** Identity: `PostingAuditTrailId`. One per `JobPosting`. Append-only log of `AuditEntry` entities.

Modelled as a **separate aggregate** (not child entities of `JobPosting`) because: it is append-only and immutable; it is **retained indefinitely and must survive the posting's archival/removal**; and it is queried and exported independently of the posting. This mirrors the exemplar BC-3's `ProfileHistory` decision.

| Member | Type | Notes |
|---|---|---|
| `Id` | `PostingAuditTrailId` | |
| `JobPostingId` | `JobPostingId` | the posting this trail belongs to |
| `Entries` | `list<AuditEntry>` | append-only |

- `AuditEntry` (entity) — `AuditEntryId`, `Kind` (`AuditEntryKind`: `StatusChange` / `FieldEdit`), `Actor` (`AuditActor` VO), `StatusTransition` (`StatusTransition?` VO — set when `Kind == StatusChange`), `ChangedFields` (`list<string>` — set when `Kind == FieldEdit`), `Reason` (`string?` — required for admin Suspend/Remove), `OccurredOnUtc` (`datetime`, second precision, UTC).

### 5.3 Projection (not an aggregate): EmployerStanding

A lightweight read model this module maintains from inbound events so it can answer "may this employer publish?" without a synchronous call.

| Member | Type | Notes |
|---|---|---|
| `EmployerId` | `uuid` | key |
| `IsVerified` | `bool` | from `EmployerVerified` / `EmployerVerificationFailed` |
| `IsActive` | `bool` | from `AccountDeactivated` / `UserAccountSuspended` / `UserAccountReinstated` |
| `UpdatedOnUtc` | `datetime` | |

### 5.4 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `JobTitle` | `Value` | non-empty, trimmed, 3–150 chars |
| `JobSummary` | `Value` | non-empty, trimmed, 20–5000 chars |
| `ContractType` | `Value` | enum: `FullTime`, `PartTime`, `Training`, `ProjectBased` |
| `EducationLevel` | `Value` | enum: `None`, `Secondary`, `Diploma`, `Bachelor`, `Master`, `Doctorate` |
| `WorkFormat` | `Value` | enum: `Physical`, `Online`, `Hybrid` |
| `EmploymentLocation` | `Line1`, `City`, `District`, `Country` | all required when present |
| `RequiredSkill` | `CanonicalRef` (`CanonicalSkillRef`), `RawLabel`, `Importance` (`Mandatory`/`Preferred`) | raw label non-empty |
| `CanonicalSkillRef` | `TaxonomyCode`, `DisplayLabel` | code non-empty |
| `LanguageRequirement` | `Language`, `Proficiency` (1–5 or CEFR-style enum) | language non-empty |
| `SalaryRange` | `Min` (`decimal`), `Max` (`decimal`), `Currency`, `Period` (`Monthly`/`Yearly`) | `Min ≤ Max`; amounts ≥ 0; default currency `BDT` |
| `ApplicationDeadline` | `DateUtc`, `AutoCloseEnabled` | `DateUtc` must be in the future at creation/publish; see §6.2 invariant 3 |
| `JobPostingLink` | `Url` | valid absolute HTTPS URL |
| `PostingVisibility` | `Level` (`Public`/`Private`/`Targeted`), `TargetingCriteria?` | `TargetingCriteria` non-null & non-empty **iff** `Level == Targeted` |
| `TargetingCriteria` | `SkillCodes`, `Locations`, `SeekerGroupIds` | at least one criterion non-empty |
| `SchemaOrgJobPosting` | `Properties` (`map<string,string>`), `IsCompliant` (`bool`), `Violations` (`list<string>`) | produced by `SchemaOrgStandardizer` — see §7 |
| `StatusTransition` | `From` (`PostingStatus`), `To` (`PostingStatus`) | must be a legal transition (§6.2) |
| `AuditActor` | `Kind` (`Employer`/`Admin`/`System`), `UserId` (`uuid?`), `DisplayName` | `UserId` required unless `Kind == System` |

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior lives on the aggregate roots. Handlers never set properties directly. Method shapes below are neutral specifications (see [[00-Shared-Foundations]] §2). Every status-changing or field-editing behavior also returns enough information for the Application layer to append the matching `AuditEntry` to the `PostingAuditTrail` (see §10 and §14).

### 6.1 JobPosting — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static CreateDraft(employerId, postedByUserId, title, summary, contractType, educationLevel, workFormat, location, requiredSkills, requiredLanguages, deadline, jobLink, salaryRange, visibility)` | All required fields valid; `WorkFormat==Physical ⇒ location != null`; `Visibility==Targeted ⇒ criteria present`; `deadline` in the future. Status `Draft`. | `JobPostingDrafted` |
| `Publish(schemaOrg, employerStanding)` | Legal only from `Draft`. `employerStanding.IsVerified && IsActive` else `E-POST-EMPLOYER-NOT-ELIGIBLE`. `schemaOrg.IsCompliant` else `E-POST-NOT-SCHEMA-COMPLIANT`. Deadline must be in the future else `E-POST-DEADLINE-IN-PAST`. Sets `SchemaOrg`, `PublishedOnUtc`. → `Active`. | `JobPostingPublished` |
| `EditDetails(...editable fields...)` | Allowed only in non-terminal status (`Draft`, `Active`, `Paused` — **not** `Expired`/`Suspended`/`Archived`/`Removed`). Re-validates all VO invariants. If `Active`, re-runs Schema.org compliance and updates `SchemaOrg`. Records which fields changed. | `JobPostingFieldsEdited` |
| `ExtendDeadline(newDeadlineUtc)` | New deadline must be **strictly later** than the current one (`E-POST-DEADLINE-NOT-LATER`). Allowed in `Draft`/`Active`/`Paused`. | `JobPostingDeadlineExtended` |
| `SetVisibility(visibility)` | `Targeted ⇒ criteria present`. Allowed in non-terminal status. Effective immediately. | `JobPostingVisibilityChanged` |
| `Pause()` | Only from `Active`. → `Paused`. | `JobPostingPaused` |
| `Resume()` | Only from `Paused`. → `Active`. | `JobPostingResumed` |
| `Expire()` | Only from `Active` or `Paused`. → `Expired`. **System actor only** — invoked by the expiration background service when `Deadline.DateUtc` has passed. | `JobPostingExpired` |
| `Archive()` | Only from `Expired`. → `Archived` (terminal). **Employer** actor. | `JobPostingArchived` |
| `Suspend(adminActor, reason)` | Only from `Active` or `Paused`. → `Suspended`. `reason` required (`E-POST-REASON-REQUIRED`). **Admin** actor. | `JobPostingSuspended` |
| `Reinstate(adminActor)` | Only from `Suspended`. → `Active`. **Admin** actor. | `JobPostingReinstated` |
| `Remove(adminActor, reason)` | From any non-terminal status. → `Removed` (terminal). `reason` required. **Admin** actor. | `JobPostingRemoved` |
| `static RenewFrom(expiredPosting, newDeadlineUtc, edits?)` | `expiredPosting.Status` must be `Expired`. Produces a **new** `JobPosting` (new id) in `Draft`, copying details, `RenewedFromPostingId = expiredPosting.Id`, fresh deadline. Caller then `Archive()`s the original. | `JobPostingRenewed` (on the new posting) |

### 6.2 Core invariants (must always hold)

1. **Status machine** — only these transitions are legal:
   `Draft → Active` (Publish) · `Draft → Removed` (admin) ·
   `Active ⇄ Paused` (Pause/Resume) · `Active → Expired` · `Paused → Expired` ·
   `Active → Suspended` / `Paused → Suspended` (admin) · `Suspended → Active` (admin Reinstate) ·
   `Expired → Archived` (employer) · `{Active,Paused,Expired,Suspended} → Removed` (admin).
   `Archived` and `Removed` are **terminal**. Any other transition returns `E-POST-ILLEGAL-TRANSITION`.
2. **Publish requires Schema.org compliance** — a `Draft` cannot become `Active` unless `SchemaOrgStandardizer` reports `IsCompliant == true` (all required Schema.org JobPosting properties present and valid).
3. **Deadline is in the future** at creation and at publish. If a `Draft` is published after its deadline has already passed, publish fails with `E-POST-DEADLINE-IN-PAST`.
4. **Physical work format requires a location** — `WorkFormat == Physical ⇒ Location != null`.
5. **Targeted visibility requires criteria** — `Visibility.Level == Targeted ⇒ TargetingCriteria` present and non-empty.
6. **Required skills reference valid taxonomy codes** — every `RequiredSkill.CanonicalRef.TaxonomyCode` is validated via the `TaxonomyApi` port (in the Application layer) before the posting can be published.
7. **Editing is forbidden in terminal/locked statuses** — no field edits in `Expired`, `Suspended`, `Archived`, `Removed`.
8. **Deadline can only move later** via `ExtendDeadline`. Shortening is not allowed (renewal sets a fresh deadline on a new posting).
9. **Expiration is automatic and System-only** — there is no manual "mark expired" command; only the expiration background service may call `Expire()`, with `AuditActor.Kind == System`.
10. **Renewal creates a new aggregate** — `RenewFrom` never mutates the expired posting's identity; it returns a new `JobPosting`. The original is then `Archived`.
11. **`EmployerId` and `PostedByUserId` are immutable** after creation.
12. **Audit completeness** — every successful status change and field edit MUST result in exactly one `AuditEntry` appended to the posting's `PostingAuditTrail`, in the same transaction (enforced by the Application layer — see §10/§14).

### 6.3 PostingAuditTrail — behaviors

| Method | Rules | Event |
|---|---|---|
| `static Start(jobPostingId)` | creates an empty trail for a posting | — |
| `RecordStatusChange(actor, transition, reason?)` | appends an `AuditEntry` with `Kind = StatusChange`; `reason` required when `transition.To ∈ {Suspended, Removed}` | — |
| `RecordFieldEdit(actor, changedFields)` | appends an `AuditEntry` with `Kind = FieldEdit`; `changedFields` non-empty | — |

**Invariants:** `Entries` is **append-only and immutable** — no method updates or deletes an existing entry. The trail is **never** physically deleted, even when its posting is `Archived` or `Removed`. Timestamps are UTC at second precision.

---

## 7. Domain services

Stateless. Live in `Domain`.

### 7.1 `SchemaOrgStandardizer`

```
Standardize(posting: JobPosting) -> SchemaOrgJobPosting
```

Validates the posting's fields against the **Schema.org JobPosting** vocabulary and maps them to canonical properties: `title` → `title`, `summary` → `description`, `contractType` → `employmentType`, `educationLevel` → `educationRequirements`, `salaryRange` → `baseSalary`, `location` → `jobLocation`, `requiredSkills` → `skills`, `deadline` → `validThrough`, etc. Returns a `SchemaOrgJobPosting` VO carrying the mapped `Properties`, an `IsCompliant` flag, and a list of `Violations` (missing/invalid required properties). Fields that do not map to Schema.org are preserved as custom extension properties (prefixed `x-`). Called by the `PublishJobPostingCommand` handler and re-run by `EditDetails` when the posting is `Active`.

### 7.2 `JobPostingRenewalService`

```
Renew(expiredPosting: JobPosting, newDeadlineUtc: datetime, edits: PostingEdits?) -> Result<JobPosting>
```

Wraps `JobPosting.RenewFrom`: verifies the source posting is `Expired`, applies any optional `edits` (a small DTO of overridable fields), produces the new `Draft` posting with `RenewedFromPostingId` set. Does **not** archive the original — the Application handler does that in the same unit of work, so the pair (`original → Archived`, `new → Draft`) commits atomically.

### 7.3 `PostingExpirationPolicy`

```
ShouldExpire(posting: JobPosting, nowUtc: datetime) -> bool
IsApproachingExpiry(posting: JobPosting, nowUtc: datetime, threshold: duration) -> bool
```

`ShouldExpire` is true when `posting.Status ∈ {Active, Paused}` and `posting.Deadline.DateUtc <= nowUtc`. `IsApproachingExpiry` is true when the deadline is within `threshold` (default 7 days, configurable) — used to emit the "approaching expiration" signal so BC-9 can remind the employer. Consumed by the `PostingExpirationBackgroundService`.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module (chiefly to keep the `PostingAuditTrail` in sync — though the Application layer also does this explicitly; pick one mechanism and be consistent — this package uses the **explicit Application-layer approach**, see §10). **Integration events** (`IntegrationEvent`) cross the BC boundary via the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

| Integration event | Raised when | Payload |
|---|---|---|
| `JobPostingCreatedIntegrationEvent` | `CreateDraft` succeeds | `JobPostingId`, `EmployerId`, `OccurredOnUtc` |
| `JobPostingPublishedIntegrationEvent` | `Publish` succeeds | `JobPostingId`, `EmployerId`, `Title`, `ContractType`, `WorkFormat`, `RequiredSkillCodes` (`list<string>`), `Location` (`{city,district,country}?`), `DeadlineUtc`, `Visibility` (`string`), `OccurredOnUtc` |
| `JobPostingUpdatedIntegrationEvent` | `EditDetails` while `Active` | `JobPostingId`, `ChangedFields` (`list<string>`), `RequiredSkillCodes` (`list<string>`), `OccurredOnUtc` |
| `JobPostingExpiredIntegrationEvent` | `Expire` | `JobPostingId`, `EmployerId`, `ExpiredOnUtc` |
| `JobPostingClosedIntegrationEvent` | `Archive` (employer ends the posting) | `JobPostingId`, `EmployerId`, `Reason` (`"archived"`), `OccurredOnUtc` |
| `JobPostingSuspendedIntegrationEvent` | `Suspend` (admin) | `JobPostingId`, `EmployerId`, `Reason`, `OccurredOnUtc` |
| `JobPostingReinstatedIntegrationEvent` | `Reinstate` (admin) | `JobPostingId`, `EmployerId`, `OccurredOnUtc` |
| `JobPostingStatusChangedIntegrationEvent` | **every** status transition (the authoritative signal) | `JobPostingId`, `FromStatus`, `ToStatus`, `IsSearchable` (`bool`), `IsAcceptingApplications` (`bool`), `ActorKind`, `OccurredOnUtc` |

`JobPostingStatusChangedIntegrationEvent` is emitted on **every** transition (including `Pause`/`Resume`, which have no dedicated named event); the named events above are semantic conveniences emitted *alongside* it for consumers that want a specific signal.

Consumers (for context only — you do not code them): BC-6 Search consumes `JobPostingPublished`/`Updated`/`Expired`/`Closed`/`Suspended` and `JobPostingStatusChanged` (for Pause/Resume) to maintain the index; BC-7 Recommendation consumes `JobPostingPublished`/`Updated` to compute embeddings; BC-5 Job Application consumes `JobPostingExpired`/`Closed`/`Suspended` to close in-flight applications; BC-8 consumes `JobPostingPublished` for export; BC-9 Notification consumes expiry/closed/suspended; BC-10 Reporting consumes all.

### 8.2 Internal domain events (NOT published outside the module)

`JobPostingDrafted`, `JobPostingPublished`*, `JobPostingFieldsEdited`, `JobPostingDeadlineExtended`, `JobPostingVisibilityChanged`, `JobPostingPaused`, `JobPostingResumed`, `JobPostingExpired`*, `JobPostingArchived`, `JobPostingSuspended`*, `JobPostingReinstated`*, `JobPostingRemoved`, `JobPostingRenewed`. (*names overlap with integration events but these are the in-module `DomainEvent` variants.) Use these for in-module reactions; they never reach the outbox.

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained for its domain. (Outbox/inbox mechanics: [[00-Shared-Foundations]] §6.2–6.3.)

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent handlers)

| Integration event | From | Payload you receive | Your reaction |
|---|---|---|---|
| `EmployerVerifiedIntegrationEvent` | BC-2 Employer Profile | `EmployerId`, `VerifiedOnUtc` | Upsert `EmployerStanding` → `IsVerified = true`. |
| `EmployerVerificationFailedIntegrationEvent` | BC-2 | `EmployerId`, `Reason` | Upsert `EmployerStanding` → `IsVerified = false`. |
| `AccountDeactivatedIntegrationEvent` | BC-1 IAM/UAM | `UserId`, `DeactivatedOnUtc` | If the `UserId` maps to an employer, set `EmployerStanding.IsActive = false` **and** auto-`Archive()` (or `Pause()` then mark) that employer's `Active`/`Paused` postings. (For the exercise: auto-close active postings — emit `JobPostingClosed`.) |
| `UserAccountSuspendedIntegrationEvent` | BC-1 | `UserId`, `Reason` | Same as deactivation — block the employer and close active postings. |
| `UserAccountReinstatedIntegrationEvent` | BC-1 | `UserId` | Set `EmployerStanding.IsActive = true`. (Postings are **not** auto-republished — the employer must renew/republish.) |
| `ExternalJobIngestedIntegrationEvent` | BC-8 External Job Sync | `ExternalRef`, `PartnerId`, `NormalizedPosting` (a fully normalised posting payload: title, summary, contractType, workFormat, location, skill codes, deadline, employerId) | Create a new `JobPosting` (or update the existing mirror by `ExternalRef`) on behalf of the partner. Treat the partner as a system actor. Idempotent on `ExternalRef`. |
| `ExternalJobUpdatedIntegrationEvent` | BC-8 | `ExternalRef`, `PartnerId`, `ChangedFields` | Apply the update to the mirrored posting via `EditDetails`. |
| `ExternalJobRetractedIntegrationEvent` | BC-8 | `ExternalRef`, `PartnerId` | `Archive()` the mirrored posting. |
| `TaxonomyUpdatedIntegrationEvent` | BC-11 Admin Config | `TaxonomyId`, `Version`, `ChangeSummary` | Re-validate stored `RequiredSkill` codes against the `TaxonomyApi` port; flag postings whose codes are now deprecated for employer review (do not auto-edit). |

**Idempotency** is mandatory — see [[00-Shared-Foundations]] §6.3 (inbox). Inbound events arrive via the module's integration-event subscription wired in the module composition entry point.

### 9.2 Public APIs this module CALLS (port interfaces — define in `Application`, implement adapters in `Infrastructure`)

```
Port: TaxonomyApi               (provided by BC-11 Admin Config; canonical skill taxonomy lookup & validation)
  MapSkill(rawSkillLabel: string)        -> Result<CanonicalSkillRef>
  IsValidSkillCode(taxonomyCode: string) -> bool

Port: AuditTrailExporter        (audit-trail export rendering; infrastructure concern)
  Export(trail: PostingAuditTrailDto, format: ExportFormat) -> Result<FileResultDto>
  // ExportFormat: Csv | Pdf
```

For the exercise, `Infrastructure` may provide a **stub adapter** for `TaxonomyApi` (in-memory canonical list, e.g. `"js"→"JavaScript"`) so the module runs standalone. `AuditTrailExporter` may render real CSV and a simple PDF. Keep the port shapes exactly as above so real adapters drop in later.

> **Note on employer eligibility:** this module does **not** call BC-2 synchronously. It decides "may this employer publish?" from the locally-maintained `EmployerStanding` projection (§5.3), kept fresh by the events in §9.1. A synchronous `EmployerProfilePublicApi.GetStanding` call would be a valid alternative — see teaching notes.

### 9.3 Public API this module EXPOSES (in the `Contracts` surface)

```
Public API: JobPostingsPublicApi
  GetSummary(jobPostingId: uuid)             -> JobPostingSummaryDto?
  IsAcceptingApplications(jobPostingId: uuid) -> bool                    // used by BC-5
  GetSchemaOrg(jobPostingId: uuid)           -> SchemaOrgJobPostingDto?  // used by BC-8 export

JobPostingSummaryDto {
  JobPostingId: uuid, EmployerId: uuid, Title: string, Status: string,
  Visibility: string, DeadlineUtc: datetime, IsSearchable: bool
}
```

---

## 10. Application layer

Every use case is a **command** or a **query** with exactly one handler. Commands return `Result`/`Result<T>`; queries return DTOs. Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

**Audit-trail rule:** every command that changes status or edits fields MUST, in the **same handler and same unit of work**, load the posting's `PostingAuditTrail` and call `RecordStatusChange` / `RecordFieldEdit`. This guarantees invariant 6.2/12. See §14 for the worked pattern.

### 10.1 Commands

| Command | Story | Handler responsibilities |
|---|---|---|
| `CreateJobPostingCommand` | US-3.2.1-01 | Validate all VO inputs → canonicalise each raw skill via `TaxonomyApi.MapSkill` → `JobPosting.CreateDraft(...)` → `PostingAuditTrail.Start(...)` → persist. |
| `UpdateJobPostingDetailsCommand` | US-3.2.1-03 | Load posting → `EditDetails(...)` (re-runs Schema.org if `Active`) → load trail → `RecordFieldEdit` → persist. |
| `UpdateRequiredSkillsCommand` | US-3.2.1-01/03 | Canonicalise raw labels via `TaxonomyApi` → `EditDetails` replacing the skills VO collection → `RecordFieldEdit` → persist. |
| `ExtendApplicationDeadlineCommand` | US-3.2.1-03 | Load → `ExtendDeadline(newDeadlineUtc)` (must be later) → `RecordFieldEdit` → persist. |
| `SetPostingVisibilityCommand` | US-3.2.1-03 | Load → `SetVisibility(visibility)` → `RecordFieldEdit` → persist. |
| `PublishJobPostingCommand` | US-3.2.1-02 / US-3.2.4-01 | Load posting → load `EmployerStanding` → validate every skill code via `TaxonomyApi.IsValidSkillCode` → `SchemaOrgStandardizer.Standardize` → `posting.Publish(schemaOrg, employerStanding)` → load trail → `RecordStatusChange` → persist. |
| `PauseJobPostingCommand` | US-3.2.4-01 | Load → `Pause()` → `RecordStatusChange` → persist. |
| `ResumeJobPostingCommand` | US-3.2.4-01 | Load → `Resume()` → `RecordStatusChange` → persist. |
| `ArchiveJobPostingCommand` | US-3.2.4-01 | Load → `Archive()` (only from `Expired`) → `RecordStatusChange` → persist. |
| `RenewJobPostingCommand` | US-3.2.1-04 | Load expired posting → `JobPostingRenewalService.Renew(...)` → `original.Archive()` → `PostingAuditTrail.Start(new)` + `RecordStatusChange` on both → persist all in one UoW. |
| `BulkRenewJobPostingsCommand` | US-3.2.1-04 | Iterate `RenewJobPostingCommand` logic over a set of posting ids, applying shared or per-posting deadline edits; aggregate per-posting `Result`s (partial success allowed). |
| `SuspendJobPostingCommand` | US-3.1.4-03 | **Admin only.** Load → `Suspend(adminActor, reason)` (reason required) → `RecordStatusChange(reason)` → persist. |
| `ReinstateJobPostingCommand` | US-3.1.4-03 | **Admin only.** Load → `Reinstate(adminActor)` → `RecordStatusChange` → persist. |
| `RemoveJobPostingCommand` | US-3.1.4-03 | **Admin only.** Load → `Remove(adminActor, reason)` → `RecordStatusChange(reason)` → persist. |
| `ProcessExpiredPostingsCommand` | US-3.2.1-04 / US-3.2.4-01 | **System.** Invoked by `PostingExpirationBackgroundService`. For each posting where `PostingExpirationPolicy.ShouldExpire` → `Expire()` → `RecordStatusChange(System actor)`. Also emits an "approaching expiry" internal signal for postings within the reminder threshold. |

### 10.2 Queries

| Query | Story | Returns |
|---|---|---|
| `GetJobPostingByIdQuery` | US-3.2.1-01/03 | `JobPostingDto` (full posting incl. Schema.org representation) |
| `GetMyJobPostingsQuery` | US-3.2.4-01 | `list<JobPostingSummaryDto>` for the employer, filterable by `status` |
| `GetPostingAuditTrailQuery` | US-3.2.4-02 | `PostingAuditTrailDto` (chronological entries: actor, timestamp, kind, transition/changed fields, reason) |
| `ExportPostingAuditTrailQuery` | US-3.2.4-02 | `FileResultDto` (CSV or PDF) via `AuditTrailExporter` |
| `GetSchemaOrgJobPostingQuery` | US-3.2.1-02 | `SchemaOrgJobPostingDto` — the standardised representation, for export/indexing |
| `AdminListJobPostingsQuery` | US-3.1.4-03 | `list<AdminJobPostingRowDto>` — filterable by employer, status, date posted, industry, location; keyword/job-id search |
| `AdminGetJobPostingDetailQuery` | US-3.1.4-03 | `AdminJobPostingDetailDto` — full details + employer info + metrics (applications/matches/views counts, days posted) read from local projections |

### 10.3 Validators — representative rules

- `CreateJobPostingCommand`: title 3–150 chars; summary 20–5000; `contractType`/`educationLevel`/`workFormat` in enums; `WorkFormat==Physical ⇒ location present`; `deadline` strictly in the future; `Visibility==Targeted ⇒ targetingCriteria non-empty`; at least one required skill; `jobLink` (if present) a valid HTTPS URL.
- `ExtendApplicationDeadlineCommand`: `newDeadlineUtc` strictly after the existing deadline (`E-POST-DEADLINE-NOT-LATER`) — note the *current* deadline check happens in the handler/aggregate since the validator doesn't load state; the validator only checks it's a future date.
- `SuspendJobPostingCommand` / `RemoveJobPostingCommand`: `reason` non-empty, ≤ 1000 chars.
- `RenewJobPostingCommand`: `newDeadlineUtc` strictly in the future.
- `PublishJobPostingCommand`: posting id present (eligibility/compliance checks are in the handler — they need loaded state).

### 10.4 DTOs

Plain data records in `Application`. Never expose Domain entities/VOs across the API. Map aggregate → DTO in the handler or a mapping helper.

---

## 11. Persistence & data model

Schema/namespace: `job_postings`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. The module-specific relational model follows.

### 11.1 Reference relational model — schema `job_postings`

```
TABLE job_postings
  id                      uuid          PK
  employer_id             uuid          NOT NULL              -- BC-2, no FK
  posted_by_user_id       uuid          NOT NULL              -- BC-1, no FK
  status                  enum          NOT NULL              -- Draft|Active|Paused|Expired|Suspended|Removed|Archived
  title                   string        NOT NULL
  summary                 string        NOT NULL
  contract_type           enum          NOT NULL
  education_level         enum          NOT NULL
  work_format             enum          NOT NULL
  location                json          NULL                  -- EmploymentLocation VO
  required_skills         json          NOT NULL              -- RequiredSkill[] VO collection
  required_languages      json          NOT NULL              -- LanguageRequirement[] VO collection
  salary_range            json          NULL                  -- SalaryRange VO
  deadline                json          NOT NULL              -- ApplicationDeadline VO (date + auto_close)
  deadline_date_utc       datetime      NOT NULL              -- denormalised from deadline for the expiry scan index
  job_link                string        NULL
  visibility              json          NOT NULL              -- PostingVisibility VO (level + targeting criteria)
  visibility_level        enum          NOT NULL              -- denormalised for filtering
  schema_org              json          NULL                  -- SchemaOrgJobPosting VO, populated on publish
  renewed_from_posting_id uuid          NULL
  created_on_utc          datetime      NOT NULL
  updated_on_utc          datetime      NOT NULL
  published_on_utc        datetime      NULL
  version_token           (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  INDEX (employer_id),
  INDEX (status),
  INDEX (status, deadline_date_utc) WHERE status IN ('Active','Paused'),   -- the expiry scan index
  INDEX (visibility_level)

TABLE posting_audit_trails
  id                      uuid          PK
  job_posting_id          uuid          NOT NULL UNIQUE       -- one trail per posting

TABLE audit_entries
  id                      uuid          PK
  trail_id                uuid          NOT NULL              -- FK -> posting_audit_trails.id ON DELETE CASCADE
  kind                    enum          NOT NULL              -- StatusChange | FieldEdit
  actor                   json          NOT NULL              -- AuditActor VO
  status_transition       json          NULL                  -- StatusTransition VO (when kind=StatusChange)
  changed_fields          json          NULL                  -- string[] (when kind=FieldEdit)
  reason                  string        NULL
  occurred_on_utc         datetime      NOT NULL
  INDEX (trail_id, occurred_on_utc)

TABLE employer_standing                                       -- read-model projection (not an aggregate)
  employer_id             uuid          PK
  is_verified             bool          NOT NULL DEFAULT false
  is_active               bool          NOT NULL DEFAULT true
  updated_on_utc          datetime      NOT NULL

TABLE posting_metrics                                         -- optional projection for the admin metrics view
  job_posting_id          uuid          PK
  applications_count      int           NOT NULL DEFAULT 0    -- fed by BC-5 events
  matches_count           int           NOT NULL DEFAULT 0    -- fed by BC-7 events
  views_count             int           NOT NULL DEFAULT 0    -- fed by BC-6/BC-10 events
  updated_on_utc          datetime      NOT NULL

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

### 11.2 Module-specific mapping notes

- Follows the persistence and mapping conventions in [[00-Shared-Foundations]] §3 and §6.
- `JobPosting` and `PostingAuditTrail` are aggregate roots; `AuditEntry` is an owned/child entity of `PostingAuditTrail`, loaded with it.
- Value objects map to `json` columns, **except** ones needed for querying: `status`, `visibility_level`, and `deadline_date_utc` are flattened to scalar columns alongside their `json` source of truth.
- Optimistic-concurrency token required on `job_postings`.
- The `employer_standing` and `posting_metrics` tables are **read-model projections** — written only by integration-event consumers, never by command handlers.
- `PostingExpirationBackgroundService` runs on an interval (e.g. every 5 min), queries the expiry-scan index, and dispatches `ProcessExpiredPostingsCommand`.

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

`JobPostingRepository` (`GetById`, `GetByEmployerId`, `GetExpirable(nowUtc)`, `GetByExternalRef`, `Add`, `Update`), `PostingAuditTrailRepository` (`GetByPostingId`, `Add`, `Update`), `EmployerStandingStore` (`Get`, `Upsert`), `PostingMetricsStore` (`Get`, `Increment`), `UnitOfWork` (`SaveChanges`).

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. Employer routes under `/api/job-postings`; admin routes under `/api/admin/job-postings`. All endpoints require a valid access token (issued by BC-1); employer endpoints derive `EmployerId`/`PostedByUserId` from the token; admin endpoints require the `MoLAdministrator` role. Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

| Verb & route | Command/Query | Success | Notable failures |
|---|---|---|---|
| `POST /api/job-postings` | `CreateJobPostingCommand` | `201` + `JobPostingId` | `400` validation, `400` invalid skill code |
| `GET /api/job-postings/mine` | `GetMyJobPostingsQuery` | `200` + summaries (filter `?status=`) | |
| `GET /api/job-postings/{id}` | `GetJobPostingByIdQuery` | `200` + `JobPostingDto` | `404` |
| `GET /api/job-postings/{id}/schema-org` | `GetSchemaOrgJobPostingQuery` | `200` + `SchemaOrgJobPostingDto` | `404` |
| `PUT /api/job-postings/{id}` | `UpdateJobPostingDetailsCommand` | `200` | `404`, `409 E-POST-ILLEGAL-TRANSITION` (edit in terminal status) |
| `PUT /api/job-postings/{id}/skills` | `UpdateRequiredSkillsCommand` | `200` | `400` invalid skill code, `404` |
| `PUT /api/job-postings/{id}/deadline` | `ExtendApplicationDeadlineCommand` | `200` | `409 E-POST-DEADLINE-NOT-LATER`, `404` |
| `PUT /api/job-postings/{id}/visibility` | `SetPostingVisibilityCommand` | `200` | `400` targeted without criteria, `404` |
| `POST /api/job-postings/{id}/publish` | `PublishJobPostingCommand` | `200` | `409 E-POST-ILLEGAL-TRANSITION`, `422 E-POST-NOT-SCHEMA-COMPLIANT`, `403 E-POST-EMPLOYER-NOT-ELIGIBLE`, `409 E-POST-DEADLINE-IN-PAST` |
| `POST /api/job-postings/{id}/pause` | `PauseJobPostingCommand` | `200` | `409 E-POST-ILLEGAL-TRANSITION` |
| `POST /api/job-postings/{id}/resume` | `ResumeJobPostingCommand` | `200` | `409 E-POST-ILLEGAL-TRANSITION` |
| `POST /api/job-postings/{id}/archive` | `ArchiveJobPostingCommand` | `200` | `409 E-POST-ILLEGAL-TRANSITION` (only from Expired) |
| `POST /api/job-postings/{id}/renew` | `RenewJobPostingCommand` | `201` + new `JobPostingId` | `409` source not Expired, `400` deadline not in future |
| `POST /api/job-postings/renew-bulk` | `BulkRenewJobPostingsCommand` | `207` multi-status (per-posting results) | |
| `GET /api/job-postings/{id}/audit-trail` | `GetPostingAuditTrailQuery` | `200` + entries | `404` |
| `GET /api/job-postings/{id}/audit-trail/export?format=csv\|pdf` | `ExportPostingAuditTrailQuery` | `200` + file download | `404`, `400` bad format |
| `GET /api/admin/job-postings` | `AdminListJobPostingsQuery` | `200` + rows (filters: `employerId`, `status`, `postedFrom/To`, `industry`, `location`, `q`) | `403` non-admin |
| `GET /api/admin/job-postings/{id}` | `AdminGetJobPostingDetailQuery` | `200` + detail + metrics | `403`, `404` |
| `POST /api/admin/job-postings/{id}/suspend` | `SuspendJobPostingCommand` | `200` | `403`, `409 E-POST-ILLEGAL-TRANSITION`, `400 E-POST-REASON-REQUIRED` |
| `POST /api/admin/job-postings/{id}/reinstate` | `ReinstateJobPostingCommand` | `200` | `403`, `409 E-POST-ILLEGAL-TRANSITION` |
| `POST /api/admin/job-postings/{id}/remove` | `RemoveJobPostingCommand` | `200` | `403`, `400 E-POST-REASON-REQUIRED` |

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** `JobTitle` length bounds; `JobSummary` length bounds; `EmploymentLocation` required-field rules; `SalaryRange` (`Min > Max` fails, negative fails); `ApplicationDeadline` (past date fails at create); `PostingVisibility` (`Targeted` without criteria fails, `Public`/`Private` with criteria fails); `JobPostingLink` (non-HTTPS fails); `StatusTransition` (illegal pair fails); `AuditActor` (`Employer`/`Admin` without `UserId` fails, `System` without `UserId` ok).
- **JobPosting status machine** — exhaustively: every legal transition in invariant 6.2/1 succeeds; a representative set of illegal ones (`Draft→Paused`, `Expired→Active`, `Archived→*`, `Removed→*`, `Paused→Archived`) returns `E-POST-ILLEGAL-TRANSITION`.
- **Publish guards:** publish from non-`Draft` fails; publish with `IsCompliant == false` fails `E-POST-NOT-SCHEMA-COMPLIANT`; publish with an ineligible `EmployerStanding` fails `E-POST-EMPLOYER-NOT-ELIGIBLE`; publish with a past deadline fails `E-POST-DEADLINE-IN-PAST`.
- **Edit guards:** `EditDetails` in `Expired`/`Suspended`/`Archived`/`Removed` fails; `EditDetails` while `Active` re-runs Schema.org and updates `SchemaOrg`.
- **`ExtendDeadline`:** earlier-or-equal date fails `E-POST-DEADLINE-NOT-LATER`; later date succeeds.
- **`WorkFormat==Physical` without location** fails at `CreateDraft`.
- **`Suspend`/`Remove` without reason** fails `E-POST-REASON-REQUIRED`.
- **`RenewFrom`:** from non-`Expired` fails; from `Expired` produces a new aggregate with a new id, `RenewedFromPostingId` set, status `Draft`, fresh deadline; the source posting is **not** mutated by `RenewFrom` itself.
- **PostingAuditTrail:** `Entries` is append-only — no behavior updates or removes an entry; `RecordStatusChange` to `Suspended`/`Removed` without a reason fails.
- **Domain services:** `SchemaOrgStandardizer` — a complete posting yields `IsCompliant == true` with all required properties mapped; a posting missing a required field yields `IsCompliant == false` with the field listed in `Violations`. `PostingExpirationPolicy.ShouldExpire` — true only for `Active`/`Paused` past deadline; `IsApproachingExpiry` — boundary at the threshold.

### 13.2 Unit tests — Application (handlers, ports replaced with test doubles)

- `CreateJobPostingCommand`: raw skills are canonicalised via `TaxonomyApi`; an unmappable skill yields a `Result` failure and **no posting persisted**; a created posting also creates its `PostingAuditTrail`.
- `PublishJobPostingCommand`: happy path runs taxonomy validation → Schema.org standardisation → `Publish` → appends a `StatusChange` audit entry → queues `JobPostingPublished` **and** `JobPostingStatusChanged` to the outbox; ineligible employer short-circuits with `E-POST-EMPLOYER-NOT-ELIGIBLE` and nothing is persisted.
- `RenewJobPostingCommand`: original ends `Archived`, a new `Draft` posting exists with `RenewedFromPostingId` set, **both** audit trails updated, all in one `SaveChanges`.
- `SuspendJobPostingCommand`: missing reason rejected by validator; admin role enforced; audit entry records `Actor.Kind == Admin` and the reason.
- `ProcessExpiredPostingsCommand`: only `Active`/`Paused` postings past deadline are expired; each expiry appends a `StatusChange` entry with `Actor.Kind == System`; postings within the reminder threshold raise the approaching-expiry signal.
- **Audit invariant:** assert that *every* status/edit command handler appends exactly one matching `AuditEntry` in the same unit of work.

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip `JobPosting` (incl. `json` VO collections) and `PostingAuditTrail` with entries; optimistic-concurrency conflict on `job_postings` is detected; `GetExpirable(nowUtc)` returns only `Active`/`Paused` past-deadline rows.
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `job_postings`.
- **Outbox:** publishing a posting writes the row change **and** the outbox messages in one transaction; rolling back leaves neither.
- **Inbox / idempotency:** delivering `ExternalJobIngestedIntegrationEvent` twice creates the mirrored posting once; delivering `EmployerVerifiedIntegrationEvent` twice is a no-op the second time.
- **API:** host-level tests for the create → publish → pause → resume → (deadline passes) → expire → renew happy path; admin suspend → reinstate; `GET .../audit-trail/export?format=csv` returns a CSV with the expected columns.
- **Consumed events:** `AccountDeactivatedIntegrationEvent` for an employer auto-closes that employer's `Active` postings and flips `EmployerStanding.IsActive`; `TaxonomyUpdatedIntegrationEvent` flags postings with now-deprecated skill codes without editing them.
- **Expiration background service:** seed an `Active` posting with a past deadline, run one scan tick, assert it transitions to `Expired` with a `System` audit entry and a `JobPostingExpired` outbox message.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "Publish a job posting"

End-to-end, to pattern-match every other status command against:

1. **API.** `POST /api/job-postings/{id}/publish`. The endpoint reads `EmployerId`/`PostedByUserId` from the access token, builds `PublishJobPostingCommand { JobPostingId, EmployerId }`, dispatches it through the mediator.
2. **Validation step.** `PublishJobPostingCommand`'s validator runs (posting id present). State-dependent checks are deferred to the handler.
3. **Handler — `PublishJobPostingCommandHandler`:**
   a. `JobPostingRepository.GetById(id)` → `JobPosting`. `404` if missing. Verify `EmployerId` matches the caller.
   b. `EmployerStandingStore.Get(posting.EmployerId)` → `EmployerStanding`. (Absent ⇒ treat as not eligible.)
   c. For each `RequiredSkill`, `TaxonomyApi.IsValidSkillCode(code)` — any invalid code ⇒ `Result` failure `E-POST-INVALID-SKILL-CODE`.
   d. `SchemaOrgStandardizer.Standardize(posting)` → `SchemaOrgJobPosting`.
   e. `posting.Publish(schemaOrg, employerStanding)` — the aggregate enforces: status is `Draft`; employer eligible; `schemaOrg.IsCompliant`; deadline in the future. On any breach it returns a `Result` failure (mapped to `409`/`422`/`403`). On success it sets `SchemaOrg` + `PublishedOnUtc`, transitions to `Active`, and raises the `JobPostingPublished` internal domain event.
   f. `PostingAuditTrailRepository.GetByPostingId(id)` → `PostingAuditTrail`; `trail.RecordStatusChange(employerActor, StatusTransition(Draft, Active), reason: null)`.
   g. `repository.Update(posting)`; `auditRepo.Update(trail)`; `unitOfWork.SaveChanges()` — posting change + audit entry commit together.
4. **Domain-event / outbox step.** After the unit of work is saved, the mediator pipeline dispatches internal domain events in-process and writes **two** integration events to the outbox in the same transaction: `JobPostingPublishedIntegrationEvent` and `JobPostingStatusChangedIntegrationEvent`. ([[00-Shared-Foundations]] §6.1–6.2.)
5. **Relay.** The background outbox relay publishes them; BC-6 indexes the posting, BC-7 computes embeddings, BC-10 records the transition.
6. **Response.** Handler returns `Result` success; the endpoint returns `200`.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.2.1-01 Create job posting | Structured form fields captured (title, summary, skills via multiple input methods, contract type, deadline + auto-close, work format + location, languages, optional link); incomplete required fields rejected with validation errors. |
| US-3.2.1-02 Standardize to Schema.org | All fields validated against the Schema.org JobPosting vocabulary; form fields mapped to canonical properties; skills/categories normalised via the `TaxonomyApi` port; non-compliant data rejected on publish; stored representation has no missing required properties. |
| US-3.2.1-03 Manage visibility & details | Editable fields can be modified and take effect immediately; deadline can be extended (later only); visibility `Public`/`Private`/`Targeted` with targeting criteria; visibility changes effective immediately; every edit recorded in the audit trail. |
| US-3.2.1-04 Expiration & renewal | Postings auto-expire when the deadline passes (no longer searchable); approaching-expiry signal raised within the configurable threshold (default 7 days); renew creates a **new** posting (new id) with a fresh deadline, original archived; bulk renewal over multiple postings. |
| US-3.2.4-01 View & manage status | Dashboard lists postings with status; `Draft→Active` publish; `Active→Paused` / `Paused→Active`; auto-`Expired`; `Expired→Archived`; filter by status; status changes effective immediately. |
| US-3.2.4-02 Status history & audit trail | Per-posting chronological status history with date/time (second precision)/timezone, previous→new status, and actor; automatic changes attributed to `System`; exportable as CSV/PDF; append-only and immutable; full history retained even after archive/remove. |
| US-3.1.4-03 Admin moderate postings | Admin can list/filter/search all postings; view detail + metrics (applications/matches/views/days posted); suspend (hidden, applications blocked, employer notified via emitted event); remove (permanent); reinstate a suspended posting; every suspend/remove/reinstate logged with reason in the audit trail. |

---

## Appendix — teaching notes & open questions

- **Two-file brief.** This package + `00-Shared-Foundations.md` together are the complete brief. The shared file carries everything stack-related and identical across all 12 BCs; this package carries only the domain design. Discuss with the class: which parts of a design are genuinely reusable across modules, and what is the cost of the one shared dependency?
- **Two histories, one design — contrast with the exemplar.** BC-3 modelled profile edit history as a separate `ProfileHistory` aggregate. This BC unifies status changes *and* field edits into one `PostingAuditTrail` aggregate, because both must be immutable, both must outlive the posting, and both are exported together. Ask the class: when do two kinds of "history" belong in one log versus two?
- **`RequiredSkill` is a value object, not an entity — unlike BC-3's `ProfileSkill`.** In BC-3 a profile skill is individually edited and carries proficiency that changes over time, so it earned entity identity. Here, a posting's skill list is replaced wholesale on edit and has no per-skill lifecycle — so it's a VO collection. Same word ("skill"), different modelling, driven by behaviour. A good side-by-side.
- **`EmployerStanding` as borrowed state.** Rather than calling BC-2 synchronously on every publish, this module keeps a tiny local projection fed by events. This mirrors BC-9's "preferences as borrowed state" and BC-2's "dashboard as read model." The trade-off: a freshly-verified employer can't publish until the `EmployerVerified` event lands. Discuss the staleness budget, and when a synchronous `EmployerProfilePublicApi` call would be the better call.
- **Admin moderation is a privileged path, not a BC.** Suspend/remove/restore reuse the status machine; only the `AuditActor.Kind` and a mandatory reason differ. Discuss the alternative (a dedicated Moderation BC) and what would justify it (e.g. moderation queues, reviewer assignment, appeals workflow — none of which are in scope here).
- **The `JobPostingStatusChanged` vs. named-events question.** This package emits a generic `JobPostingStatusChanged` on every transition *plus* named events (`Published`, `Expired`, `Closed`, `Suspended`, `Reinstated`) for convenience. `Pause`/`Resume` have no named event and rely on the generic one. Discuss: fat generic event vs. a named event per transition — which makes consumers simpler, and which makes the contract more stable?
- **Renewal as a new aggregate.** `RenewFrom` produces a new `JobPosting` rather than resetting the old one. This keeps each posting's audit trail and identity clean, and matches `US-3.2.1-04 AC-04`. Contrast with an in-place "reactivate" — and why that would muddy the audit story.
- **Auto-close ambiguity.** `US-3.2.1-01` has an optional "auto-close" toggle while `US-3.2.4-01`/`US-3.2.1-04` describe expiration as unconditional once the deadline passes. This package treats deadline expiry as **always automatic** for `Active`/`Paused` postings and keeps `AutoCloseEnabled` as a stored preference (e.g. for a future "auto-archive after expiry" behaviour). Flagged as a requirements tension to resolve with stakeholders.
- **Localization.** Salary defaults to `BDT`; per-jurisdiction Schema.org `validThrough`/`baseSalary` formatting and timezone handling for the audit trail's second-precision timestamps are configuration, not hard-coded.
