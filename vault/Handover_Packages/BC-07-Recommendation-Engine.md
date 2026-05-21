---
title: "Handover Package — BC-7 Recommendation Engine"
type: handover-package
bc_id: BC-7
bc_name: Recommendation Engine
bc_class: CORE
stack: "language-neutral — see [[00-Shared-Foundations]] (Target Stack declared there)"
generated: 2026-05-15
related:
  - "[[00-Shared-Foundations]]"
  - "[[BC_Mapping]]"
  - "[[Context_Map]]"
  - "[[Event_Catalog]]"
tags:
  - handover-package
  - bc/matching
  - bc/recommendation-engine
---

# Handover Package — BC-7 Recommendation Engine

> **Audience:** an AI coding agent. This package owns the **domain design** for the `RecommendationEngine` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `RecommendationEngine`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `recommendation_engine`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

This BC is the **CORE subdomain** (see [[Context_Map]]): the AI matching engine is the platform's reason for existing. It has **Partnership** relationships with BC-3 JobSeeker Profile and BC-4 Job Postings — their schemas *are* the matching inputs, so they evolve in lockstep with this module. Treat that coupling deliberately: §9 reproduces exactly what this BC needs from each, and nothing more.

**Critical framing — what is "the AI" here.** This module *orchestrates* matching. It does **not** implement the transformer model, the embedding network, or the vector-similarity index. Those are external concerns reached through port interfaces (`EmbeddingModelPort`, `VectorIndexPort`, `NlpExtractionPort` — §9.2). This module decides *what* to embed, *when* to recompute, *how* to combine factor scores into a final match percentage, *which* matches clear the threshold, and *who* is allowed to see them. The ports return vectors and similarities; the domain owns the scoring formula, the thresholds, the talent pools, and the privacy rules.

---

## 1. Purpose & scope boundaries

### What this BC is for

Recommendation Engine owns **two-way AI matching**: scoring how well a job seeker fits a job posting (and vice versa), turning those scores into ranked recommendations for seekers, ranked candidate shortlists for employers, and persistent talent pools recruiters curate. It owns the **matching parameters** (factor weights, match thresholds, A/B weight variants), the **embeddings** that make semantic matching possible, and the **privacy enforcement** that decides which candidates an employer is even allowed to be recommended.

It is a **CORE** subdomain. The quality of this module *is* the platform's competitive differentiator.

### In scope

The `RecommendationEngine` module is responsible for:

- **Match scoring** (`US-3.3.1-01`): computing a multi-factor `MatchScore` between a `(jobSeekerId, jobPostingId)` pair — weighted contributions from skill overlap, education, training, location proximity, experience alignment, and salary range overlap.
- **NLP semantic analysis** (`US-3.3.1-02`): orchestrating extraction of structured attributes (required skills with inferred proficiency, experience level, job categories, responsibilities) from job-posting and resume text, each attribute carrying a confidence score; recognising semantic equivalence ("JavaScript expertise" ≈ "JS development experience") via embedding cosine similarity.
- **Seeker-facing recommendations** (`US-3.3.2-01`, `US-3.3.2-03`, `US-3.3.1-07`): generating a personalised, ranked `JobRecommendationSet` per seeker — hybrid of collaborative-filtering signal, content-based similarity, and the §6 match score — respecting location/salary/work-arrangement preferences, applying the match threshold, and capturing "not interested" feedback.
- **Employer-facing candidate shortlists** (`US-3.3.1-05`, `US-3.3.3-01`, `US-3.3.3-02`, `US-3.3.3-05`): maintaining a ranked `CandidateShortlist` per published posting, with per-factor breakdowns, auto-extracted strengths/gaps, fit-analysis insights, and recruiter-set qualification thresholds.
- **Candidate database search** (`US-3.3.3-03`): a recruiter-driven faceted query over the candidate read model (skills, experience, location/radius, salary, education, certifications).
- **Talent pools** (`US-3.3.3-06`): the `TalentPool` aggregate — employer-scoped, named, multi-pool, per-candidate notes, soft-remove, optional team sharing.
- **Privacy enforcement** (`US-3.3.3-04`): excluding `Hidden` / `ApplyOnly` candidates from recommendations and search unless they applied to the posting in question; logging every recommendation exposure for audit.
- **Match threshold configuration** (`US-3.3.1-06`): a global default threshold plus per-posting overrides, range validation, audit logging, impact preview.
- **Matching parameter configuration** (`US-3.3.1-08`): the active `MatchingWeightProfile` (six factor weights summing to 1.0), A/B weight variants with stratified assignment, rollback to any of the last 10 configurations.
- **Nightly embedding recompute** (`US-3.3.2-02`): the batch process that refreshes seeker and posting embeddings, processing only entities queued since the last run.
- Publishing the integration events in §8 and reacting to the integration events in §9.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong elsewhere and are reached via the contracts in §9:

- **The transformer / embedding model itself, and the vector index** → external, reached via `EmbeddingModelPort`, `NlpExtractionPort`, `VectorIndexPort`. This module sends text/profiles and receives vectors and similarities; it never loads model weights or runs inference.
- **The job seeker's profile data** (skills, education, experience, preferences, visibility, resume) → BC-3 JobSeeker Profile. This module **consumes** `ResumeParsed`, `ProfileSkillsUpdated`, `ProfileLevel2Completed`, `ProfileVisibilityChanged` and keeps a *projection* (read model) of what it needs to match. It never edits a profile.
- **The job posting content** (title, description, requirements, salary, location, status) → BC-4 Job Postings. This module **consumes** `JobPostingPublished`, `JobPostingUpdated`, `JobPostingClosed/Expired/Suspended` and projects what it needs. It never edits a posting.
- **The skill / occupation / industry taxonomy** → BC-11 Administrators Configuration. This module references taxonomy codes and reacts to `TaxonomyUpdated` to rebuild affected embeddings; it does not own the vocabulary.
- **Employer identity, recruiter roles, recruiter team membership** → BC-2 Employer Profile / BC-1 IAM. This module receives `employerId` / `recruiterId` as plain `uuid`s and asks `EmployerAccessApi` whether a recruiter may act on a posting; it does not store roles.
- **Job search (keyword search, facets over postings)** → BC-6 Search & Discovery. This module **supplies** ranking/relevance scores to BC-6 via `MatchRankingPublicApi`; BC-6 blends them with keyword search. Note: `US-3.3.3-03` candidate-database search *is* owned here (it queries the candidate read model, not postings).
- **Sending notifications / digests / emails** → BC-9 Notification. This module emits `RecommendationGenerated` / `CandidateRecommendationGenerated`; BC-9 decides what to send.
- **Bookmarking and applying to jobs** → BC-5 Job Application. This module *consumes* `JobBookmarked` / `ApplicationSubmitted` as preference signals for re-ranking.
- **Reporting / matching-performance dashboards** → BC-10 Reporting. This module emits `MatchComputed`, `EmbeddingsRefreshed`, `MatchThresholdChanged`; BC-10 builds the dashboards. The A/B-variant *metrics dashboard* of `US-3.3.1-08 AC-05` is BC-10's surface — this module only emits the variant-tagged events it needs.

### Boundary note — the Partnership tension (teaching point)

BC-3↔BC-7 and BC-4↔BC-7 are **Partnership** relationships, not Customer/Supplier. That means this module cannot treat BC-3's profile schema or BC-4's posting schema as a stable foreign contract it merely conforms to — when the matching algorithm needs a new input (say, "years in current role"), BC-3 and BC-7 negotiate the profile change *together*. In this package we model the practical consequence: BC-7 keeps its **own projection** (`SeekerMatchProfile`, `PostingMatchProfile` — §5) built from BC-3/BC-4 events, so day-to-day matching is decoupled, but the *event payloads* in §9.1 are jointly-owned contracts. Good class discussion: Partnership vs. Conformist — when is keeping your own projection enough, and when do you genuinely need lockstep schema evolution?

### Boundary note — does parsing belong here?

`US-3.3.1-02` (NLP semantic analysis) lives in this BC; `US-3.3.1-03`/`US-3.3.1-04` (resume *parsing*) live in BC-3. The seam: BC-3 owns resume parse *orchestration* and emits `ResumeParsed` with already-extracted skills/education/experience; BC-7 owns *job-description* NLP extraction and the *semantic-equivalence* judgement that feeds matching. Both are defensible the other way. We keep the split as drawn — discuss the trade-off (ownership clarity vs. one ML pipeline) in lecture.

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **Match Score** | The `MatchScore` aggregate: a computed, multi-factor fit score for one `(jobSeekerId, jobPostingId)` pair, 0–100, with a per-factor breakdown. |
| **Factor** | One of the six scored dimensions: `Skill`, `Education`, `Training`, `Location`, `Experience`, `Salary`. |
| **Factor Score** | A 0–100 score for a single factor, before weighting. |
| **Weight Profile** | The `MatchingWeightProfile` aggregate — the six factor weights (each 0.0–1.0, summing to 1.0) currently driving scoring, plus its A/B variant identity and version. |
| **Match Threshold** | The minimum overall match percentage at which a match is *surfaced*. Global default 60%, with per-posting overrides. Applied at **display/query time**, never at storage time. |
| **Embedding** | A dense vector (default 768-dim) representing a seeker or a posting in semantic space. Stored as `EmbeddingRecord`; the actual vector math is external. |
| **Seeker Match Profile** | `SeekerMatchProfile` — this BC's own projection of a job seeker (skills, education, experience, preferences, visibility), built from BC-3 events. The matching input, owned here. |
| **Posting Match Profile** | `PostingMatchProfile` — this BC's own projection of a job posting (required skills, experience level, location, salary range, work arrangements, status), built from BC-4 events. The other matching input. |
| **Job Recommendation Set** | `JobRecommendationSet` — a seeker's personalised, ranked list of recommended postings for a given compute run. |
| **Candidate Shortlist** | `CandidateShortlist` — a posting's ranked list of top-matching candidates (default top 100). |
| **Qualification Threshold** | Recruiter-set minimum bars on a posting's shortlist: min overall match %, min skill %, min education level, min experience years, required certifications. Distinct from the admin **Match Threshold**. |
| **Talent Pool** | `TalentPool` aggregate — an employer-scoped, named collection of saved candidates with per-candidate notes. |
| **Privacy Level** | A seeker's visibility tier as projected into this BC: `Public`, `ApplyOnly`, `Hidden`. Default `ApplyOnly`. |
| **Strengths / Gaps** | Auto-derived from a `MatchScore`: factors scoring > 80 are strengths, factors < 60 are gaps. |
| **Fit Analysis** | The bundle of recruiter-facing insights for a candidate-posting pair: match %, strengths, gaps, salary-fit indicator, motivation score, time-to-productivity estimate, contact-likelihood. |
| **Recommendation Feedback** | A seeker's `NotInterested` / `Applied` / `Viewed` signal against a recommended posting, used to suppress and re-rank. |
| **Recompute Queue** | The `PendingRecompute` set — seekers/postings changed during the day, awaiting the next nightly embedding batch. |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `RecommendationEngine`.
- Public surface (`Contracts`): the integration events published in §8 + the public API in §9.4.
- **Module-specific notes:** the Domain layer is also **ML-ignorant** — it never imports an embedding library; all ML work goes through the ports in §9.2. The module runs three **scheduled background workers** (driven from `Infrastructure`, orchestrating `Application` use cases): the **nightly embedding batch** (`US-3.3.2-02` — refreshes seeker/posting embeddings, processing only entities queued since the last run), the **daily candidate-shortlist refresh**, and the outbox relay. Inbound integration-event subscriptions (the projection handlers in §9.1) are wired into the module composition entry point.

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has **seven aggregates**. Three are *projection* aggregates (`SeekerMatchProfile`, `PostingMatchProfile`, `EmbeddingRecord`) — they hold this module's own copy of data sourced from BC-3/BC-4 events. Four are *domain-owned* aggregates (`MatchScore`, `MatchingWeightProfile`, `MatchThresholdConfiguration`, `TalentPool`). `JobRecommendationSet` and `CandidateShortlist` are also aggregates (computed results).

> **Why projections are aggregates here.** This BC must keep matching working even if BC-3/BC-4 are momentarily unavailable, and it must control exactly which fields enter the algorithm. So it owns local, event-sourced-from-integration-events projections. They are mutated *only* by integration-event handlers — never by the API.

### 5.1 Aggregate: SeekerMatchProfile (projection)

**Aggregate root.** Identity: `SeekerMatchProfileId` (strongly-typed id wrapping `uuid`). One per job seeker. Built and updated from BC-3 integration events.

