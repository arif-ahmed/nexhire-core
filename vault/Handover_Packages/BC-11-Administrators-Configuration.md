---
title: "Handover Package — BC-11 Administrators Configuration"
type: handover-package
bc_id: BC-11
bc_name: Administrators Configuration
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
  - bc/admin-config
  - bc/skill-taxonomy
---

# Handover Package — BC-11 Administrators Configuration

> **Audience:** an AI coding agent. This package owns the **domain design** for the `AdministratorsConfiguration` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `AdministratorsConfiguration`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `admin_config`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

**This is an intentionally thin BC.** It owns exactly one story (`US-3.1.4-02 Manage taxonomies`). Do not pad it. Its job is to be the single owner of the platform's **skill / occupation / training-program taxonomies** and to expose them as a **Published Language** to BC-3, BC-4, BC-6, BC-7, and BC-8. The thinness is deliberate and is itself a teaching point — see the appendix.

---

## 1. Purpose & scope boundaries

### What this BC is for

Administrators Configuration owns the **platform reference vocabulary**: the canonical taxonomies of **skills**, **occupations**, and **training programs**. It is the single source of truth for what a "skill code" *means* across the whole platform. It is a **generic** subdomain — system reference-data management is a solved, non-differentiating problem — but it is *load-bearing*: five other BCs validate their data against it.

### In scope

The `AdministratorsConfiguration` module is responsible for:

- The **three taxonomies** — Skills, Occupations, Training Programs — each modelled as a `Taxonomy` aggregate holding a tree of `TaxonomyTerm` entities.
- **CRUD on terms**: add a term, edit a term's label/category/parent, **deprecate** a term (never hard-delete — deprecation preserves historical references), and optionally point a deprecated term at a `ReplacedBy` successor.
- **Hierarchy management**: occupations follow `category → job group → job title`; skills have a `Hard`/`Soft` category; training programs are flat with optional grouping.
- **Usage statistics** — a denormalised `UsageCount` per term, updated from integration events emitted by consuming BCs (how many profiles/postings reference this term).
- **Bulk import** of skills/occupations from a CSV, with per-row validation and a success/failure report.
- **Versioning** — every committed change to a taxonomy bumps its `Version` integer; the version travels in the published event so consumers can detect staleness.
- Exposing the **`TaxonomyApi`** public contract (§9.3) — synchronous lookup used by neighbouring BCs.
- Publishing the integration events in §8: `TaxonomyUpdated`, `TaxonomyTermAdded`, `TaxonomyTermDeprecated`.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong to other BCs and are reached via the contracts in §9, or are simply not this BC's concern:

- **Any other "admin configuration."** Matching-parameter weights and match thresholds → BC-7 Recommendation Engine. Notification templates and channel config → BC-9 Notification. Report access controls → BC-10 Reporting. Each lives with the aggregate it configures. This BC owns *taxonomies only* (see appendix for the debate).
- **User accounts, roles, admin login, permissions** → BC-1 IAM and UAM. This module assumes the caller is already an authenticated MoL Administrator; it does not authenticate.
- **Storing or validating *profile* skills / *posting* skills** → BC-3 and BC-4 respectively. Those BCs hold their own `CanonicalSkillRef` columns; this BC only tells them whether a code is *valid* and what it *maps to*.
- **Resume parsing / NLP skill extraction** → BC-3 (orchestration) and BC-7 (semantic analysis). This BC supplies the canonical vocabulary they map *into*; it does not parse anything.
- **Skill-demand analytics / trend reporting** → BC-10 Reporting. This BC emits `TaxonomyUpdated`; BC-10 builds the read models.
- **Search synonyms / faceting** → BC-6 Search & Discovery maintains its own synonym sets; it merely reacts to `TaxonomyUpdated`.
- **Audit log of admin actions** → BC-10 Reporting consumes the events for audit. This module keeps a *local* change log on the aggregate for its own optimistic-version bookkeeping, not a platform audit trail.
- **Computing `UsageCount` itself.** This module does not scan other modules' tables. It maintains `UsageCount` only by reacting to usage-signal events (see §9.1) — and for the course it is acceptable to stub this as eventually-consistent or even leave it at 0.

### Boundary note — Published Language, not Shared Kernel (teaching point)

The taxonomy is exposed as a **Published Language**: a formal, versioned, read-only contract (`TaxonomyApi` + the three events). Consumers do **not** share this BC's domain types — they hold plain `taxonomy_code` strings and a small `CanonicalSkillRef` value object of their own. We deliberately did **not** make this a **Shared Kernel** (a shared compiled model). The appendix discusses why, and why a reasonable team could choose the other way.

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **Taxonomy** | One of the three controlled vocabularies — `Skills`, `Occupations`, `TrainingPrograms`. The aggregate root of this BC. |
| **Taxonomy Kind** | `Skills` \| `Occupations` \| `TrainingPrograms`. Exactly three `Taxonomy` aggregates exist, one per kind. |
| **Taxonomy Term** | A single entry in a taxonomy — a skill, an occupation, or a training program. Child entity of `Taxonomy`. |
| **Term Code** | The stable, immutable, machine identifier of a term (e.g. `SKILL.PYTHON`, `OCC.SOFTWARE-ENGINEER`). This is what other BCs store. Never reused, never changed. |
| **Display Label** | The human-readable name of a term (e.g. "Python", "Software Engineer"). May change; the code may not. |
| **Term Status** | `Active` (selectable) or `Deprecated` (kept for historical references, not offered to new data). There is **no `Deleted`**. |
| **Skill Category** | For Skills taxonomy only: `Hard` (technical) or `Soft` (interpersonal). |
| **Parent / Hierarchy** | A term may have a `ParentCode` within the same taxonomy. Occupations form `category → job group → job title`. Skills are effectively flat (parent optional). Training programs flat. |
| **ReplacedBy** | When a term is deprecated, an optional pointer to the `TermCode` that supersedes it (used by consumers to migrate). |
| **Usage Count** | Denormalised integer: how many external records (profiles, postings) currently reference this term. Maintained from usage-signal events; advisory only. |
| **Taxonomy Version** | A monotonically increasing integer on the `Taxonomy` aggregate, bumped on every committed mutation. Travels in `TaxonomyUpdated` so consumers detect staleness. |
| **Bulk Import** | A one-shot CSV ingest of many terms into one taxonomy, with a per-row `ImportRowResult`. |
| **Published Language** | The strategic pattern: this BC publishes a stable, versioned vocabulary; consumers conform to it without sharing its internal model. |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `AdministratorsConfiguration`.
- Public surface (`Contracts`): the integration events published in §8 + the public API in §9.3 (`TaxonomyApi`) plus its DTOs (`CanonicalSkillRef`, `TaxonomyTermDto`).
- **Module-specific notes:** the only background worker is the standard outbox relay. There are no scheduled jobs. The `TaxonomyApi` adapter (§9.3) is registered as a cached service in the composition entry point; an in-process domain-event handler invalidates the cache when a mutation commits.

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has **one aggregate**: `Taxonomy`. There are exactly three instances of it — one per `TaxonomyKind`. (`BulkImportJob` is modelled as a transient Application-layer concern, not an aggregate — see §10.) (Notation: see [[00-Shared-Foundations]] §2.)

