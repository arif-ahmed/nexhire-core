---
title: "Handover Package — BC-10 Reporting"
type: handover-package
bc_id: BC-10
bc_name: Reporting
bc_class: core
stack: "language-neutral — see [[00-Shared-Foundations]] (Target Stack declared there)"
generated: 2026-05-15
related:
  - "[[00-Shared-Foundations]]"
  - "[[BC_Mapping]]"
  - "[[Context_Map]]"
  - "[[Event_Catalog]]"
tags:
  - handover-package
  - bc/reporting
---

# Handover Package — BC-10 Reporting

> **Audience:** an AI coding agent. This package owns the **domain design** for the `Reporting` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `Reporting`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `reporting`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

This BC is a **Conformist** consumer (see [[Context_Map]]): it subscribes to integration events from **every other domain BC** and accepts their payload shapes as-is — it never asks an upstream to reshape an event for its convenience. Its job is to **project** those "something happened" facts into read models and analytics aggregates, and to serve dashboards, reports, and exports on top of them. This is a textbook **CQRS read-model / event-sourced-projection** BC: the write side lives in eleven other modules; the read side lives here.

**Mental model for the whole module:** events come in → an idempotent **projector** updates one or more read-model tables → dashboards and reports **query** those tables → custom reports / templates / schedules / exports are themselves aggregates with their own lifecycle. There is almost no "command that mutates business state" here in the traditional sense — the two genuine write-side aggregates are `ReportDefinition` (templates + custom reports) and `ReportSchedule` (recurring runs), plus `RetentionPolicy` and `ReportRun`. Everything else is projection + query.

---

## 1. Purpose & scope boundaries

### What this BC is for

Reporting owns the **platform's read model and analytics surface**. It is one of the platform's **two CORE subdomains** (the other is BC-7 Recommendation Engine): the Labor Market Information System (LMIS) — employment statistics, industry/skill-demand analytics, employment outcomes — is a *differentiating outcome* of the platform, not table-stakes plumbing. Reporting also absorbs what was previously a separate **Audit & Activity** BC: user-activity tracking, activity-log retention, and the audit trail for report access all live here now.

Reporting has four broad responsibilities:

1. **User-activity tracking & dashboards** — projecting every user/employer/job-seeker action into an activity stream and serving real-time activity dashboards (`US-3.5.1-01..03`).
2. **Activity retention** — configurable, regulation-driven retention policies that purge or archive raw activity logs on schedule (`US-3.5.1-04`).
3. **LMIS analytics** — employment statistics, industry analytics, skill-demand trends, employment outcomes — the labor-market intelligence dashboards (`US-3.5.2-01..04`).
4. **System & matching performance metrics** — response times, error rates, resource utilisation, matching-algorithm accuracy/precision/recall, plus configurable performance alerts and anomaly detection (`US-3.5.3-01..03`).
5. **Custom report generation** — a report builder, reusable templates, recurring schedules with automated distribution, multi-format export (PDF/XLSX/CSV), and role-based report access controls (`US-3.5.4-01..04`, `US-3.1.4-04`).

### In scope

The `Reporting` module is responsible for:

- **Consuming** integration events from **all eleven** other domain BCs (BC-1, BC-2, BC-3, BC-4, BC-5, BC-6, BC-7, BC-8, BC-9, BC-11, BC-12 — full feed in §9.1) via idempotent projectors and an inbox table.
- The **activity stream** — a normalised, queryable log of `ActivityRecord`s (who, what, when, target, metadata), classified by `ActorRole` (job seeker / employer / admin / system) and `ActivityType`.
- **Activity dashboards** — current-logins / active-sessions view, last-session detail, per-user activity drill-down timelines, search by user id / email / name, aggregated activity counts over a time range.
- **Retention policy** management — per-activity-type retention periods, automatic purge/archive execution, a pre-purge warning, an immutable retention audit trail, and policy versioning with effective dates.
- **LMIS analytics read models** — pre-aggregated daily/weekly/monthly rollups for job-posting volume, application/hiring rates, time-to-fill, industry demand-vs-supply, salary-range analytics, skill-demand ranking, emerging-skill detection, skill-gap analysis, geographic distribution, placement rates and career-progression cohorts.
- **Performance metrics read models** — system metrics (response time p50/p95/p99, error rate, CPU/memory/disk/network, DB query times) sampled into time-bucketed rows; matching-algorithm metrics (accuracy, precision, recall, satisfaction, conversion) computed from BC-7 + BC-5 events.
- **Performance alerts** — `AlertRule` aggregates with thresholds, severity, channels; anomaly detection against a rolling baseline; alert lifecycle (acknowledge / suppress / escalate). The actual *delivery* of an alert is delegated to BC-9.
- **Report definitions** — both admin-authored **templates** and ad-hoc **custom reports**: selected metrics, dimensions, filters, visualization types, configurable parameters, versioning, categorisation, usage tracking.
- **Report runs** — executing a `ReportDefinition` (on demand or scheduled) as an async background job, producing a rendered artifact in PDF / XLSX / CSV, tracking run status.
- **Report schedules** — recurring runs on a cron-like cadence with a distribution list; emitting an event so BC-9 delivers the artifact.
- **Report access control** — role-based visibility of templates and reports, data-level filtering by role (an Employer Owner sees only their org's data), data masking of sensitive fields, and an immutable audit log of every report view/download.
- Publishing the (few) integration events in §8 and reacting to the integration events in §9.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong to other BCs (or external systems) and are reached via the contracts in §9:

- **Deciding *whether* a business event happened.** Every other BC owns its own write model and emits its own events. Reporting *only projects facts it is told*. It never polls another module, never recomputes a job match, never changes an application's status, never decides whether a posting expired. If a fact isn't in an event payload, Reporting does not have it.
- **The matching algorithm itself** → BC-7 Recommendation Engine. Reporting consumes `MatchComputed`, `RecommendationGenerated`, `EmbeddingsRefreshed`, `MatchThresholdChanged` and *measures* algorithm performance; it does not run the algorithm. "Accuracy/precision/recall" here are derived metrics over emitted events, not the model's own training metrics.
- **Sending emails / SMS / in-app messages** → BC-9 Notification. When a scheduled report is ready or a performance alert fires, Reporting emits an event (`ScheduledReportRun`, and an alert event) and BC-9 delivers it. Reporting never calls SMTP, never renders an email template. The "distribution list" on a schedule is data Reporting hands to BC-9.
- **The actual file bytes of a report export** → external object storage, reached via the `ObjectStorage` port. Reporting renders the artifact and stores only a `FileReference`.
- **Identity, roles, access tokens, sessions, login mechanics** → BC-1 IAM/UAM. Reporting *consumes* `UserLoggedIn` / `UserLoginFailed` to build activity dashboards and *reads* role claims off the access token for access control, but it is not the system of record for identity or roles. The "current active logins" dashboard is a projection of BC-1 session events, not a live read of BC-1's session store.
- **The skill / occupation / industry taxonomy** → BC-11 Administrators Configuration. Reporting references taxonomy codes inside analytics rows and reacts to `TaxonomyUpdated` to re-label aggregates; it does not own the vocabulary.
- **Infrastructure metric collection (APM agents, OS counters).** Raw CPU/memory/latency samples arrive as events/telemetry from the host platform; this module assumes a `SystemMetricSampled` feed exists (see §9.1 note) and projects it. It does not instrument the runtime itself.
- **Anomaly-detection ML / capacity-planning ML.** The baseline-deviation anomaly check in this module is a simple statistical rule (deviation from a 30-day rolling mean/stddev). Advanced ML forecasting is explicitly a future enhancement per `US-3.5.3-03` assumptions — do not build it.
- **Charting / visualization rendering libraries as domain logic.** Visualization *type* (`Table`, `BarChart`, `LineChart`, `PieChart`, `Heatmap`) is a value in the `ReportDefinition`; the actual server-side chart rendering into a PDF is an Infrastructure concern behind the `ReportRenderer` port.
- **Open tracking / email open rates.** `US-3.5.4-03` and the SMS stories defer open-rate tracking to BC-9 as a future enhancement. Reporting tracks *send/bounce* delivery status it receives from BC-9 events; it does not track opens.
- **Pre-deployment historical data.** Per `US-3.5.1-02` assumptions, activity tracking begins at deployment; there is no backfill of historical data.

### Boundary note — the "two cores" framing (teaching point)

[[Context_Map]] insight #1: the platform has **two** core subdomains, BC-7 (AI matching) and BC-10 (LMIS reporting). Reporting is *core* not because it is technically hard but because the **labor-market intelligence it produces is a primary reason the platform exists** — government stakeholders (MoL) consume it for policy. Yet structurally Reporting is the most *downstream* BC of all: it dictates nothing, it conforms to everyone. That tension — *core in value, conformist in posture* — is the single best discussion this BC offers. Contrast with BC-7, which is core in value **and** sits in tight Partnership relationships upstream.

### Boundary note — the absorbed Audit & Activity BC (teaching point)

[[BC_Mapping]] migration notes: the old 15-BC model had a separate "BC-11 Audit & Activity" context. The agreed 12-BC course model **folds it into Reporting**. The justification: activity tracking *is* a read-model concern — it is "project events into a queryable stream," structurally identical to LMIS analytics, just at a different grain. The cost: Reporting now spans two arguably-distinct capabilities (operational audit vs. strategic analytics). This package handles that by treating them as **two modules-worth of code inside one BC** — distinct aggregates, distinct schema table groups, distinct projectors — but one deployable, one team, one ubiquitous language. Good class question: *was folding Audit into Reporting a simplification or a smell?*

### Boundary note — Reporting must not become a coupling bottleneck (teaching point)

[[Context_Map]] insight #5 warns: if Reporting couples *too tightly* to one upstream's schema, it becomes a bottleneck for that upstream's evolution. The defence in this package is the **inbox + projector** seam: each projector translates an upstream event into *Reporting's own* read-model shape. When BC-4 adds a field to `JobPostingPublished`, only the relevant projector changes; the read models and every dashboard above them are insulated. This is the Conformist pattern done responsibly — conform at the *edge*, own your model *inside*.

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **Activity Record** | The `ActivityRecord` entity — one normalised "user X did Y at time T" fact, projected from an upstream event. The grain of the activity stream. |
| **Activity Stream** | The append-mostly collection of all `ActivityRecord`s. Not an aggregate — a projection table queried directly. |
| **Actor** | Who performed an activity: identified by `UserId` + `ActorRole`. |
| **Actor Role** | `JobSeeker`, `Employer`, `Administrator`, `System`. Determines which dashboard an activity surfaces on and which retention policy applies. |
| **Activity Type** | The classified action: `Login`, `LoginFailed`, `ProfileView`, `JobSearch`, `JobApplication`, `JobBookmark`, `JobPostingCreated`, `JobPostingPublished`, `CandidateSearch`, `ApplicationReview`, `ReportViewed`, etc. A closed enum maintained in this BC. |
| **Session Snapshot** | The `SessionSnapshot` read-model row — current/last login state per user, used by the activity dashboard. |
| **Retention Policy** | The `RetentionPolicy` aggregate — a versioned rule: for a given `ActorRole` (and optionally `ActivityType`), keep raw activity logs for N days, then purge or archive. |
| **Retention Run** | One execution of a retention policy: the `RetentionRun` entity recording what was purged/archived, when, and under which policy version. The immutable retention audit trail. |
| **Analytics Rollup** | A pre-aggregated read-model row at a fixed grain (e.g. "job postings, by industry, by day"). The LMIS dashboards query rollups, never raw events. |
| **Metric Bucket** | A time-bucketed performance row (e.g. "API latency p95, 1-minute bucket"). |
| **Matching Metric** | A derived row measuring BC-7 algorithm quality: accuracy, precision, recall, satisfaction, conversion — over a monthly batch window. |
| **Alert Rule** | The `AlertRule` aggregate — a configured threshold (metric, comparator, value), a `Severity`, and notification channels. |
| **Alert Incident** | The `AlertIncident` entity — one firing of an `AlertRule`: raised, optionally acknowledged, suppressed, or escalated. |
| **Report Definition** | The `ReportDefinition` aggregate — the *specification* of a report: selected metrics, dimensions, filters, visualization, configurable parameters. A definition is either a **Template** (`Kind = Template`, admin-authored, reusable) or a **Custom Report** (`Kind = Custom`, ad-hoc, may be saved as a template). |
| **Report Template** | A `ReportDefinition` with `Kind = Template`. Has role-based visibility and configurable parameters. |
| **Custom Report** | A `ReportDefinition` with `Kind = Custom`. |
| **Report Run** | The `ReportRun` aggregate — one execution of a `ReportDefinition` with concrete parameter values, producing a rendered artifact. Has its own async lifecycle (`Queued → Running → Completed | Failed`). |
| **Report Artifact** | The rendered output of a `ReportRun` — a `FileReference` (PDF / XLSX / CSV) plus format metadata. |
| **Report Schedule** | The `ReportSchedule` aggregate — a recurring trigger: a `ReportDefinition`, a cadence (cron-like), a distribution list, export formats. |
| **Distribution List** | The set of recipient email addresses on a `ReportSchedule`. Handed to BC-9 for delivery; Reporting never sends. |
| **Report Access Log** | The `ReportAccessLog` entity — an immutable, write-once record of a report view or download (timestamp, user, role, report id, action). |
| **Visualization Type** | `Table`, `BarChart`, `LineChart`, `PieChart`, `Heatmap`. |
| **Projector** | An idempotent integration-event handler that consumes one upstream integration event and updates one or more read-model tables. The core machinery of this BC. |
| **Inbox** | The `inbox_messages` dedupe table — every consumed integration event records its `EventId` so projectors are safe to re-run. |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `Reporting`.
- Public surface (`Contracts`): the integration events published in §8 + the public API in §9.3. Note: BC-10 is *downstream of everyone* — almost nothing references the `Contracts` surface in return.
- **Module-specific notes:**
  - The `Application` layer organises into `Activity/`, `Lmis/`, `Performance/`, `Reports/`, and `Projections/` folders so the absorbed-Audit and analytics concerns stay legible (the "two modules inside one BC" idea).
  - This module runs several **background workers / scheduled jobs** registered in its composition entry point: an **outbox relay**, a **report-execution worker** (polls queued runs and executes them), a **scheduled-report runner** (cron-style cadence dispatch), a **retention-purge job** (daily; emits a pre-purge warning then purges/archives raw activity logs per policy), and an **anomaly-detection worker** (rebuilds 30-day baselines per metric and evaluates against current buckets).

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has an unusual shape: most of its tables are **read models** (projection tables — not aggregates, just rows a projector writes and a query reads). The genuine **aggregates** — things with their own lifecycle and invariants — number five: `ReportDefinition`, `ReportRun`, `ReportSchedule`, `RetentionPolicy`, `AlertRule`. (Notation: see [[00-Shared-Foundations]] §2.)

### 5.1 Aggregate: ReportDefinition

**Aggregate root.** Identity: `ReportDefinitionId` (strongly-typed id wrapping `uuid`). Represents both admin templates and ad-hoc custom reports — distinguished by `Kind`.

| Member | Type | Notes |
|---|---|---|
| `Id` | `ReportDefinitionId` | |
| `Kind` | `ReportDefinitionKind` | enum: `Template`, `Custom` |
| `Name` | `string` | non-empty, ≤ 200 chars |
| `Description` | `string?` | |
| `Category` | `ReportCategory` | enum: `EmploymentStats`, `ActivityReports`, `Performance`, `IndustryAnalytics`, `SkillDemand`, `Outcomes`, `Custom` |
| `OwnerUserId` | `uuid` | who created it. BC-1 identity, no FK. |
| `Spec` | `ReportSpec` | VO — the metric/dimension/filter/visualization selection |
| `ConfigurableParameters` | `list<ConfigurableParameter>` | child VOs — which `Spec` fields end users may override at run time |
| `Visibility` | `ReportVisibility` | VO — which `RoleName`s may see/use this definition |
| `Versions` | `list<ReportDefinitionVersion>` | child entities; append-only version history |
| `CurrentVersionNumber` | `int` | |
| `UsageCount` | `int` | incremented on each run; for `US-3.5.4-02 AC-07` |
| `Status` | `ReportDefinitionStatus` | enum: `Active`, `Archived` |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

**Child entities / VOs:**

- `ReportDefinitionVersion` — `VersionNumber` (`int`), `Spec` snapshot (`ReportSpec`), `ChangedBy` (`uuid`), `CreatedOnUtc`, `IsCurrent` (`bool`). Append-only; `US-3.5.4-02 AC-06` keeps current + previous 5, older are flagged archived.

### 5.2 Aggregate: ReportRun

**Aggregate root.** Identity: `ReportRunId`. Kept separate from `ReportDefinition` because it has its own async lifecycle (`Queued → Running → Completed | Failed`) that can fail independently and produces a potentially-large artifact.

| Member | Type | Notes |
|---|---|---|
| `Id` | `ReportRunId` | |
| `ReportDefinitionId` | `ReportDefinitionId` | which definition was run |
| `DefinitionVersionNumber` | `int` | which version was run (definitions evolve; a run pins its version) |
| `TriggeredBy` | `RunTrigger` | VO — `OnDemand` (carries `UserId`) or `Scheduled` (carries `ReportScheduleId`) |
| `Parameters` | `ResolvedParameters` | VO — the concrete parameter values supplied for this run |
| `RoleScope` | `RoleScope` | VO — the requesting role + org scope, for data-level filtering (§7.5) |
| `Status` | `ReportRunStatus` | enum: `Queued`, `Running`, `Completed`, `Failed` |
| `Artifacts` | `list<ReportArtifact>` | child entities — one per requested export format |
| `RowCount` | `int?` | rows in the result set; null until completed |
| `FailureReason` | `string?` | set iff `Status == Failed` |
| `QueuedOnUtc` | `datetime` | |
| `StartedOnUtc` / `CompletedOnUtc` | `datetime?` | |

**Child entities:**

- `ReportArtifact` — `ReportArtifactId`, `Format` (`ExportFormat`: `Pdf`/`Xlsx`/`Csv`), `File` (`FileReference`), `GeneratedOnUtc`.

### 5.3 Aggregate: ReportSchedule

**Aggregate root.** Identity: `ReportScheduleId`. A recurring trigger for `ReportRun`s.

| Member | Type | Notes |
|---|---|---|
| `Id` | `ReportScheduleId` | |
| `ReportDefinitionId` | `ReportDefinitionId` | the definition to run |
| `Cadence` | `ScheduleCadence` | VO — frequency + day/time + skip-dates; lowers to a cron expression |
| `Parameters` | `ResolvedParameters` | VO — fixed parameter values for every run |
| `DistributionList` | `list<EmailAddress>` | recipients; handed to BC-9 |
| `ExportFormats` | `set<ExportFormat>` | which formats to attach |
| `Status` | `ScheduleStatus` | enum: `Active`, `Paused` |
| `OwnerUserId` | `uuid` | BC-1 identity, no FK |
| `LastRunOnUtc` | `datetime?` | |
| `NextRunOnUtc` | `datetime` | computed from `Cadence`; the scheduled-report runner polls on this |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

### 5.4 Aggregate: RetentionPolicy

**Aggregate root.** Identity: `RetentionPolicyId`. A versioned rule governing how long raw `ActivityRecord`s are kept.

| Member | Type | Notes |
|---|---|---|
| `Id` | `RetentionPolicyId` | |
| `Name` | `string` | e.g. "Job-seeker operational logs" |
| `Scope` | `RetentionScope` | VO — target `ActorRole` + optional `ActivityType` set |
| `RetentionDays` | `int` | > 0 |
| `Action` | `RetentionAction` | enum: `Archive` (soft — move to cold storage), `HardDelete` |
| `WarningDays` | `int` | pre-purge warning lead time; default 7 (`US-3.5.1-04 AC-06`) |
| `Versions` | `list<RetentionPolicyVersion>` | child entities; append-only |
| `CurrentVersionNumber` | `int` | |
| `EffectiveFromUtc` | `datetime` | of the current version |
| `Status` | `RetentionPolicyStatus` | enum: `Active`, `Archived` |
| `Runs` | `list<RetentionRun>` | child entities — the immutable retention audit trail |

**Child entities:**

- `RetentionPolicyVersion` — `VersionNumber`, `Scope`/`RetentionDays`/`Action`/`WarningDays` snapshot, `EffectiveFromUtc`, `ChangedBy` (`uuid`), `CreatedOnUtc`.
- `RetentionRun` — `RetentionRunId`, `PolicyVersionNumber` (`int`), `RecordsAffected` (`int`), `ActionTaken` (`RetentionAction`), `CutoffUtc` (`datetime`), `ExecutedOnUtc` (`datetime`). Append-only, never mutated — this is the compliance audit trail for `US-3.5.1-04 AC-04`.

### 5.5 Aggregate: AlertRule

**Aggregate root.** Identity: `AlertRuleId`. A configured performance-alert threshold and its firing history.

| Member | Type | Notes |
|---|---|---|
| `Id` | `AlertRuleId` | |
| `Name` | `string` | |
| `MetricKey` | `string` | which metric this watches — e.g. `api.latency.p95`, `system.error_rate`, `system.cpu` |
| `Condition` | `AlertCondition` | VO — comparator (`GreaterThan`/`LessThan`) + threshold value |
| `Severity` | `AlertSeverity` | enum: `Critical`, `Warning`, `Info` |
| `Channels` | `set<AlertChannel>` | enum set: `Email`, `InApp` (SMS/Slack out of scope per `US-3.5.3-03`) |
| `AnomalyDetectionEnabled` | `bool` | if true, also fires on statistically-significant deviation from the 30-day baseline |
| `Status` | `AlertRuleStatus` | enum: `Enabled`, `Disabled` |
| `Incidents` | `list<AlertIncident>` | child entities |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

**Child entities:**

- `AlertIncident` — `AlertIncidentId`, `TriggeredOnUtc`, `ObservedValue` (`decimal`), `Trigger` (`IncidentTrigger`: `ThresholdBreach`/`Anomaly`), `State` (`IncidentState`: `Raised`/`Acknowledged`/`Suppressed`/`Escalated`), `AcknowledgedBy` (`uuid?`), `SuppressedUntilUtc` (`datetime?`), `StateChangedOnUtc` (`datetime?`).

### 5.6 Read-model "entities" (NOT aggregates — projection rows)

These have no behavior and no invariants enforced by a root. A projector inserts/updates them; a query reads them. They are still ORM-mapped entities, just with no `AggregateRoot` base. Model them as plain classes with internal-only setter access mutated only by their projector.

- `ActivityRecord` — `ActivityRecordId`, `UserId` (`uuid`), `ActorRole`, `ActivityType`, `OccurredOnUtc`, `TargetType` (`string?`), `TargetId` (`uuid?`), `Metadata` (`json` — small free-form payload, e.g. search query text, result count), `SourceEventId` (`uuid` — the originating integration event), `ProjectedOnUtc`.
- `SessionSnapshot` — `UserId` (PK), `DisplayName`, `ActorRole`, `LastLoginOnUtc`, `LastSessionDurationSeconds` (`int?`), `ConcurrentSessionCount` (`int`), `IsCurrentlyActive` (`bool`), `UpdatedOnUtc`.
- `AnalyticsRollup` — `AnalyticsRollupId`, `Metric` (`string` — e.g. `posting.volume`, `application.count`, `hire.count`, `time_to_fill.avg`), `Grain` (`RollupGrain`: `Day`/`Week`/`Month`), `BucketStartUtc` (`datetime`), plus dimension columns `Industry` (`string?`), `OccupationCode` (`string?`), `SkillCode` (`string?`), `Region` (`string?`), `EmployerId` (`uuid?`), and `Value` (`decimal`), `SampleCount` (`int`). One row per (metric, grain, bucket, dimension-tuple).
- `SalaryStatRollup` — `SalaryStatRollupId`, `OccupationCode`, `Industry`, `Region`, `Grain`, `BucketStartUtc`, `Min`/`Max`/`Median`/`P25`/`P75` (`decimal`), `Currency`, `SampleCount`.
- `SkillDemandRollup` — `SkillDemandRollupId`, `SkillCode`, `Industry` (`string?`), `Region` (`string?`), `Grain`, `BucketStartUtc`, `PostingCount` (`int`), `CandidateSupplyCount` (`int`), `GrowthRatePct` (`decimal?` — period-over-period), `IsEmerging` (`bool`).
- `SystemMetricBucket` — `SystemMetricBucketId`, `MetricKey` (`string`), `BucketStartUtc`, `BucketSeconds` (`int` — 60 for detailed, 86400 for aggregated), `P50`/`P95`/`P99` (`decimal?`), `Avg` (`decimal?`), `Count` (`int64`), `Min`/`Max` (`decimal?`).
- `MatchingMetricRollup` — `MatchingMetricRollupId`, `BucketStartUtc`, `Grain` (always `Month` per `US-3.5.3-02`), dimension columns (`JobCategory`, `Industry`, `Region`, `SkillCode` — all nullable), `RecommendationCount` (`int`), `SelectedCount` (`int`), `Accuracy`/`Precision`/`Recall` (`decimal?`), `AvgSatisfaction` (`decimal?`), `ApplicationRate`/`OfferRate` (`decimal?`), `AbTestVariant` (`string?`).
- `OutcomeCohortRollup` — `OutcomeCohortRollupId`, `Grain`, `BucketStartUtc`, dimension columns (`Industry`, `Region`, `SkillCode` — nullable), `ApplicationCount` (`int`), `OfferCount` (`int`), `AcceptanceCount` (`int`), `PlacementRatePct` (`decimal?`), `CareerProgressionCount` (`int`).

### 5.7 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `ReportSpec` | `Metrics` (`list<string>`), `Dimensions` (`list<string>`), `Filters` (`list<ReportFilter>`), `Visualization` (`VisualizationType`) | at least one metric; all metric/dimension keys in the known catalog |
| `ReportFilter` | `Field` (`string`), `Operator` (`FilterOperator`: `Eq`/`In`/`Between`/`Gte`/`Lte`), `Values` (`list<string>`) | field non-empty; `Values` non-empty; `Between` requires exactly 2 values |
| `ConfigurableParameter` | `Name` (`string`), `Kind` (`ParameterKind`: `DateRange`/`FilterDropdown`/`MetricSelection`), `Required` (`bool`), `DefaultValue` (`string?`) | name non-empty |
| `ResolvedParameters` | `Values` (`map<string,string>`) | every `Required` configurable parameter has a value |
| `ReportVisibility` | `AllowedRoles` (`set<RoleName>`) | non-empty |
| `RoleName` | `Value` (`string`) | one of `SystemAdministrator`, `MoLAdministrator`, `DataAnalyst`, `EmployerOwner`, `Auditor` |
| `RoleScope` | `Role` (`RoleName`), `EmployerId` (`uuid?`) | `EmployerId` required iff `Role == EmployerOwner` |
| `RunTrigger` | `Mode` (`TriggerMode`: `OnDemand`/`Scheduled`), `UserId` (`uuid?`), `ReportScheduleId` (`ReportScheduleId?`) | `OnDemand ⇒ UserId set`; `Scheduled ⇒ ReportScheduleId set` |
| `ScheduleCadence` | `Frequency` (`Frequency`: `Daily`/`Weekly`/`Monthly`/`Quarterly`), `DayOfWeek` (`DayOfWeek?`), `DayOfMonth` (`int?`), `TimeOfDayUtc` (time-of-day), `SkipDates` (`list<date>`) | `Weekly ⇒ DayOfWeek set`; `Monthly/Quarterly ⇒ DayOfMonth ∈ [1,28]` |
| `RetentionScope` | `ActorRole` (`ActorRole`), `ActivityTypes` (`set<ActivityType>`) | empty `ActivityTypes` = "all types for this role" |
| `AlertCondition` | `Comparator` (`Comparator`: `GreaterThan`/`LessThan`), `Threshold` (`decimal`) | — |
| `EmailAddress` | `Value` | RFC 5322; lower-cased on store |
| `FileReference` | `StorageKey`, `OriginalFileName`, `MimeType`, `SizeBytes` (`int64`) | size > 0 |
| `DateRange` | `StartUtc`, `EndUtc` | `StartUtc ≤ EndUtc` |

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior on the **five aggregates** lives on the aggregate roots. Read-model rows (§5.6) are mutated only by **projectors** (§10.4) — they have no domain behavior. Method shapes below are neutral specifications (see [[00-Shared-Foundations]] §2).

### 6.1 ReportDefinition — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static CreateTemplate(name, category, ownerUserId, spec, configurableParameters, visibility)` | `Kind = Template`. `spec` valid. `visibility.AllowedRoles` non-empty. Creates `VersionNumber = 1`, `IsCurrent = true`. Status `Active`. | `ReportDefinitionCreated` *(internal)* |
| `static CreateCustom(name, ownerUserId, spec)` | `Kind = Custom`. `Category = Custom`. Single version. Visibility defaults to `{ owner's role }`. | `ReportDefinitionCreated` *(internal)* |
| `SaveCustomAsTemplate(category, configurableParameters, visibility)` | only valid when `Kind == Custom` (`E-REPORT-NOT-CUSTOM`); flips `Kind` to `Template`, sets category/parameters/visibility. | `ReportDefinitionCreated` *(internal)* |
| `UpdateSpec(newSpec, changedBy)` | appends a new `ReportDefinitionVersion`, increments `CurrentVersionNumber`, marks it current, marks the previous not-current. Older-than-5 versions flagged archived. | `ReportDefinitionVersioned` *(internal)* |
| `UpdateVisibility(newVisibility)` | `AllowedRoles` non-empty. | — |
| `RecordUsage()` | increments `UsageCount`. Called by the `ReportRun` flow. | — |
| `Archive()` | only from `Active`. → `Archived`. | — |

### 6.2 ReportRun — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Queue(definitionId, definitionVersionNumber, trigger, parameters, roleScope, requestedFormats)` | `parameters` must satisfy every required `ConfigurableParameter` of the definition (`E-REPORT-MISSING-PARAM`). `requestedFormats` non-empty. Status `Queued`. | `ReportRunQueued` *(internal)* |
| `MarkRunning()` | only from `Queued`. → `Running`, sets `StartedOnUtc`. | — |
| `MarkCompleted(artifacts, rowCount)` | only from `Running`. → `Completed`. One `ReportArtifact` per requested format. `rowCount ≤ 100_000` for CSV/XLSX else `E-REPORT-ROW-LIMIT`; PDF page cap handled by renderer. | `ReportRunCompleted` |
| `MarkFailed(reason)` | from `Queued` or `Running`. → `Failed`, sets `FailureReason`. | `ReportRunFailed` *(internal)* |

### 6.3 ReportSchedule — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Create(definitionId, cadence, parameters, distributionList, exportFormats, ownerUserId)` | `distributionList` non-empty; `exportFormats` non-empty; `cadence` valid. Status `Active`. `NextRunOnUtc` computed from `cadence`. | `ReportScheduleCreated` *(internal)* |
| `UpdateCadence(newCadence)` | recompute `NextRunOnUtc`. Only allowed when `Active`. | — |
| `UpdateDistributionList(list)` | non-empty. | — |
| `Pause()` | only from `Active`. → `Paused`. | — |
| `Resume()` | only from `Paused`. → `Active`, recompute `NextRunOnUtc`. | — |
| `RecordRun(executedOnUtc)` | sets `LastRunOnUtc`, advances `NextRunOnUtc` to the next cadence slot after `executedOnUtc`, honouring `SkipDates`. | — |

### 6.4 RetentionPolicy — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Create(name, scope, retentionDays, action, warningDays)` | `retentionDays > 0`, `warningDays ≥ 0`. `VersionNumber = 1`, `EffectiveFromUtc = now`. Status `Active`. | — |
| `Revise(scope, retentionDays, action, warningDays, changedBy, effectiveFromUtc)` | appends a `RetentionPolicyVersion`, archives the previous version's effective window, sets the new current version. `effectiveFromUtc ≥ now`. | `RetentionPolicyRevised` *(internal)* |
| `RecordRun(policyVersionNumber, cutoffUtc, recordsAffected, actionTaken, executedOnUtc)` | appends an **immutable** `RetentionRun`. `RetentionRun`s are never updated or deleted. | `RetentionApplied` *(internal — relayed to integration event, see §8)* |
| `Archive()` | only from `Active`. → `Archived`. No further runs after archival. | — |

### 6.5 AlertRule — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Create(name, metricKey, condition, severity, channels, anomalyDetectionEnabled)` | `metricKey` non-empty; `channels` non-empty. Status `Enabled`. | — |
| `UpdateCondition(newCondition)` / `UpdateSeverity(newSeverity)` / `UpdateChannels(newChannels)` | `channels` non-empty when updated. | — |
| `Enable()` / `Disable()` | toggle `Status`. A `Disabled` rule never fires. | — |
| `Fire(observedValue, trigger, firedOnUtc)` | only when `Status == Enabled`. Appends an `AlertIncident` in state `Raised`. **De-dup invariant:** does not append a new incident if an un-resolved (`Raised` and not suppressed) incident already exists for this rule — prevents alert storms. | `AlertIncidentRaised` *(internal — relayed to integration event so BC-9 can notify)* |
| `AcknowledgeIncident(incidentId, byUserId)` | incident must exist and be `Raised`. → `Acknowledged`. | — |
| `SuppressIncident(incidentId, untilUtc)` | → `Suppressed`, sets `SuppressedUntilUtc`. | — |
| `EscalateIncident(incidentId)` | → `Escalated`. | `AlertIncidentEscalated` *(internal — relayed)* |

### 6.6 Core invariants (must always hold)

1. **A `ReportDefinition` always has at least one version**, and exactly one version has `IsCurrent = true`.
2. **`ReportSpec.Metrics` is never empty**; every metric/dimension key references the known metric catalog (the catalog is a static list in `Domain`, see §7.1).
3. **A `ReportRun` pins its definition version** — `DefinitionVersionNumber` is set at `Queue` time and never changes; a later `UpdateSpec` on the definition does not affect in-flight runs.
4. **`ReportRun` status machine** is strictly ordered: `Queued → Running → Completed | Failed`, plus `Queued → Failed`. No other transition is legal.
5. **`ReportArtifact` count** equals the run's requested-format count exactly when `Status == Completed`.
6. **CSV/XLSX runs cap at 100,000 rows** (`E-REPORT-ROW-LIMIT`); PDF caps at 50 pages (renderer-enforced, configurable).
7. **`RetentionRun` entries are immutable and append-only** — once written they are never updated or deleted. This is the legal compliance trail.
8. **A `RetentionPolicy` always has exactly one current version**; version effective-from dates form a non-overlapping, gap-free sequence.
9. **`RetentionRun.RecordsAffected` ≥ 0**; a run with `RecordsAffected = 0` is still recorded (proof the policy executed).
10. **`AlertRule.Channels` is never empty** while the rule exists.
11. **At most one unresolved `AlertIncident` per `AlertRule`** at any time (de-dup invariant in `Fire`).
12. **A `Disabled` `AlertRule` never produces an `AlertIncident`.**
13. **Read-model rows are write-once-per-projector** — `ActivityRecord` carries `SourceEventId`, and a projector must not insert a second `ActivityRecord` for the same `SourceEventId` (idempotency — see §11.2 and §13).
14. **`AnalyticsRollup` / `SalaryStatRollup` / `SkillDemandRollup` / `SystemMetricBucket` rows are upserts** keyed by (metric, grain, bucket, dimension-tuple) — re-processing an event must be additive-idempotent (a projector that already applied event E must not double-count it; see §10.4 idempotency note).
15. **`ReportSchedule.NextRunOnUtc` is always in the future** relative to `LastRunOnUtc` (or relative to creation time if never run).

---

## 7. Domain services

Stateless. Live in `Domain`. Used when logic spans entities or needs a small external input.

### 7.1 `MetricCatalog`

```
IsKnownMetric(metricKey: string) -> bool
IsKnownDimension(dimensionKey: string) -> bool
Describe(metricKey: string) -> MetricDescriptor       // grain support, unit, which read model serves it
All() -> list<MetricDescriptor>
```

A static, in-`Domain` registry of every metric and dimension the platform can report on (e.g. `posting.volume`, `application.count`, `hire.count`, `time_to_fill.avg`, `match.accuracy`, `activity.count`, `salary.median`). `ReportSpec.Create` validates against it; the report builder UI is populated from it. This is the single source of truth that keeps `ReportDefinition`s honest. Adding a metric = adding a `MetricDescriptor` + the projector that fills its read model.

### 7.2 `RetentionCutoffCalculator`

```
ComputeCutoff(policy: RetentionPolicy, nowUtc: datetime) -> datetime
ComputeWarningDate(policy: RetentionPolicy, nowUtc: datetime) -> datetime
```

Given a policy and "now", returns the timestamp before which `ActivityRecord`s matching the policy's `Scope` are eligible for purge/archive (`now - RetentionDays`), and the date at which a pre-purge warning should fire (`cutoff + WarningDays` worth of lead time). Used by the retention-purge job. Pure function — testable in isolation, the heart of `US-3.5.1-04`.

### 7.3 `ScheduleNextRunCalculator`

```
ComputeNextRun(cadence: ScheduleCadence, afterUtc: datetime) -> datetime
```

Lowers a `ScheduleCadence` to its next concrete UTC run instant after a given point, skipping any `SkipDates`. Encapsulates the daily/weekly/monthly/quarterly logic so `ReportSchedule.Create` and `RecordRun` don't duplicate it. The "cron-like" detail from `US-3.5.4-03` is hidden behind this.

### 7.4 `AnomalyDetector`

```
Evaluate(metricKey: string, observedValue: decimal, baseline: BaselineWindow) -> AnomalyVerdict
                                                       // baseline = 30-day mean + stddev
```

Pure statistical check: returns `IsAnomaly = true` when `observedValue` deviates from the baseline mean by more than a configurable number of standard deviations (default 3). Used by the anomaly-detection worker against `SystemMetricBucket` history. Deliberately simple — `US-3.5.3-03` assumptions explicitly defer ML forecasting.

### 7.5 `ReportDataScopeFilter`

```
ApplyRoleScope(spec: ReportSpec, roleScope: RoleScope) -> ReportFilterSet
MaskedFieldsFor(roleScope: RoleScope, category: ReportCategory) -> set<string>
```

The enforcement point for `US-3.5.4-04`. Given a report spec and the requesting `RoleScope`, it (a) injects mandatory filters — e.g. an `EmployerOwner` gets an unconditional `EmployerId = <their org>` filter appended, no matter what the definition says; (b) returns the set of fields that must be **masked** in the output for that role (e.g. salary ranges and applicant names for non-admin roles). The `ReportRun` execution handler calls this before querying read models — data-level security is applied at *generation* time, never stored. A `DataAnalyst` and `MoLAdministrator` get no scope filter; an `Auditor` gets read-only-all; an `EmployerOwner` gets the org filter + masking.

### 7.6 `ActivityClassifier`

```
Classify(sourceEvent: IntegrationEvent) -> Result<ActivityRecord>
```

The translation seam between the **eleven upstream event vocabularies** and Reporting's *own* `ActivityType` enum. Given any consumed integration event, it maps it to a normalised `ActivityRecord` (or returns "not an activity-worthy event" — many consumed events feed analytics rollups but not the activity stream). This is where the Conformist boundary is actually drawn: every upstream's naming is translated *here*, once, into Reporting's language. Keeping this in a single domain service (rather than scattered across projectors) is deliberate — it is the most important teaching artifact in this BC.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module. **Integration events** (`IntegrationEvent`) cross the BC boundary — they go through the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract exactly.

Reporting is the most *downstream* BC on the platform — it **emits very few events**. The [[Event_Catalog]] lists exactly three.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

| Integration event | Raised when | Payload | Primary consumer |
|---|---|---|---|
| `ReportGeneratedIntegrationEvent` | a `ReportRun` reaches `Completed` (`ReportRunCompleted` domain event relayed) | `ReportRunId`, `ReportDefinitionId`, `ByUserId` (`uuid?` — null for scheduled), `ArtifactRefs` (`list of {format, storageKey}`), `OccurredOnUtc` | BC-9 Notification — notify the requester their report is ready |
| `ScheduledReportRunIntegrationEvent` | a scheduled `ReportRun` completes (cron-triggered) | `ReportScheduleId`, `ReportRunId`, `DistributionList` (`list<string>`), `ArtifactRefs` (`list of {format, storageKey}`), `OccurredOnUtc` | BC-9 Notification — deliver the artifact to the distribution list |
| `ActivityRetentionAppliedIntegrationEvent` | a `RetentionRun` is recorded (`RetentionApplied` domain event relayed) | `RetentionPolicyId`, `Scope` (`string`), `RecordsPurged` (`int`), `ActionTaken` (`string`), `OccurredOnUtc` | BC-9 Notification (admin notice); also self-consumed as an internal audit signal |

> **Performance-alert delivery.** When an `AlertRule` fires, the `AlertIncidentRaised` / `AlertIncidentEscalated` internal domain events are relayed to an outbox integration event so **BC-9** delivers the email/in-app alert. The [[Event_Catalog]] does not enumerate a dedicated alert event for BC-10 — treat this as a defensible local addition: emit `PerformanceAlertRaisedIntegrationEvent { AlertRuleId, IncidentId, MetricKey, Severity, ObservedValue, Channels, OccurredOnUtc }` from the `Contracts` surface, consumed by BC-9. Flag it in teaching notes (§Appendix) as a catalog gap.

### 8.2 Internal domain events (NOT published outside the module)

`ReportDefinitionCreated`, `ReportDefinitionVersioned`, `ReportRunQueued`, `ReportRunCompleted`, `ReportRunFailed`, `ReportScheduleCreated`, `RetentionPolicyRevised`, `RetentionApplied`, `AlertIncidentRaised`, `AlertIncidentEscalated`. Use these for in-module reactions (e.g. `ReportRunCompleted` → increment the definition's `UsageCount`; `ReportDefinitionCreated` for a custom report → nothing). Only the three (four, with the alert event) listed in §8.1 reach the outbox.

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained for its domain. (Outbox/inbox mechanics: [[00-Shared-Foundations]] §6.2–6.3.)

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent projectors)

Reporting is a **Conformist consumer of essentially the entire event catalog**. Below is the full consumed feed, **grouped by source BC**, with the payload you receive (from [[Event_Catalog]]) and which read model each projector updates. Every projector must be idempotent — dedupe on `EventId` via the inbox table (§11), and make rollup updates additive-idempotent.

**A note on volume:** this is ~50 distinct event types. Do **not** write 50 hand-rolled handlers with copy-pasted inbox logic. Build one generic `IntegrationEventProjectionBehavior` that (a) checks the inbox, (b) dispatches to the typed projector, (c) records the inbox row — all in one transaction. Each projector is then just the mapping logic.

#### From BC-1 · IAM and UAM

| Event | Payload you receive | Your reaction |
|---|---|---|
| `UserRegisteredIntegrationEvent` | `userId`, `role`, `email`, `createdAt` | `ActivityRecord` (`UserRegistered`); seed/update `SessionSnapshot.DisplayName`/`ActorRole`; bump `AnalyticsRollup` `registration.count` by role. |
| `UserAccountActivatedIntegrationEvent` | `userId`, `activatedAt` | `ActivityRecord` (`AccountActivated`). |
| `UserAccountSuspendedIntegrationEvent` | `userId`, `reason`, `by`, `at` | `ActivityRecord` (`AccountSuspended`); mark `SessionSnapshot.IsCurrentlyActive = false`. |
| `UserAccountReinstatedIntegrationEvent` | `userId`, `by`, `at` | `ActivityRecord` (`AccountReinstated`). |
| `AccountDeactivatedIntegrationEvent` | `userId`, `deactivatedAt` | `ActivityRecord` (`AccountDeactivated`); `SessionSnapshot.IsCurrentlyActive = false`. |
| `UserLoggedInIntegrationEvent` | `userId`, `sessionId`, `channel`, `at` | `ActivityRecord` (`Login`); upsert `SessionSnapshot` — set `LastLoginOnUtc`, increment `ConcurrentSessionCount`, `IsCurrentlyActive = true`. Feeds the activity dashboard's "current logins". |
| `UserLoginFailedIntegrationEvent` | `identifier`, `reason`, `at` | `ActivityRecord` (`LoginFailed`) — keyed by identifier; security signal for the dashboard. |
| `PasswordResetIntegrationEvent` | `userId`, `at` | `ActivityRecord` (`PasswordReset`). |
| `RoleAssignedIntegrationEvent` | `userId`, `role`, `by`, `at` | `ActivityRecord` (`RoleAssigned`). |

> **Session-end / logout note:** the [[Event_Catalog]] has no `UserLoggedOut` event. `SessionSnapshot.IsCurrentlyActive` and `LastSessionDurationSeconds` are therefore best-effort: treat a session as ended on `AccountDeactivated`/`Suspended`, or after a configurable idle timeout the `SessionSnapshot` projector applies (default 30 min since `LastLoginOnUtc`). Flag in teaching notes as a catalog gap (§Appendix).

#### From BC-2 · Employer Profile Management

| Event | Payload you receive | Your reaction |
|---|---|---|
| `EmployerRegisteredIntegrationEvent` | `employerId`, `userId`, `companyName`, `at` | `ActivityRecord` (`EmployerRegistered`); `AnalyticsRollup` `registration.count` (employer). |
| `EmployerProfileUpdatedIntegrationEvent` | `employerId`, `changedFields`, `at` | `ActivityRecord` (`EmployerProfileUpdated`). |
| `EmployerVerificationRequestedIntegrationEvent` | `employerId`, `registryRef`, `at` | `ActivityRecord` (`EmployerVerificationRequested`). |
| `EmployerVerifiedIntegrationEvent` | `employerId`, `verifiedAt`, `evidenceRef` | `ActivityRecord` (`EmployerVerified`); `AnalyticsRollup` `employer.verified.count`. |
| `EmployerVerificationFailedIntegrationEvent` | `employerId`, `reason`, `at` | `ActivityRecord` (`EmployerVerificationFailed`). |
| `CandidateSavedToTalentPoolIntegrationEvent` | `employerId`, `jobSeekerId`, `poolId`, `at` | `ActivityRecord` (`CandidateSaved`) — employer activity. |

#### From BC-3 · JobSeeker Profile

| Event | Payload you receive | Your reaction |
|---|---|---|
| `JobSeekerRegisteredIntegrationEvent` | `jobSeekerId`, `userId`, `at` | `ActivityRecord` (`JobSeekerRegistered`). |
| `ProfileLevel2CompletedIntegrationEvent` | `jobSeekerId`, `completenessScore`, `at` | `ActivityRecord` (`ProfileCompleted`); `AnalyticsRollup` `profile.l2_completed.count` (supply-side signal). |
| `ResumeUploadedIntegrationEvent` | `jobSeekerId`, `resumeId`, `mime`, `at` | `ActivityRecord` (`ResumeUploaded`). |
| `ResumeParsedIntegrationEvent` | `jobSeekerId`, `resumeId`, `skills[]`, `education[]`, `experience[]`, `at` | `ActivityRecord` (`ResumeParsed`); for each skill, bump `SkillDemandRollup.CandidateSupplyCount` (candidate-side skill supply). |
| `ProfileSkillsUpdatedIntegrationEvent` | `jobSeekerId`, `addedSkills[]`, `removedSkills[]`, `at` | `ActivityRecord` (`ProfileSkillsUpdated`); adjust `SkillDemandRollup.CandidateSupplyCount`. |
| `ProfileVisibilityChangedIntegrationEvent` | `jobSeekerId`, `visibility`, `at` | `ActivityRecord` (`ProfileVisibilityChanged`). |
| `SupplementaryDocumentUploadedIntegrationEvent` | `jobSeekerId`, `docId`, `type`, `at` | `ActivityRecord` (`DocumentUploaded`). |
| `ProfileCompletenessChangedIntegrationEvent` | `jobSeekerId`, `score`, `at` | `ActivityRecord` (`ProfileCompletenessChanged`). |

#### From BC-4 · Job Postings

| Event | Payload you receive | Your reaction |
|---|---|---|
| `JobPostingCreatedIntegrationEvent` | `postingId`, `employerId`, `draft`, `at` | `ActivityRecord` (`JobPostingCreated`) — employer activity. |
| `JobPostingPublishedIntegrationEvent` | `postingId`, `employerId`, `title`, `requirements`, `at` | `ActivityRecord` (`JobPostingPublished`); bump `AnalyticsRollup` `posting.volume` (by industry/occupation/region derived from `requirements`); for each required skill bump `SkillDemandRollup.PostingCount`; if `requirements` carries a salary range, feed `SalaryStatRollup`. **This is the single most important LMIS input event.** |
| `JobPostingUpdatedIntegrationEvent` | `postingId`, `changedFields`, `at` | `ActivityRecord` (`JobPostingUpdated`); re-derive affected rollup dimensions if industry/skill/salary changed. |
| `JobPostingExpiredIntegrationEvent` | `postingId`, `expiredAt` | `ActivityRecord` (`JobPostingExpired`); contributes to `time_to_fill` / posting-lifecycle rollups. |
| `JobPostingClosedIntegrationEvent` | `postingId`, `reason`, `at` | `ActivityRecord` (`JobPostingClosed`); if `reason` indicates "filled", contributes a data point to `time_to_fill.avg` and `hire.count` for that posting's dimensions. |
| `JobPostingSuspendedIntegrationEvent` | `postingId`, `by`, `reason`, `at` | `ActivityRecord` (`JobPostingSuspended`) — admin moderation activity. |
| `JobPostingReinstatedIntegrationEvent` | `postingId`, `by`, `at` | `ActivityRecord` (`JobPostingReinstated`). |
| `JobPostingStatusChangedIntegrationEvent` | `postingId`, `fromStatus`, `toStatus`, `at` | feeds the posting-lifecycle timeline used to compute `time_to_fill`. |

#### From BC-5 · Job Application

| Event | Payload you receive | Your reaction |
|---|---|---|
| `JobBookmarkedIntegrationEvent` | `jobSeekerId`, `postingId`, `at` | `ActivityRecord` (`JobBookmark`). |
| `JobUnbookmarkedIntegrationEvent` | `jobSeekerId`, `postingId`, `at` | `ActivityRecord` (`JobUnbookmark`). |
| `ApplicationSubmittedIntegrationEvent` | `applicationId`, `jobSeekerId`, `postingId`, `snapshot`, `at` | `ActivityRecord` (`JobApplication`); bump `AnalyticsRollup` `application.count` (by posting dimensions); seed an outcome row in `OutcomeCohortRollup.ApplicationCount`. |
| `ApplicationViewedIntegrationEvent` | `applicationId`, `employerId`, `at` | `ActivityRecord` (`ApplicationReview`) — employer activity. |
| `ApplicationStatusChangedIntegrationEvent` | `applicationId`, `fromStatus`, `toStatus`, `by`, `at` | `ActivityRecord` (`ApplicationStatusChanged`); when `toStatus ∈ {Offered, Hired}` bump `OutcomeCohortRollup.OfferCount`/`AcceptanceCount` and `AnalyticsRollup` `hire.count`; `Hired` also contributes `time_to_fill` for the posting. |
| `ApplicationWithdrawnIntegrationEvent` | `applicationId`, `jobSeekerId`, `at` | `ActivityRecord` (`ApplicationWithdrawn`). |

#### From BC-6 · Search & Discovery

| Event | Payload you receive | Your reaction |
|---|---|---|
| `SearchPerformedIntegrationEvent` | `userId`, `query`, `filters`, `resultCount`, `at` | `ActivityRecord` (`JobSearch` if actor is a seeker, `CandidateSearch` if employer — `ActivityClassifier` decides on `ActorRole`); store `query`/`resultCount` in `Metadata`; feed `AnalyticsRollup` `search.count` and the "top searched titles/skills/locations" rollups for `US-3.1.4-04 AC-05`. |
| `SavedSearchCreatedIntegrationEvent` | `savedSearchId`, `userId`, `criteria`, `at` | `ActivityRecord` (`SavedSearchCreated`). |
| `SavedSearchMatchFoundIntegrationEvent` | `savedSearchId`, `postingIds`, `at` | `ActivityRecord` (`SavedSearchMatch`). |

#### From BC-7 · Recommendation Engine

| Event | Payload you receive | Your reaction |
|---|---|---|
| `MatchComputedIntegrationEvent` | `jobSeekerId`, `postingId`, `score`, `at` | bump `MatchingMetricRollup.RecommendationCount`; retain `score` distribution for accuracy computation. |
| `RecommendationGeneratedIntegrationEvent` | `jobSeekerId`, `postingIds`, `computedAt` | bump `MatchingMetricRollup.RecommendationCount`; later joined against `ApplicationSubmitted` to compute `ApplicationRate` (conversion). |
| `CandidateRecommendationGeneratedIntegrationEvent` | `employerId`, `postingId`, `jobSeekerIds`, `at` | feeds employer-side matching metrics. |
| `EmbeddingsRefreshedIntegrationEvent` | `scope`, `vectorCount`, `at` | `ActivityRecord` (`EmbeddingsRefreshed`, `ActorRole = System`); operational metric for the nightly batch. |
| `MatchThresholdChangedIntegrationEvent` | `scope`, `oldValue`, `newValue`, `by`, `at` | `ActivityRecord` (`MatchThresholdChanged`); annotates the `MatchingMetricRollup` timeline so A/B / before-after comparison (`US-3.5.3-02 AC-06`) is possible. |

#### From BC-8 · External Job Synchronization

| Event | Payload you receive | Your reaction |
|---|---|---|
| `ExternalJobIngestedIntegrationEvent` | `externalRef`, `partnerId`, `normalizedPosting`, `at` | `ActivityRecord` (`ExternalJobIngested`, `ActorRole = System`); the resulting posting still arrives separately via BC-4's `JobPostingCreated/Published` — do not double-count posting volume here. |
| `ExternalJobUpdatedIntegrationEvent` | `externalRef`, `partnerId`, `changedFields`, `at` | `ActivityRecord` (`ExternalJobUpdated`, `System`). |
| `ExternalJobRetractedIntegrationEvent` | `externalRef`, `partnerId`, `at` | `ActivityRecord` (`ExternalJobRetracted`, `System`). |
| `IdentityVerifiedByGovernmentIntegrationEvent` | `userId`, `registry`, `at` | `ActivityRecord` (`IdentityVerified`) — supports the government-data audit trail. |
| `IdentityVerificationFailedIntegrationEvent` | `userId`, `registry`, `reason`, `at` | `ActivityRecord` (`IdentityVerificationFailed`). |
| `EducationVerifiedIntegrationEvent` | `jobSeekerId`, `credentialRef`, `at` | `ActivityRecord` (`EducationVerified`). |
| `EmployerVerifiedByGovernmentIntegrationEvent` | `employerId`, `registry`, `at` | `ActivityRecord` (`EmployerVerifiedByGovernment`). |
| `SyncErrorDetectedIntegrationEvent` | `partnerId`, `errorClass`, `payloadRef`, `at` | `ActivityRecord` (`SyncError`, `System`); feeds `SystemMetricBucket` `integration.error_rate` (operational health). |
| `SyncReconciledIntegrationEvent` | `partnerId`, `recordsAffected`, `at` | `ActivityRecord` (`SyncReconciled`, `System`). |

#### From BC-9 · Notification

| Event | Payload you receive | Your reaction |
|---|---|---|
| `NotificationDispatchedIntegrationEvent` | `notificationId`, `channel`, `recipientId`, `templateId`, `at` | bump `AnalyticsRollup` `notification.dispatched.count` by channel. |
| `NotificationDeliveredIntegrationEvent` | `notificationId`, `deliveredAt` | bump `notification.delivered.count`. |
| `NotificationFailedIntegrationEvent` | `notificationId`, `channel`, `reason`, `at` | bump `notification.failed.count`; feeds delivery-health metrics. |
| `NotificationPreferencesUpdatedIntegrationEvent` | `userId`, `channel`, `prefs`, `at` | `ActivityRecord` (`NotificationPreferencesUpdated`). |
| `DigestScheduledIntegrationEvent` / `DigestSentIntegrationEvent` | `userId`, `contents`, `at` | bump `notification.digest.count`. |

#### From BC-11 · Administrators Configuration

| Event | Payload you receive | Your reaction |
|---|---|---|
| `TaxonomyUpdatedIntegrationEvent` | `taxonomyId`, `changeSummary`, `version`, `at` | re-label / re-key affected `SkillDemandRollup` and `AnalyticsRollup` dimension values so historical analytics stay consistent under the new taxonomy version; `ActivityRecord` (`TaxonomyUpdated`, `ActorRole = Administrator`). |
| `TaxonomyTermAddedIntegrationEvent` | `taxonomyId`, `termId`, `label`, `at` | register the new term so future rollups can key on it. |
| `TaxonomyTermDeprecatedIntegrationEvent` | `taxonomyId`, `termId`, `replacedBy`, `at` | mark the term deprecated in the dimension registry; optionally roll its rollup counts forward into `replacedBy`. |

#### From BC-12 · Content Management

| Event | Payload you receive | Your reaction |
|---|---|---|
| `ArticlePublishedIntegrationEvent` | `articleId`, `title`, `categories`, `at` | `ActivityRecord` (`ArticlePublished`, `ActorRole = Administrator`). |
| `ArticleScheduledIntegrationEvent` | `articleId`, `publishAt` | `ActivityRecord` (`ArticleScheduled`, `Administrator`). |
| `ArticleArchivedIntegrationEvent` | `articleId`, `at` | `ActivityRecord` (`ArticleArchived`, `Administrator`). |
| `FAQPublishedIntegrationEvent` | `faqId`, `topic`, `at` | `ActivityRecord` (`FAQPublished`, `Administrator`). |
| `HelpFeedbackReceivedIntegrationEvent` | `helpId`, `rating`, `at` | bump `AnalyticsRollup` `help.feedback.count` / average rating. |

#### Platform telemetry (host-supplied, not a business BC)

| Event | Payload you receive | Your reaction |
|---|---|---|
| `SystemMetricSampledIntegrationEvent` | `metricKey`, `value`, `unit`, `sampledAt` | upsert `SystemMetricBucket` for the 1-minute bucket of `sampledAt` — update `Count`, `Avg`, `Min`/`Max`, and percentile estimates. **Not in [[Event_Catalog]]** — it is a host/APM telemetry feed, not a domain BC event. Treated as an external feed wired into the same inbox. The `US-3.5.3` system-performance dashboards are projected from this. Flag as a catalog gap (§Appendix). |

**Idempotency** is mandatory — see [[00-Shared-Foundations]] §6.3 (inbox). Two layers: (1) the inbox table dedupes on `EventId` so a re-delivered event is a no-op; (2) for additive rollups, the dedupe in layer (1) is what makes the increment safe — never write an additive rollup update without the inbox guard in the *same transaction*. `ActivityRecord` inserts additionally carry `SourceEventId` with a unique index as a belt-and-braces guard (§11).

### 9.2 Public APIs this module CALLS (port interfaces — define in `Application`, implement adapters in `Infrastructure`)

```
Port: ReportRenderer            (renders a resolved report — data + visualization spec — into a concrete artifact)
                                // The data has ALREADY been queried from read models and scope-filtered/masked
                                // by the handler; the renderer only formats it.
  Render(request: ReportRenderRequest, format: ExportFormat) -> Result<RenderedArtifact>
      // PDF capped at a configurable page limit (default 50). Returns the rendered bytes.
  ReportRenderRequest {
    ReportName: string, Visualization: VisualizationType,
    ColumnHeaders: list<string>, Rows: list<list<string>>,
    ChartSeries: map<string, object>
  }
  RenderedArtifact { Content: bytes, MimeType: string, PageCount: int? }

Port: ObjectStorage             (external object storage — stores rendered artifact bytes; module keeps only FileReference)
  Store(content: bytes, fileName: string, mimeType: string) -> Result<FileReference>
  Retrieve(storageKey: string) -> Result<bytes>
  Delete(storageKey: string)   -> void

Port: ColdStorageArchive        (cold-storage archival target for the retention "Archive" action — soft delete)
  ArchiveActivityRecords(activityRecordIds: list<uuid>) -> Result

Port: Clock                     (abstracted clock — every background worker depends on this so time-based logic is testable)
  UtcNow: datetime
```

For the exercise, `Infrastructure` may provide **stub adapters** for `ReportRenderer` (emit a trivial CSV/placeholder PDF), `ObjectStorage` and `ColdStorageArchive` (in-memory), and a real `Clock` (or a controllable fake in tests). Keep the port shapes exactly as above so real adapters drop in later.

> **No synchronous calls into other BCs.** Unlike BC-3 (which calls BC-1 synchronously for provisioning), Reporting calls **no other domain BC** — by design. It is purely event-fed. If a dashboard needs a fact, that fact must arrive as an event payload; if it doesn't, the answer is "propose a new event in [[Event_Catalog]]," not "add a synchronous call." This is the cleanest Conformist-read-model posture in the platform and is the point of the BC pedagogically.

### 9.3 Public API this module EXPOSES (in the `Contracts` surface)

Reporting is downstream of everyone; its public surface is intentionally minimal. The only realistic consumer is an admin UI shell or BC-1's admin console wanting a quick count.

```
Public API: ReportingPublicApi
  GetPlatformSummary() -> PlatformSummaryDto
      // lightweight platform-health summary for an admin landing page
  GetReportRunStatus(reportRunId: uuid) -> ReportRunStatusDto?
      // whether a given report run has finished — used by a UI poller after a generate request

PlatformSummaryDto {
  ActiveUsersNow: int, TotalRegistrations: int64, TotalPostings: int64,
  MatchSuccessRatePct: decimal, AsOfUtc: datetime
}
ReportRunStatusDto {
  ReportRunId: uuid, Status: string, RowCount: int?, ArtifactFormats: list<string>
}
```

---

## 10. Application layer

Three kinds of message handler live here:

1. **Commands** — the genuine write-side use cases over the five aggregates. Return `Result`/`Result<T>`. Validated via the validation step.
2. **Queries** — the dashboard/report-data reads over the read-model tables. Return DTOs. No domain logic — straight projections.
3. **Projectors** — integration-event handlers (one per consumed event type in §9.1) that update read models. Run inside the `IntegrationEventProjectionBehavior` (inbox dedupe + transaction).

Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

### 10.1 Commands

| Command | Story | Handler responsibilities |
|---|---|---|
| `CreateReportTemplateCommand` | US-3.5.4-02 | Validate spec against `MetricCatalog` → `ReportDefinition.CreateTemplate(...)` → persist. Requires a creation-capable role (§7.5). |
| `CreateCustomReportCommand` | US-3.5.4-01, US-3.1.4-04 AC-08 | `ReportDefinition.CreateCustom(...)` → persist. |
| `SaveCustomReportAsTemplateCommand` | US-3.5.4-01 AC-07 | Load definition → `SaveCustomAsTemplate(...)` → persist. `E-REPORT-NOT-CUSTOM` if not custom. |
| `UpdateReportDefinitionSpecCommand` | US-3.5.4-02 AC-06 | Load → `UpdateSpec(newSpec, changedBy)` → persist (new version appended). |
| `UpdateReportTemplateVisibilityCommand` | US-3.5.4-02 AC-05 | Load → `UpdateVisibility(...)` → persist. |
| `ArchiveReportDefinitionCommand` | US-3.5.4-02 | Load → `Archive()` → persist. |
| `GenerateReportCommand` | US-3.5.4-01, US-3.1.4-04 | Load definition → resolve & validate parameters → `ReportDataScopeFilter.ApplyRoleScope` → `ReportRun.Queue(...)` → persist (run is now `Queued`). Returns `ReportRunId` immediately; the report-execution worker picks it up. |
| `ExecuteReportRunCommand` *(internal — issued by the report-execution worker, not the API)* | US-3.5.4-01 | Load run → `MarkRunning()` → query the read models per the (scope-filtered) spec → mask fields per `ReportDataScopeFilter.MaskedFieldsFor` → for each requested format call `ReportRenderer.Render` → `ObjectStorage.Store` → `MarkCompleted(artifacts, rowCount)` → `definition.RecordUsage()` → persist. On any failure → `MarkFailed(reason)`. `ReportRunCompleted` → outbox `ReportGeneratedIntegrationEvent` (or `ScheduledReportRunIntegrationEvent` if `RunTrigger.Mode == Scheduled`). |
| `CreateReportScheduleCommand` | US-3.5.4-03 | Validate definition exists, distribution list non-empty → `ReportSchedule.Create(...)` → persist. Requires schedule-management role. |
| `UpdateReportScheduleCommand` | US-3.5.4-03 AC-07 | Load → `UpdateCadence` / `UpdateDistributionList` → persist. |
| `PauseReportScheduleCommand` / `ResumeReportScheduleCommand` | US-3.5.4-03 AC-07 | Load → `Pause()` / `Resume()` → persist. |
| `DeleteReportScheduleCommand` | US-3.5.4-03 AC-07 | Load → remove → persist. |
| `CreateRetentionPolicyCommand` | US-3.5.1-04 | `RetentionPolicy.Create(...)` → persist. |
| `ReviseRetentionPolicyCommand` | US-3.5.1-04 AC-05 | Load → `Revise(...)` (new version, effective date) → persist. |
| `ArchiveRetentionPolicyCommand` | US-3.5.1-04 | Load → `Archive()` → persist. |
| `ApplyRetentionPolicyCommand` *(internal — issued by the retention-purge job)* | US-3.5.1-04 AC-03 | Load policy → `RetentionCutoffCalculator.ComputeCutoff` → select matching `ActivityRecord`s older than cutoff → `Archive` via `ColdStorageArchive` or hard-delete per `Action` → `policy.RecordRun(...)` → persist. `RetentionApplied` → outbox `ActivityRetentionAppliedIntegrationEvent`. |
| `CreateAlertRuleCommand` | US-3.5.3-03 | `AlertRule.Create(...)` → persist. |
| `UpdateAlertRuleCommand` | US-3.5.3-03 AC-01/03 | Load → `UpdateCondition` / `UpdateSeverity` / `UpdateChannels` → persist. |
| `EnableAlertRuleCommand` / `DisableAlertRuleCommand` | US-3.5.3-03 | Load → `Enable()` / `Disable()` → persist. |
| `AcknowledgeAlertIncidentCommand` | US-3.5.3-03 AC-06 | Load rule → `AcknowledgeIncident(incidentId, byUserId)` → persist. |
| `SuppressAlertIncidentCommand` | US-3.5.3-03 AC-06 | Load → `SuppressIncident(incidentId, untilUtc)` → persist. |
| `EscalateAlertIncidentCommand` | US-3.5.3-03 AC-06 | Load → `EscalateIncident(incidentId)` → persist. `AlertIncidentEscalated` → outbox `PerformanceAlertRaisedIntegrationEvent`. |
| `EvaluateMetricForAlertsCommand` *(internal — issued by the anomaly-detection worker / metric projector)* | US-3.5.3-03 AC-02/04 | For a freshly-projected metric value: load enabled `AlertRule`s for the `MetricKey` → for each, test `AlertCondition` and (if enabled) `AnomalyDetector.Evaluate` → on breach `rule.Fire(...)` → persist. `AlertIncidentRaised` → outbox `PerformanceAlertRaisedIntegrationEvent`. |

### 10.2 Queries

| Query | Story | Returns |
|---|---|---|
| `GetUserActivityDashboardQuery` | US-3.5.1-01 | `UserActivityDashboardDto` — current active-login count, list of `SessionSnapshotDto` (name, user id, last login, session duration, concurrent count), auto-refresh interval. |
| `GetJobSeekerActivityReportQuery` | US-3.5.1-02 | `ActivityReportDto` — filtered by `DateRange` + optional user search (id/email/name); aggregated counts per `ActivityType`; paged `ActivityRecordDto` list. |
| `GetEmployerActivityReportQuery` | US-3.5.1-03 | `ActivityReportDto` — same shape, filtered to `ActorRole = Employer` + optional employer id / company name. |
| `GetActivityTimelineQuery` | US-3.5.1-02/03 AC-03/04 | `list<ActivityRecordDto>` — chronological drill-down for one user/employer over a `DateRange`. |
| `GetRetentionPoliciesQuery` / `GetRetentionAuditTrailQuery` | US-3.5.1-04 AC-04 | policy list with versions; `list<RetentionRunDto>` (what/when/why). |
| `GetEmploymentStatsDashboardQuery` | US-3.5.2-01 | `EmploymentStatsDto` — posting-volume time series, application metrics, hiring/time-to-fill metrics, for a `DateRange` + `Grain`; supports a comparison `DateRange` for YoY/PoP. |
| `GetIndustryAnalyticsQuery` | US-3.5.2-02 | `IndustryAnalyticsDto` — demand-vs-supply per selected industry, top positions, `SalaryStatRollup` breakdown by position + region. |
| `GetSkillDemandTrendsQuery` | US-3.5.2-03 | `SkillDemandDto` — ranked skills by `PostingCount`, emerging-skill flags, skill-gap (`PostingCount` / `CandidateSupplyCount`), geographic + industry cross-tab, growth-trend series. |
| `GetEmploymentOutcomesQuery` | US-3.5.2-04 | `EmploymentOutcomesDto` — placement funnel (applications → offers → acceptances), placement rate, career-progression cohort metrics; filterable by industry/location/skill. |
| `GetSystemPerformanceDashboardQuery` | US-3.5.3-01 | `SystemPerformanceDto` — response-time p50/p95/p99, error rate, CPU/memory/disk/network, DB metrics, threshold-breach flags, historical trend over a `DateRange`. |
| `GetMatchingPerformanceQuery` | US-3.5.3-02 | `MatchingPerformanceDto` — accuracy/precision/recall, satisfaction, conversion, broken down by `JobCategory`/`Industry`/`Region`/`SkillCode`, with A/B variant comparison. |
| `GetAlertRulesQuery` / `GetActiveAlertIncidentsQuery` | US-3.5.3-03 | configured rules; open incidents with state. |
| `GetPerformanceTrendReportQuery` | US-3.5.3-03 AC-07 | `PerformanceTrendDto` — capacity trend + linear forecast over a `DateRange`. |
| `GetReportTemplateLibraryQuery` | US-3.5.4-02, US-3.5.4-04 AC-01 | `list<ReportDefinitionSummaryDto>` — **filtered by the caller's role** via `ReportVisibility`; supports category filter + keyword search; includes `UsageCount` and creator metadata. |
| `GetReportDefinitionQuery` | US-3.5.4-01/02 | `ReportDefinitionDto` — full spec + configurable parameters + version history. Role-checked. |
| `GetReportRunQuery` | US-3.5.4-01 AC-05 | `ReportRunDto` — status, row count, artifact download links. Role-checked + logs a `ReportAccessLog` row on download. |
| `GetReportSchedulesQuery` | US-3.5.4-03 AC-07 | active/paused schedules with `NextRunOnUtc`, distribution list. |
| `GetReportAccessAuditQuery` | US-3.5.4-04 AC-06 | `list<ReportAccessLogDto>` — immutable view/download audit. |
| `GetAdminReportsMenuQuery` | US-3.1.4-04 AC-01 | `AdminReportsMenuDto` — the role-filtered menu of available report definitions with date-range selectors. |

### 10.3 Projectors (integration-event handlers)

One handler per consumed event type in §9.1. They are integration-event handlers registered alongside commands/queries. Each projector:

1. is wrapped by `IntegrationEventProjectionBehavior` — which first checks `inbox_messages` for `EventId`; if present, **return immediately** (no-op);
2. runs the typed mapping logic — typically: call `ActivityClassifier.Classify` to (maybe) produce an `ActivityRecord`, and/or upsert one or more rollup rows;
3. inserts the `inbox_messages` row and all read-model changes **in one transaction**;
4. if the projected metric value is alert-relevant, sends `EvaluateMetricForAlertsCommand` (in the same scope) so threshold/anomaly checks run.

Representative projectors: `JobPostingPublishedProjector` (the LMIS workhorse — feeds `posting.volume`, `SkillDemandRollup.PostingCount`, `SalaryStatRollup`, plus an `ActivityRecord`), `UserLoggedInProjector` (`SessionSnapshot` upsert + `ActivityRecord`), `ApplicationStatusChangedProjector` (`OutcomeCohortRollup` + `hire.count` + `time_to_fill`), `SystemMetricSampledProjector` (`SystemMetricBucket` upsert + alert evaluation), `MatchComputedProjector` (`MatchingMetricRollup`), `TaxonomyUpdatedProjector` (re-label rollup dimensions). The rest follow the §9.1 table mechanically.

### 10.4 Validators — representative rules

- `CreateReportTemplateCommand`: name non-empty ≤ 200; `Spec.Metrics` non-empty and every key in `MetricCatalog`; `Visibility.AllowedRoles` non-empty; caller role ∈ {SystemAdministrator, MoLAdministrator, DataAnalyst} (`E-REPORT-FORBIDDEN`).
- `GenerateReportCommand`: definition id present; every required configurable parameter supplied (`E-REPORT-MISSING-PARAM`); requested formats non-empty and ⊆ {Pdf, Xlsx, Csv}; `DateRange` filters have `start ≤ end`.
- `CreateReportScheduleCommand`: distribution list non-empty and all RFC-5322 valid; export formats non-empty; `ScheduleCadence` consistent (weekly ⇒ day-of-week; monthly/quarterly ⇒ day-of-month ∈ [1,28]); caller role ∈ {SystemAdministrator, MoLAdministrator} (`E-REPORT-FORBIDDEN`).
- `CreateRetentionPolicyCommand`: `RetentionDays > 0`; `WarningDays ≥ 0`; caller is SystemAdministrator.
- `ReviseRetentionPolicyCommand`: `effectiveFromUtc ≥ now`.
- `CreateAlertRuleCommand`: `MetricKey` non-empty and in `MetricCatalog`; `Channels` non-empty; caller is SystemAdministrator.
- All dashboard/report queries: `DateRange` valid; `Grain` in enum; page size ≤ 200.

### 10.5 DTOs

Plain data records in `Application`. Never expose Domain entities/VOs or read-model entities across the API. Map aggregate/read-model → DTO in the handler or a mapping helper. DTOs for masked reports carry already-masked values — masking happens before the DTO is built, never in the controller.

---

## 11. Persistence & data model

Schema/namespace: `reporting`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. The module-specific relational model follows.

### 11.1 Reference relational model — schema `reporting` (aggregates)

```
TABLE report_definitions
  id                       uuid        PK
  kind                     enum        NOT NULL                -- Template|Custom
  name                     string      NOT NULL
  description              string      NULL
  category                 enum        NOT NULL
  owner_user_id            uuid        NOT NULL                -- BC-1 identity, no FK
  spec                     json        NOT NULL                -- ReportSpec VO (current)
  configurable_parameters  json        NOT NULL DEFAULT '[]'   -- ConfigurableParameter[] VO
  visibility               json        NOT NULL                -- ReportVisibility VO (allowed roles)
  current_version_number   int         NOT NULL
  usage_count              int         NOT NULL DEFAULT 0
  status                   enum        NOT NULL                -- Active|Archived
  created_on_utc           datetime    NOT NULL
  updated_on_utc           datetime    NOT NULL
  version_token            (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  INDEX (category), INDEX (kind, status), INDEX (owner_user_id)

TABLE report_definition_versions
  id                  uuid        PK
  definition_id       uuid        NOT NULL                     -- FK → report_definitions.id ON DELETE CASCADE
  version_number      int         NOT NULL
  spec                json        NOT NULL
  is_current          bool        NOT NULL
  changed_by          uuid        NOT NULL
  created_on_utc      datetime    NOT NULL
  UNIQUE (definition_id, version_number)
  INDEX (definition_id) WHERE is_current = true

TABLE report_runs
  id                          uuid        PK
  report_definition_id        uuid        NOT NULL              -- FK → report_definitions.id
  definition_version_number   int         NOT NULL
  trigger                     json        NOT NULL              -- RunTrigger VO
  parameters                  json        NOT NULL              -- ResolvedParameters VO
  role_scope                  json        NOT NULL              -- RoleScope VO
  status                      enum        NOT NULL              -- Queued|Running|Completed|Failed
  row_count                   int         NULL
  failure_reason              string      NULL
  queued_on_utc               datetime    NOT NULL
  started_on_utc              datetime    NULL
  completed_on_utc            datetime    NULL
  version_token               (optimistic-concurrency token)
  INDEX (status), INDEX (report_definition_id), INDEX (queued_on_utc)

TABLE report_artifacts
  id                  uuid        PK
  report_run_id       uuid        NOT NULL                      -- FK → report_runs.id ON DELETE CASCADE
  format              enum        NOT NULL                      -- Pdf|Xlsx|Csv
  storage_key         string      NOT NULL
  original_file_name  string      NOT NULL
  mime_type           string      NOT NULL
  size_bytes          int64       NOT NULL
  generated_on_utc    datetime    NOT NULL

TABLE report_schedules
  id                     uuid        PK
  report_definition_id   uuid        NOT NULL                   -- FK → report_definitions.id
  cadence                json        NOT NULL                   -- ScheduleCadence VO
  parameters             json        NOT NULL                   -- ResolvedParameters VO
  distribution_list      json        NOT NULL                   -- EmailAddress[] VO
  export_formats         json        NOT NULL                   -- ExportFormat set
  status                 enum        NOT NULL                   -- Active|Paused
  owner_user_id          uuid        NOT NULL
  last_run_on_utc        datetime    NULL
  next_run_on_utc        datetime    NOT NULL
  created_on_utc         datetime    NOT NULL
  updated_on_utc         datetime    NOT NULL
  version_token          (optimistic-concurrency token)
  INDEX (next_run_on_utc) WHERE status = 'Active'

TABLE retention_policies
  id                       uuid        PK
  name                     string      NOT NULL
  scope                    json        NOT NULL                 -- RetentionScope VO
  retention_days           int         NOT NULL
  action                   enum        NOT NULL                 -- Archive|HardDelete
  warning_days             int         NOT NULL DEFAULT 7
  current_version_number   int         NOT NULL
  effective_from_utc       datetime    NOT NULL
  status                   enum        NOT NULL                 -- Active|Archived
  created_on_utc           datetime    NOT NULL
  updated_on_utc           datetime    NOT NULL
  version_token            (optimistic-concurrency token)

TABLE retention_policy_versions
  id                  uuid        PK
  policy_id           uuid        NOT NULL                      -- FK → retention_policies.id ON DELETE CASCADE
  version_number      int         NOT NULL
  scope               json        NOT NULL
  retention_days      int         NOT NULL
  action              enum        NOT NULL
  warning_days        int         NOT NULL
  effective_from_utc  datetime    NOT NULL
  changed_by          uuid        NOT NULL
  created_on_utc      datetime    NOT NULL
  UNIQUE (policy_id, version_number)

TABLE retention_runs                                            -- IMMUTABLE, append-only — the compliance audit trail
  id                       uuid        PK
  policy_id                uuid        NOT NULL                 -- FK → retention_policies.id
  policy_version_number    int         NOT NULL
  records_affected         int         NOT NULL
  action_taken             enum        NOT NULL
  cutoff_utc               datetime    NOT NULL
  executed_on_utc          datetime    NOT NULL
  INDEX (policy_id, executed_on_utc)

TABLE alert_rules
  id                          uuid        PK
  name                        string      NOT NULL
  metric_key                  string      NOT NULL
  condition                   json        NOT NULL              -- AlertCondition VO
  severity                    enum        NOT NULL              -- Critical|Warning|Info
  channels                    json        NOT NULL              -- AlertChannel set
  anomaly_detection_enabled   bool        NOT NULL DEFAULT false
  status                      enum        NOT NULL              -- Enabled|Disabled
  created_on_utc              datetime    NOT NULL
  updated_on_utc              datetime    NOT NULL
  version_token               (optimistic-concurrency token)
  INDEX (metric_key) WHERE status = 'Enabled'

TABLE alert_incidents
  id                     uuid        PK
  alert_rule_id          uuid        NOT NULL                   -- FK → alert_rules.id ON DELETE CASCADE
  triggered_on_utc       datetime    NOT NULL
  observed_value         decimal     NOT NULL
  trigger                enum        NOT NULL                   -- ThresholdBreach|Anomaly
  state                  enum        NOT NULL                   -- Raised|Acknowledged|Suppressed|Escalated
  acknowledged_by        uuid        NULL
  suppressed_until_utc   datetime    NULL
  state_changed_on_utc   datetime    NULL
  INDEX (alert_rule_id, state)
```

### 11.1.1 Reference relational model — read models (projection targets)

```
TABLE activity_records
  id                  uuid        PK
  user_id             uuid        NOT NULL                      -- actor; BC identity, no FK
  actor_role          enum        NOT NULL                      -- JobSeeker|Employer|Administrator|System
  activity_type       enum        NOT NULL
  occurred_on_utc     datetime    NOT NULL                       -- when the action happened (from event payload)
  target_type         string      NULL
  target_id           uuid        NULL
  metadata            json        NULL                           -- small free-form payload
  source_event_id     uuid        NOT NULL                       -- originating integration event EventId
  projected_on_utc    datetime    NOT NULL
  UNIQUE (source_event_id, activity_type)                        -- idempotency guard (an event may map to ≤1 record)
  INDEX (user_id, occurred_on_utc)
  INDEX (actor_role, activity_type, occurred_on_utc)
  INDEX (occurred_on_utc)                                        -- supports retention cutoff scans

TABLE session_snapshots
  user_id                       uuid        PK                  -- BC-1 identity, no FK
  display_name                  string      NOT NULL
  actor_role                    enum        NOT NULL
  last_login_on_utc             datetime    NULL
  last_session_duration_seconds int         NULL
  concurrent_session_count      int         NOT NULL DEFAULT 0
  is_currently_active           bool        NOT NULL DEFAULT false
  updated_on_utc                datetime    NOT NULL
  INDEX (is_currently_active) WHERE is_currently_active = true

TABLE analytics_rollups
  id                  uuid        PK
  metric              string      NOT NULL
  grain               enum        NOT NULL                       -- Day|Week|Month
  bucket_start_utc    datetime    NOT NULL
  industry            string      NULL
  occupation_code     string      NULL
  skill_code          string      NULL
  region              string      NULL
  employer_id         uuid        NULL
  value               decimal     NOT NULL DEFAULT 0
  sample_count        int         NOT NULL DEFAULT 0
  updated_on_utc      datetime    NOT NULL
  UNIQUE (metric, grain, bucket_start_utc, industry, occupation_code, skill_code, region, employer_id)
  INDEX (metric, grain, bucket_start_utc)

TABLE salary_stat_rollups
  id                  uuid        PK
  occupation_code     string      NOT NULL
  industry            string      NULL
  region              string      NULL
  grain               enum        NOT NULL
  bucket_start_utc    datetime    NOT NULL
  min                 decimal     NULL
  max                 decimal     NULL
  median              decimal     NULL
  p25                 decimal     NULL
  p75                 decimal     NULL
  currency            string      NOT NULL
  sample_count        int         NOT NULL DEFAULT 0
  UNIQUE (occupation_code, industry, region, grain, bucket_start_utc)

TABLE skill_demand_rollups
  id                       uuid        PK
  skill_code               string      NOT NULL
  industry                 string      NULL
  region                   string      NULL
  grain                    enum        NOT NULL
  bucket_start_utc         datetime    NOT NULL
  posting_count            int         NOT NULL DEFAULT 0
  candidate_supply_count   int         NOT NULL DEFAULT 0
  growth_rate_pct          decimal     NULL
  is_emerging              bool        NOT NULL DEFAULT false
  UNIQUE (skill_code, industry, region, grain, bucket_start_utc)
  INDEX (skill_code, grain, bucket_start_utc)

TABLE system_metric_buckets
  id                  uuid        PK
  metric_key          string      NOT NULL
  bucket_start_utc    datetime    NOT NULL
  bucket_seconds      int         NOT NULL                       -- 60 detailed | 86400 aggregated
  p50                 decimal     NULL
  p95                 decimal     NULL
  p99                 decimal     NULL
  avg                 decimal     NULL
  count               int64       NOT NULL DEFAULT 0
  min                 decimal     NULL
  max                 decimal     NULL
  UNIQUE (metric_key, bucket_start_utc, bucket_seconds)
  INDEX (metric_key, bucket_start_utc)

TABLE matching_metric_rollups
  id                     uuid        PK
  bucket_start_utc       datetime    NOT NULL
  grain                  enum        NOT NULL                    -- Month
  job_category           string      NULL
  industry               string      NULL
  region                 string      NULL
  skill_code             string      NULL
  recommendation_count   int         NOT NULL DEFAULT 0
  selected_count         int         NOT NULL DEFAULT 0
  accuracy               decimal     NULL
  precision_score        decimal     NULL                        -- "precision" can be reserved; column named precision_score
  recall                 decimal     NULL
  avg_satisfaction       decimal     NULL
  application_rate       decimal     NULL
  offer_rate             decimal     NULL
  ab_test_variant        string      NULL
  UNIQUE (bucket_start_utc, grain, job_category, industry, region, skill_code, ab_test_variant)

TABLE outcome_cohort_rollups
  id                          uuid        PK
  grain                       enum        NOT NULL
  bucket_start_utc            datetime    NOT NULL
  industry                    string      NULL
  region                      string      NULL
  skill_code                  string      NULL
  application_count           int         NOT NULL DEFAULT 0
  offer_count                 int         NOT NULL DEFAULT 0
  acceptance_count            int         NOT NULL DEFAULT 0
  placement_rate_pct          decimal     NULL
  career_progression_count    int         NOT NULL DEFAULT 0
  UNIQUE (grain, bucket_start_utc, industry, region, skill_code)

TABLE report_access_logs                                          -- IMMUTABLE, write-once — US-3.5.4-04 AC-06
  id                  uuid        PK
  user_id             uuid        NOT NULL
  role                string      NOT NULL
  report_definition_id uuid       NULL
  report_run_id       uuid        NULL
  action              enum        NOT NULL                       -- Viewed|Downloaded
  occurred_on_utc     datetime    NOT NULL
  INDEX (user_id, occurred_on_utc)
  INDEX (report_definition_id)

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

### 11.2 Module-specific mapping notes

- The **five aggregate roots** and their child entities/collections are mapped by the chosen ORM. Child collections (`report_definition_versions`, `report_artifacts`, `retention_policy_versions`, `retention_runs`, `alert_incidents`) are **owned** by their root and loaded with it.
- The **read-model entities** (§5.6) are mapped as plain entities with no `AggregateRoot` base. They have no domain events. Projectors mutate them directly.
- Scalar columns (`metric`, `grain`, `bucket_start_utc`, dimension columns, `metric_key`, `status`, `category`, `source_event_id`) are flattened out of `json` because they are queried, indexed, or uniquely constrained.
- **Rollup upserts:** projectors use an "insert-or-update" upsert keyed on the table's `UNIQUE` constraint that adds the increment to the existing value (`value = current + delta`, `sample_count = current + 1`). Combined with the inbox guard in the same transaction, this is the idempotency mechanism — see §13.
- Optimistic-concurrency tokens are required on the five aggregate tables (`report_definitions`, `report_runs`, `report_schedules`, `retention_policies`, `alert_rules`). Read-model tables do **not** need a concurrency token — they are single-writer (their projector) and the inbox serialises re-delivery.
- **Immutability enforcement:** `retention_runs` and `report_access_logs` are append-only at the application level (no update/delete repository methods exist for them). For defence in depth, the migration may add a `BEFORE UPDATE OR DELETE` trigger that raises an exception, or a DB role grant that withholds `UPDATE`/`DELETE`.
- Every projector records the consumed event's `EventId` in the `inbox_messages` row in the *same transaction* as the read-model write; the `IntegrationEventProjectionBehavior` skips already-processed ids.

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

- `ReportDefinitionRepository` (`GetById`, `GetByIdWithVersions`, `ListByCategoryForRole`, `Add`, `Update`).
- `ReportRunRepository` (`GetById`, `GetQueued`, `Add`, `Update`).
- `ReportScheduleRepository` (`GetById`, `GetDueForRun(nowUtc)`, `Add`, `Update`, `Remove`).
- `RetentionPolicyRepository` (`GetById`, `GetActive`, `Add`, `Update`).
- `AlertRuleRepository` (`GetById`, `GetEnabledByMetricKey`, `Add`, `Update`).
- Read-model "repositories" are thin query services, not aggregate repositories: `ActivityReadStore`, `AnalyticsReadStore`, `PerformanceReadStore`, `ReportAccessLogStore` — expose query methods and the projector upsert helpers.
- `UnitOfWork` (`SaveChanges`).

### 11.4 Background workers / scheduled jobs

Listed in §3 module-specific notes. Each is a long-running component registered in the module composition entry point: outbox relay, report-execution worker (polls queued runs), scheduled-report runner (fires due `ReportSchedule`s), retention-purge job (daily — emits pre-purge warning then runs `ApplyRetentionPolicyCommand`), anomaly-detection worker (rebuilds 30-day baselines and evaluates).

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. Route prefix `/api/reporting`. **All endpoints require a valid access token** (issued by BC-1); the authenticated `UserId` **and role claims** are taken from the token. Every endpoint that returns report/analytics data enforces role authorization (§7.5) — a forbidden role gets `403 E-REPORT-FORBIDDEN`. Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

| Verb & route | Command/Query | Success | Notable failures |
|---|---|---|---|
| `GET /api/reporting/activity/dashboard` | `GetUserActivityDashboardQuery` | `200` + `UserActivityDashboardDto` | `403` |
| `GET /api/reporting/activity/job-seekers` | `GetJobSeekerActivityReportQuery` | `200` + `ActivityReportDto` | `403`, `400` bad date range |
| `GET /api/reporting/activity/employers` | `GetEmployerActivityReportQuery` | `200` + `ActivityReportDto` | `403` |
| `GET /api/reporting/activity/{userId}/timeline` | `GetActivityTimelineQuery` | `200` + timeline | `403`, `404` |
| `GET /api/reporting/retention/policies` | `GetRetentionPoliciesQuery` | `200` + policies | `403` |
| `POST /api/reporting/retention/policies` | `CreateRetentionPolicyCommand` | `201` + id | `403`, `400` |
| `PUT /api/reporting/retention/policies/{id}` | `ReviseRetentionPolicyCommand` | `200` | `403`, `404`, `400` effective date in past |
| `DELETE /api/reporting/retention/policies/{id}` | `ArchiveRetentionPolicyCommand` | `204` | `403`, `404` |
| `GET /api/reporting/retention/audit` | `GetRetentionAuditTrailQuery` | `200` + `RetentionRunDto[]` | `403` |
| `GET /api/reporting/lmis/employment-stats` | `GetEmploymentStatsDashboardQuery` | `200` + `EmploymentStatsDto` | `403`, `400` |
| `GET /api/reporting/lmis/industry-analytics` | `GetIndustryAnalyticsQuery` | `200` + `IndustryAnalyticsDto` | `403` |
| `GET /api/reporting/lmis/skill-demand` | `GetSkillDemandTrendsQuery` | `200` + `SkillDemandDto` | `403` |
| `GET /api/reporting/lmis/employment-outcomes` | `GetEmploymentOutcomesQuery` | `200` + `EmploymentOutcomesDto` | `403` |
| `GET /api/reporting/performance/system` | `GetSystemPerformanceDashboardQuery` | `200` + `SystemPerformanceDto` | `403` |
| `GET /api/reporting/performance/matching` | `GetMatchingPerformanceQuery` | `200` + `MatchingPerformanceDto` | `403` |
| `GET /api/reporting/performance/trend-report` | `GetPerformanceTrendReportQuery` | `200` + `PerformanceTrendDto` | `403` |
| `GET /api/reporting/alerts/rules` | `GetAlertRulesQuery` | `200` + rules | `403` |
| `POST /api/reporting/alerts/rules` | `CreateAlertRuleCommand` | `201` + id | `403`, `400` |
| `PUT /api/reporting/alerts/rules/{id}` | `UpdateAlertRuleCommand` | `200` | `403`, `404` |
| `POST /api/reporting/alerts/rules/{id}/enable` | `EnableAlertRuleCommand` | `200` | `403`, `404` |
| `POST /api/reporting/alerts/rules/{id}/disable` | `DisableAlertRuleCommand` | `200` | `403`, `404` |
| `GET /api/reporting/alerts/incidents` | `GetActiveAlertIncidentsQuery` | `200` + incidents | `403` |
| `POST /api/reporting/alerts/incidents/{id}/acknowledge` | `AcknowledgeAlertIncidentCommand` | `200` | `403`, `404`, `409` not in `Raised` |
| `POST /api/reporting/alerts/incidents/{id}/suppress` | `SuppressAlertIncidentCommand` | `200` | `403`, `404` |
| `POST /api/reporting/alerts/incidents/{id}/escalate` | `EscalateAlertIncidentCommand` | `200` | `403`, `404` |
| `GET /api/reporting/reports/menu` | `GetAdminReportsMenuQuery` | `200` + `AdminReportsMenuDto` | `403` |
| `GET /api/reporting/reports/templates` | `GetReportTemplateLibraryQuery` | `200` + role-filtered list | `403` |
| `POST /api/reporting/reports/templates` | `CreateReportTemplateCommand` | `201` + id | `403 E-REPORT-FORBIDDEN`, `400` |
| `GET /api/reporting/reports/definitions/{id}` | `GetReportDefinitionQuery` | `200` + `ReportDefinitionDto` | `403`, `404` |
| `PUT /api/reporting/reports/definitions/{id}/spec` | `UpdateReportDefinitionSpecCommand` | `200` | `403`, `404`, `400` |
| `PUT /api/reporting/reports/definitions/{id}/visibility` | `UpdateReportTemplateVisibilityCommand` | `200` | `403`, `404` |
| `POST /api/reporting/reports/definitions/{id}/archive` | `ArchiveReportDefinitionCommand` | `204` | `403`, `404` |
| `POST /api/reporting/reports/custom` | `CreateCustomReportCommand` | `201` + id | `403`, `400` |
| `POST /api/reporting/reports/custom/{id}/save-as-template` | `SaveCustomReportAsTemplateCommand` | `200` | `403`, `404`, `409 E-REPORT-NOT-CUSTOM` |
| `POST /api/reporting/reports/{definitionId}/generate` | `GenerateReportCommand` | `202` + `ReportRunId` | `403`, `404`, `400 E-REPORT-MISSING-PARAM` |
| `GET /api/reporting/reports/runs/{runId}` | `GetReportRunQuery` | `200` + `ReportRunDto` (artifact links) | `403`, `404` |
| `GET /api/reporting/reports/runs/{runId}/artifacts/{format}` | (download — streams via `ObjectStorage`) | `200` + file; **logs a `ReportAccessLog` `Downloaded` row** | `403`, `404`, `409` run not completed, `422 E-REPORT-ROW-LIMIT` (if a CSV/XLSX exceeded the cap during generation) |
| `POST /api/reporting/schedules` | `CreateReportScheduleCommand` | `201` + id | `403 E-REPORT-FORBIDDEN`, `400` |
| `GET /api/reporting/schedules` | `GetReportSchedulesQuery` | `200` + schedules | `403` |
| `PUT /api/reporting/schedules/{id}` | `UpdateReportScheduleCommand` | `200` | `403`, `404` |
| `POST /api/reporting/schedules/{id}/pause` | `PauseReportScheduleCommand` | `200` | `403`, `404` |
| `POST /api/reporting/schedules/{id}/resume` | `ResumeReportScheduleCommand` | `200` | `403`, `404` |
| `DELETE /api/reporting/schedules/{id}` | `DeleteReportScheduleCommand` | `204` | `403`, `404` |
| `GET /api/reporting/reports/access-audit` | `GetReportAccessAuditQuery` | `200` + `ReportAccessLogDto[]` | `403` (SystemAdministrator / Auditor only) |

**Note:** there are **no API endpoints for projecting events** — projection is internal, driven by the message-bus subscription, not HTTP. There are also no endpoints that mutate read-model rows directly; read models change only via projectors.

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** every VO factory — valid input succeeds; each invalid case returns the right `Error`. Specifically: `ReportSpec` (empty metrics fails; unknown metric key fails); `ReportFilter` (`Between` without exactly 2 values fails); `ScheduleCadence` (weekly without day-of-week fails; monthly with day-of-month 0 or 29 fails); `RoleScope` (`EmployerOwner` without `EmployerId` fails); `RunTrigger` (`OnDemand` without `UserId` fails); `AlertCondition`, `EmailAddress`, `DateRange` (`start > end` fails).
- **ReportDefinition aggregate:** `CreateTemplate` vs `CreateCustom` set `Kind`/`Category` correctly; `SaveCustomAsTemplate` fails (`E-REPORT-NOT-CUSTOM`) on a template; `UpdateSpec` appends a version, marks exactly one current, archives versions older than the last 5; invariant — always exactly one current version.
- **ReportRun aggregate:** status machine — every legal transition succeeds, every illegal one fails (`Completed → Running`, `Failed → Completed`, etc.); `Queue` fails when a required configurable parameter is missing; `MarkCompleted` produces exactly one artifact per requested format; CSV/XLSX over 100k rows → `E-REPORT-ROW-LIMIT`; `DefinitionVersionNumber` is fixed at queue time.
- **ReportSchedule aggregate:** `Create` computes `NextRunOnUtc` in the future; `Pause`/`Resume` machine; `RecordRun` advances `NextRunOnUtc` past `SkipDates`.
- **RetentionPolicy aggregate:** `Revise` appends a version and the effective-from windows are non-overlapping and gap-free; `RecordRun` appends an immutable `RetentionRun` (assert no API exists to mutate one); `RecordRun` with `RecordsAffected = 0` still records; archived policy rejects further runs.
- **AlertRule aggregate:** `Fire` only when `Enabled`; de-dup invariant — a second `Fire` while an unresolved incident exists does **not** append a new incident; `AcknowledgeIncident` only from `Raised`; `Disabled` rule never fires.
- **Domain services:** `MetricCatalog` — known/unknown discrimination; `RetentionCutoffCalculator` — table-driven cases proving `cutoff = now - retentionDays` and the warning-date offset; `ScheduleNextRunCalculator` — daily/weekly/monthly/quarterly next-instant cases, skip-date handling; `AnomalyDetector` — value within 3σ is not an anomaly, beyond 3σ is; `ReportDataScopeFilter` — `EmployerOwner` gets an `EmployerId` filter injected and salary/name fields masked; `DataAnalyst`/`MoLAdministrator` get neither; `ActivityClassifier` — a `SearchPerformed` from a seeker classifies as `JobSearch`, from an employer as `CandidateSearch`; events that are not activity-worthy return "no record".

### 13.2 Unit tests — Application (handlers & projectors, ports replaced with test doubles)

- `GenerateReportCommand`: happy path queues a run with the scope-filtered spec and pinned definition version; missing required parameter → `E-REPORT-MISSING-PARAM`, no run persisted; a forbidden role → `E-REPORT-FORBIDDEN`.
- `ExecuteReportRunCommand`: queries read models, applies masking, renders one artifact per format via the test-double `ReportRenderer`, stores via test-double `ObjectStorage`, marks completed, increments definition `UsageCount`, queues `ReportGeneratedIntegrationEvent` (or `ScheduledReportRunIntegrationEvent` for a scheduled trigger) to the outbox; a renderer failure → run `Failed`, no artifact, `ReportRunFailed` raised.
- `ApplyRetentionPolicyCommand`: selects only `ActivityRecord`s older than the computed cutoff; `Archive` action calls `ColdStorageArchive` then deletes; `HardDelete` deletes directly; a `RetentionRun` is recorded with the correct `RecordsAffected`; `ActivityRetentionAppliedIntegrationEvent` queued to outbox.
- `EvaluateMetricForAlertsCommand`: a metric value breaching an enabled rule's threshold fires an incident and queues `PerformanceAlertRaisedIntegrationEvent`; a value within threshold does nothing; a disabled rule is skipped; a second breach while an incident is unresolved does not double-fire.
- **Projector idempotency (critical):** for a representative projector (`JobPostingPublishedProjector`), delivering the **same event twice** must (a) insert exactly one `ActivityRecord`, (b) increment `analytics_rollups.value` exactly once, (c) the second delivery is a no-op because the inbox row exists. Assert this for at least: `JobPostingPublishedProjector`, `UserLoggedInProjector`, `ApplicationStatusChangedProjector`, `SystemMetricSampledProjector`.
- `UserLoggedInProjector`: upserts `SessionSnapshot` — first login creates the row, a second concurrent login increments `ConcurrentSessionCount`, sets `IsCurrentlyActive`.
- `ActivityClassifier` wired into a projector: a `SearchPerformed` event becomes the correct `ActivityType` based on actor role.
- Validation step: each validator rejects the documented bad inputs before the handler runs.

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip each of the five aggregates including child collections and `json` VOs; optimistic-concurrency conflict on `report_runs` is detected.
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `reporting`.
- **Outbox:** completing a `ReportRun` writes both the run change and the `ReportGeneratedIntegrationEvent` outbox row in one transaction; rolling back leaves neither.
- **Inbox / idempotency:** delivering `JobPostingPublishedIntegrationEvent` twice produces exactly one `activity_records` row and increments the `analytics_rollups` row exactly once; the `inbox_messages` row is present after the first delivery.
- **Rollup upsert:** two *different* `JobPostingPublished` events in the same (industry, day) bucket sum into one `analytics_rollups` row with `value = 2`.
- **Immutability:** attempting to update or delete a `retention_runs` or `report_access_logs` row fails (no repository method exists; if a DB trigger is added, it raises).
- **Retention purge end-to-end:** seed `activity_records` spanning before and after a cutoff, run the retention-purge job's command, assert only the pre-cutoff records are archived/deleted and a `RetentionRun` is recorded with the right count.
- **Scheduled report end-to-end:** an `Active` schedule with `next_run_on_utc` in the past is picked up, generates a run, emits `ScheduledReportRunIntegrationEvent`, and `next_run_on_utc` advances.
- **API:** host-level tests for: create-template → generate → poll-run-status → download-artifact (asserts a `ReportAccessLog` `Downloaded` row); a `DataAnalyst` access token can read LMIS dashboards but gets `403` creating a schedule; an `EmployerOwner` generating a report sees only their org's data (scope filter applied) and masked salary fields.
- **Consumed events:** drive the module's subscription with a representative slice of the §9.1 feed and assert the activity dashboard, an LMIS rollup, and a performance bucket all reflect the projected events.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "Project a published job posting into LMIS analytics"

This is the **defining slice of the BC** — an inbound integration event becoming read-model state. Pattern-match every other projector against it.

1. **Subscription.** The host's message bus delivers a `JobPostingPublishedIntegrationEvent { EventId, PostingId, EmployerId, Title, Requirements (industry, occupationCode, region, requiredSkills[], salaryRange?), OccurredOnUtc }` to the `Reporting` module (wired in the module composition entry point). It is dispatched via the mediator as an integration-event notification.
2. **Projection behavior (inbox guard).** `IntegrationEventProjectionBehavior` opens a transaction and checks `inbox_messages` for `EventId`. If found → **return immediately** (idempotent no-op). If not found → continue.
3. **Projector.** `JobPostingPublishedProjector`:
   a. Calls `ActivityClassifier.Classify(event)` → an `ActivityRecord { UserId = EmployerId, ActorRole = Employer, ActivityType = JobPostingPublished, TargetType = "JobPosting", TargetId = PostingId, OccurredOnUtc, SourceEventId = EventId }`. Insert it (the `UNIQUE (source_event_id, activity_type)` index is the belt-and-braces guard).
   b. Derives dimensions from `Requirements`: `industry`, `occupationCode`, `region`. For grain `Day`/`Week`/`Month`, upserts `analytics_rollups` for metric `posting.volume` — an upsert on `(metric, grain, bucket_start_utc, industry, occupation_code, skill_code, region, employer_id)` that adds 1 to `value` and 1 to `sample_count`.
   c. For each skill in `requiredSkills[]`, upserts `skill_demand_rollups` incrementing `posting_count`.
   d. If `salaryRange` is present, feeds `salary_stat_rollups` (recompute min/max/median/percentiles for the bucket — or stage raw observations and recompute on read; the package allows either, document the choice).
4. **Inbox write — same transaction.** Insert `inbox_messages { EventId, EventType, ProcessedOnUtc }`. All read-model writes + the inbox row commit together. If anything throws, the whole transaction rolls back and the event will be re-delivered later — safely, because nothing was persisted.
5. **Alert check (if applicable).** `posting.volume` is not an alert metric, so nothing further. (For a `SystemMetricSampled` event, step 5 would dispatch an `EvaluateMetricForAlertsCommand` in the same scope.)
6. **Result.** No HTTP response — this is event-driven. The next time an MoL Administrator opens the Employment Statistics dashboard, `GetEmploymentStatsDashboardQuery` reads the now-updated `analytics_rollups` and the posting shows up in the volume time series.

**Contrast slice — a genuine command:** "Generate a custom report" follows the exemplar command pattern instead: `POST /api/reporting/reports/{definitionId}/generate` → `GenerateReportCommand` validator → `GenerateReportCommandHandler` (load definition, resolve params, `ReportDataScopeFilter.ApplyRoleScope`, `ReportRun.Queue`, persist) → returns `202 + ReportRunId`. The report-execution worker then asynchronously issues `ExecuteReportRunCommand`, which queries read models, masks, renders, stores, `MarkCompleted`, and the outbox behavior emits `ReportGeneratedIntegrationEvent` for BC-9.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.1.4-04 View admin reports | Role-filtered reports menu with date selectors; job-postings / user-registrations / search-trends / user-interactions / system-metrics reports each backed by `analytics_rollups`; custom report builder; scheduled automated reports emailed (via BC-9); export as CSV/XLSX/PDF. |
| US-3.5.1-01 User activity dashboard | Current active-login count from `session_snapshots`; last-login + session-duration per active user; profile-navigation links (return user ids/names only — no extra PII); configurable auto-refresh (default 30 s); concurrent-session count accurate. |
| US-3.5.1-02 Track job-seeker activities | Activity stream captures profile views, job searches, applications, saved jobs; date-range filtering; per-user drill-down timeline; search by id/email/name; aggregated counts per `ActivityType`. |
| US-3.5.1-03 Track employer activities | Job-posting create/edit/publish, candidate searches (with criteria + result counts in `Metadata`), application reviews tracked; filter by employer id / company name; chronological timeline; volume metrics. |
| US-3.5.1-04 Activity retention policy | Configurable retention days; different periods per `ActorRole`/`ActivityType` via `RetentionScope`; automatic purge/archive on schedule; immutable `RetentionRun` audit trail (what/when/policy version); policy versioning with effective dates; 7-day pre-purge warning to admins. |
| US-3.5.2-01 Employment stats dashboard | Posting-volume time series (day/week/month); application metrics (total, per-posting, trend); hiring + time-to-fill metrics; date-range / preset selection; drill-down to industry/location/position; YoY/PoP comparison mode. |
| US-3.5.2-02 Industry analytics | Industry filter; demand-vs-supply visualization (postings vs candidate supply); position-level drill-down; salary-range analytics (min/max/median/p25/p75) by position; salary variation by region; historical trend comparison. |
| US-3.5.2-03 Skill demand trends | Skills ranked by posting frequency; emerging-skill detection (>25% YoY growth, configurable); skill-gap analysis (demand/supply ratio); geographic skill distribution; industry × skill cross-reference; per-skill growth-trend chart. |
| US-3.5.2-04 Employment outcomes | Placement funnel (applications → offers → acceptances); placement-rate calculation by period; career-progression cohort analysis; filter by industry/location/skill; government-stakeholder report generation (anonymised/aggregated); report scheduling for distribution. |
| US-3.5.3-01 System performance dashboard | Response-time avg/p95/p99; CPU/memory/disk/network utilisation; error rate + types; DB query times + slow-query counts; threshold-breach visual indicators; historical trend over a selected range with baseline comparison. |
| US-3.5.3-02 Matching performance | Accuracy (matches selected / total recommendations); precision + recall; average satisfaction score; conversion (application rate, offer rate from recommendations); breakdown by job category/industry/location/skill; A/B variant comparison. |
| US-3.5.3-03 Performance alerts | Configurable thresholds per metric; alert severity levels; email + in-app notification delivery (via BC-9); anomaly detection vs 30-day baseline; 2-year historical retention for trend analysis; acknowledge / suppress / escalate incident lifecycle; capacity-trend report with linear forecast. |
| US-3.5.4-01 Generate custom report | Report builder (metrics, dimensions, filters, visualization); metric + dimension + filter selection; visualization types (table/bar/line/pie/heatmap); async generation with preview; export to PDF/XLSX/CSV; save a custom report as a reusable template. |
| US-3.5.4-02 Define report templates | Template creation with default parameters; mark fields configurable; template library with metadata (creator, date, usage count); search + categorisation; role-based visibility; template versioning (current + previous 5); usage tracking. |
| US-3.5.4-03 Schedule recurring reports | Select template + frequency (daily/weekly/monthly/quarterly); day/time + skip-dates; distribution list; export-format selection; automated execution + email delivery (via BC-9); delivery status tracking (send/bounce, from BC-9 events); edit/pause/resume/delete schedules; preview before scheduling. |
| US-3.5.4-04 Report access controls | Role-based report + template visibility; role-based template access; data-level filtering by role (Employer Owner → own org only); creation restricted to admin/analyst roles; schedule management restricted to admin roles; immutable audit log of every report view/download; data masking of sensitive fields (salary, applicant names) by role. |

---

## Appendix — teaching notes & open questions

- **Core in value, conformist in posture.** Reporting is one of the two CORE subdomains, yet it sits at the very bottom of the dependency graph — it dictates nothing and conforms to everyone. This is the single richest discussion the BC offers: *can a subdomain be strategically core while being structurally the most downstream?* Contrast with BC-7, core **and** upstream-coupled via Partnership.
- **The absorbed Audit & Activity BC — simplification or smell?** The 12-BC model folds the old "Audit & Activity" context into Reporting (see [[BC_Mapping]] migration notes). This package handles it as "two modules' worth of code in one BC" (distinct aggregates, distinct schema groups, distinct projectors, one team). Ask the class: was this a clean simplification, or did it create a BC that quietly does two jobs? When would you split it back out?
- **Read model vs. aggregate — the cleanest example on the platform.** Most of this BC's tables are **projection rows with no behavior and no invariants** (`ActivityRecord`, the rollups, `SystemMetricBucket`). Only five things are genuine aggregates (`ReportDefinition`, `ReportRun`, `ReportSchedule`, `RetentionPolicy`, `AlertRule`). This is a great teaching contrast: not every table is an aggregate; a read model is a first-class architectural element, not a "lesser" entity.
- **Conform at the edge, own your model inside.** The `ActivityClassifier` + per-event projectors are where eleven foreign vocabularies get translated into Reporting's own `ActivityType` / rollup schema. This is the Conformist pattern done responsibly — and the answer to [[Context_Map]] insight #5's warning about Reporting becoming a coupling bottleneck. Discuss: what breaks if you skip the classifier and let projectors read upstream payloads directly?
- **Idempotency is not optional here.** This BC consumes ~50 event types, any of which can be re-delivered. The two-layer guard (inbox dedupe + additive-idempotent upserts, both in one transaction) is mandatory. A good exercise: walk through what double-counting looks like if you forget the inbox row, and why the `UNIQUE (source_event_id, activity_type)` index on `activity_records` is a belt-and-braces second line.
- **Catalog gaps flagged (propose to add to [[Event_Catalog]]):**
  1. **No `UserLoggedOut` / session-end event** — `SessionSnapshot.IsCurrentlyActive` and session duration are best-effort (idle timeout heuristic). A real session-end event would make the activity dashboard exact.
  2. **No `SystemMetricSampled` event** — system-performance dashboards (`US-3.5.3`) need an infrastructure/APM telemetry feed; it is not a business-BC domain event. This package treats it as an external feed into the same inbox. Discuss whether a "Telemetry" concern deserves its own context (it was considered and rejected during BC selection — see [[Event_Catalog]] open question #3).
  3. **No dedicated performance-alert event for BC-10** — this package adds `PerformanceAlertRaisedIntegrationEvent` so BC-9 can deliver alerts; it should be added to the catalog.
- **Where does "matching accuracy" really live?** Reporting computes accuracy/precision/recall from BC-7's emitted events plus BC-5's application/offer events. But BC-7 also has its *own* internal model-quality metrics. Two sources of "how good is matching?" Discuss: is the LMIS-facing metric (derived from outcomes) the same thing as the ML-facing metric (derived from training)? Should they ever be reconciled, and whose number is "the truth"?
- **Async report generation as an aggregate lifecycle.** `ReportRun` is a separate aggregate precisely because report generation is long-running and can fail independently — same reasoning the exemplar (BC-3) used to split `Resume` from `JobSeekerProfile`. Good reinforcement of "when does a multi-step, independently-failing process deserve its own aggregate?"
- **Retention vs. analytics — the data that survives a purge.** `US-3.5.1-04` retention purges *raw* `ActivityRecord`s, but explicitly **not** anonymised/aggregated rollups. So the LMIS analytics (built from rollups) outlive the activity logs they were derived from. Discuss the privacy/compliance reasoning: aggregate ≠ personal data, and why the projection-then-purge order matters.
- **Localization & data quality.** Per story assumptions: outcome data is missing for 20–40% of placements (`US-3.5.2-04`), so outcome metrics are deliberately conservative; activity tracking has no pre-deployment backfill (`US-3.5.1-02`). Surface these as honest caveats in the dashboards rather than pretending the data is complete — a good lesson in modeling *known unknowns*.