| Member | Type | Notes |
|---|---|---|
| `Id` | `SeekerMatchProfileId` | |
| `JobSeekerId` | `uuid` | BC-3 identity, no FK. Unique. |
| `Skills` | `list<SkillRequirement>` | each: taxonomy code, label, proficiency 1–5, confidence |
| `EducationLevel` | `EducationLevel` | enum: `None`,`HighSchool`,`Diploma`,`Bachelor`,`Master`,`Phd` |
| `TrainingCredentials` | `list<string>` | certification / training codes |
| `TotalExperienceYears` | `decimal` | derived from BC-3 experience entries |
| `Location` | `GeoLocation?` | VO — lat/lon + city |
| `SalaryExpectation` | `SalaryRange?` | VO |
| `WorkArrangementPrefs` | `set<WorkArrangement>` | `OnSite`/`Hybrid`/`Remote` |
| `PreferredLocations` | `list<GeoLocation>` | |
| `PrivacyLevel` | `PrivacyLevel` | `Public`/`ApplyOnly`/`Hidden`, default `ApplyOnly` |
| `JobSearchStatus` | `JobSearchStatus` | `ActivelyLooking`/`OpenToOpportunities`/`Passive` |
| `ProfileCompleteness` | `int` | 0–100, projected from BC-3 |
| `IsActive` | `bool` | false when account deactivated/suspended |
| `LastProfileUpdateUtc` | `datetime` | |
| `EmbeddingVersion` | `string?` | version of the embedding currently representing this seeker |

### 5.2 Aggregate: PostingMatchProfile (projection)

**Aggregate root.** Identity: `PostingMatchProfileId`. One per published posting. Built and updated from BC-4 integration events.

| Member | Type | Notes |
|---|---|---|
| `Id` | `PostingMatchProfileId` | |
| `JobPostingId` | `uuid` | BC-4 identity, no FK. Unique. |
| `EmployerId` | `uuid` | denormalised from BC-4 event |
| `Title` | `string` | |
| `RequiredSkills` | `list<SkillRequirement>` | extracted via NLP — code, label, required proficiency, confidence |
| `RequiredEducationLevel` | `EducationLevel` | |
| `RequiredExperienceYears` | `decimal` | |
| `ExperienceLevel` | `ExperienceLevel` | enum: `Junior`,`Mid`,`Senior` |
| `RequiredCertifications` | `list<string>` | |
| `Location` | `GeoLocation?` | |
| `SalaryRange` | `SalaryRange?` | |
| `WorkArrangements` | `set<WorkArrangement>` | |
| `Status` | `PostingMatchStatus` | `Active`/`Inactive` — `Inactive` once closed/expired/suspended |
| `PerPostingThresholdOverride` | `int?` | nullable match-threshold override (set by BC-4 event payload or admin) |
| `NlpExtractionStatus` | `NlpExtractionStatus` | `Pending`/`Extracted`/`Failed` |
| `EmbeddingVersion` | `string?` | |
| `PublishedOnUtc` / `LastUpdatedUtc` | `datetime` | |

### 5.3 Aggregate: EmbeddingRecord (projection / cache)

**Aggregate root.** Identity: `EmbeddingRecordId`. The local cache of computed vectors. One per `(OwnerType, OwnerId, Version)`.

| Member | Type | Notes |
|---|---|---|
| `Id` | `EmbeddingRecordId` | |
| `OwnerType` | `EmbeddingOwnerType` | `Seeker` / `Posting` |
| `OwnerId` | `uuid` | the `JobSeekerId` or `JobPostingId` |
| `Vector` | `EmbeddingVector` | VO — `list<decimal>` of values, fixed dimension (default 768) |
| `Version` | `string` | embedding run / model version tag |
| `ComputedOnUtc` | `datetime` | |
| `TtlUtc` | `datetime` | computed + 24h; stale tolerance |
| `IsCurrent` | `bool` | exactly one current record per `(OwnerType, OwnerId)` |

### 5.4 Aggregate: MatchScore (domain-owned)

**Aggregate root.** Identity: `MatchScoreId`. The scored fit for one seeker-posting pair. Kept as its own aggregate because it has its own lifecycle (recomputed on input change), is queried at scale, and is the unit referenced by recommendations and shortlists.

| Member | Type | Notes |
|---|---|---|
| `Id` | `MatchScoreId` | |
| `JobSeekerId` | `uuid` | |
| `JobPostingId` | `uuid` | |
| `OverallScore` | `int` | 0–100 — weighted combination of factor scores |
| `Breakdown` | `MatchBreakdown` | VO — the six `FactorScore`s |
| `WeightProfileVersion` | `string` | which `MatchingWeightProfile` version produced this score |
| `WeightVariantId` | `string?` | A/B variant tag, if computed under a variant |
| `Strengths` | `list<MatchFactor>` | factors > 80 |
| `Gaps` | `list<MatchFactor>` | factors < 60 |
| `ComputedOnUtc` | `datetime` | |
| `IsStale` | `bool` | set true when an input changed; cleared on recompute |

### 5.5 Aggregate: JobRecommendationSet (domain-owned, computed)

**Aggregate root.** Identity: `JobRecommendationSetId`. One per seeker per compute run (the *current* set is the one with the latest `ComputedOnUtc`).

| Member | Type | Notes |
|---|---|---|
| `Id` | `JobRecommendationSetId` | |
| `JobSeekerId` | `uuid` | |
| `Items` | `list<RecommendationItem>` | child entities, ranked |
| `ComputedOnUtc` | `datetime` | |
| `WeightProfileVersion` | `string` | |
| `IsColdStart` | `bool` | true if seeker had < 5 interactions (content-based only) |

- `RecommendationItem` — `RecommendationItemId`, `JobPostingId`, `Rank` (`int`), `OverallScore` (`int`), `CollaborativeScore` (`int`), `ContentScore` (`int`), `HybridScore` (`int`), `Reason` (`RecommendationReason` VO — short text + top contributing factors).

### 5.6 Aggregate: CandidateShortlist (domain-owned, computed)

**Aggregate root.** Identity: `CandidateShortlistId`. One per published posting; recomputed daily / on demand.

| Member | Type | Notes |
|---|---|---|
| `Id` | `CandidateShortlistId` | |
| `JobPostingId` | `uuid` | unique |
| `EmployerId` | `uuid` | |
| `Entries` | `list<ShortlistEntry>` | child entities, ranked, default top 100 |
| `ConfiguredSize` | `int` | top-N, default 100, configurable per posting |
| `QualificationThreshold` | `QualificationThreshold` | VO — recruiter-set bars |
| `ComputedOnUtc` | `datetime` | |
| `RefreshState` | `ShortlistRefreshState` | `Fresh`/`Refreshing`/`Stale` |

- `ShortlistEntry` — `ShortlistEntryId`, `JobSeekerId`, `Rank` (`int`), `OverallScore` (`int`), `Breakdown` (`MatchBreakdown`), `Strengths`, `Gaps`, `IncludedReason` (`ShortlistInclusionReason`: `MatchAboveThreshold` / `AppliedDirectly`), `PrivacyLevelAtCompute` (`PrivacyLevel`).

### 5.7 Aggregate: MatchingWeightProfile (domain-owned)

**Aggregate root.** Identity: `MatchingWeightProfileId`. The configurable algorithm parameters. Multiple rows exist (history + variants); exactly one is `IsActive` per variant slot.

| Member | Type | Notes |
|---|---|---|
| `Id` | `MatchingWeightProfileId` | |
| `Version` | `string` | monotonically increasing version tag |
| `Weights` | `FactorWeights` | VO — six weights, each 0.0–1.0, sum == 1.0 (tolerance 0.01) |
| `VariantId` | `string` | `"control"` by default; named variants for A/B |
| `VariantAllocationPercent` | `int` | 0–100; share of new seekers/postings assigned to this variant |
| `IsActive` | `bool` | active variant currently used for new matches |
| `CreatedBy` | `uuid` | data scientist user id |
| `CreatedOnUtc` | `datetime` | |
| `SupersededByVersion` | `string?` | set when rolled back / replaced |

### 5.8 Aggregate: MatchThresholdConfiguration (domain-owned)

**Aggregate root.** Identity: `MatchThresholdConfigurationId` — a **singleton** (one row, well-known id). Holds the global threshold; per-posting overrides live on `PostingMatchProfile.PerPostingThresholdOverride`.

| Member | Type | Notes |
|---|---|---|
| `Id` | `MatchThresholdConfigurationId` | singleton |
| `GlobalThresholdPercent` | `int` | 0–100, default 60 |
| `ChangeLog` | `list<ThresholdChangeEntry>` | child entities — audit |
| `UpdatedOnUtc` | `datetime` | |

- `ThresholdChangeEntry` — `ThresholdChangeEntryId`, `OldValue` (`int`), `NewValue` (`int`), `ChangedBy` (`uuid`), `ChangedOnUtc` (`datetime`), `Scope` (`Global` or a `JobPostingId`).

### 5.9 Aggregate: TalentPool (domain-owned)

**Aggregate root.** Identity: `TalentPoolId`. Employer-scoped saved-candidate collection.

| Member | Type | Notes |
|---|---|---|
| `Id` | `TalentPoolId` | |
| `EmployerId` | `uuid` | |
| `OwnerRecruiterId` | `uuid` | recruiter who created it |
| `Name` | `string` | non-empty, ≤ 120 chars |
| `Description` | `string?` | ≤ 1000 chars |
| `AssociatedSkills` | `list<string>` | optional taxonomy codes / category |
| `IsShared` | `bool` | shared with the employer's recruiter team |
| `Members` | `list<TalentPoolCandidate>` | child entities |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

- `TalentPoolCandidate` — `TalentPoolCandidateId`, `JobSeekerId`, `AddedByRecruiterId`, `Note` (`string?`, ≤ 2000 chars), `IsActive` (`bool` — soft-remove), `AddedOnUtc`.

### 5.10 Aggregate: RecommendationFeedback (domain-owned)

**Aggregate root.** Identity: `RecommendationFeedbackId`. One per `(JobSeekerId, JobPostingId)` — the seeker's signal against a recommended posting.

| Member | Type | Notes |
|---|---|---|
| `Id` | `RecommendationFeedbackId` | |
| `JobSeekerId` | `uuid` | |
| `JobPostingId` | `uuid` | |
| `Signal` | `FeedbackSignal` | `NotInterested`/`Viewed`/`Applied` |
| `RecordedOnUtc` | `datetime` | |
| `SuppressUntilUtc` | `datetime?` | for `NotInterested`: recorded + 14 days |

### 5.11 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `FactorWeights` | `Skill`,`Education`,`Training`,`Location`,`Experience`,`Salary` (all `decimal`) | each ∈ [0,1]; sum == 1.0 ± 0.01 |
| `FactorScore` | `Factor` (enum), `Score` (`int`) | 0 ≤ score ≤ 100 |
| `MatchBreakdown` | exactly six `FactorScore`s, one per `Factor` | all six factors present, no duplicates |
| `EmbeddingVector` | `Values` (`list<decimal>`), `Dimension` (`int`) | `Values` length == `Dimension`; dimension > 0 (default 768) |
| `SkillRequirement` | `TaxonomyCode`, `DisplayLabel`, `Proficiency` (`int` 1–5), `Confidence` (`ConfidenceScore`) | code non-empty; proficiency ∈ [1,5] |
| `ConfidenceScore` | `Value` (`int` 0–100) | `NeedsReview => Value < 70` |
| `GeoLocation` | `Latitude` (`decimal`), `Longitude` (`decimal`), `City` (`string`) | lat ∈ [-90,90]; lon ∈ [-180,180] |
| `SalaryRange` | `Min` (`decimal`), `Max` (`decimal`), `Currency` (`string`) | `Min ≤ Max`; `Min ≥ 0`; currency ISO-4217 |
| `QualificationThreshold` | `MinOverallMatch` (`int`), `MinSkillMatch` (`int`), `MinEducationLevel` (`EducationLevel`), `MinExperienceYears` (`decimal`), `RequiredCertifications` (`list<string>`) | percentages 0–100; years ≥ 0 |
| `RecommendationReason` | `Summary` (`string`), `TopFactors` (`list<MatchFactor>`) | summary non-empty |
| `CandidateSearchCriteria` | `Skills` (`list<string>`), `ExperienceLevel?`, `ExperienceYearsRange?`, `Location?` + `RadiusKm?`, `SalaryRange?`, `MinEducationLevel?`, `Certifications` (`list<string>`) | radius > 0 when location set |
| `FitAnalysis` | `MatchScore` (`int`), `Strengths`, `Gaps`, `SalaryFit` (`SalaryFitIndicator`), `SalaryMatchPercent` (`int`), `MotivationScore` (`int` 0–100), `TimeToProductivity` (`TimeToProductivityEstimate` enum), `ContactLikelihood` (`ContactLikelihood` enum), `WorkArrangementCompatible` (`bool`) | scores 0–100 |
| `SalaryFitIndicator` | enum | `Green` (>80% overlap) / `Yellow` (50–80%) / `Red` (<50%) |

