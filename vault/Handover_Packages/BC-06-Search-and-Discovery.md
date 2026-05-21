---
title: "Handover Package — BC-6 Search & Discovery"
type: handover-package
bc_id: BC-6
bc_name: Search & Discovery
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
  - bc/search
---

# Handover Package — BC-6 Search & Discovery

> **Audience:** an AI coding agent. This package owns the **domain design** for the `SearchDiscovery` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `SearchDiscovery`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `search_discovery`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

This BC is a **read-model BC** — read the boundary note in §1 before modelling. It is a **Conformist** downstream of BC-4 Job Postings (it accepts BC-4's posting events as-is and projects them into its own search index) and a **Customer/Supplier** consumer of BC-7 Recommendation Engine's ranking signals. It owns *no* source-of-truth job data. Its job is to make jobs *findable* and to remember a seeker's favorites and saved searches.

---

## 1. Purpose & scope boundaries

### What this BC is for

Search & Discovery owns the **job seeker's ability to find jobs** and **the lightweight discovery artifacts a seeker keeps**: a searchable, filterable index of currently-discoverable job postings; relevance and skills-match ranking; the seeker's **favorites** (saved jobs); and the seeker's **saved searches** with per-search notification preferences. It is a **supporting** subdomain — standard search functionality; the genuine differentiation lives in BC-7's matching algorithm, which this BC merely *consumes* as a ranking signal.

### In scope

The `SearchDiscovery` module is responsible for:

- A **JobIndexEntry** read-model — a denormalized, query-optimized projection of each discoverable job posting, kept in sync by consuming BC-4's posting integration events (`US-3.2.2-01`).
- **Basic search** — single-keyword search over indexed title, summary, and skills; results within a 2-second budget (`US-3.2.2-01 AC-01`).
- **Advanced search** — a filter panel: keywords, location (with radius), salary range, employment type, date posted (relative + absolute), application deadline, job requirements (skills / education / experience), company sector/industry; multiple filters AND-combined; filters persist within the session (`US-3.2.2-01 AC-03/05/06/07`).
- **Semantic-search intent hints** — the search criteria carry an optional structured "interpreted intent" the SPA/NLP front-end supplies; this module applies it as additional filter terms. The NLP itself is **not** built here (`US-3.2.2-01 AC-04` — see boundary note).
- **Ranking** — order results by relevance (title > skill > summary match) by default; when a logged-in seeker has a skills-match signal from BC-7, blend it in; expose sort options: relevance, match score, date posted, salary, application deadline (`US-3.2.2-02 AC-01/02`).
- **Re-filtering of an existing result set** without issuing a fresh search (`US-3.2.2-02 AC-03`).
- **Recommendation surfacing** — for a logged-in seeker, surface BC-7's recommended job ids alongside query results, clearly marked, and let the seeker dismiss a recommendation for the session (`US-3.2.2-02 AC-04/05/07`).
- **Favorites** — a seeker's saved/"interested" jobs: add, remove, list (`US-3.2.2-03 AC-01/02/03`).
- **Saved searches** — persist a named filter combination; rename, edit criteria, delete, run-with-one-click; per-search notification preference (`None` / `DailyDigest` / `WeeklyDigest` / `Instant`); unsubscribe without deleting the search (`US-3.2.2-03 AC-04/05/06/08`).
- **Saved-search matching** — when a new posting enters the index, evaluate it against active saved searches and emit `SavedSearchMatchFound` for the ones it matches, so BC-9 can notify (`US-3.2.2-03 AC-07`).
- Publishing the integration events in §8 and reacting to the integration events in §9.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong to other BCs (or external systems) and are reached via the contracts in §9, or are simply not this module's concern:

- **The source-of-truth job posting** — title, description, requirements, employer, status lifecycle → BC-4 Job Postings. This module holds a *read-model copy* (`JobIndexEntry`) built from BC-4 events. It never edits a posting and never treats its copy as authoritative; on any event that says a posting is no longer discoverable, the entry is removed from the index.
- **Computing the skills-match / relevance score against a seeker's profile** → BC-7 Recommendation Engine. This module *receives* `MatchComputed` signals and *blends* them into ordering. It does not run embeddings, NLP, or any matching algorithm.
- **Generating personalized recommendations** → BC-7. This module *surfaces* the recommended posting ids BC-7 produced (via `RecommendationGenerated` and/or the `IRecommendationQueryApi` port); it does not decide what to recommend.
- **The NLP / semantic interpretation of a free-text query** → an external NLP capability (BC-7's `US-3.3.1-02` covers semantic analysis). This module accepts an already-structured intent hint and applies it as filters; it does not parse natural language. If no intent hint is supplied, basic keyword search is used.
- **Bookmarking an individual job as pre-application intent** → BC-5 Job Application owns `JobBookmarked` (see [[BC_Mapping]] migration note: bookmarking moved to BC-5). **Favorites here are distinct** — see the boundary note below.
- **Sending the saved-search alert email / SMS / in-app notification** → BC-9 Notification. This module emits `SavedSearchMatchFound` and `SavedSearchCreated`; BC-9 owns delivery, channel choice, digest batching, and rate-limiting.
- **Credentials, login, sessions, access tokens, role claims** → BC-1 IAM/UAM. The search API trusts the access token BC-1 issued; the seeker's `UserId` is read from the token. Anonymous search is allowed (no recommendations, no favorites).
- **The skill / occupation / industry taxonomy** → BC-11 Administrators Configuration. This module stores taxonomy codes inside index entries as supplied by BC-4's events and may consume `TaxonomyUpdated` to refresh facet/synonym definitions; it does not own the vocabulary.
- **Reporting / analytics on search volume and behavior** → BC-10 Reporting. This module emits `SearchPerformed`; BC-10 builds the dashboards.

### Boundary note — favorites here vs. bookmarks in BC-5 (teaching point)

[[BC_Mapping]]'s migration notes explicitly moved "bookmark an individual job" to **BC-5 Job Application** as *pre-application intent* (BC-5 emits `JobBookmarked`). Yet `US-3.2.2-03` — owned by BC-6 — asks the seeker to "save favorite jobs". These are **two different concepts that look alike**:

- **BC-5 bookmark** = "I intend to apply to this" — it lives next to the application funnel and feeds BC-7 as a preference signal.
- **BC-6 favorite** = "keep this in my discovery shortlist" — it lives next to *search*, alongside saved searches, as a discovery convenience.

This package models **favorites** as a BC-6-owned concept (`FavoriteJob` aggregate) because `US-3.2.2-03` is a BC-6 story and its favorites sit naturally with its saved searches. It does **not** call BC-5 and does **not** emit `JobBookmarked`. If the platform later decides these are truly one concept, the merge is a one-way street toward BC-5. Discuss with the class: *when two stories in two BCs describe near-identical behavior, is that duplication, or two genuinely different bounded concepts wearing similar words?* This is a deliberate, documented call — the same kind of judgment the exemplar's BC-3 made on the registration split.

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **Job Index Entry** | The `JobIndexEntry` aggregate — this BC's denormalized, search-optimized read-model of one discoverable job posting. Root of the search side of the BC. Identified by the BC-4 `PostingId`. |
| **Discoverable** | A posting is discoverable iff BC-4 has it `Published` and it has not expired/closed/suspended. The index holds *only* discoverable entries; on any "no longer discoverable" event the entry is deleted. |
| **Search Criteria** | A `SearchCriteria` value object — the full set of query inputs: keyword, filters, optional intent hint, sort, paging. |
| **Filter** | One constraint inside `SearchCriteria`: location+radius, salary range, employment type, date-posted range, deadline, required skills/education/experience, sector/industry. |
| **Intent Hint** | An `IntentHint` VO — an optional pre-parsed interpretation of a free-text query (e.g. `{ workFormat: "Remote", industries: ["IT"] }`), supplied by the NLP front-end, applied here as extra filter terms. |
| **Relevance Score** | A computed ordering score based on *where* the keyword matched: title > skill > summary. Owned and computed here. |
| **Match Score** | A skills-matching score for a (seeker, posting) pair — **computed by BC-7**, received via `MatchComputed` or `IRecommendationQueryApi`, cached here as a ranking signal. |
| **Sort Option** | `Relevance` (default), `MatchScore`, `DatePosted`, `Salary`, `ApplicationDeadline`. |
| **Search Result** | A page of ranked `JobIndexEntry` projections plus, for logged-in seekers, a clearly-marked recommendations block. |
| **Recommendation** | A posting id BC-7 produced for this seeker. Surfaced, not computed, here. |
| **Dismissed Recommendation** | A recommendation the seeker hid for the **current session** — held in `SearchSession`, not persisted long-term. |
| **Search Session** | The `SearchSession` aggregate — per-seeker, short-lived: the last-applied `SearchCriteria` (so filters persist) + session-dismissed recommendation ids. |
| **Favorite Job** | The `FavoriteJob` aggregate — one seeker's saved/"interested" job. Distinct from BC-5's bookmark (see §1 boundary note). |
| **Saved Search** | The `SavedSearch` aggregate — a named, persisted `SearchCriteria` plus a `NotificationPreference`. |
| **Notification Preference** | A saved search's alert setting: `None`, `DailyDigest`, `WeeklyDigest`, `Instant`. |
| **Saved Search Match** | An event-worthy fact: a newly-indexed posting satisfies an active saved search's criteria. |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `SearchDiscovery`.
- Public surface (`Contracts`): the integration events published in §8 + the public API in §9.3.
- **Module-specific notes:** the integration-event projection handlers (§10.3) are the only writers of `JobIndexEntry`, `match_score_cache`, and `recommendation_cache` — they are wired into the module's inbound integration-event subscription. The module also runs a **background sweep** that purges expired `search_sessions` (past `expires_on_utc`).

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has **four aggregates**: `JobIndexEntry` (the search read-model), `FavoriteJob`, `SavedSearch`, and `SearchSession`. (Notation: see [[00-Shared-Foundations]] §2.)

> **Read-model framing.** `JobIndexEntry` is unusual: it is an aggregate this BC *projects* rather than *authors*. It still goes through the aggregate pattern (it has invariants — see §6 — and is mutated only through behaviors) but its lifecycle is driven entirely by consumed BC-4 events, never by a command from this module's own API. The other three aggregates *are* authored here, by seeker actions.

### 5.1 Aggregate: JobIndexEntry

**Aggregate root.** Identity: `JobIndexEntryId` — a strongly-typed id set equal to the BC-4 `PostingId` (a `uuid`), so the projection is idempotent and upsertable.

| Member | Type | Notes |
|---|---|---|
| `Id` | `JobIndexEntryId` | == BC-4 `PostingId`. Plain `uuid`, no FK to BC-4. |
| `EmployerId` | `uuid` | from BC-4 event payload. No FK. |
| `Title` | `string` | denormalized from BC-4 |
| `Summary` | `string` | short description text |
| `CompanyName` | `string` | denormalized for display + filtering |
| `Skills` | `list<string>` | required-skill taxonomy codes / labels from the posting |
| `EducationRequirement` | `string?` | |
| `ExperienceYears` | `int?` | minimum years required |
| `Location` | `GeoLocation` | VO — district/city + optional lat/lng for radius search |
| `EmploymentType` | `EmploymentType` | enum: `FullTime`, `PartTime`, `Contract`, `Internship`, `Temporary` |
| `WorkFormat` | `WorkFormat` | enum: `OnSite`, `Hybrid`, `Remote` |
| `SalaryRange` | `SalaryRange?` | VO; nullable when employer did not disclose |
| `SectorIndustry` | `string?` | company sector/industry |
| `PostedOnUtc` | `datetime` | the posting's publish timestamp |
| `ApplicationDeadlineUtc` | `datetime?` | |
| `SourcePostingVersion` | `int64` | monotonically increasing version from BC-4 events; guards against out-of-order projection (§6 invariant 2) |
| `IndexedOnUtc` / `UpdatedOnUtc` | `datetime` | |

### 5.2 Aggregate: FavoriteJob

**Aggregate root.** Identity: `FavoriteJobId`. One row per (seeker, posting) the seeker has favorited.

| Member | Type | Notes |
|---|---|---|
| `Id` | `FavoriteJobId` | |
| `SeekerUserId` | `uuid` | BC-1 identity. No FK. |
| `PostingId` | `uuid` | the favorited job. No FK to BC-4. |
| `FavoritedOnUtc` | `datetime` | |

### 5.3 Aggregate: SavedSearch

**Aggregate root.** Identity: `SavedSearchId`. A named, persisted search with an alert preference.

| Member | Type | Notes |
|---|---|---|
| `Id` | `SavedSearchId` | |
| `SeekerUserId` | `uuid` | BC-1 identity. No FK. |
| `Name` | `string` | seeker-provided, non-empty ≤ 100 |
| `Criteria` | `SearchCriteria` | VO — the persisted filter combination |
| `NotificationPreference` | `NotificationPreference` | enum: `None`, `DailyDigest`, `WeeklyDigest`, `Instant` |
| `LastEvaluatedOnUtc` | `datetime?` | last time it was checked against the index for matches |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

### 5.4 Aggregate: SearchSession

**Aggregate root.** Identity: `SearchSessionId` (one active per seeker). Short-lived; supports "filters persist in session" and "dismiss recommendation for this session".

| Member | Type | Notes |
|---|---|---|
| `Id` | `SearchSessionId` | |
| `SeekerUserId` | `uuid` | BC-1 identity. No FK. |
| `LastCriteria` | `SearchCriteria?` | last-applied criteria, so the search form re-populates (`US-3.2.2-01 AC-07`) |
| `DismissedRecommendationPostingIds` | `list<uuid>` | recommendations hidden for this session (`US-3.2.2-02 AC-07`) |
| `ExpiresOnUtc` | `datetime` | sliding expiry (e.g. 24h); expired sessions are purged |
| `UpdatedOnUtc` | `datetime` | |

### 5.5 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `SearchCriteria` | `Keyword?`, `Filters` (`SearchFilters`), `IntentHint?`, `Sort` (`SortOption`), `Page` (`int ≥ 1`), `PageSize` (`int`, 1–100) | at least one of `Keyword` / a non-empty filter / `IntentHint` must be present for a *search*; `SavedSearch` may persist a filter-only criteria |
| `SearchFilters` | `Location?` (`GeoLocation`), `RadiusKm?`, `SalaryMin?`, `SalaryMax?`, `EmploymentTypes` (set), `WorkFormats` (set), `DatePostedFrom?`, `DatePostedTo?`, `DeadlineBefore?`, `RequiredSkills` (set), `EducationLevel?`, `MinExperienceYears?`, `SectorIndustry?` | `SalaryMin ≤ SalaryMax` when both set; `DatePostedFrom ≤ DatePostedTo` when both set; `RadiusKm > 0` when set and requires `Location` |
| `IntentHint` | `WorkFormat?`, `Industries` (`list<string>`), `SkillTerms` (`list<string>`), `LocationTerm?` | all fields optional; an all-empty hint is rejected (`E-INTENT-EMPTY`) |
| `GeoLocation` | `District`, `City?`, `Latitude?`, `Longitude?` | `District` non-empty; lat/lng both present or both absent; lat ∈ [-90,90], lng ∈ [-180,180] |
| `SalaryRange` | `Min` (`decimal`), `Max` (`decimal`), `Currency` | `Min ≥ 0`; `Min ≤ Max`; default currency `BDT` |
| `RelevanceWeights` | `TitleWeight`, `SkillWeight`, `SummaryWeight` | all `> 0`; `Title > Skill > Summary` (enforced ordering — the default ranking rule) |
| `SortOption` | `Value` | enum: `Relevance`, `MatchScore`, `DatePosted`, `Salary`, `ApplicationDeadline` |
| `EmploymentType` | `Value` | enum: `FullTime`, `PartTime`, `Contract`, `Internship`, `Temporary` |
| `WorkFormat` | `Value` | enum: `OnSite`, `Hybrid`, `Remote` |
| `NotificationPreference` | `Value` | enum: `None`, `DailyDigest`, `WeeklyDigest`, `Instant` |

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior lives on the aggregate roots. Handlers never set properties directly. Method shapes below are neutral specifications (see [[00-Shared-Foundations]] §2).

### 6.1 JobIndexEntry — behaviors (driven by consumed BC-4 events only)

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Project(postingId, payload, sourceVersion)` | builds a new index entry from a `JobPostingPublished` payload. `sourceVersion` is the BC-4 event version. | `JobIndexed` *(internal — triggers saved-search evaluation)* |
| `ApplyUpdate(payload, sourceVersion)` | applies a `JobPostingUpdated` payload. **Ignored** (no-op) if `sourceVersion <= SourcePostingVersion` — guards out-of-order delivery. Otherwise refreshes denormalized fields and bumps `SourcePostingVersion`. | `JobIndexUpdated` *(internal)* |
| `static MarkRemoved(...)` | there is no "removed" state — when a posting becomes non-discoverable the **entry is deleted** from the index by the projection handler. Modeled as repository `Delete`, not an aggregate method. | — |

> The projection handlers (§10.3) are the only callers. There is **no command** that creates or edits a `JobIndexEntry` from this module's own API surface.

### 6.2 Core invariants (must always hold)

1. **Index = discoverable only.** A `JobIndexEntry` exists **iff** BC-4 considers the posting discoverable (`Published`, not expired/closed/suspended). Search and saved-search matching only ever see discoverable entries — no filtering for status is needed at query time because non-discoverable entries are not in the table.
2. **Version monotonicity.** `JobIndexEntry.SourcePostingVersion` only ever increases. `ApplyUpdate` with a stale or equal version is a no-op. This makes projection idempotent and order-tolerant — a re-delivered or out-of-order BC-4 event cannot corrupt the entry.
3. **`JobIndexEntryId == PostingId`.** The projection upserts by posting id; consuming `JobPostingPublished` for an id already in the index updates rather than duplicates.
4. **Favorite uniqueness.** At most one `FavoriteJob` per `(SeekerUserId, PostingId)`. Re-favoriting an already-favorited job is idempotent (no error, no duplicate).
5. **Saved-search ownership.** A `SavedSearch` is mutated, run, or deleted only by its owning `SeekerUserId`. Cross-seeker access returns `E-FORBIDDEN` (enforced in the handler from the access token).
6. **Saved-search name** is non-empty and unique **per seeker** (`E-SAVED-SEARCH-NAME-DUPLICATE`) — checked by the handler via the repository.
7. **`SearchCriteria` validity.** A criteria used for an actual *search* must carry at least a keyword, a non-empty filter, or an intent hint (`E-SEARCH-EMPTY-CRITERIA`). A `SavedSearch` may persist a filter-only criteria (no keyword required).
8. **Match-score is advisory.** A cached `MatchScore` from BC-7 is never required for a result to be returned; if absent, ranking falls back to pure relevance. Match-score staleness never blocks a search.
9. **Session scoping.** `SearchSession.DismissedRecommendationPostingIds` and `LastCriteria` are per-seeker and expire with the session — they are never treated as durable preferences.
10. **No source-of-truth writes.** This module never emits an event that mutates a posting, a profile, or a recommendation. Its only outbound events are search/discovery facts (§8).

### 6.3 FavoriteJob — behaviors

| Method | Rules | Domain event raised |
|---|---|---|
| `static Add(seekerUserId, postingId)` | creates a favorite. Handler first checks the posting exists in the index (`E-JOB-NOT-FOUND`) and that no favorite already exists for the pair — if it does, return the existing one (idempotent, invariant 4). | `JobFavorited` *(internal)* |
| `Remove()` | marks the favorite for deletion (the handler `Delete`s it). Removing a non-existent favorite is a no-op success. | `JobUnfavorited` *(internal)* |

### 6.4 SavedSearch — behaviors

| Method | Rules | Domain event raised |
|---|---|---|
| `static Create(seekerUserId, name, criteria, notificationPreference)` | `name` non-empty ≤ 100, unique per seeker (handler-checked); `criteria` valid. | `SavedSearchCreated` |
| `Rename(newName)` | new name non-empty, still unique per seeker. | — |
| `UpdateCriteria(newCriteria)` | valid `SearchCriteria`. | — |
| `SetNotificationPreference(preference)` | any → any. Setting `None` is the "unsubscribe but keep the search" path (`US-3.2.2-03 AC-08`). | `SavedSearchNotificationChanged` *(internal)* |
| `RecordEvaluated(nowUtc)` | stamps `LastEvaluatedOnUtc`; called after each match-evaluation pass. | — |
| `EvaluateAgainst(JobIndexEntry entry)` | **query** (no state change): returns `true` if `entry` satisfies `Criteria`. Used by the saved-search-matching flow. Delegates the actual predicate to `SavedSearchMatcher` (§7.3). | — |

### 6.5 SearchSession — behaviors

| Method | Rules | Event |
|---|---|---|
| `static Start(seekerUserId, nowUtc)` | creates a session with a sliding `ExpiresOnUtc`. | — |
| `RememberCriteria(criteria, nowUtc)` | stores `LastCriteria`, extends expiry. | — |
| `DismissRecommendation(postingId, nowUtc)` | adds `postingId` to the dismissed set (deduped), extends expiry. | — |
| `IsExpired(nowUtc)` | query: `nowUtc >= ExpiresOnUtc`. Expired sessions are purged by a background sweep. | — |

---

## 7. Domain services

Stateless. Live in `Domain`. Used when logic spans entities or needs a small external input.

### 7.1 `RelevanceRanker`

```
Rank(keyword: string?, intentHint: IntentHint?, matches: list<JobIndexEntry>, weights: RelevanceWeights)
    -> list<ScoredEntry>
```

Computes the **relevance score** for each matched entry from *where* the keyword (and intent-hint terms) matched: a title hit scores `weights.TitleWeight`, a skill hit `weights.SkillWeight`, a summary hit `weights.SummaryWeight`; hits sum. Implements `US-3.2.2-01 AC-02` ("title matches ranked higher than skill matches") and `US-3.2.2-02 AC-01` for the relevance component. The *matching* itself (which entries contain the term) is done by the repository query; this service only **scores and orders** the matched rows.

### 7.2 `ResultRankBlender`

```
Blend(relevanceScored: list<ScoredEntry>,
      matchScoresByPostingId: map<uuid, int>,   // from BC-7, may be empty / partial
      sort: SortOption)
    -> list<RankedResult>
```

Produces the final ordered result page:

- `sort == Relevance` → order by relevance score; where a BC-7 match score exists for a logged-in seeker, use it as a tiebreaker / light blend (`US-3.2.2-02 AC-01`).
- `sort == MatchScore` → order by the BC-7 match score descending; entries without a score sort last.
- `sort == DatePosted` / `Salary` / `ApplicationDeadline` → order by that field (`US-3.2.2-02 AC-02`).
- Match scores are **advisory** — a missing or partial `matchScoresByPostingId` never drops a result (invariant 8).

### 7.3 `SavedSearchMatcher`

```
Matches(criteria: SearchCriteria, entry: JobIndexEntry) -> bool
```

The single predicate that decides whether one index entry satisfies one saved search's criteria. Applies every filter in `SearchFilters` with **AND logic across filter types** (`US-3.2.2-01 AC-06`), applies the keyword across title/summary/skills, and applies any `IntentHint` terms. Used both by `SavedSearch.EvaluateAgainst` (for new-posting alerting) and by the saved-search "run with one click" query — guaranteeing the *same* semantics whether the seeker runs the search or the system evaluates it.

### 7.4 `SearchCriteriaInterpreter`

```
Apply(baseCriteria: SearchCriteria, hint: IntentHint) -> SearchCriteria
```

Folds an `IntentHint` into a `SearchCriteria` by translating hint fields into concrete filter terms (e.g. `hint.WorkFormat == "Remote"` → adds `Remote` to `SearchFilters.WorkFormats`; `hint.Industries` → `SectorIndustry`; `hint.SkillTerms` → `RequiredSkills`). This is the extent of "semantic search" in this module (`US-3.2.2-01 AC-04`): the NLP front-end produces the `IntentHint`, this service merely maps it onto the structured filter model. No natural-language parsing happens here.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module. **Integration events** (`IntegrationEvent`) cross the BC boundary — they go through the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract exactly. Other modules depend on these payloads — do not change a field without versioning.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

Per the [[Event_Catalog]] BC-6 owns exactly **three** events. This package keeps to that set.

| Integration event | Raised when | Payload |
|---|---|---|
| `SearchPerformedIntegrationEvent` | a `SearchJobsQuery` is executed (logged-in or anonymous) | `UserId` (`uuid?` — null if anonymous), `Keyword` (`string?`), `FilterSummary` (`string` — serialized filter description), `ResultCount` (`int`), `OccurredOnUtc` |
| `SavedSearchCreatedIntegrationEvent` | `SavedSearch.Create()` succeeds | `SavedSearchId`, `SeekerUserId`, `Name`, `CriteriaSummary` (`string`), `NotificationPreference` (`string`), `OccurredOnUtc` |
| `SavedSearchMatchFoundIntegrationEvent` | a newly-indexed posting satisfies an active saved search (preference ≠ `None`) | `SavedSearchId`, `SeekerUserId`, `PostingIds` (`list<uuid>` — the new matches), `NotificationPreference` (`string`), `OccurredOnUtc` |

Consumers (for context only — you do not code them): **BC-7 Recommendation Engine** consumes `SearchPerformed` as a preference/behavior signal for re-ranking; **BC-9 Notification** consumes `SavedSearchCreated` and, crucially, `SavedSearchMatchFound` — BC-9 owns the decision of *which channel* and *whether to batch into a digest* based on the carried `NotificationPreference`; **BC-10 Reporting** consumes all three for search-behavior analytics.

### 8.2 Internal domain events (`DomainEvent` — NOT published outside the module)

`JobIndexed`, `JobIndexUpdated`, `JobFavorited`, `JobUnfavorited`, `SavedSearchNotificationChanged`. Use these for in-module reactions:

- `JobIndexed` → triggers the **saved-search evaluation** flow: the in-module handler loads active saved searches whose `NotificationPreference != None`, runs `SavedSearchMatcher`, and for each match emits `SavedSearchMatchFoundIntegrationEvent` to the outbox. This is the core in-module choreography — keep it internal; only the resulting *integration* event crosses the boundary.
- `JobIndexUpdated` → may re-evaluate saved searches if the update made a previously non-matching posting match (optional; the stories focus on *new* postings, so a minimal implementation may evaluate on `JobIndexed` only — document whichever you choose).

These never reach the outbox.

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained for its domain. (Outbox/inbox mechanics: [[00-Shared-Foundations]] §6.2–6.3.)

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent handlers)

This BC is a **Conformist** consumer of BC-4 — it accepts these payloads as-is and projects them. All handlers dedupe on `EventId` via the `inbox_messages` table and are additionally protected by `JobIndexEntry.SourcePostingVersion` monotonicity (invariant 2).

| Integration event | From | Payload you receive | Your reaction |
|---|---|---|---|
| `JobPostingPublishedIntegrationEvent` | BC-4 Job Postings | `PostingId`, `EmployerId`, `Title`, `Summary`, `CompanyName`, `Skills` (`list<string>`), `EducationRequirement` (`string?`), `ExperienceYears` (`int?`), `Location` (`{district,city,lat,lng}`), `EmploymentType` (`string`), `WorkFormat` (`string`), `SalaryRange` (`{min,max,currency}?`), `SectorIndustry` (`string?`), `PostedOnUtc`, `ApplicationDeadlineUtc` (`datetime?`), `Version` (`int64`), `OccurredOnUtc` | **Upsert** a `JobIndexEntry` keyed by `PostingId`: if absent, `JobIndexEntry.Project(...)`; if present, `ApplyUpdate(...)`. Raises internal `JobIndexed`, which triggers saved-search evaluation. Idempotent via id-keyed upsert + version guard. |
| `JobPostingUpdatedIntegrationEvent` | BC-4 | `PostingId`, `ChangedFields`, full refreshed posting payload, `Version`, `OccurredOnUtc` | Load the entry, `ApplyUpdate(payload, Version)` (no-op if `Version` stale). If the posting is not in the index (e.g. update arrived before publish), treat as a publish-upsert. |
| `JobPostingExpiredIntegrationEvent` | BC-4 | `PostingId`, `ExpiredAtUtc`, `OccurredOnUtc` | **Delete** the `JobIndexEntry` for `PostingId`. No-op if already absent. |
| `JobPostingClosedIntegrationEvent` | BC-4 | `PostingId`, `Reason`, `OccurredOnUtc` | **Delete** the `JobIndexEntry`. No-op if absent. |
| `JobPostingSuspendedIntegrationEvent` | BC-4 | `PostingId`, `By`, `Reason`, `OccurredOnUtc` | **Delete** the `JobIndexEntry`. No-op if absent. |
| `JobPostingReinstatedIntegrationEvent` | BC-4 | `PostingId`, `By`, `OccurredOnUtc` | The posting is discoverable again. If BC-4's reinstate event carries the full payload, re-`Project` it; if it carries only the id, the index will be repaired by the next `JobPostingUpdated`/republish — document the chosen approach. (Recommended: BC-4 includes the full payload on reinstate; this package assumes so.) |
| `MatchComputedIntegrationEvent` | BC-7 Recommendation Engine | `JobSeekerId`, `PostingId`, `Score` (`int` 0–100), `OccurredOnUtc` | Upsert a row in `match_score_cache` keyed by `(JobSeekerId, PostingId)` with `Score` + `OccurredOnUtc`. Used as the advisory ranking signal in `ResultRankBlender`. Idempotent — newer `OccurredOnUtc` wins. |
| `RecommendationGeneratedIntegrationEvent` | BC-7 | `JobSeekerId`, `PostingIds` (`list<uuid>`), `ComputedAtUtc`, `OccurredOnUtc` | Replace the cached recommendation list for `JobSeekerId` in `recommendation_cache` (`PostingIds` + `ComputedAtUtc`). Surfaced by `SearchJobsQuery` as the "Recommended for you" block. |
| `TaxonomyUpdatedIntegrationEvent` | BC-11 Admin Config | `TaxonomyId`, `Version`, `ChangeSummary`, `OccurredOnUtc` | Refresh any cached facet/synonym definitions used for filtering. For the MVP (substring/`LIKE`-based search) this may be a no-op; keep the handler so the wiring exists. |

**Idempotency** is mandatory — see [[00-Shared-Foundations]] §6.3 (inbox). Inbound events arrive via the module's integration-event subscription wired in the module composition entry point.

### 9.2 Public APIs this module CALLS (port interfaces — define in `Application`, implement adapters in `Infrastructure`)

```
Port: RecommendationQueryApi   (provided by BC-7 Recommendation Engine; Customer/Supplier)
  // Fallback / synchronous path when the seeker has no cached recommendation/match data yet —
  // the event-fed caches in §9.1 are the primary path; this port covers the cold-start read.
  // Skills-match scores for a seeker against a specific set of postings (the current result page).
  // Returns an empty map if the seeker has no profile or no scores yet — never fails for "no data".
  GetMatchScores(jobSeekerId: uuid, postingIds: list<uuid>) -> map<uuid, int>
  // The seeker's current personalized recommendation list.
  GetRecommendedPostingIds(jobSeekerId: uuid, maxItems: int) -> list<uuid>
```

For the exercise, `Infrastructure` may provide a **stub adapter** for `RecommendationQueryApi` (returns an empty score map and an empty recommendation list, or canned data) so the module runs standalone. Keep the port shape exactly as above so the real adapter drops in later.

> **Design note — event cache vs. synchronous port.** This package uses **both**: the event-fed `match_score_cache` / `recommendation_cache` (§9.1) is the primary, low-latency path that keeps search within its 2-second budget; `RecommendationQueryApi` is the cold-start fallback for a seeker whose caches are empty. A minimal implementation may use only the synchronous port and skip the caches — but then every search blocks on a BC-7 call. Discuss the trade-off in class: event-fed read-model (eventual consistency, fast reads) vs. synchronous query (fresh, but couples search latency to BC-7's availability).

### 9.3 Public API this module EXPOSES (in the `Contracts` surface)

```
Public API: SearchDiscoveryPublicApi
  // Used by BC-4 / BC-10 to confirm whether a posting is currently in the discoverable index.
  IsPostingIndexed(postingId: uuid) -> bool
  // A seeker's favorite posting ids — used by BC-5 Job Application to pre-fill "apply from favorites",
  // and by BC-7 as a soft preference signal if it chooses to read rather than wait for events.
  GetFavoritePostingIds(seekerUserId: uuid) -> list<uuid>
```

---

## 10. Application layer

Every use case is a **command** or a **query** with exactly one handler. Commands return `Result`/`Result<T>`; queries return DTOs. Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

### 10.1 Commands

| Command | Story | Handler responsibilities |
|---|---|---|
| `AddFavoriteJobCommand` | US-3.2.2-03 | Read `SeekerUserId` from the access token → verify `PostingId` is in the index via `JobIndexRepository` (`E-JOB-NOT-FOUND`) → if a favorite for the pair exists, return it (idempotent) else `FavoriteJob.Add(...)` → persist. |
| `RemoveFavoriteJobCommand` | US-3.2.2-03 | Load favorite by `(SeekerUserId, PostingId)` → if absent, no-op success → else `favorite.Remove()` and repository `Delete` → persist. |
| `CreateSavedSearchCommand` | US-3.2.2-03 | Validate `Criteria` + name → check name unique per seeker (`E-SAVED-SEARCH-NAME-DUPLICATE`) → `SavedSearch.Create(seekerUserId, name, criteria, notificationPreference)` → persist; `SavedSearchCreated` to outbox. |
| `RenameSavedSearchCommand` | US-3.2.2-03 | Load (ownership-checked, `E-FORBIDDEN`) → `Rename(newName)` (re-check uniqueness) → persist. |
| `UpdateSavedSearchCriteriaCommand` | US-3.2.2-03 | Load (ownership-checked) → `UpdateCriteria(newCriteria)` → persist. |
| `SetSavedSearchNotificationCommand` | US-3.2.2-03 | Load (ownership-checked) → `SetNotificationPreference(preference)`. Setting `None` is the unsubscribe path (`AC-08`) — the search row stays. → persist. |
| `DeleteSavedSearchCommand` | US-3.2.2-03 | Load (ownership-checked) → repository `Delete` → persist. |
| `DismissRecommendationCommand` | US-3.2.2-02 | Load-or-start the seeker's `SearchSession` → `DismissRecommendation(postingId, now)` → persist (`AC-07`). |
| `RememberSearchCriteriaCommand` | US-3.2.2-01 | Load-or-start `SearchSession` → `RememberCriteria(criteria, now)` → persist. Called by `SearchJobsQuery`'s handler internally, or directly by the front-end, to make filters persist (`AC-07`). |

> **Projection "commands" are internal.** Index upserts/deletes are performed by the integration-event handlers in §10.3, not by API-facing commands. They still go through repositories + `UnitOfWork` and the outbox/inbox conventions.

### 10.2 Queries

| Query | Story | Returns |
|---|---|---|
| `SearchJobsQuery` | US-3.2.2-01, US-3.2.2-02 | `SearchResultDto` — the ranked result page **plus** (for a logged-in seeker) a `Recommendations` block. Handler: validate `SearchCriteria` (`E-SEARCH-EMPTY-CRITERIA`) → if `IntentHint` present, fold via `SearchCriteriaInterpreter` → repository keyword+filter match against the index → `RelevanceRanker.Rank` → if logged-in, gather match scores (from `match_score_cache`, fallback `RecommendationQueryApi.GetMatchScores`) and recommendation ids (from `recommendation_cache`, fallback port), minus session-dismissed ids → `ResultRankBlender.Blend` → page → emit `SearchPerformed` to the outbox → also `RememberSearchCriteria`. 2-second budget (`AC-01`). |
| `RefineSearchResultsQuery` | US-3.2.2-02 | `SearchResultDto` — re-applies adjusted filters to produce a fresh page **without** treating it as a brand-new search (still emits `SearchPerformed`; `AC-03`). In practice this is `SearchJobsQuery` with the updated criteria — provided as a distinct query name for API clarity. |
| `GetFavoritesQuery` | US-3.2.2-03 | `list<FavoriteJobDto>` — the seeker's favorites joined to their `JobIndexEntry` projection for display (title, company, location, salary); entries whose posting has left the index are returned with a `NoLongerAvailable` flag. |
| `GetSavedSearchesQuery` | US-3.2.2-03 | `list<SavedSearchDto>` — the seeker's saved searches with name, criteria summary, notification preference, last-evaluated time. |
| `RunSavedSearchQuery` | US-3.2.2-03 | `SearchResultDto` — loads the `SavedSearch` (ownership-checked), runs its `Criteria` through the same path as `SearchJobsQuery` (one-click run, `AC-05`). |
| `GetSearchSessionQuery` | US-3.2.2-01 | `SearchSessionDto` — the seeker's `LastCriteria` (to re-populate the search form, `AC-07`) and dismissed-recommendation ids. |

### 10.3 Integration-event projection handlers (Infrastructure, wired in module registration)

These are the inbound integration-event handlers for the consumed events in §9.1. They are the *only* writers of `JobIndexEntry`, `match_score_cache`, and `recommendation_cache`.

| Handler | Consumes | Action |
|---|---|---|
| `JobPostingPublishedProjectionHandler` | `JobPostingPublished` | Inbox-dedupe → upsert `JobIndexEntry` (`Project` or `ApplyUpdate`) → `SaveChanges` (raises internal `JobIndexed`). |
| `JobPostingUpdatedProjectionHandler` | `JobPostingUpdated` | Inbox-dedupe → `ApplyUpdate` with version guard (upsert if absent) → `SaveChanges`. |
| `JobPostingRemovedProjectionHandler` | `JobPostingExpired` / `Closed` / `Suspended` | Inbox-dedupe → `Delete` the `JobIndexEntry` (no-op if absent). |
| `JobPostingReinstatedProjectionHandler` | `JobPostingReinstated` | Inbox-dedupe → re-`Project` from the carried payload. |
| `MatchComputedCacheHandler` | `MatchComputed` | Inbox-dedupe → upsert `match_score_cache` (newer `OccurredOnUtc` wins). |
| `RecommendationGeneratedCacheHandler` | `RecommendationGenerated` | Inbox-dedupe → replace `recommendation_cache` for the seeker. |
| `TaxonomyUpdatedHandler` | `TaxonomyUpdated` | Inbox-dedupe → refresh facet/synonym cache (may be a no-op in MVP). |
| `SavedSearchEvaluationHandler` | internal `JobIndexed` | Load active saved searches with `NotificationPreference != None`, `SavedSearchMatcher.Matches` each against the new entry, group matches by saved search, emit `SavedSearchMatchFound` to the outbox, `RecordEvaluated`. |

### 10.4 Validators — representative rules

- `SearchJobsQueryValidator`: `Criteria` must satisfy invariant 7 (`E-SEARCH-EMPTY-CRITERIA`); `PageSize` 1–100; `Page ≥ 1`; `SalaryMin ≤ SalaryMax`; `DatePostedFrom ≤ DatePostedTo`; `RadiusKm > 0` requires a `Location`.
- `AddFavoriteJobCommandValidator`: `PostingId` non-empty.
- `CreateSavedSearchCommandValidator`: `Name` non-empty ≤ 100; `NotificationPreference` in enum; `Criteria` valid as a saved (filter-only allowed) criteria.
- `RenameSavedSearchCommandValidator`: `NewName` non-empty ≤ 100.
- `SetSavedSearchNotificationCommandValidator`: `Preference` in enum.
- `DismissRecommendationCommandValidator`: `PostingId` non-empty.
- All saved-search commands: the handler (not the validator) enforces ownership (`E-FORBIDDEN`) from the access token's `UserId`.

### 10.5 DTOs

Plain data records in `Application`. Never expose Domain entities/VOs across the API. `SearchResultDto` carries `Items` (`list<RankedJobDto>`), `Recommendations` (`list<RankedJobDto>`, empty for anonymous users, each flagged `IsRecommended = true`), `Page`, `PageSize`, `TotalCount`, `AppliedSort`, and a `NoResults` flag. Map aggregate/projection → DTO in the handler.

---

## 11. Persistence & data model

Schema/namespace: `search_discovery`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. No foreign key crosses a module boundary — `posting_id`, `employer_id`, `seeker_user_id`, `job_seeker_id` are plain `uuid` columns with no FK constraint. The module-specific relational model follows.

### 11.1 Reference relational model — schema `search_discovery`

```
TABLE job_index_entries
  id                       uuid        PK                        -- == BC-4 PostingId, no FK
  employer_id              uuid        NOT NULL                  -- no FK
  title                    string      NOT NULL
  summary                  string      NOT NULL
  company_name             string      NOT NULL
  skills                   json        NOT NULL DEFAULT '[]'     -- list<string> of skill codes/labels
  education_requirement    string      NULL
  experience_years         int         NULL
  location                 json        NOT NULL                  -- GeoLocation VO {district,city,lat,lng}
  location_district        string      NOT NULL                  -- flattened for fast filtering
  employment_type          enum        NOT NULL
  work_format              enum        NOT NULL
  salary_min               decimal     NULL                      -- flattened from SalaryRange for range filters
  salary_max               decimal     NULL
  salary_currency          string      NULL
  sector_industry          string      NULL
  posted_on_utc            datetime    NOT NULL
  application_deadline_utc datetime    NULL
  source_posting_version   int64       NOT NULL                  -- monotonic guard (invariant 2)
  indexed_on_utc           datetime    NOT NULL
  updated_on_utc           datetime    NOT NULL
  INDEX (title)                                                  -- keyword search
  INDEX (posted_on_utc DESC)                                     -- date sort
  INDEX (location_district)
  INDEX (employment_type)
  INDEX (salary_min, salary_max)
  INDEX (application_deadline_utc)
  -- skills: a containment index on the json column for skill-containment filters (e.g. a GIN index
  --   where the database supports it)
  -- title/summary keyword match: substring/LIKE matching is sufficient for the MVP per US-3.2.2
  --   assumptions; a full-text index is a documented future optimization.

TABLE favorite_jobs
  id                       uuid        PK
  seeker_user_id           uuid        NOT NULL                  -- BC-1 identity, no FK
  posting_id               uuid        NOT NULL                  -- BC-4 posting, no FK
  favorited_on_utc         datetime    NOT NULL
  UNIQUE (seeker_user_id, posting_id)                            -- invariant 4
  INDEX (seeker_user_id)

TABLE saved_searches
  id                       uuid        PK
  seeker_user_id           uuid        NOT NULL                  -- BC-1 identity, no FK
  name                     string      NOT NULL
  criteria                 json        NOT NULL                  -- SearchCriteria VO
  notification_preference  enum        NOT NULL                  -- None|DailyDigest|WeeklyDigest|Instant
  last_evaluated_on_utc    datetime    NULL
  created_on_utc           datetime    NOT NULL
  updated_on_utc           datetime    NOT NULL
  version_token            (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  UNIQUE (seeker_user_id, name)                                  -- invariant 6
  INDEX (seeker_user_id)
  INDEX (notification_preference) WHERE notification_preference <> 'None'  -- evaluation scan

TABLE search_sessions
  id                       uuid        PK
  seeker_user_id           uuid        NOT NULL UNIQUE           -- one active session per seeker
  last_criteria            json        NULL                      -- SearchCriteria VO
  dismissed_recommendation_posting_ids json NOT NULL DEFAULT '[]' -- list<uuid>
  expires_on_utc           datetime    NOT NULL
  updated_on_utc           datetime    NOT NULL
  version_token            (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  INDEX (expires_on_utc)                                         -- purge sweep

TABLE match_score_cache                                          -- BC-7 MatchComputed projection
  job_seeker_id            uuid        NOT NULL                  -- no FK
  posting_id               uuid        NOT NULL                  -- no FK
  score                    int         NOT NULL                 -- 0-100
  computed_on_utc          datetime    NOT NULL                  -- event OccurredOnUtc; newer wins
  PRIMARY KEY (job_seeker_id, posting_id)
  INDEX (job_seeker_id)

TABLE recommendation_cache                                       -- BC-7 RecommendationGenerated projection
  job_seeker_id            uuid        PK                        -- no FK; one current list per seeker
  posting_ids              json        NOT NULL DEFAULT '[]'
  computed_at_utc          datetime    NOT NULL

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

### 11.2 Module-specific mapping notes

- `JobIndexEntry`, `FavoriteJob`, `SavedSearch`, `SearchSession` are each their own table; there are no owned child collections (this is a flat read-model BC). `match_score_cache` and `recommendation_cache` are projection tables — model them as plain keyed entities, not domain aggregates.
- **Flatten** the fields needed for indexed filtering/sorting to scalar columns: `location_district`, `salary_min`/`salary_max`/`salary_currency`, `employment_type`, `work_format`, `posted_on_utc`, `application_deadline_utc`. Keep the full VO in `json` for round-tripping; the scalar columns are derived projections kept in sync inside the aggregate behaviors.
- `JobIndexEntryId` is constructed from the BC-4 `PostingId` value.
- Optimistic-concurrency tokens are required on `saved_searches` and `search_sessions` (the seeker-edited aggregates). `job_index_entries` does not need one — the `source_posting_version` guard already serializes projection writes.
- Every consumed integration event (the eight in §9.1) records its `EventId` in `inbox_messages`; projection handlers skip already-processed IDs. The `source_posting_version` guard is a second, independent line of defence against out-of-order or replayed posting events.
- **Search performance**: the `US-3.2.2-01` 2-second budget is met by (a) the flattened scalar filter columns + their indexes, (b) the containment index on `skills`, and (c) the event-fed match/recommendation caches so no synchronous BC-7 call sits on the hot path. Substring/`LIKE` keyword matching is acceptable for the MVP per the story assumptions.
- **Session purge**: a background sweep deletes `search_sessions` past `expires_on_utc`.
- Persistence conventions (the persistence context, ORM mappings, `json` value-object conversion, strongly-typed id conversion, the outbox/inbox transaction rules) follow **[[00-Shared-Foundations]] §3 and §6**.

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

`JobIndexRepository` (`GetById`, `Exists`, `Search(criteria)` → matched entries, `Upsert`, `Delete`), `FavoriteJobRepository` (`Get(seekerUserId, postingId)`, `ListBySeeker`, `Add`, `Delete`), `SavedSearchRepository` (`GetById`, `ListBySeeker`, `IsNameTaken(seekerUserId, name)`, `ListActiveForEvaluation`, `Add`, `Update`, `Delete`), `SearchSessionRepository` (`GetBySeeker`, `Add`, `Update`, `DeleteExpired`), `MatchScoreCacheRepository` (`GetScores(seekerId, postingIds)`, `Upsert`), `RecommendationCacheRepository` (`Get(seekerId)`, `Replace`), `UnitOfWork` (`SaveChanges`).

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. Route prefix `/api/search`. **Search itself is anonymous-allowed** — `SearchJobsQuery` works without an access token (no recommendations, no session persistence beyond an anonymous session token). Favorites, saved searches, recommendation dismissal, and session retrieval require a valid access token (issued by BC-1); the seeker's `UserId` is taken from the token. Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

| Verb & route | Command/Query | Auth | Success | Notable failures |
|---|---|---|---|---|
| `POST /api/search/jobs` | `SearchJobsQuery` | optional | `200` + `SearchResultDto` | `400 E-SEARCH-EMPTY-CRITERIA`, `400` invalid filter ranges |
| `POST /api/search/jobs/refine` | `RefineSearchResultsQuery` | optional | `200` + `SearchResultDto` | `400` invalid filter ranges |
| `GET /api/search/session` | `GetSearchSessionQuery` | seeker | `200` + `SearchSessionDto` | |
| `POST /api/search/recommendations/{postingId}/dismiss` | `DismissRecommendationCommand` | seeker | `204` | |
| `GET /api/search/favorites` | `GetFavoritesQuery` | seeker | `200` + favorites | |
| `POST /api/search/favorites` | `AddFavoriteJobCommand` | seeker | `201` + `FavoriteJobId` | `404 E-JOB-NOT-FOUND` |
| `DELETE /api/search/favorites/{postingId}` | `RemoveFavoriteJobCommand` | seeker | `204` | (no-op success if not favorited) |
| `GET /api/search/saved-searches` | `GetSavedSearchesQuery` | seeker | `200` + saved searches | |
| `POST /api/search/saved-searches` | `CreateSavedSearchCommand` | seeker | `201` + `SavedSearchId` | `409 E-SAVED-SEARCH-NAME-DUPLICATE`, `400` invalid criteria |
| `PUT /api/search/saved-searches/{id}/name` | `RenameSavedSearchCommand` | seeker | `200` | `403 E-FORBIDDEN`, `404`, `409` name duplicate |
| `PUT /api/search/saved-searches/{id}/criteria` | `UpdateSavedSearchCriteriaCommand` | seeker | `200` | `403 E-FORBIDDEN`, `404`, `400` invalid criteria |
| `PUT /api/search/saved-searches/{id}/notification` | `SetSavedSearchNotificationCommand` | seeker | `200` | `403 E-FORBIDDEN`, `404` |
| `DELETE /api/search/saved-searches/{id}` | `DeleteSavedSearchCommand` | seeker | `204` | `403 E-FORBIDDEN`, `404` |
| `POST /api/search/saved-searches/{id}/run` | `RunSavedSearchQuery` | seeker | `200` + `SearchResultDto` | `403 E-FORBIDDEN`, `404` |

Notes:
- `POST` is used for `/jobs` and `/jobs/refine` because `SearchCriteria` is a rich structured body, not a flat query string. The handler still treats them as queries (no state mutation beyond the `SearchPerformed` event + session-criteria memory, which are side-effects of *observing* a search, consistent with the [[Event_Catalog]]).
- Setting a saved search's notification preference to `None` via `PUT .../notification` is the **unsubscribe** action (`US-3.2.2-03 AC-08`) — the saved search itself is **not** deleted.
- Anonymous callers of `/api/search/jobs` get `Recommendations = []`; the handler skips the BC-7 lookups entirely when there is no `UserId`.

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** every VO factory — valid input succeeds; each invalid case returns the right `Error`. Specifically: `SearchCriteria` (all-empty criteria fails `E-SEARCH-EMPTY-CRITERIA`; filter-only allowed for a saved search; `PageSize` 0 and 101 fail, 1 and 100 pass), `SearchFilters` (`SalaryMin > SalaryMax` fails; `DatePostedFrom > DatePostedTo` fails; `RadiusKm` set without `Location` fails), `IntentHint` (all-empty fails `E-INTENT-EMPTY`), `GeoLocation` (lat without lng fails; lat 91 fails), `SalaryRange` (negative min fails, `min > max` fails), `RelevanceWeights` (non-decreasing title→skill→summary ordering enforced).
- **JobIndexEntry aggregate:**
  - `Project` builds an entry with `SourcePostingVersion` set from the event.
  - `ApplyUpdate` with a **stale or equal** version is a **no-op** (invariant 2) — the entry is byte-identical afterward; with a **newer** version, fields refresh and the version bumps.
  - `Project` for an id that already exists upserts, not duplicates (invariant 3) — tested at the repository level too.
- **FavoriteJob aggregate:** `Add` twice for the same `(seeker, posting)` is idempotent — the handler test asserts no duplicate; `Remove` on a non-existent favorite is a no-op success.
- **SavedSearch aggregate:** `Create` raises `SavedSearchCreated`; `SetNotificationPreference(None)` keeps the search alive (unsubscribe-not-delete); `Rename`/`UpdateCriteria` validate inputs; `EvaluateAgainst` delegates to `SavedSearchMatcher`.
- **SearchSession aggregate:** `RememberCriteria` stores the last criteria and extends expiry; `DismissRecommendation` dedupes; `IsExpired` boundary at `ExpiresOnUtc`.
- **Domain services:**
  - `RelevanceRanker` — table-driven: a title-only match outranks a skill-only match outranks a summary-only match (`US-3.2.2-01 AC-02`); multiple hits sum.
  - `ResultRankBlender` — `sort = MatchScore` orders by BC-7 score with un-scored entries last; `sort = Relevance` uses match score only as a tiebreaker; a **missing/partial** match-score map never drops a result (invariant 8); `DatePosted`/`Salary`/`ApplicationDeadline` sorts order correctly.
  - `SavedSearchMatcher` — AND logic across filter types (`US-3.2.2-01 AC-06`): an entry matching the keyword but failing one filter does **not** match; an entry satisfying every filter does; keyword applies across title/summary/skills.
  - `SearchCriteriaInterpreter` — `IntentHint{ WorkFormat="Remote" }` adds `Remote` to `WorkFormats`; industries/skill terms fold into the corresponding filters; base filters are preserved.

### 13.2 Unit tests — Application (handlers, ports & repositories replaced with test doubles)

- `SearchJobsQuery`: logged-in seeker → results ranked, recommendations block populated from `recommendation_cache`, session-dismissed ids excluded, `SearchPerformed` queued to outbox; anonymous caller → `Recommendations` empty, **no** BC-7 lookup performed; empty criteria → `E-SEARCH-EMPTY-CRITERIA`, no event emitted.
- `SearchJobsQuery` cold-start: when `match_score_cache` is empty, the handler falls back to `RecommendationQueryApi.GetMatchScores`; when that also returns empty, results still return ranked purely by relevance.
- `AddFavoriteJobCommand`: posting not in index → `E-JOB-NOT-FOUND`; favoriting an already-favorited job returns the existing favorite (idempotent), no duplicate persisted.
- `CreateSavedSearchCommand`: duplicate name for the same seeker → `E-SAVED-SEARCH-NAME-DUPLICATE`; same name for a *different* seeker is allowed.
- `SetSavedSearchNotificationCommand`: setting `None` leaves the `SavedSearch` row present; setting it by a non-owner → `E-FORBIDDEN`.
- All saved-search commands by a non-owning seeker → `E-FORBIDDEN`.
- `JobPostingPublishedProjectionHandler`: a fresh event creates an entry and raises internal `JobIndexed`; re-delivering the same `EventId` is a no-op (inbox dedupe).
- `SavedSearchEvaluationHandler`: on `JobIndexed`, only saved searches with `NotificationPreference != None` whose criteria match the new entry produce `SavedSearchMatchFound`; a non-matching new posting produces none.
- `JobPostingRemovedProjectionHandler`: `JobPostingExpired`/`Closed`/`Suspended` deletes the entry; deleting an absent entry is a no-op.
- Validation behavior: each validator rejects the documented bad inputs before the handler runs.

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip each aggregate including `json` VOs and flattened scalar columns; `JobIndexRepository.Search` honours every filter type with AND logic and substring keyword matching; the skills-containment index is used for skill-containment filters.
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `search_discovery`.
- **Projection idempotency / ordering:** delivering `JobPostingPublished` then a *lower*-version `JobPostingUpdated` leaves the entry at the higher version (no-op update); delivering the same event twice (same `EventId`) creates exactly one entry.
- **Discoverable-only invariant:** after `JobPostingExpired`, the entry is gone and `SearchJobsQuery` never returns it; no status filtering is needed at query time.
- **Outbox:** running `SearchJobsQuery` writes a `SearchPerformed` outbox row; creating a saved search writes `SavedSearchCreated`; a `JobIndexed` that matches an active saved search writes `SavedSearchMatchFound` — each in one transaction with its trigger.
- **Inbox / idempotency:** delivering `MatchComputed` twice upserts one cache row; delivering an out-of-order older `MatchComputed` does not overwrite a newer score.
- **Saved-search matching end-to-end:** create a saved search with `Instant`; project a new posting that matches its criteria; assert a `SavedSearchMatchFound` row exists with the correct `PostingIds` and `NotificationPreference`; project a non-matching posting and assert none.
- **API:** host-level tests — anonymous `POST /api/search/jobs` succeeds with no recommendations; logged-in search returns a recommendations block; the favorite happy path (add → list → remove); a non-owner `DELETE` of a saved search returns `403`.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "Search jobs (logged-in seeker, with recommendations)"

End-to-end, to pattern-match every other query/command against:

1. **API.** `POST /api/search/jobs` with a `SearchCriteria` body `{ keyword, filters, intentHint?, sort, page, pageSize }`. If an access token is present, the endpoint reads `UserId` from it; otherwise the caller is anonymous. It builds `SearchJobsQuery { Criteria, UserId? }` and dispatches it through the mediator.
2. **Validation step.** `SearchJobsQueryValidator` runs: criteria not empty (`E-SEARCH-EMPTY-CRITERIA`), `SalaryMin ≤ SalaryMax`, `DatePostedFrom ≤ DatePostedTo`, `PageSize` 1–100. On failure → `Result` with `Error`, mapped to `400`.
3. **Handler.** `SearchJobsQueryHandler`:
   a. If `Criteria.IntentHint` is present → `SearchCriteriaInterpreter.Apply(criteria, hint)` folds the hint into concrete filters.
   b. `JobIndexRepository.Search(criteria)` → the matched `JobIndexEntry` set (substring keyword match over title/summary/skills + every filter, AND logic). Because the index holds **only discoverable entries**, no status filtering is needed.
   c. `RelevanceRanker.Rank(keyword, intentHint, matches, RelevanceWeights.Default)` → `list<ScoredEntry>` (title > skill > summary).
   d. If `UserId` is present: `MatchScoreCacheRepository.GetScores(userId, matchedPostingIds)`; if empty, fall back to `RecommendationQueryApi.GetMatchScores`. Also load `recommendation_cache` for the seeker (fallback `RecommendationQueryApi.GetRecommendedPostingIds`), and the seeker's `SearchSession` to drop `DismissedRecommendationPostingIds`.
   e. `ResultRankBlender.Blend(scored, matchScores, sort)` → the final ordered page.
   f. Build `SearchResultDto`: the ranked page + a `Recommendations` block (each `RankedJobDto.IsRecommended = true`) for logged-in seekers, empty for anonymous.
   g. `RememberSearchCriteria` on the `SearchSession` (so the form re-populates next time), and write `SearchPerformedIntegrationEvent` to the outbox.
   h. `unitOfWork.SaveChanges()`.
4. **Domain-event / outbox step.** After the unit of work is saved, the mediator pipeline writes `SearchPerformedIntegrationEvent` (with `ResultCount`) into the outbox — same transaction as the session update. ([[00-Shared-Foundations]] §6.1–6.2.)
5. **Relay.** The background outbox relay publishes the integration event; BC-7 uses it as a behavior signal, BC-10 records it for analytics.
6. **Response.** Handler returns `Result<SearchResultDto>` success; the endpoint returns `200` within the 2-second budget.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.2.2-01 Search jobs (basic & advanced) | Single-keyword search over indexed title/summary/skills within a 2-second budget; results ranked by relevance (title > skill > summary); advanced filter panel with keywords, location+radius, salary range, employment type, date posted (relative + absolute), deadline, required skills/education/experience, sector/industry; multiple filters AND-combined; semantic `IntentHint` folded into structured filters via `SearchCriteriaInterpreter` (the NLP itself is upstream); filters persist within the session via `SearchSession.LastCriteria`. |
| US-3.2.2-02 Filter & rank results with AI recommendations | Results ranked by skills-matching score when a BC-7 signal is available, otherwise pure relevance; sort options relevance / match score / date posted / salary / application deadline; result set re-filterable without a fresh search (`RefineSearchResultsQuery`); for logged-in seekers a clearly-marked "Recommended for you" block sourced from BC-7; anonymous seekers see query results only; a recommendation can be dismissed for the session; match scores are advisory and never block a result. |
| US-3.2.2-03 Save favorites & searches with notifications | Add/remove/list favorites (favorite uniqueness per seeker, idempotent re-favorite); save a named filter combination; rename, edit criteria, delete, and one-click run a saved search; per-search notification preference `None`/`DailyDigest`/`WeeklyDigest`/`Instant`; a new indexed posting matching an active saved search emits `SavedSearchMatchFound` for BC-9 to deliver; unsubscribe (set preference to `None`) keeps the saved search; ownership enforced so a seeker only touches their own favorites and saved searches. |

---

## Appendix — teaching notes & open questions

- **A BC that projects rather than authors.** `JobIndexEntry` is an aggregate this BC never *creates by command* — its whole lifecycle is driven by consumed BC-4 events. This is the cleanest example in the platform of the **CQRS read-model / Conformist** pattern: BC-6 accepts BC-4's published language as-is and maintains a query-optimized copy. Ask the class: *is `JobIndexEntry` really an "aggregate", or just a denormalized view?* (It earns the name because it has invariants — version monotonicity, discoverable-only — enforced through behaviors.)
- **Favorites vs. bookmarks — duplication or two concepts?** §1's boundary note: `US-3.2.2-03` (BC-6) wants "favorites" while [[BC_Mapping]] moved "bookmarks" to BC-5. This package treats them as **two bounded concepts** that happen to share a verb. Strong discussion material on the limits of ubiquitous language — the *same word* ("save a job") means different things in the discovery context and the application context.
- **Event-fed cache vs. synchronous port.** §9.2 keeps *both* a `MatchComputed`/`RecommendationGenerated`-fed cache (fast, eventually consistent) and an `IRecommendationQueryApi` synchronous fallback (fresh, but couples search latency to BC-7). The 2-second search budget is the forcing function. Discuss when each is appropriate and the cost of the hybrid (two code paths, two consistency models).
- **Customer/Supplier, not Partnership.** [[Context_Map]] marks BC-7→BC-6 as Customer/Supplier: Search *can* adapt to the Recommendation Engine on its own schedule, and degrades gracefully (pure relevance ranking) when BC-7 signals are absent. Contrast with BC-3↔BC-7 Partnership, where the coupling is structural. Invariant 8 ("match score is advisory") is what *makes* the relationship C/S rather than Partnership — point this out explicitly.
- **Out-of-order and replayed events.** The `source_posting_version` monotonicity guard (invariant 2) plus the inbox dedupe table give two independent defences. Worth walking through: what corruption would a re-delivered or out-of-order `JobPostingUpdated` cause *without* the version guard?
- **Where does saved-search *matching* run?** This package evaluates saved searches in-module on the internal `JobIndexed` event (`SavedSearchEvaluationHandler`), then emits `SavedSearchMatchFound` for BC-9. An alternative is a full **SavedSearchAlertSaga** (named in [[BC_Mapping]]) orchestrating index → match → recommendation recompute → digest. This package does the *match* step and hands off; discuss where the saga boundary should sit and whether choreography (this design) or orchestration (a process manager) is clearer at ~70 events.
- **`SearchPerformed` is a side-effect of a query.** Issuing a search emits an event and updates the session — a query with side-effects. Note for the class: this is consistent with the [[Event_Catalog]] (which lists `SearchPerformed` as a BC-6 event) and does not violate CQRS, because the side-effects are *observations of the read*, not mutations of search's source data (it has none).
- **Localization / radius search.** `GeoLocation` carries optional lat/lng so radius search is *possible*, but the MVP substring + `location_district` filtering is the documented baseline. True geo-radius is a documented future enhancement, not required by the ACs.