### 5.1 Aggregate: Taxonomy

**Aggregate root.** Identity: `TaxonomyId` (strongly-typed id wrapping `uuid`). The `TaxonomyTerm` collection is owned by the root and only mutated through it — this is what lets the root enforce "no duplicate code", "valid parent", and "version bump on every change" as invariants.

| Member | Type | Notes |
|---|---|---|
| `Id` | `TaxonomyId` | |
| `Kind` | `TaxonomyKind` | enum: `Skills`, `Occupations`, `TrainingPrograms`. Immutable. Unique. |
| `Name` | `string` | display name, e.g. "Skills Taxonomy" |
| `Version` | `int` | starts at 1; bumped by `IncrementVersion()` on every committed mutation |
| `Terms` | `list<TaxonomyTerm>` | child entities; the tree |
| `CreatedOnUtc` | `datetime` | |
| `UpdatedOnUtc` | `datetime` | stamped on every mutation |

**Child entity: `TaxonomyTerm`** (identity local to the aggregate; only mutated through `Taxonomy`):

| Member | Type | Notes |
|---|---|---|
| `Id` | `TaxonomyTermId` | strongly-typed id wrapping `uuid` — surrogate key for persistence |
| `Code` | `TermCode` | VO — the stable public identifier. Immutable after creation. Unique within the taxonomy. |
| `Label` | `string` | display label; editable; non-empty; ≤ 200 chars |
| `Category` | `SkillCategory?` | only meaningful for `Kind == Skills` (`Hard`/`Soft`); null otherwise |
| `ParentCode` | `TermCode?` | optional; must reference an existing `Active` term in the same taxonomy; no cycles |
| `Status` | `TermStatus` | `Active` (default) or `Deprecated` |
| `ReplacedByCode` | `TermCode?` | set only when `Status == Deprecated`; must reference an existing term in the same taxonomy |
| `UsageCount` | `int` | denormalised; default 0; updated by usage-signal event handlers |
| `CreatedOnUtc` | `datetime` | |
| `DeprecatedOnUtc` | `datetime?` | non-null iff `Status == Deprecated` |

### 5.2 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `TermCode` | `Value` (`string`) | non-empty; ≤ 64 chars; uppercased on store; allowed chars `[A-Z0-9.\-_]`; must contain at least one `.` separating a namespace prefix (`SKILL.`, `OCC.`, `TRAIN.`) from the rest. The prefix must agree with the owning `TaxonomyKind`. |
| `TaxonomyKind` | `Value` (enum) | `Skills`, `Occupations`, `TrainingPrograms` |
| `SkillCategory` | `Value` (enum) | `Hard`, `Soft` |
| `TermStatus` | `Value` (enum) | `Active`, `Deprecated` |
| `CanonicalSkillRef` | `TaxonomyCode` (`string`), `DisplayLabel` (`string`) | **This VO also lives in `Contracts`** — it is the shape `TaxonomyApi` returns. Code non-empty; label non-empty. Consumers (BC-3 etc.) hold their *own* copy of this shape; we publish it as part of the Published Language. |

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior lives on the `Taxonomy` aggregate root. Handlers never set `TaxonomyTerm` properties directly. Method shapes below are neutral specifications (see [[00-Shared-Foundations]] §2).