Enums used as VO-equivalents: `MatchFactor` (`Skill`,`Education`,`Training`,`Location`,`Experience`,`Salary`), `EducationLevel`, `ExperienceLevel`, `WorkArrangement`, `PrivacyLevel`, `JobSearchStatus`, `FeedbackSignal`, `EmbeddingOwnerType`, `PostingMatchStatus`, `NlpExtractionStatus`, `ShortlistRefreshState`, `ShortlistInclusionReason`, `TimeToProductivityEstimate` (`OneWeek`,`TwoToThreeWeeks`,`FourPlusWeeks`), `ContactLikelihood` (`High`,`Medium`,`Low`).

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior lives on the aggregate roots. Handlers never set properties directly.

### 6.1 SeekerMatchProfile — behaviors (mutated only by integration-event handlers)

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static CreateFromRegistration(jobSeekerId)` | empty projection in `IsActive = true` | — |
| `ApplyLevel2Completed(skills, educationLevel, experienceYears, completeness)` | replaces projected employability data; marks all this seeker's `MatchScore`s stale; queues for recompute | `SeekerMatchInputChanged` *(internal)* |
| `ApplyResumeParsed(skills, educationLevel, experienceYears)` | merges parsed data; same staling | `SeekerMatchInputChanged` *(internal)* |
| `ApplySkillsUpdated(addedSkills, removedSkills)` | applies skill delta; same staling | `SeekerMatchInputChanged` *(internal)* |
| `ApplyVisibilityChanged(privacyLevel)` | sets `PrivacyLevel`; if now `Hidden`/`ApplyOnly`, this seeker must be re-evaluated for exclusion from shortlists | `SeekerPrivacyChanged` *(internal)* |
| `ApplyPreferencesChanged(salaryExpectation, locations, workArrangementPrefs)` | replaces preference fields | `SeekerMatchInputChanged` *(internal)* |
| `Deactivate()` / `Reactivate()` | toggles `IsActive`; deactivated seekers excluded from all matching | `SeekerMatchInputChanged` *(internal)* |
| `MarkEmbeddingRefreshed(version)` | sets `EmbeddingVersion`; clears this seeker from the recompute queue | — |

### 6.2 PostingMatchProfile — behaviors (mutated only by integration-event handlers)

| Method | Rules | Domain event raised |
|---|---|---|
| `static CreateFromPublished(jobPostingId, employerId, title, salaryRange, location, workArrangements, requiredEducationLevel, requiredExperienceYears, perPostingThresholdOverride)` | `Status = Active`, `NlpExtractionStatus = Pending`; queue for NLP extraction + embedding | `PostingMatchInputChanged` *(internal)* |
| `ApplyNlpExtraction(requiredSkills, experienceLevel, requiredCertifications)` | only from `Pending`/`Failed` → `Extracted`; sets extracted attributes | `PostingMatchInputChanged` *(internal)* |
| `RecordNlpExtractionFailure(reason)` | `Pending` → `Failed`; matching falls back to keyword skills until retried | — |
| `ApplyPostingUpdated(changedFields...)` | updates projected fields; marks affected `MatchScore`s stale; re-queues NLP if description-affecting fields changed | `PostingMatchInputChanged` *(internal)* |
| `Deactivate(reason)` | from `Active` → `Inactive` on close/expire/suspend; its `CandidateShortlist` is no longer refreshed | `PostingMatchInputChanged` *(internal)* |
| `SetPerPostingThresholdOverride(percent?)` | `percent` ∈ [0,100] or null | — |
| `MarkEmbeddingRefreshed(version)` | sets `EmbeddingVersion`; clears from recompute queue | — |

### 6.3 MatchScore — behaviors

| Method | Rules / invariants | Domain event raised |
|---|---|---|
| `static Compute(seeker, posting, weightProfile, factorScores)` | `factorScores` is a full `MatchBreakdown`; `OverallScore` = weighted sum rounded to int; derives `Strengths`/`Gaps`; stamps `WeightProfileVersion` + `WeightVariantId`; `IsStale = false` | `MatchComputed` |
| `Recompute(weightProfile, factorScores)` | replaces breakdown + overall; `IsStale = false`; re-derives strengths/gaps | `MatchComputed` |
| `MarkStale()` | sets `IsStale = true`; idempotent | — |

**Invariants:** `OverallScore` always equals the weighted sum of `Breakdown` factor scores under `WeightProfileVersion`'s weights (recompute if weights change); a `MatchScore` exists for at most one `(JobSeekerId, JobPostingId)` pair; `Strengths` = factors with score > 80, `Gaps` = factors with score < 60, recomputed on every (re)compute; the threshold is **never** applied here — `MatchScore` stores the raw score, surfacing decisions happen at query time.

### 6.4 MatchingWeightProfile — behaviors

| Method | Rules | Domain event raised |
|---|---|---|
| `static CreateInitial()` | seeds the documented defaults: skill 0.25, education 0.15, training 0.10, location 0.15, experience 0.20, salary 0.15; `VariantId = "control"`, `IsActive = true` | — |
| `static CreateVariant(version, weights, variantId, allocationPercent, createdBy)` | `weights` valid `FactorWeights`; `allocationPercent` ∈ [0,100]; not active yet | — |
| `Activate()` | makes this the active profile for its `VariantId` slot; the previously active profile in that slot is superseded | `MatchThresholdChanged` *(reused for parameter changes — see §8 note)* |
| `UpdateWeights(newWeights, version, changedBy)` | creates a new version (history preserved); validates sum == 1.0 | as above |
| `RollbackTo(targetVersion)` | only among the **last 10** retained versions; reactivates the target, marks current superseded; audit-logged | as above |

**Invariants:** `Weights` always sum to 1.0 ± 0.01; at most one `IsActive == true` profile per `VariantId`; exactly one `"control"` variant always exists and is always active; the last 10 versions are retained, older ones may be purged; rollback target must be one of the retained 10.

### 6.5 MatchThresholdConfiguration — behaviors

| Method | Rules | Domain event raised |
|---|---|---|
| `static CreateDefault()` | `GlobalThresholdPercent = 60` | — |
| `UpdateGlobalThreshold(newPercent, changedBy)` | `newPercent` ∈ [0,100] (`E-THRESHOLD-OUT-OF-RANGE` otherwise); appends a `ThresholdChangeEntry` with old/new/by/at, `Scope = Global`; takes effect immediately for future queries | `MatchThresholdChanged` |
| `RecordPerPostingOverride(jobPostingId, oldValue, newValue, changedBy)` | appends a `ThresholdChangeEntry` with `Scope = jobPostingId` for audit (the value itself lives on `PostingMatchProfile`) | `MatchThresholdChanged` |

**Invariants:** `GlobalThresholdPercent` ∈ [0,100]; `ChangeLog` is append-only; the threshold is applied at **display/query time only** — changing it never rewrites stored `MatchScore`s, it changes which ones are *surfaced*.

### 6.6 TalentPool — behaviors

| Method | Rules / invariants | Domain event raised |
|---|---|---|
| `static Create(employerId, ownerRecruiterId, name, description, associatedSkills, isShared)` | `name` non-empty ≤ 120; **soft limit 20 active pools per employer** (`E-POOL-LIMIT-EXCEEDED`) checked by the handler | `TalentPoolCreated` *(internal)* |
| `AddCandidate(jobSeekerId, addedByRecruiterId, note)` | no duplicate **active** `TalentPoolCandidate` for the same `jobSeekerId`; re-adding a soft-removed candidate reactivates it | `CandidateSavedToTalentPool` |
| `RemoveCandidate(jobSeekerId)` | soft-remove — sets `IsActive = false`; candidate stays in the row, can be re-added | `CandidateRemovedFromTalentPool` *(internal)* |
| `UpdateCandidateNote(jobSeekerId, note)` | candidate must be an active member; `note` ≤ 2000 chars | — |
| `Rename(name)` / `UpdateDescription(desc)` / `SetAssociatedSkills(skills)` | `name` non-empty ≤ 120 | — |
| `SetShared(isShared)` | sharing requires explicit recruiter action — never auto-shared | — |

**Invariants:** a pool belongs to exactly one `EmployerId`; no two **active** `TalentPoolCandidate`s share a `JobSeekerId` within one pool; removal is always soft; sharing is opt-in only.

### 6.7 RecommendationFeedback — behaviors

| Method | Rules | Domain event raised |
|---|---|---|
| `static Record(jobSeekerId, jobPostingId, signal)` | `NotInterested` ⇒ `SuppressUntilUtc = now + 14 days`; `Viewed`/`Applied` ⇒ no suppression but used as positive signal | `RecommendationFeedbackRecorded` *(internal)* |
| `Refresh(signal)` | updates an existing feedback (e.g. `NotInterested` then later `Applied`) | as above |

**Invariant:** one feedback row per `(JobSeekerId, JobPostingId)`; a posting under active `NotInterested` suppression is excluded from new recommendation sets until `SuppressUntilUtc` passes or the posting is updated.

### 6.8 JobRecommendationSet & CandidateShortlist — behaviors

| Method | Rules / invariants | Domain event raised |
|---|---|---|
| `JobRecommendationSet.Generate(jobSeekerId, rankedItems, weightProfileVersion, isColdStart)` | 5–10 items for the seeker feed; items ranked by `HybridScore` desc; every item's `OverallScore` ≥ effective threshold; suppressed (`NotInterested`) postings excluded; cold-start ⇒ content-only items | `RecommendationGenerated` |
| `CandidateShortlist.Generate(jobPostingId, employerId, rankedEntries, configuredSize, qualificationThreshold)` | entries ranked by `OverallScore` desc, ties broken by most recent profile update; size ≤ `configuredSize`; every entry either above threshold **or** `AppliedDirectly`; **privacy filter applied** (see §7.6) | `CandidateRecommendationGenerated` |
| `CandidateShortlist.Refresh(rankedEntries)` | re-ranks; sets `RefreshState = Fresh`; only for `Active` postings | `CandidateRecommendationGenerated` |
| `CandidateShortlist.SetQualificationThreshold(threshold)` | recruiter-set; takes effect at next retrieval (filtering is at query time, no recompute) | — |
| `CandidateShortlist.SetConfiguredSize(n)` | `n` > 0; admin/recruiter-configurable; applied on next computation | — |

**Invariants:** the *current* recommendation set for a seeker is the one with the latest `ComputedOnUtc`; a `CandidateShortlist` never contains a `Hidden` candidate unless that candidate's `IncludedReason == AppliedDirectly`; shortlist ranking is deterministic given the same inputs (ties broken by `LastProfileUpdateUtc`).

### 6.9 Core invariants (must always hold — summary)

1. **Projections are read-only to the API.** `SeekerMatchProfile`, `PostingMatchProfile`, `EmbeddingRecord` are mutated *only* by integration-event handlers and the embedding batch — never by a command from the API.
2. **The threshold is a query-time filter.** No stored `MatchScore`, `JobRecommendationSet`, or `CandidateShortlist` row is rewritten when the threshold changes.
3. **Factor weights always sum to 1.0** (± 0.01); a `"control"` variant always exists and is always active.
4. **One current embedding** per `(OwnerType, OwnerId)`; superseded embeddings retained 7 days for debugging then purged.
5. **Privacy is enforced at shortlist generation and at candidate search.** A `Hidden` seeker appears to an employer only via `AppliedDirectly`; an `ApplyOnly` seeker never appears in candidate *search* and only in posting-specific shortlists if they applied.
6. **Match scores carry their provenance** — `WeightProfileVersion` and `WeightVariantId` — so A/B metrics and rollbacks are auditable.
7. **No cross-module FK.** `JobSeekerId`, `JobPostingId`, `EmployerId`, `recruiterId` are plain `uuid` columns.
8. **Talent-pool removal is always soft**; sharing is always opt-in.

---

## 7. Domain services

Stateless. Live in `Domain`. Used when logic spans entities or needs a small external input (the external input arrives via a port — the service receives already-fetched values, it does not call the port itself; the *handler* calls the port).

### 7.1 `MatchScoringService`

```
Score(seeker: SeekerMatchProfile, posting: PostingMatchProfile,
      weights: MatchingWeightProfile, skillSimilarity: SemanticSimilarity) -> Result<MatchScore>
  // skillSimilarity supplied by the handler via the VectorIndex port
