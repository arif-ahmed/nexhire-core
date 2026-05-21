---
title: "Handover Package — BC-5 Job Application"
type: handover-package
bc_id: BC-5
bc_name: Job Application
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
  - bc/applications
---

# Handover Package — BC-5 Job Application

> **Audience:** an AI coding agent. This package owns the **domain design** for the `JobApplication` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `JobApplication`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `job_application`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

This BC is a **supporting** subdomain (see [[Context_Map]]): the application funnel is necessary but conventional — the platform's differentiators are BC-7 Recommendation Engine and BC-10 Reporting. BC-5 is a **Customer/Supplier** downstream of BC-2 Employer Profile, BC-3 JobSeeker Profile, and BC-4 Job Postings: at the moment of submission it *reads a snapshot* from all three, then owns the application lifecycle thereafter.

**Saga note.** Submitting an application triggers the **ApplicationSubmissionSaga**. This module is the saga's *initiator*: it does the validation, persists the `Application`, and publishes `ApplicationSubmitted`. The saga's downstream choreography (seeker confirmation, employer notification, analytics increment, audit) is a **separate package** — this module does *not* implement the saga steps in BC-9/BC-10/etc. It only emits the trigger event and trusts the outbox to deliver it. Where this package says "the saga continues," that is out-of-scope downstream work.

---

## 1. Purpose & scope boundaries

### What this BC is for

Job Application owns the **seeker-to-employer hiring interaction from the seeker's side of the funnel**: bookmarking jobs into an "Interested List", formally applying to a job (capturing an immutable profile snapshot at apply-time), and viewing/managing the resulting applications — including withdrawal. It owns the `Application` aggregate as the single source of truth for "this seeker expressed formal interest in this posting," its append-only stage history, and the seeker's bookmarks.

It is a **supporting** subdomain — the funnel must exist and be auditable, but it is not where the platform competes.

### In scope

The `JobApplication` module is responsible for:

- **Bookmarks** (`US-3.2.3-01`): the `Bookmark` aggregate — a job seeker's persistent "Interested List", add/remove, listed with key job details. *(Saved search filters, also mentioned in `US-3.2.3-01`, are NOT owned here — see out-of-scope.)*
- **Applying to a job** (`US-3.2.3-02`): the `Application` aggregate — created in status `Submitted`, with a frozen `CandidateSnapshot` (Level 1 + Level 2 profile + chosen resume reference), optional cover letter, and an informational `MatchScoreAtApply`. Triggers the **ApplicationSubmissionSaga** by publishing `ApplicationSubmitted`.
- **Duplicate prevention & re-apply** (`US-3.2.3-02 AC-07/08`): at most one *non-terminal* application per `(seeker, posting)`; re-apply after a terminal outcome is a new `Application` linked via `ReplacesApplicationId`.
- **Idempotent submission** (`US-3.2.3-02 AC-14`): a client-supplied idempotency key guarantees at-most-one `Application` per submission attempt.
- **Withdrawal** (`US-3.2.3-02 AC-12`): the seeker transitions an application to `Withdrawn` with a reason; publishes `ApplicationWithdrawn`.
- **Viewing & managing applications** (`US-3.2.4-03`): the seeker's "My Applications" list and detail view; the *append-only* `ApplicationStage` history. The recruiter-facing pipeline-management surface of `US-3.2.4-03` (stage transitions, decisions, notes, offers) is partially modelled here as the **read side** and the *consumed* events — see the boundary note below.
- **Reacting to posting and account lifecycle**: auto-closing applications when a posting closes/expires/is suspended, and withdrawing open applications when a seeker deactivates their account.
- Publishing the integration events in §8 and reacting to the integration events in §9.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong to other BCs and are reached via the contracts in §9:

- **Job postings themselves** (creation, status, expiry, content) → BC-4 Job Postings. This module *reads* a posting snapshot via the `JobPostingApi` port at apply-time and *consumes* `JobPostingClosed/Expired/Suspended`. It never edits a posting.
- **The job seeker's master profile / resume** → BC-3 JobSeeker Profile. This module *reads* a profile snapshot via the `JobSeekerProfileApi` port and the resume *reference* — it stores a frozen copy in `CandidateSnapshot`. Edits a seeker makes on the application form affect *only the snapshot*, never the master profile.
- **Employer identity, recruiter roles, recruiter team membership** → BC-2 Employer Profile / BC-1 IAM. This module receives `EmployerId` (denormalised onto the `Application` for scoping) and asks the `EmployerAccessApi` port whether a recruiter may view an application; it does not own roles.
- **Match scores** → BC-7 Recommendation Engine. This module *reads* a best-effort `MatchScoreAtApply` via the `MatchRankingPublicApi` port and snapshots it; absence is not blocking. It never computes a score.
- **Saved search filters** → BC-6 Search & Discovery. `US-3.2.3-01` bundles bookmarks *and* saved searches; the [[BC_Mapping]] migration note explicitly keeps saved searches in BC-6 and moves only *bookmarking* to BC-5. This module owns bookmarks only.
- **The recruiter hiring pipeline beyond seeker visibility** — recruiter-driven stage transitions, `EmployerDecision`, internal notes, `Offer` aggregates, saved recruiter views (`US-3.2.4-03`'s recruiter-side ACs) — are **owned by this BC's aggregate** but for *this package's build scope* are deferred: model the `Application` state machine and `ApplicationStage` history (both sides write to it), expose the seeker-facing read/write surface fully, and provide the recruiter-facing **read** queries; treat recruiter *mutation* commands (`PATCH stage`, decisions, notes, offers) as a documented follow-on. See the boundary note. The seeker-facing slice (`US-3.2.3-01`, `US-3.2.3-02`, the read side of `US-3.2.4-03`) is the must-build.
- **Sending notifications / emails / SMS** → BC-9 Notification. This module emits `ApplicationSubmitted` / `ApplicationStatusChanged` / `ApplicationWithdrawn`; BC-9 decides what to send.
- **Analytics / dashboards / audit trail** → BC-10 Reporting. This module emits events; BC-10 builds the read models and the `AdminAuditEntry` records.
- **The ApplicationSubmissionSaga's downstream steps** → a separate saga package. This module only *initiates* the saga.

### Boundary note — the seeker/recruiter split inside one aggregate (teaching point)

`US-3.2.3-02` (seeker applies) and `US-3.2.4-03` (recruiter manages applications) both mutate the *same* `Application` aggregate and the *same* `ApplicationStage` history — they are two sides of one entity. The [[BC_Mapping]] assigns *both* to this BC (BC-5). For a teachable, buildable module we draw the line at **seeker-facing first**: the full `Application` state machine, the `ApplicationStage` append-only log, bookmarks, and every seeker command/query are the must-build deliverable; the recruiter *write* surface (`StageTransition`, `EmployerDecision`, `ApplicationNote`, `Offer`) is reproduced here in the domain model and the API table marked `[recruiter follow-on]` so the aggregate is *complete and correct* but the build can be staged. Good class discussion: when one aggregate has two very different actors, is that one BC or two? (We keep it one — the consistency boundary is the `Application`.)

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **Job Seeker** | A person seeking employment. Their `JobSeekerId` and profile are owned by BC-3; here they are an applicant. |
| **Bookmark** | The `Bookmark` aggregate — one job a seeker has saved to their Interested List. |
| **Interested List** | The collection of a seeker's `Bookmark`s. Not a separate aggregate — just "all bookmarks for this seeker". |
| **Application** | The `Application` aggregate — the single source of truth that a seeker formally applied to a posting. |
| **Application Status** | The `Application`'s lifecycle state: `Submitted`, `UnderReview`, `Shortlisted`, `Interview`, `Offered`, `Hired`, `Rejected`, `Withdrawn`, `Expired`. |
| **Terminal Status** | `Hired`, `Rejected`, `Withdrawn`, `Expired` — no further transitions from the seeker side; a re-apply is a new `Application`. |
| **Non-terminal Status** | `Submitted`, `UnderReview`, `Shortlisted`, `Interview`, `Offered` — only one of these may exist per `(seeker, posting)` at a time. |
| **Candidate Snapshot** | `CandidateSnapshot` VO — an *immutable* copy of the seeker's Level 1 + Level 2 profile fields taken at apply-time. Frozen forever; later master-profile edits never change it. |
| **Snapshot Overrides** | Edits the seeker makes on the application form. Merged into the `CandidateSnapshot` at submit — they do **not** touch the master profile. |
| **Cover Letter** | Optional free text (≤ 4000 chars, plain text / markdown subset) attached to one `Application`. |
| **Match Score At Apply** | An informational `int?` (0–100) snapshot of the BC-7 match score at submit time. Best-effort; never blocks submission. |
| **Idempotency Key** | A client-generated UUID (created when the application form opens) guaranteeing at-most-one `Application` per submission attempt. Retained 24 h. |
| **Replaces Application** | `ReplacesApplicationId` — links a re-applied `Application` to the prior terminal one so the employer sees history. |
| **Application Stage** | An entry in the append-only `ApplicationStage` history — one row per status transition, recording who/when/why. |
| **Withdrawal Reason** | A lookup code (`WithdrawalReason`) the seeker selects when withdrawing. |
| **Withdrawal** | The seeker-initiated transition of a non-terminal application to `Withdrawn`. |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `JobApplication`.
- Public surface (`Contracts`): the integration events published in §8 + the public API in §9.3.
- **Module-specific notes:** a background outbox relay runs as described in [[00-Shared-Foundations]] §6.2. In addition, this module runs a small **idempotency-key purge job** that periodically deletes `idempotency_keys` rows older than 24 h (§11). Both are registered by the module composition entry point.

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has **two aggregates**: `Application` (the root of the BC) and `Bookmark`. (Notation: see [[00-Shared-Foundations]] §2.)

### 5.1 Aggregate: Application

**Aggregate root.** Identity: `ApplicationId` (strongly-typed id wrapping `uuid`). The single source of truth for "this seeker applied to this posting."

| Member | Type | Notes |
|---|---|---|
| `Id` | `ApplicationId` | |
| `JobPostingId` | `uuid` | BC-4 identity, no FK |
| `JobSeekerId` | `uuid` | BC-3 identity, no FK |
| `EmployerId` | `uuid` | BC-2 identity, no FK — denormalised at apply-time for fast recruiter scoping |
| `Status` | `ApplicationStatus` | enum, see §6.2 |
| `CandidateSnapshot` | `CandidateSnapshot` | VO — immutable profile copy at apply-time |
| `ResumeDocumentId` | `uuid` | BC-3 document reference chosen for this application |
| `CoverLetter` | `CoverLetter?` | VO, optional, ≤ 4000 chars |
| `MatchScoreAtApply` | `int?` | informational snapshot from BC-7, 0–100 |
| `ReplacesApplicationId` | `ApplicationId?` | links to a prior terminal application on re-apply |
| `IdempotencyKey` | `uuid` | unique; guarantees at-most-once submission |
| `Stages` | `list<ApplicationStage>` | child entities, append-only history |
| `AppliedOnUtc` | `datetime` | |
| `LastStatusChangeOnUtc` | `datetime` | |
| `WithdrawnOnUtc` / `HiredOnUtc` / `RejectedOnUtc` | `datetime?` | set when entering those states |

**Child entity:** `ApplicationStage` (identity local to the aggregate; only appended through the root):

- `ApplicationStage` — `ApplicationStageId`, `Stage` (`ApplicationStatus`), `EnteredOnUtc` (`datetime`), `EnteredByRole` (`StageActorRole`: `Seeker`/`Recruiter`/`System`), `EnteredByUserId` (`uuid?`), `ReasonCode` (`string?`), `Comment` (`string?`).

### 5.2 Aggregate: Bookmark

**Aggregate root.** Identity: `BookmarkId`. One per `(JobSeekerId, JobPostingId)` — a seeker's saved job. Kept as its own aggregate (not a child of anything) because it has an independent lifecycle and no consistency relationship with `Application` — a seeker can bookmark without applying and apply without bookmarking.

| Member | Type | Notes |
|---|---|---|
| `Id` | `BookmarkId` | |
| `JobSeekerId` | `uuid` | BC-3 identity, no FK |
| `JobPostingId` | `uuid` | BC-4 identity, no FK |
| `BookmarkedOnUtc` | `datetime` | |

> Bookmarks store only the `(seeker, posting)` pair. Job details for the "Interested List" view (title, company, location, salary) are fetched at query time from BC-4 via the `JobPostingApi` port — this module does not duplicate posting content.

### 5.3 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `CandidateSnapshot` | `FullName`, `Email`, `Mobile`, `CurrentLocation`, `EducationSummary` (`string`), `ExperienceSummary` (`string`), `Skills` (`list<string>`), `IsLevel2Complete` (`bool`), `CapturedOnUtc` (`datetime`) | `FullName`/`Email`/`Mobile` non-empty; `IsLevel2Complete` must be `true` at construction (a snapshot is only ever taken for an eligible profile — enforced by the handler, asserted by the VO) |
| `CoverLetter` | `Text` (`string`) | ≤ 4000 chars; sanitised plain-text / markdown subset; non-empty if present |
| `WithdrawalReason` | `Code` (`string`), `Comment` (`string?`) | `Code` from the known lookup set; `Comment` ≤ 1000 chars |

**Withdrawal reason lookup** (seeded reference data, not an aggregate): `code`, `label_en`, `label_ar`, `is_seeker_facing` — seed at least `ChangedMind`, `AcceptedAnotherOffer`, `NoLongerInterested`, `RoleNotAsExpected`, `AccountDeactivated` (system-only, `is_seeker_facing = false`).

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior lives on the aggregate roots. Handlers never set properties directly. Method shapes below are neutral specifications (see [[00-Shared-Foundations]] §2).

### 6.1 Application — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Submit(jobPostingId, jobSeekerId, employerId, candidateSnapshot, resumeDocumentId, coverLetter?, matchScoreAtApply?, idempotencyKey, replacesApplicationId?)` | Creates the application in `Submitted`. `candidateSnapshot.IsLevel2Complete` must be `true` (`E-APP-PROFILE-INCOMPLETE`). Appends the initial `ApplicationStage` (`Submitted`, `Seeker`). `replacesApplicationId` set only when re-applying after a terminal outcome. | `ApplicationSubmitted` |
| `Withdraw(withdrawalReason, byUserId)` | Only from a **non-terminal** status. → `Withdrawn`. Sets `WithdrawnOnUtc`. Appends an `ApplicationStage` (`Withdrawn`, `Seeker`, reason). Idempotent if already `Withdrawn`. Fails from any other terminal status (`E-APP-INVALID-TRANSITION`). | `ApplicationWithdrawn` |
| `MarkExpiredDueToPostingClosure(reason)` | Only from a non-terminal status. → `Expired`. Appends `ApplicationStage` (`Expired`, `System`, reason). Reaction to posting close/expire/suspend. | `ApplicationStatusChanged` |
| `WithdrawDueToAccountDeactivation()` | Only from a non-terminal status. → `Withdrawn` with system `WithdrawalReason = AccountDeactivated`. Appends `ApplicationStage` (`Withdrawn`, `System`). | `ApplicationWithdrawn` |
| `TransitionStage(toStatus, byRole, byUserId, reasonCode?, comment?, expectedCurrentStatus)` *[recruiter follow-on]* | Optimistic check: `Status == expectedCurrentStatus` else `E-APP-STALE`. Transition must be in the allowed set (§6.2). Reason required for `Rejected` / employer-initiated `Withdrawn` (`E-APP-REASON-REQUIRED`). Appends an `ApplicationStage`. Terminal statuses are not transitionable. Sets `Hired/RejectedOnUtc` as applicable. | `ApplicationStatusChanged` (+ `ApplicationRejected` when → `Rejected`) |
| `RecordView(byEmployerId)` *[recruiter follow-on]* | Records that the employer opened the application (no status change). | `ApplicationViewed` |

### 6.2 Core invariants (must always hold)

1. **Status machine.** Legal transitions:
   - `Submitted → UnderReview | Shortlisted | Rejected | Withdrawn | Expired`
   - `UnderReview → Shortlisted | Interview | Rejected | Withdrawn | Expired`
   - `Shortlisted → Interview | Offered | Rejected | Withdrawn | Expired`
   - `Interview → Offered | Rejected | Withdrawn | Expired`
   - `Offered → Hired | Rejected | Withdrawn | Expired`
   - Terminal: `Hired`, `Rejected`, `Withdrawn`, `Expired` — **no transitions out** (admin override is a separate, out-of-scope story).
   No other transition is legal; an illegal one returns `E-APP-INVALID-TRANSITION`.
2. **One non-terminal application per `(JobSeekerId, JobPostingId)`.** A second submission while a non-terminal application exists returns `E-APP-DUPLICATE` (enforced by the handler + a partial unique index — §11). Re-apply is only allowed when the prior application is *terminal*.
3. **Idempotency.** `IdempotencyKey` is unique. A repeat `Submit` with a seen key returns the *existing* application, not a new one (`200`, not `201`).
4. **The snapshot is immutable.** `CandidateSnapshot` is set once at `Submit` and never mutated. Seeker form edits ("snapshot overrides") are merged into the snapshot *before* construction — they never reach BC-3's master profile.
5. **`IsLevel2Complete` gate.** An `Application` can only be `Submit`-ted with a `CandidateSnapshot` whose `IsLevel2Complete == true`. The handler verifies eligibility via `JobSeekerProfileApi.IsLevel2Complete` before building the snapshot; the VO asserts it.
6. **`ApplicationStage` is append-only.** Stages are only ever appended, never updated or deleted. Every status change appends exactly one stage row.
7. **`ReplacesApplicationId` chain.** Set only when re-applying after a terminal outcome, and only to an application for the *same* `(seeker, posting)` pair.
8. **Withdrawal is seeker-or-system only** from a non-terminal status; it is idempotent against an already-`Withdrawn` application and fails from other terminal states.
9. **`MatchScoreAtApply` is informational.** It is never used to block or gate submission; a null value (BC-7 unavailable) is acceptable.
10. **`EmployerId` is denormalised and immutable** after `Submit`.

### 6.3 Bookmark — behaviors

| Method | Rules | Domain event raised |
|---|---|---|
| `static Create(jobSeekerId, jobPostingId)` | One bookmark per `(seeker, posting)` — a duplicate returns `E-BOOKMARK-DUPLICATE` (enforced by the handler + unique index). | `JobBookmarked` |
| *(removal)* | Removal is a repository delete driven by `RemoveBookmarkCommand`; the handler raises `JobUnbookmarked` before deletion. | `JobUnbookmarked` |

**Bookmark invariants:** at most one `Bookmark` per `(JobSeekerId, JobPostingId)`; a bookmark has no relationship to any `Application` — they are independent; bookmarks do not store posting content.

---

## 7. Domain services

Stateless. Live in `Domain`. Used when logic spans entities or needs a small external input (the external input arrives via a port — the *handler* calls the port and passes the value in).

### 7.1 `ApplicationEligibilityService`

```
CheckCanApply(
  jobSeekerId: uuid,
  jobPostingId: uuid,
  posting: PostingApplicabilitySnapshot,        // supplied by handler via the JobPostingApi port
  isLevel2Complete: bool,                       // supplied by handler via the JobSeekerProfileApi port
  existingNonTerminalApplication: Application?   // supplied by handler via repository
) -> Result
```

Encapsulates the pre-submission gate of `US-3.2.3-02`:

- Posting must be in `Active` status with `deadline > now` — else `E-APP-POSTING-CLOSED`.
- `isLevel2Complete` must be `true` — else `E-APP-PROFILE-INCOMPLETE`.
- No `existingNonTerminalApplication` may be present — else `E-APP-DUPLICATE` (with the existing application's id surfaced).
- If a *terminal* prior application exists, it is allowed and the caller should set `ReplacesApplicationId`.

Returns `Result` success when the seeker may apply, otherwise the specific `Error`.

### 7.2 `CandidateSnapshotBuilder`

```
Build(
  profile: JobSeekerProfileSnapshotDto,         // supplied by handler via the JobSeekerProfileApi port
  overrides: SnapshotOverrides?                 // editable form fields the seeker changed
) -> Result<CandidateSnapshot>
```

Builds the immutable `CandidateSnapshot` from the live profile snapshot plus any seeker form edits. **Overrides are merged into the snapshot only** — the service has no path to BC-3's master profile. Validates `IsLevel2Complete` is true. Stamps `CapturedOnUtc`.

### 7.3 `ApplicationStatusTransitionPolicy`

```
IsTransitionAllowed(from: ApplicationStatus, to: ApplicationStatus) -> bool
IsTerminal(status: ApplicationStatus) -> bool
RequiresReason(to: ApplicationStatus) -> bool
```

The single authority for the §6.2 status machine. `Application.Withdraw`, `MarkExpiredDueToPostingClosure`, and `TransitionStage` all consult it. Keeping the machine in one stateless policy (rather than scattered conditionals) makes the legal-transition table testable in isolation and is a clean teaching artefact.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module. **Integration events** (`IntegrationEvent`) cross the BC boundary — they go through the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract exactly. Other modules depend on these payloads — do not change a field without versioning.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

| Integration event | Raised when | Payload |
|---|---|---|
| `JobBookmarkedIntegrationEvent` | `Bookmark.Create` succeeds | `JobSeekerId`, `JobPostingId`, `OccurredOnUtc` |
| `JobUnbookmarkedIntegrationEvent` | a bookmark is removed | `JobSeekerId`, `JobPostingId`, `OccurredOnUtc` |
| `ApplicationSubmittedIntegrationEvent` | `Application.Submit` succeeds | `ApplicationId`, `JobSeekerId`, `JobPostingId`, `EmployerId`, `Snapshot` (`{fullName, email, isLevel2Complete, resumeDocumentId, skills[]}` — a compact snapshot fingerprint, **not** the full PII blob), `MatchScoreAtApply` (`int?`), `AppliedOnUtc`, `OccurredOnUtc` |
| `ApplicationViewedIntegrationEvent` *[recruiter follow-on]* | `Application.RecordView` | `ApplicationId`, `EmployerId`, `OccurredOnUtc` |
| `ApplicationStatusChangedIntegrationEvent` | any status transition (`TransitionStage`, `MarkExpiredDueToPostingClosure`) | `ApplicationId`, `FromStatus` (`string`), `ToStatus` (`string`), `ByRole` (`string`), `OccurredOnUtc` |
| `ApplicationWithdrawnIntegrationEvent` | `Application.Withdraw` / `WithdrawDueToAccountDeactivation` | `ApplicationId`, `JobSeekerId`, `WithdrawalReasonCode` (`string`), `OccurredOnUtc` |

Consumers (for context only — you do not code them): BC-2 Employer Profile consumes `ApplicationSubmitted` (dashboard new-applicant count) and `ApplicationWithdrawn`; BC-3 JobSeeker Profile consumes `ApplicationSubmitted` / `ApplicationStatusChanged` (the seeker's dashboard projection); BC-7 Recommendation Engine consumes `JobBookmarked` and `ApplicationSubmitted` as preference signals; BC-9 Notification consumes `ApplicationSubmitted`, `ApplicationStatusChanged`, `ApplicationWithdrawn`; BC-10 Reporting consumes all six.

> **PII note.** `ApplicationSubmittedIntegrationEvent.Snapshot` carries only a *compact fingerprint*, not the full `CandidateSnapshot`. The full snapshot stays inside this module; consumers that need profile detail read it via BC-3 or via `JobApplicationPublicApi`. This keeps PII off the event bus — a deliberate, discussable choice.

### 8.2 Internal domain events (NOT published outside the module)

`ApplicationRejected` (raised when `TransitionStage` reaches `Rejected` — used in-module to set `RejectedOnUtc` and could drive an in-module read-model update; the cross-BC fact travels on `ApplicationStatusChanged`). Use internal events for in-module reactions only — they never reach the outbox.

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained for its domain. (Outbox/inbox mechanics: [[00-Shared-Foundations]] §6.2–6.3.)

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent handlers)

| Integration event | From | Payload you receive | Your reaction |
|---|---|---|---|
| `JobPostingExpiredIntegrationEvent` | BC-4 Job Postings | `PostingId`, `ExpiredAt` | For every non-terminal `Application` on that posting, call `MarkExpiredDueToPostingClosure("posting-expired")`. Idempotent. |
| `JobPostingClosedIntegrationEvent` | BC-4 | `PostingId`, `Reason`, `ClosedAt` | Same — `MarkExpiredDueToPostingClosure("posting-closed")` for each non-terminal application. |
| `JobPostingSuspendedIntegrationEvent` | BC-4 | `PostingId`, `By`, `Reason`, `At` | Same — `MarkExpiredDueToPostingClosure("posting-suspended")`. |
| `AccountDeactivatedIntegrationEvent` | BC-1 IAM/UAM | `UserId`, `DeactivatedAt` | For every non-terminal `Application` of that seeker, call `WithdrawDueToAccountDeactivation()`. (Part of the AccountDeactivationCascade.) |
| `UserAccountSuspendedIntegrationEvent` | BC-1 | `UserId`, `Reason`, `By`, `At` | Treat like account deactivation — withdraw the seeker's non-terminal applications. |

**Idempotency** is mandatory — see [[00-Shared-Foundations]] §6.3 (inbox). Note also that the aggregate methods `MarkExpiredDueToPostingClosure` / `WithdrawDueToAccountDeactivation` are naturally idempotent — no-ops on already-terminal applications. Inbound events arrive via the module's integration-event subscription wired in the module composition entry point.

### 9.2 Public APIs this module CALLS (port interfaces — define in `Application`, implement adapters in `Infrastructure`)

```
Port: JobPostingApi             (provided by BC-4 Job Postings; read posting status/deadline at apply-time,
                                 and posting detail for the Interested List view)
  GetApplicability(jobPostingId: uuid)            -> Result<PostingApplicabilitySnapshot>
  GetSummaries(jobPostingIds: list<uuid>)         -> Result<list<PostingSummaryDto>>
  PostingApplicabilitySnapshot {
    JobPostingId: uuid, EmployerId: uuid, Status: string, DeadlineUtc: datetime?
  }
  // Status is one of: Draft, Active, Paused, Expired, Closed, Suspended, Archived.
  PostingSummaryDto {
    JobPostingId: uuid, Title: string, CompanyName: string, Location: string,
    SalaryDisplay: string?, Status: string
  }

Port: JobSeekerProfileApi       (provided by BC-3 JobSeeker Profile; read the live profile snapshot and
                                 the Level-2 gate at apply-time)
  IsLevel2Complete(jobSeekerProfileId: uuid)                          -> bool
  GetSnapshot(jobSeekerProfileId: uuid)                               -> Result<JobSeekerProfileSnapshotDto>
  IsResumeUsable(jobSeekerProfileId: uuid, resumeDocumentId: uuid)     -> Result<bool>
  // IsResumeUsable verifies the resume document belongs to this seeker and is virus-scan-clean.
  JobSeekerProfileSnapshotDto {
    JobSeekerProfileId: uuid, UserId: uuid, FullName: string, Email: string, Mobile: string,
    CurrentLocation: string, EducationSummary: string, ExperienceSummary: string,
    Skills: list<string>, IsLevel2Complete: bool, Visibility: string
  }

Port: MatchRankingPublicApi     (provided by BC-7 Recommendation Engine; best-effort match-score snapshot
                                 for match_score_at_apply — absence is NOT blocking, a null result is acceptable)
  GetMatchScore(jobSeekerId: uuid, jobPostingId: uuid) -> Result<int?>

Port: EmployerAccessApi         (provided by BC-2 Employer Profile / BC-1 IAM; recruiter access checks for
                                 the recruiter-facing read queries)
  CanRecruiterAccessPosting(recruiterId: uuid, jobPostingId: uuid) -> bool
  GetPostingsForRecruiter(recruiterId: uuid)                       -> Result<list<uuid>>
```

For the exercise, `Infrastructure` may provide **stub adapters** for `JobPostingApi`, `JobSeekerProfileApi`, `MatchRankingPublicApi`, and `EmployerAccessApi` (in-memory or fake — e.g. always returns an `Active` posting, an `IsLevel2Complete = true` profile, a fixed match score) so the module runs standalone. Keep the port shapes exactly as above so real adapters drop in later.

### 9.3 Public API this module EXPOSES (in the `Contracts` surface)

```
Public API: JobApplicationPublicApi
  GetActiveApplicationCountForPosting(jobPostingId: uuid)              -> int
  HasNonTerminalApplication(jobSeekerId: uuid, jobPostingId: uuid)     -> bool
  GetSummary(applicationId: uuid)                                     -> ApplicationSummaryDto?
  // Used by BC-2 employer dashboards and BC-3 seeker dashboards for counts/scoping.

ApplicationSummaryDto {
  ApplicationId: uuid, JobPostingId: uuid, JobSeekerId: uuid, EmployerId: uuid,
  Status: string, AppliedOnUtc: datetime, LastStatusChangeOnUtc: datetime
}
```

---

## 10. Application layer

Every use case is a **command** or a **query** with exactly one handler. Commands return `Result`/`Result<T>`; queries return DTOs. Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

### 10.1 Commands

| Command | Story | Handler responsibilities |
|---|---|---|
| `AddBookmarkCommand` | US-3.2.3-01 | Check no existing `Bookmark` for `(seeker, posting)` (`E-BOOKMARK-DUPLICATE`); `Bookmark.Create(...)`; persist. Raises `JobBookmarked`. |
| `RemoveBookmarkCommand` | US-3.2.3-01 | Load the `Bookmark` (`404` if absent); raise `JobUnbookmarked`; delete; persist. |
| `SubmitApplicationCommand` | US-3.2.3-02 | **The core flow.** (1) Check idempotency: if `IdempotencyKey` already seen, return the existing application (`200`). (2) `JobPostingApi.GetApplicability` → posting must be `Active`, deadline future. (3) `JobSeekerProfileApi.IsLevel2Complete` + `GetSnapshot`; `IsResumeUsable` for the chosen resume. (4) Load any existing non-terminal application for `(seeker, posting)`. (5) `ApplicationEligibilityService.CheckCanApply(...)` — surfaces `E-APP-POSTING-CLOSED` / `E-APP-PROFILE-INCOMPLETE` / `E-APP-DUPLICATE` / `E-APP-RESUME-MISSING`. (6) `CandidateSnapshotBuilder.Build(profile, overrides)`. (7) Best-effort `MatchRankingPublicApi.GetMatchScore` (null on failure — non-blocking). (8) If a *terminal* prior application exists, set `ReplacesApplicationId`. (9) `Application.Submit(...)`; persist. Raises `ApplicationSubmitted` → **initiates the ApplicationSubmissionSaga**. |
| `WithdrawApplicationCommand` | US-3.2.3-02 AC-12 | Load the application (must belong to the calling seeker — else `E-APP-FORBIDDEN`); build `WithdrawalReason`; `Application.Withdraw(reason, seekerUserId)` (`E-APP-INVALID-TRANSITION` from terminal states); persist. Raises `ApplicationWithdrawn`. |
| `TransitionApplicationStageCommand` *[recruiter follow-on]* | US-3.2.4-03 | Verify recruiter access via `EmployerAccessApi`; load application; `Application.TransitionStage(toStatus, Recruiter, recruiterId, reasonCode, comment, expectedCurrentStatus)` (`E-APP-STALE` on optimistic mismatch, `E-APP-INVALID-TRANSITION`, `E-APP-REASON-REQUIRED`); persist. Raises `ApplicationStatusChanged`. |
| `RecordApplicationViewCommand` *[recruiter follow-on]* | US-3.2.4-03 | Verify recruiter access; `Application.RecordView(employerId)`; persist. Raises `ApplicationViewed`. |

### 10.2 Queries

| Query | Story | Returns |
|---|---|---|
| `GetMyBookmarksQuery` | US-3.2.3-01 | `list<BookmarkedJobDto>` — the seeker's Interested List; the handler fetches posting details for each bookmark via `JobPostingApi.GetSummaries` (title, company, location, salary, status); sortable/filterable client-side |
| `GetMyApplicationsQuery` | US-3.2.4-03 (seeker side) | `PagedResult<ApplicationListItemDto>` — the seeker's "My Applications": application id, posting title/company, status, applied date, last status change; filter by status, paged, sorted `applied desc` |
| `GetMyApplicationDetailQuery` | US-3.2.4-03 (seeker side) | `ApplicationDetailDto` — full detail for one of the seeker's applications: posting summary, frozen `CandidateSnapshot`, resume reference, cover letter, `MatchScoreAtApply`, the full `ApplicationStage` history; `E-APP-FORBIDDEN` if not the seeker's |
| `GetApplicationsForPostingQuery` *[recruiter follow-on]* | US-3.2.4-03 (recruiter side) | `PagedResult<ApplicationListItemDto>` — applications for one posting the recruiter has access to; default filter excludes `Withdrawn`/`Rejected`; `403` if no access |
| `GetApplicationsForRecruiterQuery` *[recruiter follow-on]* | US-3.2.4-03 (recruiter side) | `PagedResult<ApplicationListItemDto>` — cross-posting view scoped to the recruiter's permitted postings (via `EmployerAccessApi.GetPostingsForRecruiter`) |
| `GetApplicationDetailForRecruiterQuery` *[recruiter follow-on]* | US-3.2.4-03 (recruiter side) | `RecruiterApplicationDetailDto` — frozen snapshot, stage history, current status; `403` if no access |

### 10.3 Validators — representative rules

- `AddBookmarkCommand`: `JobPostingId` non-empty.
- `SubmitApplicationCommand`: `JobPostingId`, `ResumeDocumentId`, `IdempotencyKey` non-empty; `IdempotencyKey` must be a UUID; `CoverLetter` ≤ 4000 chars when present; `SnapshotOverrides`, if present, may only contain the editable fields (name, email, phone, current location) — reject any attempt to override skills/education/experience.
- `WithdrawApplicationCommand`: `ApplicationId` non-empty; `ReasonCode` non-empty and in the known seeker-facing lookup set; `Comment` ≤ 1000 chars.
- `TransitionApplicationStageCommand` *[recruiter follow-on]*: `ToStatus` in the enum; `ExpectedCurrentStatus` provided (optimistic concurrency); `Comment` ≤ 1000 chars.

### 10.4 DTOs

Plain data records in `Application`. Never expose Domain entities/VOs across the API. Map aggregate → DTO in the handler or a mapping helper. `BookmarkedJobDto` and the list DTOs are *composed* from this module's data plus the posting summaries fetched from the `JobPostingApi` port.

---

## 11. Persistence & data model

Schema/namespace: `job_application`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. The module-specific relational model follows.

### 11.1 Reference relational model — schema `job_application`

```
TABLE applications
  id                        uuid          PK
  job_posting_id            uuid          NOT NULL                 -- BC-4 identity, no FK
  job_seeker_id             uuid          NOT NULL                 -- BC-3 identity, no FK
  employer_id               uuid          NOT NULL                 -- BC-2 identity, no FK (denormalised)
  status                    enum          NOT NULL                 -- Submitted|UnderReview|Shortlisted|
                                                                   --  Interview|Offered|Hired|Rejected|
                                                                   --  Withdrawn|Expired
  candidate_snapshot        json          NOT NULL                 -- CandidateSnapshot VO (immutable)
  resume_document_id        uuid          NOT NULL                 -- BC-3 document reference, no FK
  cover_letter              string        NULL                     -- CoverLetter VO (<=4000 chars)
  match_score_at_apply      int           NULL                     -- 0..100, informational
  replaces_application_id   uuid          NULL                     -- FK → applications.id (self), nullable
  idempotency_key           uuid          NOT NULL UNIQUE
  applied_on_utc            datetime      NOT NULL
  last_status_change_on_utc datetime      NOT NULL
  withdrawn_on_utc          datetime      NULL
  hired_on_utc              datetime      NULL
  rejected_on_utc           datetime      NULL
  version_token             (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4;
                             needed for the recruiter-side E-APP-STALE check)
  INDEX (job_seeker_id, status),
  INDEX (employer_id, status, applied_on_utc DESC),
  INDEX (job_posting_id),
  UNIQUE INDEX (job_seeker_id, job_posting_id)
        WHERE status IN ('Submitted','UnderReview','Shortlisted','Interview','Offered')
        -- enforces invariant #2: at most one non-terminal application per (seeker, posting)

TABLE application_stages              -- append-only history; child of applications
  id                 uuid         PK
  application_id     uuid         NOT NULL   -- FK → applications.id ON DELETE CASCADE
  stage              enum         NOT NULL   -- ApplicationStatus value
  entered_on_utc     datetime     NOT NULL
  entered_by_role    enum         NOT NULL   -- Seeker | Recruiter | System
  entered_by_user_id uuid         NULL
  reason_code        string       NULL
  comment            string       NULL
  INDEX (application_id, entered_on_utc)

TABLE bookmarks
  id                uuid          PK
  job_seeker_id     uuid          NOT NULL                         -- BC-3 identity, no FK
  job_posting_id    uuid          NOT NULL                         -- BC-4 identity, no FK
  bookmarked_on_utc datetime      NOT NULL
  UNIQUE (job_seeker_id, job_posting_id)
  INDEX (job_seeker_id)

TABLE withdrawal_reasons              -- seeded lookup reference data
  code             string        PK
  label_en         string        NOT NULL
  label_ar         string        NOT NULL
  is_seeker_facing bool          NOT NULL DEFAULT true

TABLE idempotency_keys                -- submission idempotency (24h retention)
  idempotency_key  uuid          PK
  application_id   uuid          NOT NULL                          -- the application that key produced
  created_on_utc   datetime      NOT NULL
  INDEX (created_on_utc)               -- supports the 24h purge job

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

### 11.2 Module-specific mapping notes

- Follows the persistence and mapping conventions in [[00-Shared-Foundations]] §3 and §6.
- The `Application` aggregate root owns the `application_stages` child collection, loaded with the root. `Bookmark` is a standalone aggregate.
- Value objects map to `json` / scalar columns: `CandidateSnapshot` → `json`; `CoverLetter` → the `cover_letter` text column; `WithdrawalReason` is captured into the relevant `application_stages.reason_code` / `comment` columns at withdrawal time.
- `ReplacesApplicationId` maps to a nullable self-referencing `uuid` — an FK *within* this schema is allowed because it does not cross a module boundary.
- The **partial unique index** on `(job_seeker_id, job_posting_id)` is the database-level guard for invariant #2; the handler still checks first to return a friendly `E-APP-DUPLICATE` with the existing id, but the index is the backstop against a race.
- Optimistic-concurrency token required on `applications` (needed for the recruiter-side `E-APP-STALE` check).
- Seed `withdrawal_reasons` in the initial migration (`ChangedMind`, `AcceptedAnotherOffer`, `NoLongerInterested`, `RoleNotAsExpected`, `AccountDeactivated`).
- **Idempotency:** `idempotency_keys` maps a submission key to the application it created; a background job purges rows older than 24 h.

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

`ApplicationRepository` (`GetById`, `GetByIdempotencyKey`, `GetNonTerminalFor(seekerId, postingId)`, `GetTerminalFor(seekerId, postingId)`, `GetNonTerminalByPosting`, `GetNonTerminalBySeeker`, `ListBySeeker`, `ListByPosting`, `ListByPostings`, `Add`, `Update`), `BookmarkRepository` (`GetById`, `Get(seekerId, postingId)`, `ListBySeeker`, `Add`, `Remove`), `IdempotencyKeyStore` (`TryGet`, `Save`, `PurgeOlderThan`), `UnitOfWork` (`SaveChanges`).

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. Route prefix `/api/applications` and `/api/bookmarks`. All endpoints require a valid access token (issued by BC-1); the authenticated `JobSeekerId` (or recruiter identity) is taken from the token. Recruiter endpoints additionally pass through the `EmployerAccessApi` port for posting-scope checks. Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

| Verb & route | Command/Query | Success | Notable failures |
|---|---|---|---|
| `POST /api/bookmarks` | `AddBookmarkCommand` | `201` + `BookmarkId` | `409 E-BOOKMARK-DUPLICATE` |
| `GET /api/bookmarks` | `GetMyBookmarksQuery` | `200` + `BookmarkedJobDto[]` | |
| `DELETE /api/bookmarks/{postingId}` | `RemoveBookmarkCommand` | `204` | `404` not bookmarked |
| `POST /api/applications` *(header `Idempotency-Key`)* | `SubmitApplicationCommand` | `201` + `{ applicationId, status, matchScoreAtApply }`; `200` + same body on idempotent retry | `409 E-APP-DUPLICATE` (+ `existingApplicationId`), `410 E-APP-POSTING-CLOSED`, `422 E-APP-PROFILE-INCOMPLETE` (+ `missingFields`), `422 E-APP-RESUME-MISSING` |
| `GET /api/applications` | `GetMyApplicationsQuery` | `200` + paged `ApplicationListItemDto` | |
| `GET /api/applications/{id}` | `GetMyApplicationDetailQuery` | `200` + `ApplicationDetailDto` | `403 E-APP-FORBIDDEN`, `404` |
| `POST /api/applications/{id}/withdraw` | `WithdrawApplicationCommand` | `200` + `{ status, withdrawnOnUtc }` | `403 E-APP-FORBIDDEN`, `404`, `409 E-APP-INVALID-TRANSITION` |
| `GET /api/jobs/{postingId}/applications` *[recruiter follow-on]* | `GetApplicationsForPostingQuery` | `200` + paged list | `403 E-APP-FORBIDDEN` |
| `GET /api/employers/applications` *[recruiter follow-on]* | `GetApplicationsForRecruiterQuery` | `200` + paged list | `403` |
| `GET /api/applications/{id}/recruiter-view` *[recruiter follow-on]* | `GetApplicationDetailForRecruiterQuery` | `200` + `RecruiterApplicationDetailDto` | `403`, `404` |
| `PATCH /api/applications/{id}/stage` *[recruiter follow-on]* | `TransitionApplicationStageCommand` | `200` + `{ status }` | `403`, `404`, `409 E-APP-STALE`, `409 E-APP-INVALID-TRANSITION`, `422 E-APP-REASON-REQUIRED` |
| `POST /api/applications/{id}/view` *[recruiter follow-on]* | `RecordApplicationViewCommand` | `204` | `403`, `404` |

Rate limits (from `US-3.2.3-02`): `POST /api/applications` 10/hr/seeker; `POST /api/applications/{id}/withdraw` 5/hr/seeker.

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** every VO factory — valid input succeeds; each invalid case returns the right `Error`. Specifically: `CandidateSnapshot` (empty name/email/mobile fails; `IsLevel2Complete = false` fails), `CoverLetter` (4001 chars fails, empty-when-present fails), `WithdrawalReason` (unknown code fails, comment > 1000 fails).
- **Application aggregate:**
  - `Submit` creates the application in `Submitted`, appends exactly one initial `ApplicationStage` (`Submitted`, `Seeker`), and raises `ApplicationSubmitted`.
  - `Submit` with a `CandidateSnapshot` whose `IsLevel2Complete == false` fails.
  - **Status machine:** every legal transition in §6.2 succeeds; a representative set of illegal ones (`Submitted → Hired`, `Hired → UnderReview`, `Rejected → Shortlisted`) returns `E-APP-INVALID-TRANSITION`.
  - `Withdraw` from each non-terminal status succeeds → `Withdrawn`, sets `WithdrawnOnUtc`, appends a stage, raises `ApplicationWithdrawn`; `Withdraw` from `Hired`/`Rejected`/`Expired` fails; `Withdraw` of an already-`Withdrawn` application is idempotent.
  - `MarkExpiredDueToPostingClosure` from a non-terminal status → `Expired`; from a terminal status is a no-op.
  - `WithdrawDueToAccountDeactivation` → `Withdrawn` with system reason `AccountDeactivated`, `EnteredByRole = System`.
  - `TransitionStage` with a mismatched `expectedCurrentStatus` fails with `E-APP-STALE`; transition to `Rejected` without a reason fails with `E-APP-REASON-REQUIRED`.
  - `ApplicationStage` history is append-only — every status change appends exactly one row; no method updates or removes a stage.
- **Bookmark aggregate:** `Create` succeeds and raises `JobBookmarked`; the aggregate stores only the `(seeker, posting)` pair.
- **Domain services:** `ApplicationStatusTransitionPolicy` — table-driven over the full legal/illegal transition matrix; `IsTerminal` and `RequiresReason` correct for every status. `ApplicationEligibilityService` — posting not `Active` → `E-APP-POSTING-CLOSED`; deadline passed → `E-APP-POSTING-CLOSED`; `isLevel2Complete = false` → `E-APP-PROFILE-INCOMPLETE`; existing non-terminal application → `E-APP-DUPLICATE`; existing *terminal* application → success. `CandidateSnapshotBuilder` — overrides merged into the snapshot only; `IsLevel2Complete = false` profile → failure.

### 13.2 Unit tests — Application (handlers, ports replaced with test doubles)

- `SubmitApplicationCommand`:
  - Happy path: `Active` posting + Level-2 profile → `Application` persisted in `Submitted`, `ApplicationSubmitted` queued to outbox, the idempotency key recorded.
  - `JobPostingApi` returns a `Closed`/`Expired` posting → `E-APP-POSTING-CLOSED`, **no application persisted**.
  - `JobSeekerProfileApi.IsLevel2Complete` returns false → `E-APP-PROFILE-INCOMPLETE`, nothing persisted.
  - An existing non-terminal application for `(seeker, posting)` → `E-APP-DUPLICATE` with the existing id; nothing new persisted.
  - An existing *terminal* application → new `Application` created with `ReplacesApplicationId` populated.
  - **Idempotent retry:** the same `Idempotency-Key` submitted twice → exactly one `Application`; the second call returns the existing id with `200`.
  - `MatchRankingPublicApi` fails/times out → submission still succeeds, `MatchScoreAtApply` is null (non-blocking).
  - `JobSeekerProfileApi.IsResumeUsable` returns false → `E-APP-RESUME-MISSING`.
- `WithdrawApplicationCommand`: a seeker withdrawing their own non-terminal application succeeds; withdrawing another seeker's application → `E-APP-FORBIDDEN`; withdrawing a terminal application → `E-APP-INVALID-TRANSITION`.
- `AddBookmarkCommand` / `RemoveBookmarkCommand`: duplicate bookmark → `E-BOOKMARK-DUPLICATE`; removing a non-existent bookmark → `404`.
- Integration-event handlers: `JobPostingClosedIntegrationEvent` marks every non-terminal application on that posting `Expired`; delivering it twice (same `EventId`) is a no-op the second time. `AccountDeactivatedIntegrationEvent` withdraws all the seeker's non-terminal applications.
- Validation step: each validator rejects the documented bad inputs before the handler runs — especially the `SnapshotOverrides` guard (rejecting attempts to override skills/education).

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip the `Application` aggregate including the `application_stages` child collection and the `candidate_snapshot` `json` VO; round-trip `Bookmark`; `GetByIdempotencyKey` / `GetNonTerminalFor` / `ListBySeeker` work; optimistic-concurrency conflict on `applications` is detected.
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `job_application`; `withdrawal_reasons` is seeded.
- **Partial unique index:** inserting a second non-terminal `Application` for the same `(seeker, posting)` violates the partial unique index; a re-apply after the first is `Withdrawn` succeeds.
- **Outbox:** submitting an application writes both the `applications` row and the `ApplicationSubmitted` outbox message in one transaction; rolling back the transaction leaves neither.
- **Inbox / idempotency:** delivering `JobPostingExpiredIntegrationEvent` twice expires the affected applications once and is a no-op the second time; submitting with the same `Idempotency-Key` twice creates exactly one row.
- **API:** host-level tests for the bookmark → apply → view-my-applications → withdraw happy path; an idempotent double-submit returns the same `applicationId`; applying to a closed posting returns `410`; applying with a Level-1-only profile returns `422` with `missingFields`.
- **Consumed events:** `AccountDeactivatedIntegrationEvent` transitions all the seeker's non-terminal applications to `Withdrawn` with the system reason and appends a `System` stage row.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "Apply to a job" (the ApplicationSubmissionSaga trigger)

End-to-end, to pattern-match every other command against:

1. **API.** `POST /api/applications` with header `Idempotency-Key: <uuid>` (generated by the client when the form opened) and body `{ jobPostingId, resumeDocumentId, coverLetter?, snapshotOverrides? }`. The endpoint reads `JobSeekerId` from the access token, builds `SubmitApplicationCommand`, dispatches it through the mediator.
2. **Validation step.** `SubmitApplicationCommand`'s validator runs: ids non-empty, `IdempotencyKey` a UUID, `coverLetter` ≤ 4000 chars, `snapshotOverrides` restricted to editable fields. On failure → `Result` with `Error`, mapped to `400`.
3. **Handler — `SubmitApplicationCommandHandler`:**
   a. **Idempotency check.** `IdempotencyKeyStore.TryGet(idempotencyKey)` — if found, load that `Application` and return it (`200`, exactly-once from the seeker's view). Otherwise continue.
   b. **Posting check.** `JobPostingApi.GetApplicability(jobPostingId)` → `PostingApplicabilitySnapshot`. Carries `EmployerId` (needed to denormalise onto the `Application`).
   c. **Profile check.** `JobSeekerProfileApi.IsLevel2Complete(jobSeekerId)` + `GetSnapshot(jobSeekerId)`; `IsResumeUsable(jobSeekerId, resumeDocumentId)`.
   d. **Existing-application check.** `ApplicationRepository.GetNonTerminalFor(jobSeekerId, jobPostingId)` and `GetTerminalFor(...)`.
   e. **Eligibility.** `ApplicationEligibilityService.CheckCanApply(...)` — returns `E-APP-POSTING-CLOSED` / `E-APP-PROFILE-INCOMPLETE` / `E-APP-DUPLICATE` (with the existing id) / success. Propagate failures.
   f. **Snapshot.** `CandidateSnapshotBuilder.Build(profileSnapshot, snapshotOverrides)` → the immutable `CandidateSnapshot` (overrides merged in, master profile untouched).
   g. **Match score.** `MatchRankingPublicApi.GetMatchScore(jobSeekerId, jobPostingId)` — best-effort; null on failure, never blocks.
   h. **Re-apply linkage.** If a terminal prior application exists, capture its id as `replacesApplicationId`.
   i. `Application.Submit(jobPostingId, jobSeekerId, employerId, snapshot, resumeDocumentId, coverLetter, matchScore, idempotencyKey, replacesApplicationId)` — the aggregate creates itself in `Submitted`, appends the initial `ApplicationStage`, raises `ApplicationSubmitted`.
   j. `repository.Add(application)`; `IdempotencyKeyStore.Save(idempotencyKey, application.Id)`; `unitOfWork.SaveChanges()`.
4. **Domain-event / outbox step.** After the unit of work is saved, the mediator pipeline dispatches internal domain events in-process and writes `ApplicationSubmittedIntegrationEvent` (compact snapshot fingerprint, no full PII) into the outbox — same transaction as the `applications` row. ([[00-Shared-Foundations]] §6.1–6.2.)
5. **Relay & saga.** The background outbox relay publishes `ApplicationSubmitted`. This is the **trigger of the ApplicationSubmissionSaga** — downstream, BC-9 sends the seeker confirmation + the employer new-applicant notification, BC-2 increments the dashboard count, BC-10 records analytics + audit. Those steps are a *separate package*; this module's responsibility ends at reliable emission.
6. **Response.** Handler returns `Result<SubmitApplicationResult>`; the endpoint returns `201` + `{ applicationId, status: "Submitted", matchScoreAtApply }`.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.2.3-01 Bookmark jobs | Add a job to the Interested List; list is persistent across sessions; Interested List view shows key job details (title, company, location, salary — fetched from BC-4); remove a job from the list. *(Saved search filters in this story are owned by BC-6, not built here.)* |
| US-3.2.3-02 Apply to jobs | Application created in `Submitted` with a frozen `CandidateSnapshot`; form pre-fill / snapshot overrides affect only the snapshot, never the master profile; optional cover letter ≤ 4000 chars; duplicate non-terminal application → `E-APP-DUPLICATE` surfacing the existing one; re-apply after a terminal outcome → new `Application` linked via `ReplacesApplicationId`; closed/expired/paused posting at submit → `E-APP-POSTING-CLOSED`; below-Level-2 profile → `E-APP-PROFILE-INCOMPLETE`; idempotency key → at-most-one application per submission attempt; `ApplicationSubmitted` published (initiating the ApplicationSubmissionSaga); withdraw a non-terminal application → `Withdrawn` + `ApplicationWithdrawn`; private/recruiters-only visibility does not block a direct application. |
| US-3.2.4-03 View / manage applications | **Seeker side (must-build):** "My Applications" list filterable by status, paged; application detail showing the frozen snapshot, resume reference, cover letter, match-score-at-apply, and the full append-only `ApplicationStage` history. **Recruiter side (follow-on):** the `Application` state machine, allowed-transition constraints, optimistic-concurrency `E-APP-STALE` on concurrent stage changes, reason-required transitions, permission scoping (`E-APP-FORBIDDEN`), and the `ApplicationStatusChanged` event are all modelled in the aggregate and reproduced in the API table as `[recruiter follow-on]`; the recruiter *write* commands may be staged after the seeker-facing slice. |

---

## Appendix — teaching notes & open questions

- **Two-file brief.** This package + `00-Shared-Foundations.md` together are the complete brief. The shared file carries everything stack-related and identical across all 12 BCs; this package carries only the domain design. Discuss with the class: which parts of a design are genuinely reusable across modules, and what is the cost of the one shared dependency?
- **One aggregate, two actors.** `Application` is mutated by both the seeker (`Submit`, `Withdraw`) and the recruiter (`TransitionStage`, decisions, offers). The [[BC_Mapping]] keeps both `US-3.2.3-02` and `US-3.2.4-03` in BC-5. We resolved the build by drawing the line at *seeker-facing first* — but the deeper question is worth a class debate: when one entity has two very different actors with different vocabularies, is that a signal to split the BC, or proof that the *consistency boundary* (the `Application`) is the real arbiter of BC scope? We argue the latter — the status machine is one invariant set, so it is one aggregate, so it is one BC.
- **The immutable snapshot.** `CandidateSnapshot` is the textbook example of *why* you copy data at a point in time rather than referencing it live: an employer reviewing an application six weeks later must see the profile *as it was when the seeker applied*, not as it is now. Contrast with the "live profile link" the recruiter *also* gets — two views, deliberately different, both correct. Discuss: what else on the platform deserves a snapshot rather than a reference?
- **Idempotency as a domain concern.** The `IdempotencyKey` is generated client-side at form-open and carried through to a unique DB column plus an `idempotency_keys` table. This is `US-3.2.3-02 AC-14` made concrete. Good prompt: is idempotency an infrastructure concern (a middleware) or a domain concern (a property of the `Application`)? This package treats it as both — the key is on the aggregate *and* there is a store — and that tension is the lesson.
- **`ApplicationSubmitted` carries a fingerprint, not PII.** §8.1's note: the integration event deliberately does not put the full `CandidateSnapshot` on the bus. Consumers needing detail call back via a public API. Discuss the trade-off — fatter events (everything a consumer needs, no callback) vs. thinner events (less PII exposure, more coupling on the callback API).
- **The duplicate-prevention double guard.** Invariant #2 is enforced *both* by a handler pre-check (to return a friendly `E-APP-DUPLICATE` with the existing id) *and* by a partial unique index (the backstop against a race between two concurrent submits). Neither alone is sufficient — a useful illustration of "validate for UX, constrain for correctness."
- **Saga initiator vs. saga.** This module *triggers* the ApplicationSubmissionSaga but does not *implement* it. The outbox guarantees the trigger event is emitted exactly once even on post-commit failure; everything after that — the choreography across BC-9/BC-2/BC-10 — is a separate package. Discuss where a saga "lives": in the initiator, in a dedicated process-manager BC, or nowhere (pure choreography)?
- **Bookmarks vs. saved searches — a BC-boundary split inside one story.** `US-3.2.3-01` bundles two features; the [[BC_Mapping]] migration note splits them — bookmarking to BC-5 (pre-application *intent*), saved searches to BC-6 (a *search* concern). A clean example of resisting "the story said so" and letting the *ubiquitous language* decide ownership: a bookmark is about a *job you might apply to*; a saved search is about *how you look for jobs*.
- **Posting-closure cascade.** When BC-4 closes a posting, this module expires the in-flight applications — but only the *non-terminal* ones, and the operation is naturally idempotent. Note this is *choreography* (react to an event), not a saga: there is no compensation, no multi-step coordination, just a local reaction. Contrast with the AccountDeactivationCascade, which the [[BC_Mapping]] flags as a genuine (if currently un-triggered) saga.