### 6.1 Taxonomy — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Create(kind, name)` | Creates an empty taxonomy at `Version = 1`. One per `Kind` (uniqueness enforced by the repository + a DB unique index). | `TaxonomyCreated` *(internal)* |
| `AddTerm(code, label, category, parentCode)` | `code` valid `TermCode` with prefix matching `Kind` (`E-TAXO-CODE-PREFIX-MISMATCH`). **No existing term with the same `Code`** (`E-TAXO-DUPLICATE-CODE`). If `parentCode` given it must exist and be `Active` (`E-TAXO-PARENT-NOT-FOUND` / `E-TAXO-PARENT-DEPRECATED`); adding must not create a cycle (`E-TAXO-CYCLE`). For `Kind == Skills`, `category` is **required** (`E-TAXO-CATEGORY-REQUIRED`); for other kinds it must be null. New term is `Active`. Calls `IncrementVersion()`. | `TaxonomyTermAdded`; `TaxonomyUpdated` |
| `RenameTerm(code, newLabel)` | term must exist; `newLabel` non-empty ≤ 200. `Code` is **not** touched. Calls `IncrementVersion()`. | `TaxonomyUpdated` |
| `RecategorizeTerm(code, newCategory)` | only valid for `Kind == Skills`; term must exist; `newCategory` in enum. Calls `IncrementVersion()`. | `TaxonomyUpdated` |
| `ReparentTerm(code, newParentCode)` | term must exist; `newParentCode` (if non-null) must exist, be `Active`, same taxonomy, and not create a cycle (`E-TAXO-CYCLE`). Calls `IncrementVersion()`. | `TaxonomyUpdated` |
| `DeprecateTerm(code, replacedByCode?)` | term must exist and be `Active` (idempotent no-op if already `Deprecated`, returns success). If `replacedByCode` given it must exist in the same taxonomy and not equal `code` (`E-TAXO-SELF-REPLACE`). Sets `Status = Deprecated`, `DeprecatedOnUtc = now`. **Does not delete.** Terms that have `Active` children may be deprecated, but emit a warning in the result message — children are *not* cascaded (consumer's decision). Calls `IncrementVersion()`. | `TaxonomyTermDeprecated`; `TaxonomyUpdated` |
| `ReactivateTerm(code)` | term must exist and be `Deprecated`. Clears `ReplacedByCode` and `DeprecatedOnUtc`. Its `parentCode`, if set, must still point to an `Active` term (`E-TAXO-PARENT-DEPRECATED`). Calls `IncrementVersion()`. | `TaxonomyUpdated` |
| `ImportTerms(rows)` | applies a batch of add-term operations (see `TaxonomyImportService` §7). Each row succeeds or fails independently; the aggregate applies only the valid rows. `IncrementVersion()` called **once** for the whole batch if ≥ 1 row succeeded. | `TaxonomyTermAdded` *(one per imported term)*; `TaxonomyUpdated` *(once)* |
| `ApplyUsageDelta(code, delta)` | term must exist; `UsageCount = max(0, UsageCount + delta)`. **Does NOT** bump `Version` and does **NOT** raise `TaxonomyUpdated` — usage is advisory metadata, not part of the published vocabulary. | — |
| `IncrementVersion()` *(private/protected)* | `Version += 1`; `UpdatedOnUtc = now`. Called internally by every vocabulary-changing behavior above. | — |

### 6.2 Core invariants (must always hold)

1. **Exactly three taxonomies** exist platform-wide, one per `TaxonomyKind`. `Kind` is immutable and unique (DB unique index on `kind`).
2. **Term codes are unique within a taxonomy** and **immutable** once created. A code is never reused, never renamed.
3. **Code prefix agrees with kind**: `SKILL.*` only in the Skills taxonomy, `OCC.*` only in Occupations, `TRAIN.*` only in Training Programs.
4. **No hard delete.** A term leaves circulation only by `DeprecateTerm`. `Deprecated` terms remain queryable forever so external references never dangle.
5. **Parent integrity**: `ParentCode`, when set, references an existing term in the *same* taxonomy. The parent must be `Active` at the time of `AddTerm` / `ReparentTerm` / `ReactivateTerm`. The hierarchy is a forest — **no cycles**.
6. **Skill category rule**: for `Kind == Skills`, every term has a non-null `SkillCategory`; for the other two kinds, `Category` is always null.
7. **`ReplacedByCode` is set iff `Status == Deprecated`** and never points to itself; `DeprecatedOnUtc` is non-null iff `Status == Deprecated`.
8. **`Version` is monotonically increasing** and is bumped on **every** vocabulary mutation (add/rename/recategorize/reparent/deprecate/reactivate/import) and on **no** non-vocabulary mutation (usage deltas do not bump it).
9. **`TaxonomyUpdated` is published exactly once per committed mutation transaction** (a bulk import that touched 500 terms still emits **one** `TaxonomyUpdated`, plus one `TaxonomyTermAdded` per term).

### 6.3 Why a single aggregate (not one per term)

A `TaxonomyTerm` is **not** its own aggregate. The "unique code", "valid parent", "no cycle", and "one version per change" rules all span the whole term set, so the consistency boundary must be the whole `Taxonomy`. The cost is contention — two admins editing the Skills taxonomy concurrently will hit the optimistic-concurrency token (§11.2). For a generic, low-write reference-data BC this is the right trade; see the appendix.

---

## 7. Domain services

Stateless. Live in `Domain`. Used when logic spans entities or needs a small external input.

### 7.1 `TaxonomyImportService`

```
ValidateAndStage(taxonomy: Taxonomy, rows: list<RawImportRow>) -> ImportResult

RawImportRow    { RowNumber: int, Code: string, Label: string, Category: string?, ParentCode: string? }
ImportRowResult { RowNumber: int, Succeeded: bool, ErrorCode: string?, Message: string? }
ImportResult    { Rows: list<ImportRowResult>, SucceededCount: int, FailedCount: int }
```

Takes the loaded `Taxonomy` aggregate and a list of parsed CSV rows. For each row it runs the *same* validation `AddTerm` would (prefix match, duplicate code, parent exists & active, no cycle, category rule), accumulating per-row results **without throwing**. Rows that depend on earlier rows in the same batch (a child whose parent is also being imported) are resolved by **topologically ordering** the batch first. Valid rows are applied via `taxonomy.AddTerm(...)`; invalid rows are reported. Partial import is allowed and expected — mirrors `US-3.1.4-02 AC-10` ("reporting success/failure for each row").

### 7.2 `TermCodeValidator`

```
Validate(code: TermCode, kind: TaxonomyKind) -> Result
```

Encapsulates invariant #3 (prefix-agrees-with-kind) so both `AddTerm` and `ImportTerms` share one implementation. Maps `Skills → "SKILL."`, `Occupations → "OCC."`, `TrainingPrograms → "TRAIN."`.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module. **Integration events** (`IntegrationEvent`) cross the BC boundary — they go through the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract exactly. Five other BCs depend on these payloads — do not change a field without versioning.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

| Integration event | Raised when | Payload | Consumers (context only) |
|---|---|---|---|
| `TaxonomyUpdatedIntegrationEvent` | **Any** committed vocabulary mutation (add / rename / recategorize / reparent / deprecate / reactivate / import). Emitted **once per transaction**. | `TaxonomyId`, `Kind` (`string`), `Version` (`int`), `ChangeSummary` (`string` — e.g. `"3 terms added, 1 deprecated"`), `OccurredOnUtc` | BC-3, BC-4, BC-6, BC-7, BC-8, BC-10 |
| `TaxonomyTermAddedIntegrationEvent` | A new term is added (one event per term, including each imported row that succeeded). | `TaxonomyId`, `Kind` (`string`), `TermCode` (`string`), `Label` (`string`), `Category` (`string?`), `ParentCode` (`string?`), `OccurredOnUtc` | BC-3, BC-4, BC-6, BC-7 |
| `TaxonomyTermDeprecatedIntegrationEvent` | A term is deprecated. | `TaxonomyId`, `Kind` (`string`), `TermCode` (`string`), `ReplacedByCode` (`string?`), `OccurredOnUtc` | BC-3, BC-4, BC-6, BC-7 |

**Ordering note:** within one transaction, write `TaxonomyTermAdded` / `TaxonomyTermDeprecated` rows to the outbox *before* the single `TaxonomyUpdated` row, so a consumer that processes the outbox in order sees the term-level facts before the version bump. Consumers must still tolerate any order (the inbox dedupes; `Version` lets them reconcile).

### 8.2 Internal domain events (NOT published outside the module)

`TaxonomyCreated`. Used only for in-module bookkeeping (e.g. logging the seeding of the three taxonomies at startup). It never reaches the outbox. (`TaxonomyTermAdded`, `TaxonomyTermDeprecated`, `TaxonomyUpdated` are raised as domain events on the aggregate too, but a pipeline behavior maps them to their integration-event counterparts and writes those to the outbox — see §10.)

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained for its domain. (Outbox/inbox mechanics: [[00-Shared-Foundations]] §6.2–6.3.)

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent handlers)