```

Computes the six `FactorScore`s and combines them. **This is the heart of the CORE subdomain — implement it carefully.** Factor rules (from `US-3.3.1-01`):

- **Skill** — overlap of seeker skills vs. posting required skills, *semantic* not just literal: two skills count as matching if their cosine similarity > 0.75 (the `skillSimilarity` input carries the precomputed pairwise similarities). Proficiency shortfall reduces the factor score.
- **Education** — seeker `EducationLevel` vs. posting `RequiredEducationLevel`; meets-or-exceeds = 100, one level short scaled down, etc.
- **Training** — overlap of `TrainingCredentials` with `RequiredCertifications`.
- **Location** — haversine distance between seeker location (or preferred locations) and posting location vs. configurable radius (default 50 km); remote postings score 100 when the seeker's `WorkArrangementPrefs` include `Remote`.
- **Experience** — `TotalExperienceYears` vs. `RequiredExperienceYears` (and `ExperienceLevel` band); within band = 100, below scaled down.
- **Salary** — overlap of seeker `SalaryExpectation` with posting `SalaryRange`; full overlap = 100, within 10% above = soft-included with reduced score, far above = low.
- **Cold-start** — if the seeker has no parsed resume / sparse profile, score on education + location only (other factors contribute 0 weight-adjusted neutral). If the posting is brand-new with no behavioural data, fall back to keyword skill match.

`OverallScore = round( Σ factorScore_i × weight_i )`, clamped 0–100.

### 7.2 `RecommendationRankingService`

```
RankForSeeker(seeker: SeekerMatchProfile, candidateMatches: list<MatchScore>,
              collaborativeSignals: list<CollaborativeSignal>,   // supplied by the handler
              feedback: list<RecommendationFeedback>,
              effectiveThreshold: int) -> list<RankedCandidatePosting>
```

Implements the hybrid ranking of `US-3.3.2-01`: `hybridScore = 0.4 × collaborativeScore + 0.4 × contentScore + 0.2 × matchScore`. Drops anything below `effectiveThreshold`; drops postings under active `NotInterested` suppression; respects location/salary/work-arrangement preferences (`US-3.3.2-03`) — non-matching postings deprioritised or excluded; for cold-start seekers (< 5 interactions) uses content score only. Returns 5–10 ranked items for the feed.

### 7.3 `CandidateRankingService`

```
RankForPosting(posting: PostingMatchProfile, candidateMatches: list<MatchScore>,
               qualificationThreshold: QualificationThreshold,
               directApplicantSeekerIds: list<uuid>) -> list<RankedSeekerForPosting>
```

Produces the ranked shortlist for `US-3.3.1-05` / `US-3.3.3-01`. Sorts by `OverallScore` desc, ties broken by `LastProfileUpdateUtc`. Applies the recruiter's `QualificationThreshold` (overall %, skill %, education level, experience years, certifications) — but candidates in `directApplicantSeekerIds` are always included regardless of threshold. Truncates to `configuredSize`.

### 7.4 `FitAnalysisService`

```
Analyze(seeker: SeekerMatchProfile, posting: PostingMatchProfile, score: MatchScore) -> FitAnalysis
```

Builds the `FitAnalysis` VO for `US-3.3.3-05`: strengths (factors > 80), gaps (factors < 60), `SalaryFitIndicator` from expectation/range overlap, `MotivationScore = 0.4×completeness + 0.4×recentActivity + 0.2×applicationsSent`, `TimeToProductivity` heuristic (high match + senior = `OneWeek`; medium + mid = `TwoToThreeWeeks`; else `FourPlusWeeks`), `ContactLikelihood` from `JobSearchStatus` + `LastProfileUpdateUtc`, work-arrangement compatibility.

### 7.5 `MatchThresholdResolver`

```
Resolve(config: MatchThresholdConfiguration, posting: PostingMatchProfile?) -> int
```

Returns the **effective** threshold for a posting: the posting's `PerPostingThresholdOverride` if set, else the global `GlobalThresholdPercent`. Used everywhere a match must be surfaced-or-hidden.

### 7.6 `CandidatePrivacyFilter`

```
IsVisibleToEmployer(seeker: SeekerMatchProfile, jobPostingId: uuid,
                    seekerAppliedToThisPosting: bool) -> bool
FilterShortlistEntries(entries: list<T>, ...) -> list<T>   // for US-3.3.3-04
```

Enforces `US-3.3.3-04`: `Public` → always visible; `ApplyOnly` → visible only in posting-specific shortlists where the seeker applied, never in candidate *search*; `Hidden` → visible only if `seekerAppliedToThisPosting`. Every exposure decision must be recorded in `recommendation_exposure_log` (§11) for compliance.

### 7.7 `AbVariantAllocator`

```
AssignVariant(subjectId: uuid, activeVariants: list<MatchingWeightProfile>) -> string
```

Stratified-random assignment of a new seeker/posting to a weight variant per `US-3.3.1-08`: deterministic hash of `subjectId` bucketed by each variant's `VariantAllocationPercent` (50/50 control/variant by default). Deterministic so the same subject always lands in the same variant.

### 7.8 `ImpactPreviewCalculator`

```
Preview(proposedThreshold: int, sampleScores: list<int>) -> ThresholdImpactPreview
```

For `US-3.3.1-06 AC-06`: given a statistical sample of current match scores, estimates the percentage of matches that would be filtered if the threshold changed (e.g. "changing to 70% would hide 15% of current matches"). Sampling-based — never re-filters all matches.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module. **Integration events** (`IntegrationEvent`) cross the BC boundary — they go through the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract exactly. Other modules depend on these payloads — do not change a field without versioning.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

| Integration event | Raised when | Payload |
|---|---|---|
| `MatchComputedIntegrationEvent` | `MatchScore.Compute` / `Recompute` | `JobSeekerId`, `JobPostingId`, `Score` (`int` 0–100), `WeightProfileVersion`, `OccurredOnUtc` |
| `RecommendationGeneratedIntegrationEvent` | `JobRecommendationSet.Generate` | `JobSeekerId`, `JobPostingIds` (`list<uuid>`, ranked), `ComputedOnUtc`, `OccurredOnUtc` |
| `CandidateRecommendationGeneratedIntegrationEvent` | `CandidateShortlist.Generate` / `Refresh` | `EmployerId`, `JobPostingId`, `JobSeekerIds` (`list<uuid>`, ranked), `OccurredOnUtc` |
| `EmbeddingsRefreshedIntegrationEvent` | nightly batch completes | `Scope` (`string` — `"nightly"` / `"incremental"`), `VectorCount` (`int`), `Version` (`string`), `OccurredOnUtc` |
| `MatchThresholdChangedIntegrationEvent` | threshold updated **or** weight profile activated/rolled back | `Scope` (`string` — `"global"`, a `JobPostingId`, or `"weights"`), `OldValue` (`string`), `NewValue` (`string`), `ChangedBy` (`uuid`), `OccurredOnUtc` |

> **Catalog note.** [[Event_Catalog]] lists `MatchThresholdChanged` for "threshold/parameters reconfigured" — we reuse this single event for both the match-threshold change *and* the weight-profile change, since both are "matching configuration changed" facts BC-10 audits. `OldValue`/`NewValue` are stringified (e.g. `"60"`→`"65"`, or a serialized `FactorWeights`). Defensible to split into two events — discuss in lecture.

Consumers (for context only — you do not code them): BC-2 Employer Profile consumes `MatchComputed` (employer view) and `CandidateRecommendationGenerated`; BC-3 JobSeeker Profile consumes `MatchComputed` (seeker view); BC-9 Notification consumes `RecommendationGenerated` (weekly digest) and `CandidateRecommendationGenerated` (notify employer); BC-10 Reporting consumes all five.

### 8.2 Internal domain events (NOT published outside the module)

`SeekerMatchInputChanged`, `SeekerPrivacyChanged`, `PostingMatchInputChanged`, `TalentPoolCreated`, `CandidateRemovedFromTalentPool`, `RecommendationFeedbackRecorded`. Use these for in-module reactions — e.g. `SeekerMatchInputChanged` → enqueue the seeker into `PendingRecompute` and mark its `MatchScore`s stale; `SeekerPrivacyChanged` → re-evaluate the seeker's presence in active `CandidateShortlist`s. They never reach the outbox.

> Note: `CandidateSavedToTalentPool` is owned by **BC-2** in the [[Event_Catalog]] (`employerId, jobSeekerId, poolId`). This BC raises a *domain* event `CandidateSavedToTalentPool` internally; if the platform wants the cross-BC `CandidateSavedToTalentPool` integration event emitted from here instead of BC-2, add it to `Contracts` with that exact payload. For this package, treat talent-pool membership as owned here and emit it as an internal event only — BC-2's catalog entry is the authoritative cross-BC contract and a teaching point on event ownership.

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained.

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent handlers)

| Integration event | From | Payload you receive | Your reaction |
|---|---|---|---|
| `UserRegisteredIntegrationEvent` (role = jobseeker) | BC-1 IAM/UAM | `UserId`, `Role`, `Email`, `CreatedAt` | `SeekerMatchProfile.CreateFromRegistration(UserId)` — empty projection. Idempotent. |
| `JobSeekerRegisteredIntegrationEvent` | BC-3 JobSeeker Profile | `JobSeekerProfileId`, `UserId`, `OccurredOnUtc` | Ensure a `SeekerMatchProfile` exists for `UserId` (idempotent with the BC-1 event). |
| `ProfileLevel2CompletedIntegrationEvent` | BC-3 | `JobSeekerProfileId`, `CompletenessPercentage`, `OccurredOnUtc` | `SeekerMatchProfile.ApplyLevel2Completed(...)`; enqueue for embedding recompute. *(payload also implies skills/education — fetch via the `SeekerProfileApi` port if not in event)* |
| `ResumeParsedIntegrationEvent` | BC-3 | `JobSeekerProfileId`, `ResumeId`, `Skills` (`[{taxonomyCode,label,confidence}]`), `Education` (`[{degree,institution,start,end}]`), `Experience` (`[{role,company,start,end}]`), `OccurredOnUtc` | `SeekerMatchProfile.ApplyResumeParsed(...)`; mark match scores stale; enqueue recompute. **This is the ResumeIntakeSaga hook.** |
| `ProfileSkillsUpdatedIntegrationEvent` | BC-3 | `JobSeekerProfileId`, `AddedSkills` (`list<string>`), `RemovedSkills` (`list<string>`), `OccurredOnUtc` | `SeekerMatchProfile.ApplySkillsUpdated(...)`; enqueue recompute. |
| `ProfileVisibilityChangedIntegrationEvent` | BC-3 | `JobSeekerProfileId`, `Visibility` (`string` — `Private`/`RecruitersOnly`/`Public`) | Map to `PrivacyLevel` (`Private`→`Hidden`, `RecruitersOnly`→`ApplyOnly`, `Public`→`Public`); `SeekerMatchProfile.ApplyVisibilityChanged(...)`; re-evaluate active shortlists. |
| `ProfileCompletenessChangedIntegrationEvent` | BC-3 | `JobSeekerProfileId`, `Score` (`int`), `OccurredOnUtc` | Update projected `ProfileCompleteness` (feeds motivation score). |
| `JobPostingPublishedIntegrationEvent` | BC-4 Job Postings | `PostingId`, `EmployerId`, `Title`, `Requirements` (description/skills/education/experience/salary/location/workArrangements), `OccurredOnUtc` | `PostingMatchProfile.CreateFromPublished(...)`; enqueue NLP extraction + embedding; trigger initial `CandidateShortlist` generation. |
| `JobPostingUpdatedIntegrationEvent` | BC-4 | `PostingId`, `ChangedFields`, `OccurredOnUtc` | `PostingMatchProfile.ApplyPostingUpdated(...)`; mark affected match scores stale; re-queue NLP if description changed. |
| `JobPostingExpiredIntegrationEvent` / `JobPostingClosedIntegrationEvent` / `JobPostingSuspendedIntegrationEvent` | BC-4 | `PostingId`, (`Reason`/`ExpiredAt`/etc.) | `PostingMatchProfile.Deactivate(reason)`; stop refreshing its shortlist. |
| `JobBookmarkedIntegrationEvent` | BC-5 Job Application | `JobSeekerId`, `PostingId`, `OccurredOnUtc` | Record as a positive preference signal; `RecommendationFeedback.Record(Viewed-equivalent)` / store as collaborative-filtering interaction. |
| `ApplicationSubmittedIntegrationEvent` | BC-5 | `ApplicationId`, `JobSeekerId`, `PostingId`, `Snapshot`, `OccurredOnUtc` | Record as a strong preference signal; `RecommendationFeedback.Record(Applied)`; mark this seeker as a *direct applicant* for the posting (privacy override for shortlists). |
| `EmployerProfileUpdatedIntegrationEvent` | BC-2 Employer Profile | `EmployerId`, `ChangedFields` (may include qualification-threshold preferences), `OccurredOnUtc` | If qualification thresholds changed, re-rank that employer's affected shortlists. |
| `TaxonomyUpdatedIntegrationEvent` | BC-11 Admin Config | `TaxonomyId`, `Version`, `ChangeSummary` | Flag affected seeker/posting embeddings for rebuild; enqueue into `PendingRecompute`. |
| `TaxonomyTermDeprecatedIntegrationEvent` | BC-11 | `TaxonomyId`, `TermId`, `ReplacedBy`, `OccurredOnUtc` | Remap deprecated skill codes in projections; enqueue affected entities for recompute. |
| `AccountDeactivatedIntegrationEvent` / `UserAccountSuspendedIntegrationEvent` | BC-1 | `UserId`, (`DeactivatedAt`/`Reason`) | If a seeker: `SeekerMatchProfile.Deactivate()` — drop from all matching. If an employer: deactivate their `PostingMatchProfile`s' shortlists. |
| `UserAccountReinstatedIntegrationEvent` | BC-1 | `UserId` | `SeekerMatchProfile.Reactivate()`. |

**Idempotency:** every consumer handler must be safe to run twice — dedupe on `EventId` via the `inbox_messages` table ([[00-Shared-Foundations]] §6.3), or make the operation naturally idempotent. Inbound events arrive via the module's integration-event subscription wired in the module composition entry point.

### 9.2 Ports this module CALLS (port interfaces — define in `Application`, implement adapters in `Infrastructure`)

The ML boundary. This module orchestrates; these ports do the actual ML.

```
Port: EmbeddingModelPort        (turns text / a profile into a dense vector — external transformer service)
  EmbedSeeker(input: SeekerEmbeddingInput)  -> Result<EmbeddingVector>
      // embeds a job seeker's matchable text (skills, experience, education) into a vector
  EmbedPosting(input: PostingEmbeddingInput) -> Result<EmbeddingVector>
      // embeds a posting's matchable text (description, requirements) into a vector
  EmbedBatch(ownerType: EmbeddingOwnerType, items: list<EmbeddingBatchItem>)
      -> Result<list<{ OwnerId: uuid, Vector: EmbeddingVector }>>
      // batch variant for the nightly job — must handle 100K+ inputs efficiently
  ModelVersion: string          // version tag stamped onto EmbeddingRecord

  SeekerEmbeddingInput  { JobSeekerId: uuid, SkillTexts: list<string>,
                          ExperienceText: string, EducationText: string }
  PostingEmbeddingInput { JobPostingId: uuid, Description: string,
                          RequiredSkillTexts: list<string> }
  EmbeddingBatchItem    { OwnerId: uuid, Text: string }

Port: VectorIndexPort           (stores vectors and answers nearest-neighbour / similarity queries)
  Upsert(ownerType: EmbeddingOwnerType, ownerId: uuid, vector: EmbeddingVector, version: string) -> void
  Similarity(aType: EmbeddingOwnerType, aId: uuid,
             bType: EmbeddingOwnerType, bId: uuid) -> Result<decimal>
      // cosine similarity between two specific owners' current vectors
  Nearest(queryType: EmbeddingOwnerType, queryId: uuid,
          resultType: EmbeddingOwnerType, k: int) -> Result<list<{ OwnerId: uuid, Similarity: decimal }>>
      // top-K nearest postings for a seeker (or seekers for a posting) — drives candidate generation
  SkillSimilarity(skillsA: list<string>, skillsB: list<string>) -> Result<SemanticSimilarity>
      // pairwise skill-to-skill cosine similarities — feeds MatchScoringService skill factor
  Delete(ownerType: EmbeddingOwnerType, ownerId: uuid) -> void

  SemanticSimilarity { Pairs: map<(string, string), decimal> }
      // EquivalenceThreshold = 0.75 — cosine > 0.75 ⇒ semantically equivalent

Port: NlpExtractionPort         (pulls structured attributes out of free-text job descriptions / resumes)
  ExtractFromJobDescription(jobPostingId: uuid, description: string, languageHint: string)
      -> Result<NlpExtractionResult>
      // extracts required skills (+inferred proficiency), experience level, job categories,
      // responsibilities, each with a confidence score. Arabic + English supported.
  ModelVersion: string

  NlpExtractionResult { RequiredSkills: list<SkillRequirement>, ExperienceLevel: ExperienceLevel,
                        JobCategories: list<string>, Responsibilities: list<string>,
                        RequiredCertifications: list<string> }

Port: CollaborativeFilteringPort  (the "similar users did X" signal source — may be the vector index
                                   or a separate model service; abstracted as a port either way)
  GetCollaborativeSignals(jobSeekerId: uuid, topSimilarUsers: int) -> Result<list<CollaborativeSignal>>
      // postings interacted with by the top-N seekers most similar to this seeker, with a score each
  CollaborativeSignal { JobPostingId: uuid, Score: int }   // Score 0–100

Port: GeoDistancePort           (haversine; could be in-process, kept as a port for testability)
  DistanceKm(a: GeoLocation, b: GeoLocation) -> decimal
```

### 9.3 Public APIs this module CALLS on neighbouring BCs (port interfaces)

```
Port: SeekerProfileApi          (provided by BC-3 JobSeeker Profile; used to backfill projection
                                 fields not carried in an event payload, and for live drill-down)
  GetSnapshot(jobSeekerId: uuid) -> Result<SeekerProfileSnapshotDto>
  SeekerProfileSnapshotDto { JobSeekerProfileId: uuid, UserId: uuid, FullName: string, Status: string,
    Skills: list<SkillDto>, EducationLevel: string, TotalExperienceYears: decimal,
    Location: GeoLocationDto?, SalaryExpectation: SalaryRangeDto?,
    WorkArrangementPrefs: list<string>, Visibility: string, CompletenessPercentage: int }
  SkillDto       { TaxonomyCode: string, Label: string, Proficiency: int, Confidence: int }
  GeoLocationDto { Latitude: decimal, Longitude: decimal, City: string }
  SalaryRangeDto { Min: decimal, Max: decimal, Currency: string }

Port: JobPostingApi             (provided by BC-4 Job Postings; used to backfill posting projection fields)
  GetSnapshot(jobPostingId: uuid) -> Result<JobPostingSnapshotDto>
  JobPostingSnapshotDto { JobPostingId: uuid, EmployerId: uuid, Title: string, Description: string,
    Status: string, RequiredSkills: list<SkillDto>, RequiredEducationLevel: string,
    RequiredExperienceYears: decimal, RequiredCertifications: list<string>,
    Location: GeoLocationDto?, SalaryRange: SalaryRangeDto?,
    WorkArrangements: list<string>, PerPostingThresholdOverride: int? }

Port: EmployerAccessApi         (provided by BC-2 Employer Profile / BC-1 IAM; recruiter access
                                 checks for talent pools & shortlist views)
  CanRecruiterAccessPosting(recruiterId: uuid, jobPostingId: uuid) -> bool
      // is this recruiter on the employer's team with the right role for this posting?
  GetRecruiterTeam(employerId: uuid) -> Result<list<uuid>>
      // all recruiter ids on the employer's team — for shared-talent-pool visibility
```

For the exercise, `Infrastructure` may provide **stub adapters** for every port in §9.2 and §9.3 (in-memory / fake — e.g. `EmbeddingModelPort` returns deterministic pseudo-random vectors seeded by id, `VectorIndexPort` does brute-force cosine in memory, `NlpExtractionPort` does keyword extraction). Keep the port shapes exactly as above so real adapters drop in later.

### 9.4 Public API this module EXPOSES (in the `Contracts` surface)

```
Public API: MatchRankingPublicApi
  GetMatchScoresForSeeker(jobSeekerId: uuid, jobPostingIds: list<uuid>)
      -> Result<list<PostingMatchScoreDto>>
      // used by BC-6 Search & Discovery to blend match relevance into keyword search results
  GetMatchScore(jobSeekerId: uuid, jobPostingId: uuid) -> Result<int?>
      // used by BC-5 Job Application to record match-score-at-apply (informational snapshot)

  PostingMatchScoreDto { JobPostingId: uuid, OverallScore: int, AboveThreshold: bool }
```

---

## 10. Application layer

Every use case is a **command** or a **query** with exactly one handler. Commands return `Result`/`Result<T>`; queries return DTOs. Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

Three **scheduled background workers** also live here (driven from `Infrastructure` but orchestrating `Application` use cases): the nightly embedding batch, the daily shortlist refresh, and the outbox relay.

### 10.1 Commands

| Command | Story | Handler responsibilities |
|---|---|---|
| `ComputeMatchScoreCommand` | US-3.3.1-01 | Load `SeekerMatchProfile` + `PostingMatchProfile` + active `MatchingWeightProfile`; call `VectorIndexPort.SkillSimilarity`; `MatchScoringService.Score(...)`; upsert `MatchScore`; persist. Raises `MatchComputed`. |
| `RecomputeStaleMatchesCommand` | US-3.3.1-01 | Batch: for each `MatchScore` with `IsStale`, reload inputs and `Recompute`. Used by background recompute. |
| `ExtractPostingAttributesCommand` | US-3.3.1-02 | Load `PostingMatchProfile`; call `NlpExtractionPort.ExtractFromJobDescription`; on success `ApplyNlpExtraction`, on failure `RecordNlpExtractionFailure`; persist. |
| `GenerateCandidateShortlistCommand` | US-3.3.1-05 / US-3.3.3-01 | Load posting + candidate `MatchScore`s + direct applicants; `CandidateRankingService.RankForPosting`; apply `CandidatePrivacyFilter`; `CandidateShortlist.Generate`; persist. Raises `CandidateRecommendationGenerated`. |
| `RefreshCandidateShortlistCommand` | US-3.3.1-05 AC-02 | Recompute an active posting's shortlist (daily batch or recruiter "refresh" button). |
| `GenerateJobRecommendationsCommand` | US-3.3.2-01 / US-3.3.2-03 / US-3.3.1-07 | Load seeker; `VectorIndexPort.Nearest` for candidate postings; `CollaborativeFilteringPort.GetCollaborativeSignals`; load feedback; `RecommendationRankingService.RankForSeeker`; `JobRecommendationSet.Generate`; persist. Raises `RecommendationGenerated`. |
| `RecordRecommendationFeedbackCommand` | US-3.3.2-01 AC-06 | `RecommendationFeedback.Record/Refresh(signal)`; persist. |
| `UpdateMatchThresholdCommand` | US-3.3.1-06 | Load singleton config; `UpdateGlobalThreshold(percent, adminId)` — validates 0–100; persist. Raises `MatchThresholdChanged`. |
| `SetPerPostingThresholdCommand` | US-3.3.1-06 AC-04 | Load `PostingMatchProfile`; `SetPerPostingThresholdOverride`; load config; `RecordPerPostingOverride` for audit; persist. |
| `CreateWeightVariantCommand` | US-3.3.1-08 | `MatchingWeightProfile.CreateVariant(...)` — validates sum 1.0; persist. |
| `UpdateMatchingWeightsCommand` | US-3.3.1-08 | Load active profile; `UpdateWeights(...)`; persist. Raises `MatchThresholdChanged` (scope `"weights"`). |
| `ActivateWeightProfileCommand` | US-3.3.1-08 | `Activate()` on a profile; supersedes the prior active in that variant slot; persist. |
| `RollbackWeightProfileCommand` | US-3.3.1-08 AC-06 | `RollbackTo(targetVersion)` — must be one of last 10; audit-logged; persist. |
| `SetQualificationThresholdCommand` | US-3.3.3-02 | Verify recruiter access via the `EmployerAccessApi` port; load `CandidateShortlist`; `SetQualificationThreshold`; persist (no recompute — filter at retrieval). |
| `SetShortlistSizeCommand` | US-3.3.1-05 AC-04 | `SetConfiguredSize(n)`; persist. |
| `CreateTalentPoolCommand` | US-3.3.3-06 | Verify recruiter access; check ≤ 20 active pools per employer (`E-POOL-LIMIT-EXCEEDED`); `TalentPool.Create`; persist. |
| `AddCandidateToTalentPoolCommand` | US-3.3.3-06 | Verify access; load pool; `AddCandidate(...)`; persist. Raises `CandidateSavedToTalentPool` (internal). |
| `RemoveCandidateFromTalentPoolCommand` | US-3.3.3-06 | Load pool; `RemoveCandidate` (soft); persist. |
| `UpdateTalentPoolCandidateNoteCommand` | US-3.3.3-06 | Load pool; `UpdateCandidateNote`; persist. |
| `UpdateTalentPoolCommand` | US-3.3.3-06 | `Rename`/`UpdateDescription`/`SetAssociatedSkills`/`SetShared`; persist. |
| `RunNightlyEmbeddingBatchCommand` | US-3.3.2-02 | Drain `PendingRecompute`; call `EmbeddingModelPort.EmbedBatch` for seekers and postings; `VectorIndexPort.Upsert`; create new `EmbeddingRecord`s, mark prior `IsCurrent = false`; `MarkEmbeddingRefreshed` on each profile; emit batch report. Raises `EmbeddingsRefreshed`. Alerts if compute time > 30 min or error rate > 5%. |

### 10.2 Queries

| Query | Story | Returns |
|---|---|---|
| `GetJobRecommendationsQuery` | US-3.3.2-01 / US-3.3.1-07 | `JobRecommendationFeedDto` — 5–10 ranked items with match %, reason, breakdown; honours the seeker's "show low matches" toggle |
| `GetMatchDetailsQuery` | US-3.3.1-07 AC-03 | `MatchBreakdownDto` — per-factor scores for one seeker-posting pair |
| `GetCandidateShortlistQuery` | US-3.3.1-05 / US-3.3.3-01 | `CandidateShortlistDto` — ranked entries with match %, breakdown, strengths/gaps, privacy-filtered; supports the recruiter's qualification-threshold filter + "show all candidates" bypass; banner counts ("42 of 315") |
| `GetCandidateFitAnalysisQuery` | US-3.3.3-05 | `FitAnalysisDto` — availability, salary fit, motivation, time-to-productivity, contact likelihood, work-arrangement compat |
| `SearchCandidatesQuery` | US-3.3.3-03 | `CandidateSearchResultDto` — faceted query over `SeekerMatchProfile` read model (skills, experience, location/radius, salary, education, certifications); **privacy-filtered** (no `Hidden`/`ApplyOnly` unless applied); sortable |
| `GetMatchThresholdConfigQuery` | US-3.3.1-06 AC-01 | `ThresholdConfigDto` — global threshold + per-posting overrides |
| `PreviewThresholdImpactQuery` | US-3.3.1-06 AC-06 | `ThresholdImpactPreviewDto` — estimated % of matches filtered at a proposed threshold |
| `GetMatchingWeightsQuery` | US-3.3.1-08 AC-01 | `WeightProfileDto` — current factor weights + active variant + version |
| `GetWeightProfileHistoryQuery` | US-3.3.1-08 AC-06 | `list<WeightProfileVersionDto>` — last 10 versions for rollback |
| `GetTalentPoolsQuery` | US-3.3.3-06 AC-03 | `list<TalentPoolSummaryDto>` — pools the recruiter owns or that are shared with their team, with candidate counts |
| `GetTalentPoolQuery` | US-3.3.3-06 AC-04 | `TalentPoolDetailDto` — pool + active candidates with notes and refreshed availability status |

### 10.3 Validators — representative rules

- `UpdateMatchThresholdCommandValidator`: `Percent` ∈ [0,100] (`E-THRESHOLD-OUT-OF-RANGE`).
- `SetPerPostingThresholdCommandValidator`: `Percent` ∈ [0,100] or null.
- `CreateWeightVariantCommandValidator` / `UpdateMatchingWeightsCommandValidator`: all six weights ∈ [0,1]; sum == 1.0 ± 0.01 (`E-WEIGHTS-INVALID-SUM`); `AllocationPercent` ∈ [0,100].
- `RollbackWeightProfileCommandValidator`: `TargetVersion` non-empty (handler enforces "one of last 10" — `E-WEIGHTS-ROLLBACK-UNKNOWN`).
- `SetQualificationThresholdCommandValidator`: percentages 0–100; `MinExperienceYears` ≥ 0; education level in enum.
- `CreateTalentPoolCommandValidator`: `Name` non-empty ≤ 120; `Description` ≤ 1000.
- `AddCandidateToTalentPoolCommandValidator` / `UpdateTalentPoolCandidateNoteCommandValidator`: `Note` ≤ 2000.
- `SearchCandidatesQueryValidator`: `RadiusKm` > 0 when `Location` set; `SalaryRange.Min ≤ Max`.
- `SetShortlistSizeCommandValidator`: `Size` > 0.

### 10.4 DTOs

Plain records in `Application`. Never expose Domain entities/VOs across the API. Map aggregate → DTO in the handler or a mapping extension.

---

## 11. Persistence & data model

Schema/namespace: `recommendation_engine`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. No foreign key may cross into another module's schema — references to BC-1/BC-2/BC-3/BC-4 identities are plain `uuid` columns with **no** FK constraint. The module-specific relational model follows.

### 11.1 Reference relational model — schema `recommendation_engine`

```
TABLE seeker_match_profiles            -- projection from BC-3
  id                      uuid        PK
  job_seeker_id           uuid        NOT NULL UNIQUE         -- BC-3 identity, no FK
  skills                  json        NOT NULL DEFAULT '[]'   -- SkillRequirement[]
  education_level         enum        NOT NULL
  training_credentials    json        NOT NULL DEFAULT '[]'
  total_experience_years  decimal     NOT NULL DEFAULT 0
  location                json        NULL                    -- GeoLocation VO
  salary_expectation      json        NULL                    -- SalaryRange VO
  work_arrangement_prefs  json        NOT NULL DEFAULT '[]'
  preferred_locations     json        NOT NULL DEFAULT '[]'
  privacy_level           enum        NOT NULL DEFAULT 'ApplyOnly'
  job_search_status       enum        NOT NULL DEFAULT 'OpenToOpportunities'
  profile_completeness    int         NOT NULL DEFAULT 0
  is_active               bool        NOT NULL DEFAULT true
  last_profile_update_utc datetime    NOT NULL
  embedding_version       string      NULL
  INDEX (job_seeker_id), INDEX (privacy_level), INDEX (is_active), INDEX (education_level)