This BC consumes **nothing required** for the course's core scope — the [[Event_Catalog]] lists BC-11's "Consumes" as empty. However, to keep `UsageCount` (`US-3.1.4-02 AC-09`) non-trivial, the module *optionally* subscribes to usage-signal events from the BCs that reference taxonomy codes. Treat this as a stretch goal; a stub that leaves `UsageCount` at 0 is acceptable for the exercise.

| Integration event (optional) | From | Payload you receive | Your reaction |
|---|---|---|---|
| `ProfileSkillsUpdatedIntegrationEvent` | BC-3 JobSeeker Profile | `JobSeekerProfileId`, `AddedSkills` (`list<string>` taxonomy codes), `RemovedSkills` (`list<string>`), `OccurredOnUtc` | For each added code `ApplyUsageDelta(code, +1)`; for each removed code `ApplyUsageDelta(code, -1)`. Idempotent via inbox dedupe on `EventId`. |
| `JobPostingPublishedIntegrationEvent` | BC-4 Job Postings | `PostingId`, `EmployerId`, `Title`, `SkillCodes` (`list<string>`), `OccupationCode` (`string?`), `OccurredOnUtc` | `ApplyUsageDelta(+1)` for each referenced skill/occupation code. |
| `JobPostingClosedIntegrationEvent` / `JobPostingExpiredIntegrationEvent` | BC-4 | `PostingId`, `OccurredOnUtc` (+ the prior skill set, if the consumer chooses to cache it) | Decrement usage for the posting's codes. For the course it is fine to skip decrements and treat `UsageCount` as a cumulative counter. |

**Idempotency** is mandatory — see [[00-Shared-Foundations]] §6.3 (inbox). Inbound events arrive via the module's integration-event subscription wired in the module composition entry point.

### 9.2 Public APIs this module CALLS

**None.** This BC is a pure upstream. It does not call any other module's API and does not depend on any other module's `Contracts` project. (This is part of what makes it "generic" and a clean Published-Language supplier.) The only external dependency is a `CsvReader` port for bulk import, which is local infrastructure, not another BC:

```
Port: CsvReader                 (local infrastructure — parses an uploaded CSV stream into raw rows)
  Read(csvContent: bytes) -> Result<list<RawImportRow>>
```

For the exercise, `Infrastructure` provides a real CSV adapter or a simple hand-rolled parser — either is fine.

### 9.3 Public API this module EXPOSES (in the `Contracts` surface)

This is the **Published Language** surface. BC-3's exemplar package (§9.2) already references `TaxonomyApi` with `MapSkill` and `IsValidSkillCode` — implement it precisely so BC-3, BC-4, BC-7, BC-8 can drop it in. Keep the synchronous lookup methods fast (they sit on those BCs' write paths); back them with an in-memory cache of the Skills/Occupations taxonomies, invalidated when this module commits a mutation.

```
Public API: TaxonomyApi

  // --- Skills (the surface BC-3's exemplar §9.2 already depends on) ---

  // Maps a raw, free-text skill label to a canonical skill. Used by BC-3 (resume merge,
  // manual skill add) and BC-7 (NLP extraction). Fuzzy/normalised match against the
  // Skills taxonomy's Active terms; returns the best canonical match or an Error
  // (E-TAXO-NO-MATCH) if nothing maps confidently.
  MapSkill(rawSkillLabel: string) -> Result<CanonicalSkillRef>

  // Cheap validity check for a skill code. Returns true only if the code exists in the
  // Skills taxonomy AND is Active. Deprecated codes return false (callers should migrate
  // via ReplacedByCode — see GetTerm). Used by BC-3/BC-4 validators.
  IsValidSkillCode(taxonomyCode: string) -> bool

  // --- General term lookup (Occupations / Training Programs / Skills) ---

  // Returns the canonical term (any kind) for a code, including Status and ReplacedByCode,
  // so a consumer reacting to TaxonomyTermDeprecated can resolve the successor. Null if the
  // code does not exist in any taxonomy.
  GetTerm(taxonomyCode: string) -> TaxonomyTermDto?

  // Bulk validity check — used by BC-4 when validating a whole posting's skill list, or
  // BC-8 when normalising an ingested foreign posting. Returns, per input code, whether it
  // is a valid Active code.
  AreValidCodes(taxonomyCodes: list<string>) -> map<string, bool>

  // Current version of a taxonomy — lets a consumer cheaply detect whether its cached copy
  // is stale after receiving a TaxonomyUpdated event.
  GetTaxonomyVersion(kind: string) -> int        // kind: "Skills"|"Occupations"|"TrainingPrograms"

CanonicalSkillRef { TaxonomyCode: string, DisplayLabel: string }

TaxonomyTermDto {
  TaxonomyCode: string,
  Kind: string,                  // "Skills" | "Occupations" | "TrainingPrograms"
  Label: string,
  Category: string?,             // "Hard" | "Soft" | null
  ParentCode: string?,
  Status: string,                // "Active" | "Deprecated"
  ReplacedByCode: string?,
  UsageCount: int
}
```