TABLE posting_match_profiles           -- projection from BC-4
  id                          uuid      PK
  job_posting_id              uuid      NOT NULL UNIQUE       -- BC-4 identity, no FK
  employer_id                 uuid      NOT NULL             -- BC-2 identity, no FK
  title                       string    NOT NULL
  required_skills             json      NOT NULL DEFAULT '[]'
  required_education_level    enum      NOT NULL
  required_experience_years   decimal   NOT NULL DEFAULT 0
  experience_level            enum      NOT NULL
  required_certifications     json      NOT NULL DEFAULT '[]'
  location                    json      NULL
  salary_range                json      NULL
  work_arrangements           json      NOT NULL DEFAULT '[]'
  status                      enum      NOT NULL DEFAULT 'Active'
  per_posting_threshold_override int    NULL
  nlp_extraction_status       enum      NOT NULL DEFAULT 'Pending'
  embedding_version           string    NULL
  published_on_utc            datetime  NOT NULL
  last_updated_utc            datetime  NOT NULL
  INDEX (job_posting_id), INDEX (employer_id), INDEX (status), INDEX (nlp_extraction_status)

TABLE embedding_records                -- local vector cache
  id              uuid      PK
  owner_type      enum      NOT NULL                 -- Seeker | Posting
  owner_id        uuid      NOT NULL
  vector          json      NOT NULL                 -- EmbeddingVector VO (values + dimension)
  version         string    NOT NULL
  computed_on_utc datetime  NOT NULL
  ttl_utc         datetime  NOT NULL
  is_current      bool      NOT NULL DEFAULT true
  INDEX (owner_type, owner_id) WHERE is_current = true, INDEX (version), INDEX (ttl_utc)

TABLE match_scores
  id                      uuid      PK
  job_seeker_id           uuid      NOT NULL
  job_posting_id          uuid      NOT NULL
  overall_score           int       NOT NULL                 -- 0..100
  breakdown               json      NOT NULL                 -- MatchBreakdown VO (6 FactorScores)
  weight_profile_version  string    NOT NULL
  weight_variant_id       string    NULL
  strengths               json      NOT NULL DEFAULT '[]'
  gaps                    json      NOT NULL DEFAULT '[]'
  is_stale                bool      NOT NULL DEFAULT false
  computed_on_utc         datetime  NOT NULL
  version_token           (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  UNIQUE (job_seeker_id, job_posting_id)
  INDEX (job_posting_id, overall_score DESC), INDEX (job_seeker_id, overall_score DESC),
  INDEX (is_stale) WHERE is_stale = true

TABLE job_recommendation_sets
  id                      uuid      PK
  job_seeker_id           uuid      NOT NULL
  computed_on_utc         datetime  NOT NULL
  weight_profile_version  string    NOT NULL
  is_cold_start           bool      NOT NULL DEFAULT false
  INDEX (job_seeker_id, computed_on_utc DESC)

TABLE recommendation_items
  id                  uuid  PK
  set_id              uuid  NOT NULL              -- FK → job_recommendation_sets.id ON DELETE CASCADE
  job_posting_id      uuid  NOT NULL
  rank                int   NOT NULL
  overall_score       int   NOT NULL
  collaborative_score int   NOT NULL
  content_score       int   NOT NULL
  hybrid_score        int   NOT NULL
  reason              json  NOT NULL              -- RecommendationReason VO
  INDEX (set_id, rank)

TABLE candidate_shortlists
  id                       uuid      PK
  job_posting_id           uuid      NOT NULL UNIQUE
  employer_id              uuid      NOT NULL
  configured_size          int       NOT NULL DEFAULT 100
  qualification_threshold  json      NOT NULL                 -- QualificationThreshold VO
  computed_on_utc          datetime  NOT NULL
  refresh_state            enum      NOT NULL DEFAULT 'Fresh'
  version_token            (optimistic-concurrency token)
  INDEX (employer_id)

TABLE shortlist_entries
  id                      uuid  PK
  shortlist_id            uuid  NOT NULL          -- FK → candidate_shortlists.id ON DELETE CASCADE
  job_seeker_id           uuid  NOT NULL
  rank                    int   NOT NULL
  overall_score           int   NOT NULL
  breakdown               json  NOT NULL
  strengths               json  NOT NULL DEFAULT '[]'
  gaps                    json  NOT NULL DEFAULT '[]'
  included_reason         enum  NOT NULL          -- MatchAboveThreshold | AppliedDirectly
  privacy_level_at_compute enum NOT NULL
  INDEX (shortlist_id, rank)

TABLE matching_weight_profiles
  id                          uuid      PK
  version                     string    NOT NULL UNIQUE
  weights                     json      NOT NULL               -- FactorWeights VO
  variant_id                  string    NOT NULL DEFAULT 'control'
  variant_allocation_percent  int       NOT NULL DEFAULT 100
  is_active                   bool      NOT NULL DEFAULT false
  created_by                  uuid      NOT NULL
  created_on_utc              datetime  NOT NULL
  superseded_by_version       string    NULL
  version_token               (optimistic-concurrency token)
  INDEX (variant_id) WHERE is_active = true, INDEX (created_on_utc DESC)

TABLE match_threshold_configuration   -- singleton
  id                      uuid      PK                         -- well-known fixed id
  global_threshold_percent int      NOT NULL DEFAULT 60
  updated_on_utc          datetime  NOT NULL
  version_token           (optimistic-concurrency token)

TABLE threshold_change_entries
  id              uuid      PK
  config_id       uuid      NOT NULL                            -- FK → match_threshold_configuration.id
  old_value       int       NOT NULL
  new_value       int       NOT NULL
  changed_by      uuid      NOT NULL
  changed_on_utc  datetime  NOT NULL
  scope           string    NOT NULL                            -- 'Global' | a job_posting_id
  INDEX (config_id, changed_on_utc DESC)

TABLE talent_pools
  id                  uuid      PK
  employer_id         uuid      NOT NULL
  owner_recruiter_id  uuid      NOT NULL
  name                string    NOT NULL
  description         string    NULL
  associated_skills   json      NOT NULL DEFAULT '[]'
  is_shared           bool      NOT NULL DEFAULT false
  created_on_utc      datetime  NOT NULL
  updated_on_utc      datetime  NOT NULL
  version_token       (optimistic-concurrency token)
  INDEX (employer_id), INDEX (owner_recruiter_id)

TABLE talent_pool_candidates
  id                    uuid      PK
  talent_pool_id        uuid      NOT NULL          -- FK → talent_pools.id ON DELETE CASCADE
  job_seeker_id         uuid      NOT NULL
  added_by_recruiter_id uuid      NOT NULL
  note                  string    NULL
  is_active             bool      NOT NULL DEFAULT true
  added_on_utc          datetime  NOT NULL
  UNIQUE (talent_pool_id, job_seeker_id)
  INDEX (talent_pool_id) WHERE is_active = true

TABLE recommendation_feedback
  id                  uuid      PK
  job_seeker_id       uuid      NOT NULL
  job_posting_id      uuid      NOT NULL
  signal              enum      NOT NULL                         -- NotInterested | Viewed | Applied
  recorded_on_utc     datetime  NOT NULL
  suppress_until_utc  datetime  NULL
  UNIQUE (job_seeker_id, job_posting_id)
  INDEX (job_seeker_id), INDEX (suppress_until_utc)

TABLE pending_recompute                 -- the recompute queue
  id              uuid      PK
  owner_type      enum      NOT NULL                              -- Seeker | Posting
  owner_id        uuid      NOT NULL
  reason          enum      NOT NULL                              -- InputChanged | TaxonomyUpdated | New
  queued_on_utc   datetime  NOT NULL
  UNIQUE (owner_type, owner_id)

TABLE recommendation_exposure_log       -- privacy audit (US-3.3.3-04)
  id              uuid      PK
  job_seeker_id   uuid      NOT NULL
  employer_id     uuid      NOT NULL
  job_posting_id  uuid      NULL
  privacy_level   enum      NOT NULL
  exposure_basis  enum      NOT NULL                              -- Public | AppliedDirectly
  exposed_on_utc  datetime  NOT NULL
  INDEX (job_seeker_id), INDEX (exposed_on_utc)

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

### 11.2 Module-specific mapping notes

- Child collections (`recommendation_items`, `shortlist_entries`, `threshold_change_entries`, `talent_pool_candidates`) are **owned** by their aggregate and loaded with it.
- Value objects map to `json` columns: `MatchBreakdown`, `FactorWeights`, `EmbeddingVector`, `GeoLocation`, `SalaryRange`, `QualificationThreshold`, `RecommendationReason`, skill lists. Scalars that need querying — `overall_score`, `privacy_level`, `status`, `is_active`, `is_current` — are flattened to columns.
- The embedding `vector` is stored as `json` (a list of values) in this module's own cache; the *authoritative* vector index is external via the `VectorIndexPort`. The `embedding_records` table is a local cache + audit trail, not the search structure.
- Optimistic-concurrency tokens are required on `match_scores`, `candidate_shortlists`, `matching_weight_profiles`, `match_threshold_configuration`, and `talent_pools` (all updatable concurrently).
- `match_threshold_configuration` is a singleton — seed it with `CreateDefault()` and a well-known fixed id in the initial migration. Seed one `matching_weight_profiles` row via `CreateInitial()`.
- General persistence conventions (one persistence context per module, outbox/inbox wiring, value-object and strongly-typed-id mapping) follow [[00-Shared-Foundations]] §3 and §6.

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

`SeekerMatchProfileRepository` (`GetByJobSeekerId`, `Add`, `Update`, `QueryForCandidateSearch`), `PostingMatchProfileRepository` (`GetByJobPostingId`, `GetActiveByEmployerId`, `Add`, `Update`), `EmbeddingRecordRepository` (`GetCurrent`, `Add`, `MarkSuperseded`, `PurgeExpired`), `MatchScoreRepository` (`Get`, `GetByPosting`, `GetBySeeker`, `GetStale`, `Add`, `Update`, `UpdateRange`), `JobRecommendationSetRepository` (`GetLatestForSeeker`, `Add`), `CandidateShortlistRepository` (`GetByPostingId`, `GetByEmployerId`, `Add`, `Update`), `MatchingWeightProfileRepository` (`GetActive`, `GetActiveVariants`, `GetByVersion`, `GetLastN`, `Add`, `Update`), `MatchThresholdConfigurationRepository` (`GetSingleton`, `Update`), `TalentPoolRepository` (`GetById`, `GetByEmployerId`, `GetByOwnerOrShared`, `CountActiveByEmployer`, `Add`, `Update`), `RecommendationFeedbackRepository` (`Get`, `GetActiveSuppressions`, `Add`, `Update`), `PendingRecomputeRepository` (`Enqueue`, `Drain`, `Remove`), `ExposureLogRepository` (`Add`), `UnitOfWork` (`SaveChanges`).

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. Route prefix `/api/recommendations`. All endpoints require a valid access token (issued by BC-1). Seeker endpoints take the seeker's `UserId` from the token; recruiter/employer endpoints additionally pass the recruiter's identity through the `EmployerAccessApi` port for posting-scope checks; admin/data-scientist endpoints require the corresponding role claim. Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

| Verb & route | Command/Query | Success | Notable failures |
|---|---|---|---|
| `GET /api/recommendations/jobs` | `GetJobRecommendationsQuery` | `200` + `JobRecommendationFeedDto` | `404` no seeker profile |
| `GET /api/recommendations/jobs/{postingId}/match` | `GetMatchDetailsQuery` | `200` + `MatchBreakdownDto` | `404` no match score |
| `POST /api/recommendations/jobs/{postingId}/feedback` | `RecordRecommendationFeedbackCommand` | `204` | `400` invalid signal |
| `GET /api/recommendations/postings/{postingId}/candidates` | `GetCandidateShortlistQuery` | `200` + `CandidateShortlistDto` | `403 E-APP-FORBIDDEN` no recruiter access, `404` |
| `POST /api/recommendations/postings/{postingId}/candidates/refresh` | `RefreshCandidateShortlistCommand` | `202` | `403`, `409` posting inactive |
| `GET /api/recommendations/postings/{postingId}/candidates/{seekerId}/fit` | `GetCandidateFitAnalysisQuery` | `200` + `FitAnalysisDto` | `403`, `404` |
| `PUT /api/recommendations/postings/{postingId}/qualification-threshold` | `SetQualificationThresholdCommand` | `200` | `400` out-of-range, `403` |
| `PUT /api/recommendations/postings/{postingId}/shortlist-size` | `SetShortlistSizeCommand` | `200` | `400`, `403` |
| `POST /api/recommendations/candidates/search` | `SearchCandidatesQuery` | `200` + `CandidateSearchResultDto` | `400` invalid criteria |
| `GET /api/recommendations/config/threshold` | `GetMatchThresholdConfigQuery` | `200` + `ThresholdConfigDto` | `403` not admin |
| `PUT /api/recommendations/config/threshold` | `UpdateMatchThresholdCommand` | `200` | `400 E-THRESHOLD-OUT-OF-RANGE`, `403` |
| `PUT /api/recommendations/config/threshold/postings/{postingId}` | `SetPerPostingThresholdCommand` | `200` | `400`, `403` |
| `POST /api/recommendations/config/threshold/preview` | `PreviewThresholdImpactQuery` | `200` + `ThresholdImpactPreviewDto` | `400`, `403` |
| `GET /api/recommendations/config/weights` | `GetMatchingWeightsQuery` | `200` + `WeightProfileDto` | `403` not data scientist |
| `GET /api/recommendations/config/weights/history` | `GetWeightProfileHistoryQuery` | `200` + versions | `403` |
| `POST /api/recommendations/config/weights/variants` | `CreateWeightVariantCommand` | `201` | `400 E-WEIGHTS-INVALID-SUM`, `403` |
| `PUT /api/recommendations/config/weights` | `UpdateMatchingWeightsCommand` | `200` | `400 E-WEIGHTS-INVALID-SUM`, `403` |
| `POST /api/recommendations/config/weights/{version}/activate` | `ActivateWeightProfileCommand` | `200` | `404`, `403` |
| `POST /api/recommendations/config/weights/rollback` | `RollbackWeightProfileCommand` | `200` | `400 E-WEIGHTS-ROLLBACK-UNKNOWN`, `403` |
| `GET /api/recommendations/talent-pools` | `GetTalentPoolsQuery` | `200` + summaries | |
| `POST /api/recommendations/talent-pools` | `CreateTalentPoolCommand` | `201` + `TalentPoolId` | `409 E-POOL-LIMIT-EXCEEDED`, `403` |
| `GET /api/recommendations/talent-pools/{poolId}` | `GetTalentPoolQuery` | `200` + `TalentPoolDetailDto` | `403`, `404` |
| `PUT /api/recommendations/talent-pools/{poolId}` | `UpdateTalentPoolCommand` | `200` | `400`, `403`, `404` |
| `POST /api/recommendations/talent-pools/{poolId}/candidates` | `AddCandidateToTalentPoolCommand` | `201` | `403`, `404`, `409` duplicate active member |
| `PUT /api/recommendations/talent-pools/{poolId}/candidates/{seekerId}/note` | `UpdateTalentPoolCandidateNoteCommand` | `200` | `400`, `403`, `404` |
| `DELETE /api/recommendations/talent-pools/{poolId}/candidates/{seekerId}` | `RemoveCandidateFromTalentPoolCommand` | `204` | `403`, `404` |

Internal/background-only commands (`ComputeMatchScoreCommand`, `RecomputeStaleMatchesCommand`, `ExtractPostingAttributesCommand`, `GenerateCandidateShortlistCommand`, `GenerateJobRecommendationsCommand`, `RunNightlyEmbeddingBatchCommand`) are **not** exposed on the public API — they are driven by integration-event handlers and scheduled background workers. Optionally expose a guarded `POST /api/recommendations/admin/recompute` for ops to trigger the nightly batch manually (`US-3.3.2-02` AC mentions a manual retry trigger).

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** every VO factory — valid input succeeds; each invalid case returns the right `Error`. Specifically: `FactorWeights` (sum ≠ 1.0 fails, weight > 1 fails, sum within 0.01 tolerance passes), `MatchBreakdown` (missing a factor fails, duplicate factor fails), `EmbeddingVector` (length ≠ dimension fails), `SkillRequirement` (proficiency 0/6 fail), `GeoLocation` (lat 91 fails), `SalaryRange` (min > max fails, bad currency fails), `QualificationThreshold` (percentage 101 fails), `ConfidenceScore` (`NeedsReview` boundary at 70).
- **MatchScore aggregate:** `Compute` produces `OverallScore` equal to the weighted sum of the breakdown under the supplied weights (table-driven across several weight profiles); `Strengths` = factors > 80, `Gaps` = factors < 60, derived correctly; `Recompute` under different weights changes the overall score; `MarkStale` is idempotent.
- **MatchingWeightProfile aggregate:** `CreateInitial` seeds the documented defaults and is active; `CreateVariant` with weights summing to 0.9 fails; `Activate` supersedes the prior active in the same variant slot; a second `"control"` cannot be created/activated alongside the first; `RollbackTo` an unknown/too-old version fails; `RollbackTo` one of the last 10 reactivates it.
- **MatchThresholdConfiguration aggregate:** `UpdateGlobalThreshold(101)` fails with `E-THRESHOLD-OUT-OF-RANGE`; a valid update appends exactly one `ThresholdChangeEntry` with correct old/new; `ChangeLog` is append-only.
- **TalentPool aggregate:** `AddCandidate` twice for the same seeker fails (duplicate active member); re-adding a soft-removed candidate reactivates the existing row, not a new one; `RemoveCandidate` only sets `IsActive = false`; `SetShared` never happens implicitly.
- **RecommendationFeedback aggregate:** `Record(NotInterested)` sets `SuppressUntilUtc = now + 14 days`; `Record(Applied)` sets no suppression.
- **CandidateShortlist aggregate:** `Generate` never includes a `Hidden` candidate unless its `IncludedReason == AppliedDirectly`; entries ranked by score desc with ties broken by `LastProfileUpdateUtc`; size never exceeds `ConfiguredSize`.
- **JobRecommendationSet aggregate:** every item's `OverallScore` ≥ the supplied effective threshold; suppressed postings excluded; cold-start sets carry only content-scored items; 5–10 items.
- **PostingMatchProfile / SeekerMatchProfile:** the projection state machines — `ApplyNlpExtraction` only from `Pending`/`Failed`; `Deactivate` from `Active`; input-changing applies raise `SeekerMatchInputChanged`/`PostingMatchInputChanged`.
- **Domain services:** `MatchScoringService` — table-driven cases proving each factor's contribution and that semantic skill matches (similarity > 0.75) count while < 0.75 do not; cold-start path scores on education + location only. `RecommendationRankingService` — the 0.4/0.4/0.2 hybrid weighting; threshold cut-off; suppression honoured. `CandidateRankingService` — qualification-threshold filtering; direct applicants always included. `CandidatePrivacyFilter` — the full `Public`/`ApplyOnly`/`Hidden` × applied/not-applied matrix. `MatchThresholdResolver` — per-posting override beats global. `AbVariantAllocator` — deterministic (same id → same variant), allocation buckets respected. `ImpactPreviewCalculator` — estimate matches the sample distribution.

### 13.2 Unit tests — Application (handlers, ports replaced with test doubles)

- `ComputeMatchScoreCommand`: happy path scores and persists a `MatchScore`, `MatchComputed` queued to outbox; when `VectorIndexPort.SkillSimilarity` fails, the handler returns the error and persists nothing.
- `GenerateCandidateShortlistCommand`: a `Hidden` candidate who did **not** apply is excluded; the same candidate after `ApplicationSubmitted` is included with `IncludedReason = AppliedDirectly`; an exposure-log row is written per exposed candidate.
- `GenerateJobRecommendationsCommand`: cold-start seeker (test double for `CollaborativeFilteringPort` returns empty) gets content-only items; a posting under `NotInterested` suppression is not in the set; items respect the effective threshold.
- `UpdateMatchThresholdCommand`: out-of-range → `E-THRESHOLD-OUT-OF-RANGE`, nothing persisted; valid → `MatchThresholdChanged` queued; **no existing `MatchScore` row is rewritten** (assert the score table is untouched).
- `UpdateMatchingWeightsCommand` / `CreateWeightVariantCommand`: weights not summing to 1.0 → `E-WEIGHTS-INVALID-SUM`.
- `RollbackWeightProfileCommand`: rollback to a version outside the last 10 → `E-WEIGHTS-ROLLBACK-UNKNOWN`; valid rollback reactivates the target and supersedes the current.
- `SetQualificationThresholdCommand` / talent-pool commands: `EmployerAccessApi.CanRecruiterAccessPosting` returns false → `403`-mapped error, nothing mutated.
- `CreateTalentPoolCommand`: 21st active pool for an employer → `E-POOL-LIMIT-EXCEEDED`.
- `RunNightlyEmbeddingBatchCommand`: drains only queued entities; calls `EmbedBatch`; marks prior `EmbeddingRecord`s `IsCurrent = false`; emits `EmbeddingsRefreshed` with the correct `VectorCount`; raises an alert when the test clock reports > 30 min.
- Integration-event handlers: `ResumeParsedIntegrationEvent` updates the `SeekerMatchProfile` and enqueues a `PendingRecompute` row; delivering it twice (same `EventId`) is a no-op the second time.
- Validation step: each validator rejects the documented bad inputs before the handler runs.

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip each aggregate including child collections and `json` VOs; `MatchScore` unique `(seeker, posting)` constraint enforced; optimistic-concurrency conflict is detected; `QueryForCandidateSearch` faceted query returns correct results.
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `recommendation_engine`; the `match_threshold_configuration` singleton and the initial `matching_weight_profiles` row are seeded.
- **Outbox:** computing a match writes both the `match_scores` row and the `MatchComputed` outbox message in one transaction; rolling back leaves neither.
- **Inbox / idempotency:** delivering `JobPostingPublishedIntegrationEvent` twice creates one `PostingMatchProfile` and is a no-op the second time.
- **Privacy end-to-end:** publish a posting, project a `Hidden` seeker, generate a shortlist → seeker absent; deliver `ApplicationSubmitted` for that seeker → regenerate → seeker present; an exposure-log row exists.
- **Threshold is query-time:** persist match scores at 55/65/75; set global threshold 60 → `GetCandidateShortlistQuery` returns 65/75 only; raise to 70 → returns 75 only, **and the stored `match_scores` rows are unchanged**.
- **API:** host-level tests for: seeker recommendation feed; recruiter shortlist with a qualification threshold + "show all" bypass and the "X of Y" banner count; talent-pool create → add candidate → soft-remove → re-add reactivates.
- **Consumed events:** `TaxonomyUpdatedIntegrationEvent` enqueues affected entities into `pending_recompute`; `AccountDeactivatedIntegrationEvent` for a seeker sets `is_active = false` and drops them from new shortlists.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "Generate a candidate shortlist when a job posting is published"

End-to-end, to pattern-match every other flow against:

1. **Inbound integration event.** BC-4 publishes `JobPostingPublishedIntegrationEvent { PostingId, EmployerId, Title, Requirements, OccurredOnUtc }`. The module's subscription receives it.
2. **Inbox dedupe.** The handler checks `inbox_messages` for `EventId`; if present, returns immediately (idempotent). Otherwise records it.
3. **Projection.** `PostingMatchProfileHandler` calls `PostingMatchProfile.CreateFromPublished(...)` — a new projection in `Status = Active`, `NlpExtractionStatus = Pending`. It also enqueues two `PendingRecompute` rows (the posting itself, for embedding) and dispatches an in-process command to extract NLP attributes.
4. **NLP extraction.** `ExtractPostingAttributesCommandHandler` calls `NlpExtractionPort.ExtractFromJobDescription(postingId, description, languageHint)` — the external model returns required skills (with confidence), experience level, categories, certifications. On success the handler calls `PostingMatchProfile.ApplyNlpExtraction(...)`; on failure `RecordNlpExtractionFailure(...)` (matching falls back to keyword skills, non-blocking).
5. **Embedding.** The next nightly batch (or, for a freshly published posting, an immediate enqueue-driven run) calls `EmbeddingModelPort.EmbedPosting` and `VectorIndexPort.Upsert`, creates an `EmbeddingRecord`, marks any prior one `IsCurrent = false`, and calls `posting.MarkEmbeddingRefreshed(version)`.
6. **Candidate generation.** `GenerateCandidateShortlistCommandHandler`:
   a. Loads the `PostingMatchProfile`, the active `MatchingWeightProfile`, and the `MatchThresholdConfiguration` singleton.
   b. `VectorIndexPort.Nearest(Posting, postingId, Seeker, k)` returns the K nearest seekers.
   c. For each candidate seeker without a fresh `MatchScore`, calls `VectorIndexPort.SkillSimilarity` and `MatchScoringService.Score(...)`, persisting the `MatchScore` (each raises `MatchComputed`).
   d. Resolves the effective threshold via `MatchThresholdResolver`; loads direct applicants (seekers who submitted `ApplicationSubmitted` for this posting).
   e. `CandidateRankingService.RankForPosting(...)` ranks and applies the `QualificationThreshold`.
   f. `CandidatePrivacyFilter` removes `Hidden`/`ApplyOnly` candidates who did not apply; each surviving exposure is written to `recommendation_exposure_log`.
   g. `CandidateShortlist.Generate(...)` builds the aggregate; `repository.Add`; `unitOfWork.SaveChangesAsync()`.
7. **Domain-event / outbox behavior.** After `SaveChanges`, the pipeline behavior dispatches internal domain events in-process and writes `CandidateRecommendationGeneratedIntegrationEvent` (and the `MatchComputedIntegrationEvent`s) into `outbox_messages` — same transaction.
8. **Relay.** The background outbox relay publishes the integration events; BC-2 (employer dashboard), BC-9 (notify employer), BC-10 (reporting) receive them.
9. **Retrieval.** Later, the recruiter calls `GET /api/recommendations/postings/{postingId}/candidates`. `GetCandidateShortlistQuery` checks recruiter access via the `EmployerAccessApi` port, loads the shortlist, applies the recruiter's qualification-threshold filter (and "show all" bypass if requested), and returns the ranked `CandidateShortlistDto` with the "X of Y" banner count.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.3.1-01 AI matching algorithm | Multi-factor weighted `MatchScore` (skill, education, training, location, experience, salary); weights configurable and respected on next compute; bi-directional output (seeker→jobs, posting→candidates); match threshold applied at display time only; cold-start fallback for new seekers/postings. |
| US-3.3.1-02 NLP semantic analysis | Job-description extraction → structured skills (inferred proficiency), experience level, categories, responsibilities; semantic equivalence via cosine > 0.75; each attribute carries a confidence score, < 70 flagged; Arabic + English supported (the `NlpExtractionPort` languageHint). |
| US-3.3.1-05 Shortlist top candidates | Ranked top-N (default 100, configurable) per published posting, sorted by match % desc; daily/on-demand refresh; retrievable by employer with scores + key qualifications; privacy-respecting; computation scoped to active, non-duplicate profiles. |
| US-3.3.1-06 Match threshold config | View global threshold + per-posting overrides; update takes effect immediately on future queries; range validated 0–100 (`E-THRESHOLD-OUT-OF-RANGE`); per-posting override beats global; every change audit-logged with old/new/by/at; impact preview via sampling. |
| US-3.3.1-07 Rank jobs by match % | Seeker job list ordered by match % desc; match % shown per listing; per-factor breakdown on demand; below-threshold jobs hidden by default with a user-toggle to show; scores stable within a session. |
| US-3.3.1-08 Configure matching parameters | View/edit the six factor weights; validated sum to 1.0 (`E-WEIGHTS-INVALID-SUM`); persisted across restart; A/B weight variants with stratified allocation; rollback to any of the last 10 versions; changes audit-logged. |
| US-3.3.2-01 Personalized job recommendations | 5–10 ranked recommendations per seeker; hybrid collaborative (0.4) + content (0.4) + match (0.2) ranking; preferences respected; daily refresh; declined jobs suppressed 14 days; feedback (`NotInterested`/`Applied`/`Viewed`) recorded and used; cold-start = content-only. |
| US-3.3.2-02 Nightly embeddings | Batch processes all active seekers + postings; embeddings cached with version + timestamp + TTL; only queued (changed/new) entities recomputed incrementally; batch report emitted; alert on compute > 30 min or error rate > 5%; prior embeddings retained on failure. |
| US-3.3.2-03 Preference-aware recommendations | Recommendations retrieve and respect location (haversine, default 50 km radius), salary range (within range boosted, ≤10% above soft-included, far above excluded), and work arrangement; preference changes invalidate cached recommendations; unset preferences inferred from profile/behaviour. |
| US-3.3.3-01 Ranked candidate recommendations | Ranked candidate list per posting with overall match % + per-factor breakdown; strengths (> 80) and gaps (< 60) auto-derived; privacy-respecting display; candidate profile drill-down; batch-action support on visible candidates. |
| US-3.3.3-02 Qualification thresholds | Recruiter sets min overall %, min skill %, education level, experience years, certifications on a posting; candidates filtered at retrieval (no recompute); thresholds persisted and editable; "X of Y" banner + "show all candidates" bypass. |
| US-3.3.3-03 Candidate database search | Faceted recruiter search over the candidate read model (skills with taxonomy autocomplete, experience, location/radius, salary, education, certifications); results sortable; filters persist across pagination; privacy-respecting (no `Hidden`/`ApplyOnly` unless applied). |
| US-3.3.3-04 Respect candidate privacy | `Hidden` excluded from all recommendations/search unless applied to the posting; `ApplyOnly` excluded from search and general recs, shown only in posting-specific shortlists if applied; `Public` visible; privacy change re-evaluated on next batch; every exposure logged for audit. |
| US-3.3.3-05 Candidate insights & fit analysis | `FitAnalysis` per candidate-posting: availability, salary expectation + `SalaryFitIndicator`, fit summary (match %, strengths, gaps, time-to-productivity), motivation/engagement signals, work-arrangement compatibility. |
| US-3.3.3-06 Talent pools | Save candidate to a pool with confirmation; multiple named pools (soft limit 20/employer); pool list with counts; in-pool candidate management (notes, soft-remove, re-add); pool metadata; reusable over time with refreshed availability; opt-in team sharing. |

---

## Appendix — teaching notes & open questions

- **This is the CORE — and the algorithm is deliberately *in the domain*.** The single most important modelling decision in this package: `MatchScoringService`, `RecommendationRankingService`, the threshold logic, the privacy filter, and the talent-pool invariants are all in the `Domain` layer. Only the *embedding math* and the *vector search* are pushed behind ports. Ask the class: where exactly is the line between "the AI" (external) and "the domain" (ours)? Why is the *scoring formula* a domain concern but the *transformer* not?
- **Projections-as-aggregates.** `SeekerMatchProfile` and `PostingMatchProfile` are this BC's own copies of BC-3/BC-4 data, mutated only by integration-event handlers. This is the Partnership relationship made concrete: tight contract coupling on the *events*, but operational decoupling via the local projection. Discuss: when does an event-built projection stop being "a cache" and become "an aggregate with its own invariants"?
- **Threshold as a query-time filter.** A recurring exam-favourite invariant: changing the match threshold must never rewrite a stored score. Storage holds *facts* (this seeker scored 64 against this posting); surfacing decisions are *policy* applied at read time. Contrast with a naïve design that filters at write time and has to re-scan everything when the threshold moves.
- **Two thresholds, two owners.** The admin **Match Threshold** (`US-3.3.1-06`) and the recruiter **Qualification Threshold** (`US-3.3.3-02`) are different concepts with different owners and different scopes. Keeping them as separate VOs/aggregates is intentional — a good example of resisting the urge to unify two things that merely look similar.
- **Where does `ResumeParsed` / parsing belong?** §1's boundary note. BC-3 emits `ResumeParsed`; BC-7 consumes it. Defensible the other way (one ML pipeline owned by the CORE). The [[Event_Catalog]] flags this as an open question — narrate both designs in lecture.
- **`CandidateSavedToTalentPool` ownership.** The [[Event_Catalog]] assigns this *integration* event to BC-2 (`employerId, jobSeekerId, poolId`), yet the `TalentPool` aggregate lives here in BC-7. This package raises it as an *internal* event only and leaves the cross-BC contract to BC-2. Ask: should the aggregate and its integration event live in the same BC? When is it acceptable for them to diverge?
- **A/B testing in the domain model.** `MatchingWeightProfile` carries `VariantId` + `VariantAllocationPercent`, and every `MatchScore` records the `WeightVariantId` that produced it. The metrics *dashboard* is BC-10's job — but the *provenance* must be captured here, at compute time, or the experiment is unmeasurable. Discuss the seam between "run the experiment" (here) and "measure the experiment" (Reporting).
- **Cold-start.** Both new seekers and new postings need a fallback path. This is modelled explicitly (`IsColdStart` on `JobRecommendationSet`, the cold-start branch in `MatchScoringService`). A good prompt: how much of cold-start is a domain rule vs. an ML concern?
- **Scale.** `US-3.3.1-05 AC-05` demands shortlists for 10K+ candidates × 100+ postings within 5 minutes. The design answer here is the nightly batch + cached shortlists + query-time filtering — not real-time scoring on every page load. Discuss the CQRS-flavoured split between the write-side (batch compute) and the read-side (cached, filtered retrieval).