**Implementation:** the `TaxonomyApi` adapter lives in `Infrastructure`, reads through the repository, and is registered as a singleton-friendly cached service in the module composition entry point. After any committed mutation, the in-process domain-event handler for `TaxonomyUpdated` invalidates the cache. Other modules get `TaxonomyApi` from the DI container; they reference only the `Contracts` surface.

---

## 10. Application layer

Every use case is a **command** or a **query** with exactly one handler. Commands return `Result`/`Result<T>`; queries return DTOs. Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

### 10.1 Commands

| Command | Story / AC | Handler responsibilities |
|---|---|---|
| `SeedTaxonomiesCommand` | bootstrap | Idempotent. If the three `Taxonomy` aggregates do not exist, create them (`Taxonomy.Create(Skills,…)` etc.). Run once at startup (or via an admin endpoint). |
| `AddTaxonomyTermCommand` | US-3.1.4-02 AC-02/06/08 | Load the `Taxonomy` for `Kind` → `taxonomy.AddTerm(code, label, category, parentCode)` → `repository.Update` → `unitOfWork.SaveChanges`. Surface `E-TAXO-*` codes. |
| `RenameTaxonomyTermCommand` | US-3.1.4-02 AC-03 | Load → `RenameTerm` → persist. |
| `RecategorizeSkillCommand` | US-3.1.4-02 AC-03 | Load Skills taxonomy → `RecategorizeTerm` → persist. Rejects non-Skills kinds at the validator. |
| `ReparentTaxonomyTermCommand` | US-3.1.4-02 AC-06 | Load → `ReparentTerm` → persist. |
| `DeprecateTaxonomyTermCommand` | US-3.1.4-02 AC-04 | Load → `DeprecateTerm(code, replacedByCode?)` → persist. This is the "Disable" action in the story — note the term is **kept**, not deleted (AC-04: "existing profiles retain it"). |
| `ReactivateTaxonomyTermCommand` | US-3.1.4-02 (implied) | Load → `ReactivateTerm` → persist. |
| `BulkImportTaxonomyCommand` | US-3.1.4-02 AC-10 | `CsvReader.Read` → load `Taxonomy` → `TaxonomyImportService.ValidateAndStage` → persist (one `IncrementVersion`, one `TaxonomyUpdated`, N `TaxonomyTermAdded`) → return the `ImportResult` so the caller gets the per-row report. |

### 10.2 Queries

| Query | Story / AC | Returns |
|---|---|---|
| `GetTaxonomyQuery` (by kind) | US-3.1.4-02 AC-01/05/07 | `TaxonomyDto` — kind, version, and the full hierarchical term list (`TaxonomyTermNodeDto` tree). |
| `GetTaxonomyTermQuery` (by code) | US-3.1.4-02 AC-09 | `TaxonomyTermDetailDto` — single term incl. `UsageCount`, status, parent, children codes. |
| `SearchTaxonomyTermsQuery` | US-3.1.4-02 AC-01 | `list<TaxonomyTermDto>` — filter by kind, label substring, category, status; for the admin UI's term picker. |
| `GetTaxonomyUsageStatsQuery` | US-3.1.4-02 AC-09 | `list<TermUsageDto>` — `(code, label, usageCount)` ordered by usage desc. |

> Note: `TaxonomyApi` (§9.3) is **not** a mediator query — it is a synchronous public contract for *other modules*. The mediator queries above are for **this BC's own admin API/UI**. Both read through the same repository; do not duplicate logic.

### 10.3 Validators — representative rules

- `AddTaxonomyTermCommand`: `kind` in enum; `code` non-empty, ≤ 64, matches `^[A-Z0-9.\-_]+$`, contains `.`; `label` non-empty ≤ 200; for `kind == Skills` `category` ∈ {`Hard`,`Soft`} and required, else `category` must be null; `parentCode` (if present) well-formed. (Existence / cycle / prefix-mismatch are *aggregate* invariants, not validator rules — the validator only checks shape.)
- `RenameTaxonomyTermCommand`: `code` well-formed; `newLabel` non-empty ≤ 200.
- `RecategorizeSkillCommand`: `kind` must be `Skills` (`E-TAXO-CATEGORY-REQUIRED` family); `newCategory` ∈ {`Hard`,`Soft`}.
- `DeprecateTaxonomyTermCommand`: `code` well-formed; `replacedByCode` (if present) well-formed and `!= code`.
- `BulkImportTaxonomyCommand`: `kind` in enum; uploaded file present; size ≤ 5 MB; mime `text/csv`.

### 10.4 DTOs

Plain data records in `Application` (the admin-facing ones) and in `Contracts` (the Published-Language ones — `CanonicalSkillRef`, `TaxonomyTermDto`). Never expose Domain entities/VOs across either boundary. Map `Taxonomy`/`TaxonomyTerm` → DTO in the handler or a mapping helper.

---

## 11. Persistence & data model

Schema/namespace: `admin_config`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. This BC has *no* references to other modules' IDs at all (it is pure upstream), so the no-cross-module-FK rule is trivially satisfied. The only FK is internal: `taxonomy_terms.taxonomy_id → taxonomies.id`.

### 11.1 Reference relational model — schema `admin_config`

```
TABLE taxonomies
  id                  uuid          PK
  kind                enum          NOT NULL UNIQUE          -- Skills|Occupations|TrainingPrograms
  name                string        NOT NULL
  version             int           NOT NULL DEFAULT 1
  created_on_utc      datetime      NOT NULL
  updated_on_utc      datetime      NOT NULL
  version_token       (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  INDEX (kind)

TABLE taxonomy_terms
  id                  uuid          PK                       -- surrogate
  taxonomy_id         uuid          NOT NULL                 -- FK → taxonomies.id ON DELETE CASCADE
  code                string        NOT NULL                 -- TermCode VO, uppercased
  label               string        NOT NULL
  category            enum          NULL                     -- Hard|Soft, only for Skills taxonomy
  parent_code         string        NULL                     -- references code within same taxonomy (logical, no FK)
  status              enum          NOT NULL DEFAULT 'Active' -- Active|Deprecated
  replaced_by_code    string        NULL
  usage_count         int           NOT NULL DEFAULT 0
  created_on_utc      datetime      NOT NULL
  deprecated_on_utc   datetime      NULL
  UNIQUE (taxonomy_id, code)
  INDEX (taxonomy_id)
  INDEX (code)
  INDEX (taxonomy_id, status)
  INDEX (taxonomy_id, parent_code)
  INDEX (taxonomy_id, lower(label))                          -- supports MapSkill / search

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

> **`parent_code` has no FK** even though it points within the same module. It references `code` (a non-PK natural key), and a self-referential FK would block topological bulk import (a child row inserted before its parent). Integrity is enforced by the aggregate (invariant #5), which is the correct place — the index on `(taxonomy_id, parent_code)` is for read performance only.

### 11.2 Module-specific mapping notes

- The `Taxonomy` aggregate root and its `TaxonomyTerm` children are mapped by the chosen ORM. `Terms` is an **owned collection** of the `Taxonomy` aggregate — load it together with its root. Never load or mutate a `TaxonomyTerm` outside its `Taxonomy`.
- `TermCode` flattens to a scalar `string` column with a value converter (uppercased on store). `TaxonomyKind` / `SkillCategory` / `TermStatus` map to enum columns via converters.
- An optimistic-concurrency token is required on `taxonomies`. Because the whole taxonomy is one aggregate, two concurrent admin edits to the same taxonomy will conflict — the second `SaveChanges` fails, which the handler maps to `E-TAXO-CONCURRENCY-CONFLICT` (`409`). This is acceptable for low-write reference data; see appendix.
- A bulk import writes N `TaxonomyTermAdded` + 1 `TaxonomyUpdated` rows into the outbox in the *same transaction* as the term inserts.
- **Seeding:** the migration (or `SeedTaxonomiesCommand`) creates the three `Taxonomy` rows. The course may also seed a starter set of common skill/occupation terms — optional.

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

`TaxonomyRepository` (`GetByKind`, `GetByKindWithTerms`, `Add`, `Update`, `FindTermByCode(kind, code)`, `SearchTerms(kind, labelLike, category, status)`), `UnitOfWork` (`SaveChanges`). The `TaxonomyApi` adapter (§9.3) also depends on `TaxonomyRepository` plus an in-memory cache.

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. Route prefix `/api/admin/taxonomies`. **Every endpoint requires a valid access token with the `MoLAdministrator` role** (issued by BC-1) — this is admin-only configuration. Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

| Verb & route | Command/Query | Success | Notable failures |
|---|---|---|---|
| `POST /api/admin/taxonomies/seed` | `SeedTaxonomiesCommand` | `200` (idempotent) | — |
| `GET /api/admin/taxonomies/{kind}` | `GetTaxonomyQuery` | `200` + `TaxonomyDto` (hierarchical) | `404` unknown kind |
| `GET /api/admin/taxonomies/{kind}/terms` | `SearchTaxonomyTermsQuery` | `200` + `TaxonomyTermDto[]` | |
| `GET /api/admin/taxonomies/{kind}/terms/{code}` | `GetTaxonomyTermQuery` | `200` + `TaxonomyTermDetailDto` | `404` |
| `POST /api/admin/taxonomies/{kind}/terms` | `AddTaxonomyTermCommand` | `201` + term code | `409 E-TAXO-DUPLICATE-CODE`, `400 E-TAXO-CODE-PREFIX-MISMATCH`, `400 E-TAXO-CATEGORY-REQUIRED`, `404 E-TAXO-PARENT-NOT-FOUND`, `409 E-TAXO-PARENT-DEPRECATED`, `409 E-TAXO-CYCLE` |
| `PUT /api/admin/taxonomies/{kind}/terms/{code}/label` | `RenameTaxonomyTermCommand` | `200` | `404` |
| `PUT /api/admin/taxonomies/{kind}/terms/{code}/category` | `RecategorizeSkillCommand` | `200` | `404`, `400` non-Skills kind |
| `PUT /api/admin/taxonomies/{kind}/terms/{code}/parent` | `ReparentTaxonomyTermCommand` | `200` | `404`, `409 E-TAXO-CYCLE`, `409 E-TAXO-PARENT-DEPRECATED` |
| `POST /api/admin/taxonomies/{kind}/terms/{code}/deprecate` | `DeprecateTaxonomyTermCommand` | `200` | `404`, `400 E-TAXO-SELF-REPLACE` |
| `POST /api/admin/taxonomies/{kind}/terms/{code}/reactivate` | `ReactivateTaxonomyTermCommand` | `200` | `404`, `409 E-TAXO-PARENT-DEPRECATED` |
| `POST /api/admin/taxonomies/{kind}/import` | `BulkImportTaxonomyCommand` (multipart CSV) | `200` + `ImportResult` (per-row report; **`200` even with partial failures** — the body carries row results) | `400` bad file / mime, `413` too large |
| `GET /api/admin/taxonomies/{kind}/usage` | `GetTaxonomyUsageStatsQuery` | `200` + `TermUsageDto[]` | |

All endpoints return `409 E-TAXO-CONCURRENCY-CONFLICT` if an optimistic-concurrency conflict occurs on save.

> The `TaxonomyApi` (§9.3) is an **in-process** contract — it is **not** exposed as HTTP. Other modules call it through DI, not over the network. (In a future microservices split it would become an HTTP/gRPC OHS; for the modular monolith it stays in-process.)

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** `TermCode` — valid codes pass; empty / too-long / bad-chars / missing-`.` fail; uppercasing applied; prefix-vs-kind agreement checked by `TermCodeValidator` for all three kinds.
- **Taxonomy aggregate:**
  - `AddTerm`: happy path adds an `Active` term and bumps `Version` by 1; duplicate code → `E-TAXO-DUPLICATE-CODE`; wrong prefix for kind → `E-TAXO-CODE-PREFIX-MISMATCH`; Skills term with null category → `E-TAXO-CATEGORY-REQUIRED`; non-Skills term with non-null category fails; parent that does not exist → `E-TAXO-PARENT-NOT-FOUND`; parent that is `Deprecated` → `E-TAXO-PARENT-DEPRECATED`; adding a term whose parent chain loops back → `E-TAXO-CYCLE`.
  - `RenameTerm`: changes `Label`, leaves `Code` untouched, bumps `Version`.
  - `DeprecateTerm`: `Active → Deprecated`, sets `DeprecatedOnUtc`, optional `ReplacedByCode`; `replacedByCode == code` → `E-TAXO-SELF-REPLACE`; deprecating an already-`Deprecated` term is an idempotent success; the term is **still present** in `Terms` afterward (no delete).
  - `ReactivateTerm`: `Deprecated → Active`, clears `ReplacedByCode`/`DeprecatedOnUtc`; fails if its parent is now `Deprecated`.
  - `ReparentTerm`: rejects a move that creates a cycle.
  - `ApplyUsageDelta`: changes `UsageCount`, clamps at 0, and does **NOT** bump `Version`.
  - **Version invariant:** every vocabulary mutation bumps `Version` exactly once; a bulk import of N terms bumps it exactly once total.
- **Domain services:** `TaxonomyImportService` — a batch with valid + invalid rows applies only the valid ones and returns a per-row report; a child row whose parent is also in the batch succeeds via topological ordering; a duplicate code within the batch fails that row only.

### 13.2 Unit tests — Application (handlers, ports replaced with test doubles)

- `AddTaxonomyTermCommand`: happy path persists and the outbox-writing behavior queues one `TaxonomyTermAdded` + one `TaxonomyUpdated`; `E-TAXO-DUPLICATE-CODE` from the aggregate is returned and nothing is persisted.
- `BulkImportTaxonomyCommand`: `CsvReader` returns 5 rows (3 valid, 2 invalid) → handler persists 3 terms, returns an `ImportResult` with 3 succeeded / 2 failed, and the outbox has 3 `TaxonomyTermAdded` + exactly 1 `TaxonomyUpdated`.
- `DeprecateTaxonomyTermCommand`: queues `TaxonomyTermDeprecated` + `TaxonomyUpdated`; the term is still readable via `GetTaxonomyTermQuery`.
- `RecategorizeSkillCommand`: rejected by the validator when `kind != Skills`.
- Validation step: each validator rejects the documented bad inputs before the handler runs.

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip the `Taxonomy` aggregate with its `Terms`; the `UNIQUE (taxonomy_id, code)` constraint rejects a duplicate; `FindTermByCode` and `SearchTerms` work.
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `admin_config`; the three taxonomies are seedable.
- **Concurrency:** two `SaveChanges` against the same `Taxonomy` from stale copies → the second is rejected by the optimistic-concurrency token, mapped to `E-TAXO-CONCURRENCY-CONFLICT`.
- **Outbox:** an `AddTaxonomyTerm` writes the row change and the two outbox messages in one transaction; rolling back leaves none.
- **Inbox / idempotency:** delivering `ProfileSkillsUpdatedIntegrationEvent` twice applies the usage delta once.
- **`TaxonomyApi`:** `IsValidSkillCode` returns `true` for an `Active` code, `false` for a `Deprecated` one and for an unknown one; `MapSkill("python")` resolves to `SKILL.PYTHON`; `GetTerm` on a deprecated code returns its `ReplacedByCode`; after a committed mutation the cache is invalidated and `GetTaxonomyVersion` reflects the new version.
- **API:** host-level test for add-term → deprecate-term → bulk-import happy path; `POST .../import` with a partly-invalid CSV returns `200` with a per-row report.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "Add a new skill term"

End-to-end, to pattern-match every other command against:

1. **API.** `POST /api/admin/taxonomies/Skills/terms` with body `{ code: "SKILL.RUST", label: "Rust", category: "Hard", parentCode: null }`. The endpoint checks the access token carries the `MoLAdministrator` role, builds `AddTaxonomyTermCommand { Kind: Skills, Code: "SKILL.RUST", Label: "Rust", Category: Hard, ParentCode: null }`, dispatches it through the mediator.
2. **Validation step.** `AddTaxonomyTermCommand`'s validator runs: code shape ok, contains `.`, `kind == Skills` so `category` required and present, label non-empty. On failure → `Result` with `Error`, mapped to `400`.
3. **Handler.** `AddTaxonomyTermCommandHandler`:
   a. `TaxonomyRepository.GetByKindWithTerms(TaxonomyKind.Skills)` → the `Taxonomy` aggregate with its term list.
   b. `taxonomy.AddTerm(TermCode.Create("SKILL.RUST").Value, "Rust", SkillCategory.Hard, parentCode: null)` — the aggregate runs `TermCodeValidator` (prefix `SKILL.` matches `Skills` ✓), checks no existing `SKILL.RUST` (✓), no parent to validate, then appends the `TaxonomyTerm` as `Active`, calls `IncrementVersion()` (`Version: 7 → 8`), and raises domain events `TaxonomyTermAdded` and `TaxonomyUpdated`.
   c. `repository.Update(taxonomy)`; `unitOfWork.SaveChanges()`.
4. **Domain-event / outbox step.** After the unit of work is saved, the mediator pipeline dispatches the internal domain events in-process (which invalidates the `TaxonomyApi` cache) and writes `TaxonomyTermAddedIntegrationEvent` and `TaxonomyUpdatedIntegrationEvent { Version: 8, ChangeSummary: "1 term added" }` into `outbox_messages` — same transaction. ([[00-Shared-Foundations]] §6.1–6.2.)
5. **Relay.** The background outbox relay publishes both integration events; BC-3, BC-4, BC-6, BC-7 receive `TaxonomyTermAdded`; BC-8 and BC-10 also receive `TaxonomyUpdated`.
6. **Response.** Handler returns `Result` success; the endpoint returns `201` with `"SKILL.RUST"`.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.1.4-02 AC-01 View skills taxonomy | `GET /api/admin/taxonomies/Skills` returns the hierarchical term list with name, category, and usage count. |
| US-3.1.4-02 AC-02 Add new skill | `AddTaxonomyTermCommand` with name + `Hard`/`Soft` category creates an `Active` term, bumps `Version`, emits `TaxonomyTermAdded` + `TaxonomyUpdated`. |
| US-3.1.4-02 AC-03 Edit skill | `RenameTaxonomyTermCommand` / `RecategorizeSkillCommand` change label/category, leave `Code` immutable, emit `TaxonomyUpdated`. |
| US-3.1.4-02 AC-04 Disable skill | `DeprecateTaxonomyTermCommand` sets `Status = Deprecated` — the term is **kept**, `IsValidSkillCode` now returns `false`, existing external references still resolve via `GetTerm`. No hard delete. |
| US-3.1.4-02 AC-05 View occupations taxonomy | `GET /api/admin/taxonomies/Occupations` returns the `category → job group → job title` hierarchy. |
| US-3.1.4-02 AC-06 Add/update occupation | `AddTaxonomyTermCommand` / `ReparentTaxonomyTermCommand` on the Occupations taxonomy; parent integrity and no-cycle enforced. |
| US-3.1.4-02 AC-07 View training programs taxonomy | `GET /api/admin/taxonomies/TrainingPrograms` returns the training-program terms. |
| US-3.1.4-02 AC-08 Manage training programs | Add/rename/deprecate work on the `TrainingPrograms` taxonomy with `TRAIN.*` codes. |
| US-3.1.4-02 AC-09 View reference data usage | `GetTaxonomyTermQuery` / `GetTaxonomyUsageStatsQuery` expose `UsageCount`; it is maintained from usage-signal events (or stubbed at 0 for the exercise) and does **not** bump `Version`. |
| US-3.1.4-02 AC-10 Bulk import taxonomies | `BulkImportTaxonomyCommand` ingests a CSV, validates each row, applies the valid ones, and returns a per-row success/failure report; one `TaxonomyUpdated` for the whole batch. |
| Published Language (Context_Map insight #6) | `TaxonomyApi` exposes `MapSkill` + `IsValidSkillCode` exactly as BC-3's exemplar §9.2 expects, plus `GetTerm` / `AreValidCodes` / `GetTaxonomyVersion`; the cache invalidates on commit. |

---

## Appendix — teaching notes & open questions

- **This BC is deliberately thin — and that is the lesson.** BC-11 owns exactly one story. The instinct to "balance" the model by giving every BC a similar story count is wrong. A bounded context exists because something needs a *single owner with a consistent ubiquitous language*, not because it needs to be busy. The taxonomy needs one owner; therefore BC-11 exists; therefore it is small. Use it as the counter-example to "every BC needs many stories."

- **Published Language vs. Shared Kernel — the central debate (Context_Map insight #6).** We modelled the taxonomy as a **Published Language**: a versioned, read-only contract (`TaxonomyApi` + three events). The five consuming BCs hold their *own* small `CanonicalSkillRef` value object and plain `taxonomy_code` strings — they do **not** share BC-11's `Taxonomy`/`TaxonomyTerm` types. The alternative is a **Shared Kernel**: a single compiled `TaxonomyModel` library that BC-3, BC-4, BC-6, BC-7, BC-8, and BC-11 all reference directly.
  - *Arguments for Shared Kernel:* no translation, no drift between "what BC-11 means by a skill" and "what BC-7 means by a skill", one place to change the shape.
  - *Arguments for Published Language (what we chose):* the consumers can evolve independently; a change to BC-11's *internal* model (e.g. adding `UsageCount`, splitting hierarchy logic) does **not** force a synchronized recompile-and-redeploy of five other BCs; the boundary is explicit and versioned (`Version` integer in every event); it is the strategic pattern the [[Context_Map]] already assigns to this edge. The cost is the small `CanonicalSkillRef` duplication and the `TaxonomyApi` translation surface.
  - *The honest answer:* both are defensible. Shared Kernel is tempting precisely because the taxonomy is so stable and so widely used — the very conditions under which a Shared Kernel is *least* dangerous. But Published Language keeps deployment independence, and for a teaching model that wants to *show* a clean upstream/downstream contract, it is the clearer illustration. Ask the class: *at what point does the cost of the translation layer exceed the cost of the synchronized change?* (Rough heuristic: when the shared model changes more often than the consumers can absorb a Published-Language version bump.)

- **Should BC-11 even be its own BC, or fold into BC-1?** [[BC_Mapping]] raises option 3: merge BC-11 into BC-1 under a "Platform Administration" framing. Counter-argument: IAM's ubiquitous language is *identity, credentials, sessions*; taxonomy's is *skills, occupations, hierarchy, deprecation*. Those are different languages — merging them would create a BC with two unrelated cores. The thin-but-separate model keeps each language clean. Good debate: *is "it's all admin" a real bounded context, or just an org-chart artifact?*

- **One aggregate for the whole taxonomy — contention vs. consistency.** We made the entire `Taxonomy` (all its terms) a single aggregate because "unique code", "valid parent", "no cycle", and "one version per change" are set-spanning invariants. The price: two admins editing the Skills taxonomy concurrently collide on the optimistic-concurrency token. For low-write reference data this is fine. Ask the class: *what would have to change for `TaxonomyTerm` to deserve its own aggregate?* (Answer sketch: if the per-term invariants stopped spanning the set — e.g. if codes were globally unique and parents were soft references resolved eventually — each term could stand alone, trading the transactional guarantee for write throughput.)

- **`UsageCount` is eventually consistent and advisory.** It is maintained by reacting to events from BC-3/BC-4, not by querying their tables (which the no-cross-module-reads rule forbids). It can drift if events are missed. We deliberately keep it out of the `Version` bump and out of `TaxonomyUpdated` — it is *metadata about* the vocabulary, not *part of* the vocabulary. Discuss: when is "approximately right, asynchronously" good enough, and when is it not?

- **No hard delete, ever.** `DeprecateTerm` is the only way out. This is a direct consequence of being upstream of five BCs that store your codes: a hard delete would dangle their references. The `ReplacedByCode` pointer is the migration path. This is a small, concrete illustration of how being a Published Language *constrains your own domain model* — you cannot delete because your consumers depend on permanence.
