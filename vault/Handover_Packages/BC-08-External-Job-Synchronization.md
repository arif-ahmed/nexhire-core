---
title: "Handover Package — BC-8 External Job Synchronization"
type: handover-package
bc_id: BC-8
bc_name: External Job Synchronization
bc_class: supporting (ACL)
stack: "language-neutral — see [[00-Shared-Foundations]] (Target Stack declared there)"
generated: 2026-05-15
related:
  - "[[00-Shared-Foundations]]"
  - "[[BC_Mapping]]"
  - "[[Context_Map]]"
  - "[[Event_Catalog]]"
tags:
  - handover-package
  - bc/external-job-sync
---

# Handover Package — BC-8 External Job Synchronization

> **Audience:** an AI coding agent. This package owns the **domain design** for the `ExternalJobSync` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `ExternalJobSync`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `external_job_sync`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

**This BC is unusually large (20 stories).** It is organised internally as **three modules-within-the-module** — keep them in separate folders/namespaces but ship them as one deployable module with one schema:

- **`PartnerJobSync`** — inbound ingestion + outbound export of jobs to/from external job portals (ACL toward BC-4 Job Postings). Stories: US-3.1.3-01..04, US-3.4.1-01..07.
- **`GovernmentVerification`** — identity / education / employer verification against government registries (MoL, PEF, ID services, institutions). ACL toward BC-3, BC-2, BC-1. Stories: US-3.4.2-01..05.
- **`PublicApiFramework`** — the Open Host Service: the versioned public REST API, OpenAPI docs, the published-language contract third-party integrators consume. Stories: US-3.4.3-01, -02, -03, -05.

The three sub-modules share the shared-kernel building blocks ([[00-Shared-Foundations]] §5), the `external_job_sync` schema, and the outbox/inbox infrastructure, but each has its own aggregates and is independently testable.

---

## 1. Purpose & scope boundaries

### What this BC is for

External Job Synchronization is the platform's **boundary with the outside world**. It plays **two strategic roles at once**:

1. **Anti-Corruption Layer (ACL)** for everything coming *in* — foreign job-portal data and government-registry data are translated into the platform's own ubiquitous language *before* they touch BC-4, BC-3, BC-2, or BC-1. Without this layer, foreign field naming and foreign data shapes would corrupt those contexts.
2. **Open Host Service (OHS)** for everything going *out* — a stable, versioned public REST API that third-party integrators consume, backed by a published-language contract.

It is a **supporting** subdomain. It is plumbing — necessary, but not the platform's competitive core. It folds in what was once a separate "Government Interfaces" BC.

### In scope

The `ExternalJobSync` module is responsible for:

- **Partner registration** — onboarding third-party job portals, issuing/rotating/revoking API keys, IP whitelisting, per-portal rate limits, key expiration.
- **Inbound ingestion** — pulling jobs from approved external portals on a schedule, accepting pushed jobs via the public API, receiving real-time webhook updates; deduplicating by external ID; quarantining incomplete jobs.
- **Outbound export** — pushing platform jobs to configured external portals on publish or in scheduled batches; tracking export status; retrying failures.
- **Data mapping & transformation** — per-portal mapping profiles that normalise heterogeneous foreign job formats into the platform's posting schema (and back out), supporting multiple foreign schema versions.
- **Sync monitoring & reconciliation** — integration dashboards, submission logs, sync-event logs, failed-sync queue, manual retry/override.
- **Government verification** — verifying job-seeker identity and educational credentials against government/institutional databases; verifying employer registration; integrating MoL/PEF databases; caching enrichment data.
- **Government data audit & privacy** — immutable 7-year audit trail of every government-database exchange; consent tracking; data minimisation; right-to-be-forgotten deletion; AES-256 encryption at rest.
- **Public API framework** — the versioned `/api/v{n}/` REST surface, OpenAPI 3.0 spec generation, the interactive docs portal, sandbox environment, API versioning and deprecation lifecycle.
- Publishing the integration events in §8 and reacting to the integration events in §9.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong to other BCs and are reached via the contracts in §9:

- **The canonical `JobPosting` aggregate, posting lifecycle, posting moderation** → BC-4 Job Postings. This module *translates* a foreign job into a normalised shape and emits `ExternalJobIngested`; BC-4 creates/updates the actual mirrored posting. This module never owns posting state.
- **Job standardisation against the internal taxonomy** (occupation codes, the canonical skill ontology itself) → BC-11 Admin Config. This module references taxonomy codes and applies *mapping rules*; it does not own the taxonomy.
- **The `JobSeekerProfile` aggregate and the identity/education verification flags on it** → BC-3. This module performs the *verification call* and emits `IdentityVerifiedByGovernment` / `EducationVerified`; BC-3 reacts and sets the flag.
- **The `EmployerProfile` aggregate and its verification badge** → BC-2. This module performs the government check and emits `EmployerVerifiedByGovernment`; BC-2 reacts.
- **The user `Credentials` / access-token issuance / OAuth authorization server** → BC-1 IAM/UAM. The public API authenticates *partner* API keys (owned here) and validates *user* access tokens issued by BC-1 (via the `TokenValidationApi` port). BC-1 owns the OAuth 2.0 server itself.
- **The actual government databases, the external job portals, the geocoding service** → external systems, reached via the ports in §9.2. This module orchestrates calls; it does not implement those systems. For the exercise, provide **stub adapters**.
- **Sending the ops-alert emails / SMS** → BC-9 Notification. This module emits `SyncErrorDetected`; BC-9 decides what to send.
- **Reporting dashboards & LMIS analytics** → BC-10 Reporting. This module emits events and exposes its *own* operational integration dashboard (a thin read of its own data), but cross-platform analytics is BC-10.
- **Search indexing of ingested jobs** → BC-6 / BC-4. Once `ExternalJobIngested` fans out and BC-4 publishes `JobPostingPublished`, indexing is downstream.

### Boundary note — the ACL is the whole point (teaching point)

This BC is the textbook example of *why you isolate foreign models behind an ACL*. A foreign portal calls a field `job_title`, another calls it `position_name`, MoL calls a person's status `work_permit_status`. If those names leaked into BC-4 or BC-3, those contexts' ubiquitous language would rot. The **`JobDataTransformer` domain service (§7)** and the **`MappingProfile` aggregate (§5)** are the ACL's beating heart: nothing foreign passes through untranslated. Discuss with the class: the cost of the ACL is real (a translation layer to maintain), but the cost of *not* having it is your core contexts speaking a vendor's language.

### Boundary note — one BC, three modules (teaching point)

BC_Mapping flags this BC as "asking to be split." We deliberately keep it as one BC with three internal modules. Defensible both ways. Pro-merge: all three share the "we talk to outside systems" responsibility, the same outbox/inbox plumbing, the same ops concern. Pro-split: ingestion and government-verification have almost no shared domain vocabulary. Use this as the class exercise on **bounded-context sizing heuristics** — when does cohesion of *responsibility* outweigh divergence of *language*?

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **Partner** | A registered third-party job portal. The `Partner` aggregate. Has lifecycle `PendingActivation → Active → Suspended/Revoked`. |
| **API Key** | A long (≥32 char) random token issued to a `Partner`. May have an expiry, IP whitelist, and rate limit. Stored hashed. |
| **Connector** | An admin-configured connection to an external portal: endpoint, encrypted credentials, sync options. The `ExternalConnector` aggregate. Used for *system-driven* pull/push (distinct from a Partner who *pushes to us*). |
| **Mapping Profile** | A per-portal, per-schema-version set of field-mapping and transformation rules. The `MappingProfile` aggregate. The ACL's rulebook. |
| **Ingestion** | Pulling/receiving a foreign job, translating it, and emitting `ExternalJobIngested`. |
| **Export** | Pushing a platform job out to an external portal. |
| **Sync Record** | One row tracking the state of one job's synchronisation: `SyncRecord` entity. Status `Accepted → Synced` / `Failed` / `Quarantined` / `Archived`. |
| **Quarantine** | A holding state for a foreign job that failed validation/transformation and needs manual review. |
| **Reconciliation** | The Integration Engineer manually retrying or overriding a failed `SyncRecord`. |
| **Normalised Job** | A foreign job after the ACL has translated it into the platform's posting shape (`NormalisedJobPosting` value object). The payload of `ExternalJobIngested`. |
| **External Ref** | The `(partnerId, externalJobId)` pair — the foreign system's identity for a job. The dedupe key. |
| **Verification Request** | A government/institutional check. The `VerificationRequest` aggregate. Kinds: `Identity`, `Education`, `Employer`. Lifecycle `Pending → InProgress → Verified / Unverified / Error`. |
| **Registry** | A government or institutional data source (MoL, PEF, a national ID service, a university database). |
| **Consent Record** | A user's explicit, versioned opt-in (or withholding) for government data access. The `ConsentRecord` value object on `VerificationRequest`. |
| **Government Audit Entry** | An immutable, 7-year-retained log line for every government-database exchange. The `GovernmentAuditEntry` aggregate. |
| **API Version** | A supported version of the public API (`v1`, `v2`). The `ApiVersion` value object — has a status `Active / Deprecated / Sunset` and a sunset date. |
| **Webhook Subscription** | An opt-in registration that lets an external portal POST real-time updates to us. Child entity of `ExternalConnector`. |
| **Rate Limit** | A per-Partner request ceiling (e.g., 100/hour). Enforced at the API edge. |
| **Source Attribution** | The tag (`Source: samplejobsite.ps` + backlink) stamped onto every ingested job. Public iff the Partner enabled public attribution. |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `ExternalJobSync`. Internally organised as **three sub-modules** (`PartnerJobSync`, `GovernmentVerification`, `PublicApiFramework`) — separate folders/namespaces *inside* each layer, but shipped as one deployable module with one schema. They share the shared kernel, the `external_job_sync` schema, and the outbox/inbox infrastructure; each has its own aggregates and is independently testable.
- Public surface (`Contracts`): the integration events published in §8 + the public API in §9.3.
- **Module-specific notes — background workers / scheduled jobs:** this module runs (a) a **scheduled poller** that triggers inbound ingestion per `ExternalConnector` on its `PullInterval`; (b) a **scheduled exporter** for batch outbound pushes; (c) a **stale-record archiver** that ages out quarantined `SyncRecord`s after 30 days; (d) an **API-key-expiry sweep** that moves keys past `ExpiresOnUtc` to `Expired`; (e) an **API-version sunset sweep** that moves `Deprecated` versions past their sunset date to `Sunset`; (f) the outbox relay. Each is a scheduled job in the `Infrastructure` layer.

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has **seven aggregates**, grouped by sub-module.

**PartnerJobSync:** `Partner`, `ExternalConnector`, `MappingProfile`, `SyncRecord`.
**GovernmentVerification:** `VerificationRequest`, `GovernmentAuditEntry`.
**PublicApiFramework:** `ApiVersionRegistry`.

### 5.1 Aggregate: Partner  *(PartnerJobSync)*

**Aggregate root.** Identity: `PartnerId` (strongly-typed id wrapping `uuid`). A third-party job portal that pushes jobs to us via the public API.

| Member | Type | Notes |
|---|---|---|
| `Id` | `PartnerId` | |
| `Name` | `string` | portal name, required |
| `ContactEmail` | `EmailAddress` | VO |
| `Website` | `string?` | |
| `CompanyInfo` | `string?` | free text |
| `Status` | `PartnerStatus` | enum: `PendingActivation`, `Active`, `Suspended`, `Revoked` |
| `ApiKeys` | `list<ApiKey>` | child entities; at most **one** in `Active` state |
| `IpWhitelist` | `list<string>` | optional; empty = any IP allowed |
| `RateLimit` | `RateLimit?` | VO; null = unlimited |
| `PublicAttribution` | `bool` | if true, source name shown publicly on ingested jobs |
| `RegisteredOnUtc` / `ActivatedOnUtc` | `datetime` / `datetime?` | |

**Child entity** — `ApiKey`: `ApiKeyId`, `KeyHash` (`string` — SHA-256 of the token; plaintext never stored after issue), `KeyPrefix` (`string` — first 8 chars, shown for identification), `Status` (`ApiKeyStatus`: `Active`, `Revoked`, `Expired`), `ExpiresOnUtc` (`datetime?`), `IssuedOnUtc`, `RevokedOnUtc` (`datetime?`).

### 5.2 Aggregate: ExternalConnector  *(PartnerJobSync)*

**Aggregate root.** Identity: `ExternalConnectorId`. An admin-configured connection used for *system-driven* pull/push (the system reaches *out*, as opposed to a `Partner` pushing *in*).

| Member | Type | Notes |
|---|---|---|
| `Id` | `ExternalConnectorId` | |
| `PortalName` | `string` | required |
| `ApiEndpoint` | `string` | base URL, required |
| `Credentials` | `EncryptedCredentials` | VO — AES-256 ciphertext + key ref; never logged |
| `ConnectionStatus` | `ConnectionStatus` | enum: `Unverified`, `Connected`, `Failed` |
| `SyncOptions` | `SyncOptions` | VO: `PullInterval` (`Hourly`/`Daily`/`Weekly`/`Off`), `PushOnPublish` (`bool`), `MappingProfileId` (`MappingProfileId?`) |
| `WebhookSubscriptions` | `list<WebhookSubscription>` | child entities |
| `SchemaVersion` | `string` | which foreign API version this connector speaks |
| `LastPullOnUtc` / `LastPushOnUtc` | `datetime?` | |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

**Child entity** — `WebhookSubscription`: `WebhookSubscriptionId`, `CallbackPath` (`string` — `/api/webhooks/external-portal/{portalId}`), `SigningSecret` (`EncryptedCredentials`), `SigningAlgorithm` (`WebhookSigningAlgorithm`: `HmacSha256`, `BearerToken`), `IsEnabled` (`bool`).

### 5.3 Aggregate: MappingProfile  *(PartnerJobSync)*

**Aggregate root.** Identity: `MappingProfileId`. The ACL rulebook for one portal + one foreign schema version. Inbound *and* outbound.

| Member | Type | Notes |
|---|---|---|
| `Id` | `MappingProfileId` | |
| `PortalName` | `string` | |
| `SchemaVersion` | `string` | which foreign schema version these rules apply to |
| `Direction` | `MappingDirection` | enum: `Inbound`, `Outbound`, `Bidirectional` |
| `FieldMappings` | `list<FieldMapping>` | child entities |
| `IsActive` | `bool` | |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

**Child entity** — `FieldMapping`: `FieldMappingId`, `SourcePath` (`string` — e.g. `my_job_title`), `TargetPath` (`string` — e.g. `job_title`), `TransformKind` (`TransformKind`: `Direct`, `SalaryRange`, `LocationNormalise`, `SkillTaxonomyMap`, `DateParse`, `Constant`), `TransformArgs` (`string?` — JSON args), `IsRequired` (`bool`).

### 5.4 Aggregate: SyncRecord  *(PartnerJobSync)*

**Aggregate root.** Identity: `SyncRecordId`. One row per job-synchronisation lifecycle (inbound *or* outbound). The audit & reconciliation unit.

| Member | Type | Notes |
|---|---|---|
| `Id` | `SyncRecordId` | |
| `Direction` | `SyncDirection` | enum: `Inbound`, `Outbound` |
| `PartnerId` | `PartnerId?` | set for partner-pushed inbound |
| `ConnectorId` | `ExternalConnectorId?` | set for system pull/push |
| `ExternalRef` | `ExternalRef` | VO: `(PortalName, ExternalJobId)` — dedupe key |
| `InternalJobId` | `uuid?` | BC-4 posting id once mirrored; plain uuid, no FK |
| `Status` | `SyncStatus` | enum: `Accepted`, `Quarantined`, `Synced`, `Failed`, `Archived` |
| `RawPayload` | `string` | the foreign payload as received (stored as `json`) — for replay/RCA |
| `NormalisedSnapshot` | `string?` | the translated `NormalisedJobPosting` (stored as `json`) |
| `Attempts` | `list<SyncAttempt>` | child entities — append-only |
| `ErrorCode` | `string?` / `ErrorMessage` | `string?` — set when `Failed`/`Quarantined` |
| `LastSyncOnUtc` | `datetime` | |
| `CreatedOnUtc` | `datetime` | |

**Child entity** — `SyncAttempt`: `SyncAttemptId`, `AttemptNo` (`int`), `Outcome` (`AttemptOutcome`: `Success`, `TransientFailure`, `PermanentFailure`), `ResponseCode` (`int?`), `Detail` (`string?` — diagnostic / response excerpt), `ProcessingMs` (`int`), `AttemptedOnUtc`, `WasManualOverride` (`bool`).

### 5.5 Aggregate: VerificationRequest  *(GovernmentVerification)*

**Aggregate root.** Identity: `VerificationRequestId`. One government/institutional check. Has its own multi-step async lifecycle that can fail independently.

| Member | Type | Notes |
|---|---|---|
| `Id` | `VerificationRequestId` | |
| `Kind` | `VerificationKind` | enum: `Identity`, `Education`, `Employer` |
| `SubjectUserId` | `uuid?` | BC-1 user id (identity / education subject); plain uuid, no FK |
| `SubjectJobSeekerProfileId` | `uuid?` | BC-3 profile id (education); plain uuid, no FK |
| `SubjectEmployerId` | `uuid?` | BC-2 employer id (employer verification); plain uuid, no FK |
| `Registry` | `Registry` | VO: `Name` (e.g. `MoL`, `PEF`, `NationalID`, `UniversityOfX`), `Endpoint` |
| `Consent` | `ConsentRecord` | VO: `Granted` (`bool`), `ConsentVersion` (`string`), `RecordedOnUtc` |
| `RequestPayload` | `MinimisedRequestPayload` | VO — *only* the minimum fields (e.g. ID number + type; never full personal record) |
| `Status` | `VerificationStatus` | enum: `Pending`, `InProgress`, `Verified`, `Unverified`, `Error` |
| `Result` | `VerificationResult?` | VO: `Outcome`, `CredentialRef` (`string?` — for education), `RegistrySource` (`string`), `RespondedOnUtc` |
| `FailureReason` | `string?` | set when `Status = Error` |
| `CachedUntilUtc` | `datetime?` | result cached 12 months (credentials) / per refresh policy (enrichment) |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

### 5.6 Aggregate: GovernmentAuditEntry  *(GovernmentVerification)*

**Aggregate root.** Identity: `GovernmentAuditEntryId`. Immutable, write-once, append-only. 7-year retention. Tamper-resistant.

| Member | Type | Notes |
|---|---|---|
| `Id` | `GovernmentAuditEntryId` | |
| `VerificationRequestId` | `VerificationRequestId?` | linked check, if any |
| `ActorId` | `uuid?` | user/system that triggered the exchange |
| `RegistryName` | `string` | |
| `Direction` | `AuditDirection` | enum: `Query`, `Response` |
| `QueryParameters` | `string` | masked per privacy rules (stored as `json`) |
| `ResultCode` | `string` | |
| `ResponseSizeBytes` | `int?` | |
| `TransformationsApplied` | `string?` | |
| `ConsentStatusAtTime` | `string` | |
| `IntegrityHash` | `string` | SHA-256 of the entry's canonical form + previous entry's hash (hash chain — tamper-evidence) |
| `OccurredOnUtc` | `datetime` | |

### 5.7 Aggregate: ApiVersionRegistry  *(PublicApiFramework)*

**Aggregate root.** Identity: `ApiVersionRegistryId` (singleton — one row). Tracks the lifecycle of every public API version.

| Member | Type | Notes |
|---|---|---|
| `Id` | `ApiVersionRegistryId` | singleton |
| `Versions` | `list<ApiVersion>` | child entities |

**Child entity** — `ApiVersion`: `ApiVersionId`, `Version` (`string` — `v1`, `v2`), `Status` (`ApiVersionStatus`: `Active`, `Deprecated`, `Sunset`), `ReleasedOnUtc`, `DeprecationAnnouncedOnUtc` (`datetime?`), `SunsetOnUtc` (`datetime?`), `MigrationGuideUrl` (`string?`).

### 5.8 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `EmailAddress` | `Value` | RFC 5322; lower-cased on store |
| `RateLimit` | `MaxRequests` (`int`), `Window` (`RateWindow`: `PerMinute`/`PerHour`/`PerDay`) | `MaxRequests > 0` |
| `EncryptedCredentials` | `CipherText`, `KeyRef`, `Algorithm` | `Algorithm == "AES-256-GCM"`; ciphertext non-empty; **string representation returns `"***"`** |
| `SyncOptions` | `PullInterval`, `PushOnPublish`, `MappingProfileId?` | — |
| `ExternalRef` | `PortalName`, `ExternalJobId` | both non-empty |
| `NormalisedJobPosting` | `Title`, `Description`, `Location` (`NormalisedLocation`), `SalaryRange` (`SalaryRange?`), `EmploymentType`, `Requirements` (`list<string>`), `SkillCodes` (`list<string>` — canonical taxonomy codes), `SourceAttribution`, `ExternalRef`, `PostedOnUtc`, `DeadlineUtc?` | `Title`, `Description`, `Location` required — **missing required field ⇒ caller quarantines** |
| `NormalisedLocation` | `City`, `District?`, `Country`, `Lat?`, `Lon?` | `City`, `Country` required |
| `SalaryRange` | `Min` (`decimal`), `Max` (`decimal`), `CurrencyCode` (`string` — ISO 4217) | `Min ≤ Max`; `Min ≥ 0`; currency in ISO 4217 |
| `SourceAttribution` | `SourceName`, `Backlink`, `IsPublic` | `SourceName` non-empty |
| `Registry` | `Name`, `Endpoint` | both non-empty |
| `ConsentRecord` | `Granted`, `ConsentVersion`, `RecordedOnUtc` | `ConsentVersion` non-empty |
| `MinimisedRequestPayload` | `Fields` (`map<string, string>`) | **whitelist-enforced** — only fields in the per-`VerificationKind` allow-list may be present (data minimisation invariant) |
| `VerificationResult` | `Outcome` (`VerificationOutcome`: `Match`/`NoMatch`), `CredentialRef?`, `RegistrySource`, `RespondedOnUtc` | `RegistrySource` non-empty |
| `ApiVersion` *(used as child entity above; also exposed as VO-style record)* | see §5.7 | `Version` matches `^v\d+$` |
| `WebhookSignature` | `Algorithm`, `SignatureValue` | non-empty |

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior lives on the aggregate roots. Handlers never set properties directly.

### 6.1 Partner — behaviors  *(PartnerJobSync)*

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Register(name, contactEmail, website, companyInfo)` | Creates partner in `PendingActivation`. Name + email valid. | `PartnerRegistered` *(internal)* |
| `Approve()` | Only from `PendingActivation`. → `Active`. Issues the first `ApiKey` (see `IssueApiKey`). | `PartnerActivated` *(internal)* |
| `IssueApiKey(token, keyHash, keyPrefix, expiresOnUtc?)` | Partner must be `Active`. Revokes any existing `Active` key. New key `Active`. Invariant: **at most one `Active` key**. | `ApiKeyIssued` *(internal)* |
| `RegenerateApiKey(...)` | Same as `IssueApiKey` — old key `Revoked` immediately. | `ApiKeyIssued` *(internal)* |
| `RevokeApiKey(apiKeyId)` | Key must exist. → `Revoked`. | `ApiKeyRevoked` *(internal)* |
| `ExpireApiKey(apiKeyId)` | Called by scheduler when `ExpiresOnUtc` passes. → `Expired`. | — |
| `SetIpWhitelist(ips)` | Each must be a valid IPv4/IPv6 or CIDR. | — |
| `SetRateLimit(rateLimit?)` | null clears it. | — |
| `SetPublicAttribution(enabled)` | — | — |
| `Suspend()` / `Revoke()` | From `Active`. `Revoke` also revokes all keys. | `PartnerStatusChanged` *(internal)* |

### 6.2 ExternalConnector — behaviors  *(PartnerJobSync)*

| Method | Rules | Event |
|---|---|---|
| `static Configure(portalName, apiEndpoint, encryptedCredentials, schemaVersion)` | endpoint must be a valid absolute URL. `ConnectionStatus = Unverified`. | — |
| `MarkConnectionVerified()` / `MarkConnectionFailed()` | set after a `Test Connection` health-check call | — |
| `UpdateCredentials(encryptedCredentials)` | rotates credentials; resets `ConnectionStatus` to `Unverified` until re-tested | — |
| `SetSyncOptions(syncOptions)` | if `MappingProfileId` set, profile must exist (checked by handler) | — |
| `AddWebhookSubscription(callbackPath, signingSecret, algorithm)` | callback path must match `/api/webhooks/external-portal/{id}` | — |
| `DisableWebhookSubscription(id)` | — | — |
| `RecordPull()` / `RecordPush()` | stamps `LastPullOnUtc` / `LastPushOnUtc` | — |

### 6.3 MappingProfile — behaviors  *(PartnerJobSync)*

| Method | Rules | Event |
|---|---|---|
| `static Create(portalName, schemaVersion, direction)` | — | — |
| `AddFieldMapping(sourcePath, targetPath, transformKind, transformArgs, isRequired)` | no duplicate `SourcePath` within a direction; `transformArgs` must be valid JSON if present | — |
| `RemoveFieldMapping(id)` | must exist | — |
| `Activate()` / `Deactivate()` | only one `Active` profile per `(portalName, schemaVersion, direction)` — enforced by handler via repository | — |

### 6.4 SyncRecord — behaviors  *(PartnerJobSync)*

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static StartInbound(externalRef, rawPayload, partnerId?, connectorId?)` | exactly one of `partnerId`/`connectorId` set. Status `Accepted`. | — |
| `static StartOutbound(externalRef, internalJobId, connectorId)` | Status `Accepted`. | — |
| `RecordNormalised(normalisedSnapshot, internalJobId?)` | only from `Accepted`. Stores translated payload. | — |
| `MarkSynced(internalJobId)` | → `Synced`. Sets `InternalJobId`. | `ExternalJobIngested` *(inbound)* or export-tracking event |
| `MarkUpdated(changedFields)` | from `Synced`; records an attempt | `ExternalJobUpdated` |
| `MarkRetracted()` | from `Synced`; partner removed the job | `ExternalJobRetracted` |
| `Quarantine(errorCode, errorMessage)` | from `Accepted`. → `Quarantined`. **Missing required field on the `NormalisedJobPosting` ⇒ this path.** | `SyncErrorDetected` |
| `RecordAttempt(attemptNo, outcome, responseCode, detail, processingMs, wasManualOverride)` | append-only. **Max 3 automatic attempts** then status `Failed` (`AttemptOutcome.PermanentFailure`). | `SyncErrorDetected` *(on final failure)* |
| `Retry()` | from `Failed` or `Quarantined`. Re-opens to `Accepted` for one more attempt. Records a `SyncAttempt` with `WasManualOverride=true` if engineer-triggered. | — |
| `ManualOverride(correctedPayload, engineerId)` | from `Failed`/`Quarantined`. Records the override attempt, re-processes. | `SyncReconciled` |
| `Archive()` | terminal — quarantined records age out after 30 days | — |

### 6.5 Core invariants — PartnerJobSync (must always hold)

1. **One active API key per Partner.** Issuing/regenerating revokes the prior active key.
2. **API key plaintext is never persisted.** Only `KeyHash` (SHA-256) and `KeyPrefix` are stored. The plaintext is returned exactly once (issue/regenerate) and delivered out-of-band.
3. **Partner status machine:** `PendingActivation → Active`; `Active → Suspended`; `Active/Suspended → Revoked`. No reverse from `Revoked`.
4. **`SyncRecord` dedupe:** `(PortalName, ExternalJobId)` is unique across non-archived inbound records. A re-fetch of an existing external job **updates** that record; it never creates a second.
5. **Quarantine on incomplete data:** if the `NormalisedJobPosting` cannot be built because a required field (title/description/location) is absent, the `SyncRecord` goes to `Quarantined`, **not** `Failed` — it needs human eyes, not a retry.
6. **Retry ceiling:** at most **3 automatic attempts** with exponential backoff; after that, status is `Failed` and only a manual `Retry`/`ManualOverride` moves it.
7. **`SyncAttempt` list is append-only.** Never updated or deleted.
8. **Credentials are encrypted at rest** (`EncryptedCredentials`, AES-256-GCM) and **never logged** — the VO's `ToString()` returns `"***"`.
9. **Every ingested job carries `SourceAttribution`.** Non-negotiable — it's how MoL admins audit provenance.

### 6.6 VerificationRequest — behaviors  *(GovernmentVerification)*

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static StartIdentity(subjectUserId, registry, consent, minimisedPayload)` | **`consent.Granted` must be true** else returns `E-GOV-CONSENT-REQUIRED`. Status `Pending`. | — |
| `static StartEducation(subjectJobSeekerProfileId, subjectUserId, registry, consent, minimisedPayload)` | same consent gate. Status `Pending`. | — |
| `static StartEmployer(subjectEmployerId, registry, consent, minimisedPayload)` | same consent gate. Status `Pending`. | — |
| `BeginProcessing()` | only from `Pending`. → `InProgress`. | — |
| `RecordVerified(verificationResult)` | from `InProgress`. → `Verified`. Sets `CachedUntilUtc = now + 12 months`. | `IdentityVerifiedByGovernment` / `EducationVerified` / `EmployerVerifiedByGovernment` *(by `Kind`)* |
| `RecordUnverified(verificationResult)` | from `InProgress`. → `Unverified`. | `IdentityVerificationFailed` *(Identity kind only)* |
| `RecordError(reason)` | from `Pending`/`InProgress`. → `Error`. Non-blocking — caller falls back to cache or "verification pending". | `IdentityVerificationFailed` *(Identity kind only; reason carried)* |
| `IsCacheValid(nowUtc)` | true if `Status == Verified && CachedUntilUtc > now` — caller short-circuits a fresh registry call | — |
| `RevokeForDataDeletion()` | right-to-be-forgotten: clears `RequestPayload`/`Result` cached data, status stays but payload tombstoned | `GovernmentDataDeleted` *(internal)* |

### 6.7 GovernmentAuditEntry — behaviors  *(GovernmentVerification)*

| Method | Rules | Event |
|---|---|---|
| `static Record(verificationRequestId?, actorId, registryName, direction, queryParameters, resultCode, responseSizeBytes?, transformationsApplied, consentStatusAtTime, previousEntryHash)` | computes `IntegrityHash` over canonical form + `previousEntryHash` (hash chain). **No mutator methods exist** — the aggregate is write-once. | — |

### 6.8 Core invariants — GovernmentVerification (must always hold)

10. **Consent gate:** no `VerificationRequest` can be started without `ConsentRecord.Granted == true`. Withholding consent ⇒ `E-GOV-CONSENT-REQUIRED`, the registry is **never called**.
11. **Data minimisation:** `MinimisedRequestPayload` only permits whitelisted fields per `VerificationKind`. Full ID numbers / full personal records are never stored — only the verification *result* and a `CredentialRef`.
12. **Every government exchange is audited.** Each query *and* each response produces a `GovernmentAuditEntry`. The audit write is in the **same transaction** as the `VerificationRequest` state change.
13. **Audit entries are immutable.** Write-once, append-only, hash-chained for tamper-evidence. Retention ≥ 7 years.
14. **Government data is encrypted at rest** (AES-256), keys managed separately.
15. **Verification status machine:** `Pending → InProgress → {Verified, Unverified, Error}`. No transition out of a terminal state except `RevokeForDataDeletion` (tombstoning).
16. **Cache-before-call:** if a valid cached `Verified` result exists (< 12 months), the handler must not hit the registry again.

### 6.9 ApiVersionRegistry — behaviors  *(PublicApiFramework)*

| Method | Rules / invariants enforced | Event |
|---|---|---|
| `RegisterVersion(version, releasedOnUtc)` | version matches `^v\d+$`; not already registered. Status `Active`. | — |
| `DeprecateVersion(version, migrationGuideUrl)` | only from `Active`. Sets `DeprecationAnnouncedOnUtc`. **`SunsetOnUtc` must be ≥ 6 months out.** | — |
| `SunsetVersion(version)` | only from `Deprecated`, and only once `now ≥ SunsetOnUtc`. → `Sunset`. Requests to a `Sunset` version return `410 Gone`. | — |

17. **Backward compatibility:** an `Active` or `Deprecated` version must keep its documented behavior and schema. Breaking changes go in a new MAJOR version.
18. **At least 2 MAJOR versions remain non-`Sunset`** at any time (minimum support window).
19. **Deprecation gives 6 months' notice** before sunset; `Deprecated`-version responses carry a `Deprecation` + `Sunset` header.

---

## 7. Domain services

Stateless. Live in `Domain`. Used when logic spans entities or needs a small external input.

### 7.1 `JobDataTransformer`  *(PartnerJobSync — the ACL core)*

```
ToNormalised(rawForeignPayload: string, profile: MappingProfile,
             mapSkillToTaxonomy: (string) -> Result<string>,           // delegates to the TaxonomyApi port
             normaliseLocation: (string) -> Result<NormalisedLocation> // delegates to the Geocoding port
            ) -> Result<NormalisedJobPosting>

ToForeign(posting: NormalisedJobPosting, outboundProfile: MappingProfile) -> Result<string>
```

Applies a `MappingProfile`'s `FieldMapping`s to translate a foreign job into a `NormalisedJobPosting` (inbound) or back into a portal-specific shape (outbound). Runs each `FieldMapping.TransformKind` (direct copy, salary-range parse, location normalisation, skill→taxonomy map, date parse, constant). **If a `IsRequired` mapping yields nothing, returns a failure `Result` carrying `E-SYNC-MISSING-FIELD`** — the caller then quarantines the `SyncRecord`. This service is *the* anti-corruption layer: foreign vocabulary enters, platform vocabulary leaves.

### 7.2 `SchemaVersionDetector`  *(PartnerJobSync)*

```
Detect(rawForeignPayload: string, knownSchemaVersions: list<string>) -> Result<string>
```

Inspects a foreign payload (version marker field, shape heuristics) and returns which foreign schema version it is, so the caller can pick the right `MappingProfile`. Fails with `E-SYNC-UNKNOWN-SCHEMA` if it cannot tell.

### 7.3 `DuplicateJobDetector`  *(PartnerJobSync)*

```
IsDuplicate(incoming: ExternalRef, incomingJob: NormalisedJobPosting,
            findByExternalRef: (ExternalRef) -> SyncRecord?,
            fuzzyMatchExists: ((title: string, company: string, location: string)) -> bool) -> bool
```

Two-tier dedupe: (1) exact match on `ExternalRef` — the strong key; (2) configurable fuzzy match on `(title, company, location)` for portals that don't supply stable IDs. Used by ingestion and by the public push API (`E-API-DUPLICATE-JOB`).

### 7.4 `WebhookSignatureVerifier`  *(PartnerJobSync)*

```
Verify(rawBody: string, providedSignature: WebhookSignature,
       signingSecret: EncryptedCredentials, algorithm: WebhookSigningAlgorithm) -> Result
```

Re-computes the HMAC-SHA256 (or validates the bearer token) over the raw webhook body and compares constant-time against the provided signature. Fails with `E-WEBHOOK-SIGNATURE-INVALID`.

### 7.5 `ApiKeyGenerator`  *(PartnerJobSync)*

```
Generate() -> (plaintext: string, keyHash: string, keyPrefix: string)
```

Produces a cryptographically-random token ≥ 32 chars, returns the plaintext (to deliver out-of-band, once), its SHA-256 hash (to store), and the 8-char prefix (to display). The aggregate only ever stores hash + prefix.

### 7.6 `ConsentEvaluator`  *(GovernmentVerification)*

```
EnsureConsentAndMinimisation(kind: VerificationKind, consent: ConsentRecord,
                             requestedFields: map<string, string>) -> Result
```

Single chokepoint that enforces invariants #10 and #11 together: consent must be granted, and the requested-field set must be a subset of the per-`VerificationKind` whitelist. Called by every `VerificationRequest` factory and by the verification handlers before any registry call.

### 7.7 `AuditHashChainer`  *(GovernmentVerification)*

```
ComputeIntegrityHash(candidate: GovernmentAuditEntry, previousEntryHash: string?) -> string
```

Computes the tamper-evidence hash: SHA-256 over the entry's canonical serialised form concatenated with the previous entry's hash. Lets a compliance officer verify the chain has not been altered.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module. **Integration events** (`IntegrationEvent`) cross the BC boundary — they go through the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract exactly. Other modules depend on these payloads — do not change a field without versioning.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

| Integration event | Raised when | Payload |
|---|---|---|
| `ExternalJobIngestedIntegrationEvent` | `SyncRecord.MarkSynced` on an inbound record | `ExternalRef` (`{portalName, externalJobId}`), `PartnerId?`, `NormalisedPosting` (the full `NormalisedJobPosting`: title, description, location, salaryRange, employmentType, requirements, skillCodes, sourceAttribution, postedOnUtc, deadlineUtc), `OccurredOnUtc` |
| `ExternalJobUpdatedIntegrationEvent` | `SyncRecord.MarkUpdated` | `ExternalRef`, `PartnerId?`, `ChangedFields` (`list<string>`), `NormalisedPosting`, `OccurredOnUtc` |
| `ExternalJobRetractedIntegrationEvent` | `SyncRecord.MarkRetracted` | `ExternalRef`, `PartnerId?`, `OccurredOnUtc` |
| `IdentityVerifiedByGovernmentIntegrationEvent` | `VerificationRequest.RecordVerified`, `Kind == Identity` | `UserId`, `Registry` (`string`), `VerifiedOnUtc`, `OccurredOnUtc` |
| `IdentityVerificationFailedIntegrationEvent` | `RecordUnverified` / `RecordError`, `Kind == Identity` | `UserId`, `Registry` (`string`), `Reason` (`string`), `OccurredOnUtc` |
| `EducationVerifiedIntegrationEvent` | `RecordVerified`, `Kind == Education` | `JobSeekerProfileId`, `CredentialRef` (`string`), `VerifiedOnUtc`, `OccurredOnUtc` |
| `EmployerVerifiedByGovernmentIntegrationEvent` | `RecordVerified`, `Kind == Employer` | `EmployerId`, `Registry` (`string`), `VerifiedOnUtc`, `OccurredOnUtc` |
| `SyncErrorDetectedIntegrationEvent` | `SyncRecord.Quarantine` or final-attempt `Failed` | `PartnerId?`, `ConnectorId?`, `ErrorClass` (`string`), `PayloadRef` (`SyncRecordId`), `OccurredOnUtc` |
| `SyncReconciledIntegrationEvent` | `SyncRecord.ManualOverride` succeeds | `PartnerId?`, `ConnectorId?`, `RecordsAffected` (`int`), `OccurredOnUtc` |

Consumers (for context only — you do not code them): BC-4 Job Postings consumes `ExternalJobIngested`, `ExternalJobUpdated`, `ExternalJobRetracted` (creates/updates/closes mirrored postings); BC-1 IAM consumes `IdentityVerifiedByGovernment`, `IdentityVerificationFailed`; BC-3 JobSeeker Profile consumes `IdentityVerifiedByGovernment`, `EducationVerified`; BC-2 Employer Profile consumes `EmployerVerifiedByGovernment`; BC-9 Notification consumes `SyncErrorDetected` (ops alert); BC-10 Reporting consumes all.

### 8.2 Internal domain events (NOT published outside the module)

`PartnerRegistered`, `PartnerActivated`, `PartnerStatusChanged`, `ApiKeyIssued`, `ApiKeyRevoked`, `GovernmentDataDeleted`. Use these for in-module reactions (e.g., `ApiKeyRevoked` → also write a `GovernmentAuditEntry`-style internal log; `PartnerActivated` → trigger the out-of-band API-key delivery email request). They never reach the outbox.

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained.

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent handlers)

| Integration event | From | Payload you receive | Your reaction |
|---|---|---|---|
| `JobPostingPublishedIntegrationEvent` | BC-4 Job Postings | `PostingId`, `EmployerId`, `Title`, `Requirements`, `OccurredOnUtc` | For each `ExternalConnector` with `SyncOptions.PushOnPublish == true`: start an outbound `SyncRecord`, transform via the outbound `MappingProfile`, push. |
| `JobPostingUpdatedIntegrationEvent` | BC-4 | `PostingId`, `ChangedFields`, `OccurredOnUtc` | Re-export to portals that already have this posting (find by `InternalJobId` on outbound `SyncRecord`s). |
| `JobPostingClosedIntegrationEvent` | BC-4 | `PostingId`, `Reason`, `OccurredOnUtc` | Push a close/retract to portals holding this posting. |
| `EmployerVerificationRequestedIntegrationEvent` | BC-2 Employer Profile | `EmployerId`, `RegistryRef`, `OccurredOnUtc` | Start an `Employer`-kind `VerificationRequest`, call the government registry, emit `EmployerVerifiedByGovernment` (or failure). |
| `TaxonomyUpdatedIntegrationEvent` | BC-11 Admin Config | `TaxonomyId`, `Version`, `ChangeSummary`, `OccurredOnUtc` | Re-validate / refresh `FieldMapping`s of kind `SkillTaxonomyMap`; flag mapping profiles whose target codes are now deprecated. |

**Idempotency:** every consumer handler must be safe to run twice (dedupe on `EventId` via the `inbox_messages` table, [[00-Shared-Foundations]] §6.3, or make the operation naturally idempotent). Inbound events arrive via the module's integration-event subscription wired in the module composition entry point.

### 9.2 Ports this module CALLS (port interfaces — define in `Application`, implement adapters in `Infrastructure`)

```
Port: JobPostingPublicApi       (provided by BC-4 Job Postings; confirm a mirrored posting's status
                                 before export, and look up the posting id <-> external ref)
  GetStatus(postingId: uuid) -> JobPostingStatusDto?
  JobPostingStatusDto { PostingId: uuid, Status: string, DeadlineUtc: datetime? }

Port: TaxonomyApi               (provided by BC-11 Admin Config; canonical taxonomy lookup,
                                 used by JobDataTransformer's SkillTaxonomyMap transform)
  MapSkillToTaxonomyCode(rawSkillLabel: string) -> Result<string>
  IsValidTaxonomyCode(taxonomyCode: string)     -> bool

Port: TokenValidationApi        (provided by BC-1 IAM/UAM; validates the access tokens that
                                 authenticated *users* present to the public API. Partner API-key
                                 auth is owned HERE; user-token validation is BC-1's.)
  Validate(bearerToken: string) -> Result<ValidatedToken>
  ValidatedToken { UserId: uuid, Role: string, Scopes: list<string> }

Port: ExternalPortalPort        (external job portals — pull/push foreign jobs; orchestrated, not
                                 implemented, here)
  FetchJobs(endpoint: string, credentials: EncryptedCredentials, since: datetime?)
      -> Result<list<string>>
  PushJob(endpoint: string, credentials: EncryptedCredentials, foreignPayload: string)
      -> Result<string>
  HealthCheck(endpoint: string, credentials: EncryptedCredentials) -> Result

Port: GovernmentRegistryPort    (government / institutional registries — identity, education,
                                 employer, MoL/PEF enrichment)
  Verify(registry: Registry, kind: VerificationKind, payload: MinimisedRequestPayload)
      -> Result<VerificationResult>
      // 1-10s synchronous for ID checks; async-poll for slow institution checks
  QueryEnrichment(registry: Registry, subjectKey: string) -> Result<string>
      // MoL/PEF enrichment data

Port: GeocodingPort             (location normalisation — used by JobDataTransformer's
                                 LocationNormalise transform)
  Normalise(rawLocation: string) -> Result<NormalisedLocation>

Port: CredentialEncryptionPort  (symmetric encryption for credentials & government data at rest —
                                 AES-256-GCM, key via KMS/HSM)
  Encrypt(plaintext: string)              -> Result<EncryptedCredentials>
  Decrypt(credentials: EncryptedCredentials) -> Result<string>
```

For the exercise, `Infrastructure` may provide **stub adapters** for `ExternalPortalPort`, `GovernmentRegistryPort`, `GeocodingPort`, `TaxonomyApi`, `TokenValidationApi`, `JobPostingPublicApi`, and an in-memory `CredentialEncryptionPort` so the module runs standalone. Keep the port shapes exactly as above so real adapters drop in later.

### 9.3 Public API this module EXPOSES (in the `Contracts` surface)

```
Public API: ExternalJobSyncPublicApi
  GetSyncRecordByExternalRef(portalName: string, externalJobId: string) -> SyncRecordSummaryDto?
      // used by BC-5 Job Application to record match-score-at-apply context; also general lookup
  GetLatestVerification(subjectId: uuid, kind: string) -> VerificationSummaryDto?
      // used by BC-3 / BC-2 to check whether a subject already has a valid government verification

  SyncRecordSummaryDto { SyncRecordId: uuid, PortalName: string, ExternalJobId: string,
                         Status: string, InternalJobId: uuid?, LastSyncOnUtc: datetime }
  VerificationSummaryDto { VerificationRequestId: uuid, Kind: string, Status: string,
                           CredentialRef: string?, CachedUntilUtc: datetime? }
```

---

## 10. Application layer

Every use case is a **command** or a **query** with exactly one handler. Commands return `Result`/`Result<T>`; queries return DTOs. Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

### 10.1 Commands — PartnerJobSync

| Command | Story | Handler responsibilities |
|---|---|---|
| `RegisterPartnerCommand` | US-3.1.3-01 | `Partner.Register(...)` in `PendingActivation` → persist. |
| `ApprovePartnerCommand` | US-3.1.3-01 | Load → `Approve()` → `ApiKeyGenerator.Generate()` → `IssueApiKey(...)` → persist → queue out-of-band key-delivery email request (internal event). |
| `RegenerateApiKeyCommand` | US-3.1.3-01 | Load → `ApiKeyGenerator.Generate()` → `RegenerateApiKey(...)` → persist. Return plaintext once. |
| `RevokeApiKeyCommand` | US-3.1.3-01 | Load → `RevokeApiKey(id)` → persist. |
| `SetPartnerIpWhitelistCommand` / `SetPartnerRateLimitCommand` / `SetApiKeyExpiryCommand` | US-3.1.3-01 | Load → mutate → persist. |
| `PushJobViaApiCommand` | US-3.1.3-02 | Authenticated by API key (middleware). Validate payload (`E-API-VALIDATION-ERROR`) → `SchemaVersionDetector.Detect` → load `MappingProfile` → `JobDataTransformer.ToNormalised` → `DuplicateJobDetector.IsDuplicate` (`E-API-DUPLICATE-JOB`) → `SyncRecord.StartInbound` → `RecordNormalised` → `MarkSynced` (emits `ExternalJobIngested`) → persist. Returns `job_id`. |
| `UpdateJobViaApiCommand` | US-3.1.3-02 | Find `SyncRecord` by `ExternalRef` → `JobDataTransformer.ToNormalised` → `MarkUpdated(changedFields)` → persist. |
| `ChangeJobStatusViaApiCommand` | US-3.1.3-02 | Find record → `MarkRetracted()` (close/deactivate) → persist. |
| `ConfigureMappingProfileCommand` / `AddFieldMappingCommand` / `RemoveFieldMappingCommand` / `ActivateMappingProfileCommand` | US-3.1.3-03, US-3.4.1-03 | Load/create `MappingProfile` → mutate → persist. |
| `ConfigureExternalConnectorCommand` | US-3.4.1-04 | Encrypt credentials via the `CredentialEncryptionPort` → `ExternalConnector.Configure(...)` → persist. |
| `TestConnectorConnectionCommand` | US-3.4.1-04 | Load → `ExternalPortalPort.HealthCheck` → `MarkConnectionVerified()`/`MarkConnectionFailed()` → persist. |
| `RotateConnectorCredentialsCommand` | US-3.4.1-04 | Encrypt new → `UpdateCredentials(...)` → persist. |
| `SetConnectorSyncOptionsCommand` | US-3.4.1-04 | Load → `SetSyncOptions(...)` (validate `MappingProfileId` exists) → persist. |
| `AddWebhookSubscriptionCommand` / `DisableWebhookSubscriptionCommand` | US-3.4.1-07 | Load connector → mutate → persist. |
| `IngestExternalJobsCommand` | US-3.4.1-01 | *(triggered by the scheduled poller per connector)* `ExternalPortalPort.FetchJobs(since=LastPullOnUtc)` → for each foreign job: `SchemaVersionDetector` → `JobDataTransformer.ToNormalised` → on missing field `SyncRecord.Quarantine`; else dedupe → `StartInbound`/update → `MarkSynced` → `RecordPull()` → persist. Network errors retried ×3 exp-backoff. |
| `ExportJobCommand` | US-3.4.1-02 | `JobDataTransformer.ToForeign` → `ExternalPortalPort.PushJob` → `SyncRecord.StartOutbound` + `MarkSynced` or `RecordAttempt` (×3 then `Failed` → emits `SyncErrorDetected`) → `RecordPush()` → persist. |
| `ProcessWebhookUpdateCommand` | US-3.4.1-07 | `WebhookSignatureVerifier.Verify` (`E-WEBHOOK-SIGNATURE-INVALID`) → queue async → dedupe vs polling → transform → update `SyncRecord`. |
| `RetrySyncRecordCommand` | US-3.4.1-06 | Load → `Retry()` → re-run ingest/export → persist. |
| `ManualOverrideSyncRecordCommand` | US-3.4.1-06 | Load → `ManualOverride(correctedPayload, engineerId)` → persist (emits `SyncReconciled`). |
| `ArchiveStaleSyncRecordsCommand` | US-3.4.1-06 | *(scheduler)* `Archive()` quarantined records older than 30 days. |

### 10.2 Commands — GovernmentVerification

| Command | Story | Handler responsibilities |
|---|---|---|
| `VerifyEducationalCredentialCommand` | US-3.4.2-01 | `ConsentEvaluator.EnsureConsentAndMinimisation` → check cache (`IsCacheValid`) → if stale: `VerificationRequest.StartEducation` → `GovernmentAuditEntry.Record(Query)` → `BeginProcessing()` → `GovernmentRegistryPort.Verify` → `RecordVerified`/`RecordUnverified`/`RecordError` → `GovernmentAuditEntry.Record(Response)` → persist (emits `EducationVerified` on success). Institution not integrated ⇒ `RecordError` + `E-GOV-INSTITUTION-NOT-INTEGRATED` (seeker prompted to upload docs). |
| `VerifyIdentityViaGovernmentCommand` | US-3.4.2-02 | Same shape, `StartIdentity`; emits `IdentityVerifiedByGovernment` / `IdentityVerificationFailed`. Declined consent ⇒ `E-GOV-CONSENT-REQUIRED` (profile stays "unverified", use continues). |
| `VerifyEmployerViaGovernmentCommand` | US-3.4.2-02 / consumed `EmployerVerificationRequested` | Same shape, `StartEmployer`; emits `EmployerVerifiedByGovernment`. |
| `QueryMolPefEnrichmentCommand` | US-3.4.2-03 | Check cache → `GovernmentRegistryPort.QueryEnrichment` → cache with refresh policy → `GovernmentAuditEntry.Record` ×2. Registry down ⇒ fall back to cache or "verification pending". |
| `RecordConsentDecisionCommand` | US-3.4.2-05 | Persist a `ConsentRecord` (granted/withheld) with version + timestamp on the relevant `VerificationRequest` (or a standalone consent ledger). |
| `DeleteGovernmentDataForUserCommand` | US-3.4.2-05 | Right-to-be-forgotten: for every `VerificationRequest` of the user, `RevokeForDataDeletion()`; write a `GovernmentAuditEntry` for the deletion; honored within 30 days. |
| `ExportGovernmentAuditTrailCommand` | US-3.4.2-04 | Compliance-officer only. Produce a signed, tamper-evident export (verify hash chain) over a date range. |

### 10.3 Commands — PublicApiFramework

| Command | Story | Handler responsibilities |
|---|---|---|
| `RegisterApiVersionCommand` | US-3.4.3-05 | Load singleton `ApiVersionRegistry` → `RegisterVersion(...)` → persist. |
| `DeprecateApiVersionCommand` | US-3.4.3-05 | `DeprecateVersion(version, migrationGuideUrl)` (sunset ≥ 6 months out) → persist. |
| `SunsetApiVersionCommand` | US-3.4.3-05 | *(scheduler)* `SunsetVersion(version)` once past sunset date. |

### 10.4 Queries

| Query | Story | Returns |
|---|---|---|
| `GetSubmissionLogsQuery` | US-3.1.3-04 | `PagedResult<SubmissionLogDto>` — timestamp, job title, status, response code; filter by date/title/ID. |
| `GetSubmissionLogDetailQuery` | US-3.1.3-04 | `SubmissionLogDetailDto` — request payload, response code, error message, processing time. |
| `GetPartnerSyncDashboardQuery` | US-3.1.3-04 | `SyncDashboardDto` — counts by status (synced/failed/pending/archived), per-job sync status. |
| `GetPartnerUsageStatsQuery` | US-3.1.3-04 | `UsageStatsDto` — jobs submitted/synced, applications/matches received, views; trend series by date range. |
| `GetIntegrationAuditLogsQuery` | US-3.1.3-04 (AC-10) | MoL-admin view — which portal created/modified each job. |
| `GetIntegrationDashboardQuery` | US-3.4.1-05 | `IntegrationHealthDto[]` — per-connector: last sync, health (healthy/warning/error per the AC-defined thresholds), jobs ingested/exported, error count. |
| `GetConnectorSyncLogQuery` | US-3.4.1-05 | last 100 sync events, paginated. |
| `ExportSyncLogsQuery` | US-3.4.1-05 | CSV of full sync-event history for a date range. |
| `GetFailedSyncQueueQuery` | US-3.4.1-06 | `FailedSyncDto[]` — job ID, portal, error code/message, timestamp. |
| `GetSyncRecordDetailQuery` | US-3.4.1-06 | full `SyncRecord` incl. attempts, raw + response payloads, retry count (RCA view). |
| `GetVerificationStatusQuery` | US-3.4.2-01/02 | `VerificationSummaryDto` — status, result, cached-until. |
| `GetGovernmentAuditTrailQuery` | US-3.4.2-04 | compliance-officer-only paged audit entries; **every read is itself audited**. |
| `GetOpenApiSpecQuery` | US-3.4.3-01/02/03 | the generated OpenAPI 3.0.x document for a given API version. |
| `GetApiVersionsQuery` | US-3.4.3-05 | `ApiVersionDto[]` — version, status, released/deprecated/sunset dates, migration guide URL. |

### 10.5 Validators — representative rules

- `RegisterPartnerCommandValidator`: name non-empty ≤ 200; `ContactEmail` RFC 5322; website a valid URL if present.
- `PushJobViaApiCommandValidator`: `title`, `description`, `location` present (`E-API-VALIDATION-ERROR` with field-level messages); salary min ≤ max; deadline in the future if present.
- `ConfigureExternalConnectorCommandValidator`: `apiEndpoint` a valid absolute HTTPS URL; credentials non-empty; schema version non-empty.
- `AddFieldMappingCommandValidator`: `sourcePath`/`targetPath` non-empty; `transformArgs` valid JSON if present; `transformKind` in enum.
- `VerifyIdentityViaGovernmentCommandValidator`: registry name + endpoint present; `consentVersion` present; requested fields ⊆ identity whitelist.
- `DeprecateApiVersionCommandValidator`: `sunsetOnUtc ≥ now + 6 months`; `migrationGuideUrl` present.

### 10.6 DTOs

Plain records in `Application`. Never expose Domain entities/VOs across the API. Map aggregate → DTO in the handler or a mapping extension. **Government DTOs never carry raw ID numbers or full personal records** — only verification status, result, and `CredentialRef`.

---

## 11. Persistence & data model

Schema/namespace: `external_job_sync`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. No foreign key may cross into another module's schema — references to BC-4 postings, BC-1 users, BC-2 employers, BC-3 profiles are plain `uuid` columns with **no** FK constraint. The module-specific relational model follows.

### 11.1 Reference relational model — schema `external_job_sync`

```
TABLE partners
  id                  uuid        PK
  name                string      NOT NULL
  contact_email       string      NOT NULL
  website             string      NULL
  company_info        string      NULL
  status              enum        NOT NULL                -- PendingActivation|Active|Suspended|Revoked
  ip_whitelist        json        NOT NULL DEFAULT '[]'
  rate_limit          json        NULL                    -- RateLimit VO
  public_attribution  bool        NOT NULL DEFAULT false
  registered_on_utc   datetime    NOT NULL
  activated_on_utc    datetime    NULL
  version_token       (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  INDEX (status)

TABLE api_keys
  id              uuid        PK
  partner_id      uuid        NOT NULL                    -- FK → partners.id ON DELETE CASCADE
  key_hash        string      NOT NULL                    -- SHA-256; plaintext NEVER stored
  key_prefix      string      NOT NULL
  status          enum        NOT NULL                    -- Active|Revoked|Expired
  expires_on_utc  datetime    NULL
  issued_on_utc   datetime    NOT NULL
  revoked_on_utc  datetime    NULL
  INDEX (key_hash)
  UNIQUE (partner_id) WHERE status = 'Active'

TABLE external_connectors
  id                  uuid        PK
  portal_name         string      NOT NULL
  api_endpoint        string      NOT NULL
  credentials         json        NOT NULL                -- EncryptedCredentials VO (ciphertext)
  connection_status   enum        NOT NULL                -- Unverified|Connected|Failed
  sync_options        json        NOT NULL                -- SyncOptions VO
  schema_version      string      NOT NULL
  last_pull_on_utc    datetime    NULL
  last_push_on_utc    datetime    NULL
  created_on_utc      datetime    NOT NULL
  updated_on_utc      datetime    NOT NULL
  version_token       (optimistic-concurrency token)

TABLE webhook_subscriptions
  id                  uuid        PK
  connector_id        uuid        NOT NULL                -- FK → external_connectors.id ON DELETE CASCADE
  callback_path       string      NOT NULL
  signing_secret      json        NOT NULL                -- EncryptedCredentials VO
  signing_algorithm   enum        NOT NULL                -- HmacSha256|BearerToken
  is_enabled          bool        NOT NULL DEFAULT true

TABLE mapping_profiles
  id              uuid        PK
  portal_name     string      NOT NULL
  schema_version  string      NOT NULL
  direction       enum        NOT NULL                    -- Inbound|Outbound|Bidirectional
  is_active       bool        NOT NULL DEFAULT false
  created_on_utc  datetime    NOT NULL
  updated_on_utc  datetime    NOT NULL
  UNIQUE (portal_name, schema_version, direction) WHERE is_active = true

TABLE field_mappings
  id              uuid        PK
  profile_id      uuid        NOT NULL                    -- FK → mapping_profiles.id ON DELETE CASCADE
  source_path     string      NOT NULL
  target_path     string      NOT NULL
  transform_kind  enum        NOT NULL
  transform_args  json        NULL
  is_required     bool        NOT NULL DEFAULT false
  UNIQUE (profile_id, source_path)

TABLE sync_records
  id                  uuid        PK
  direction           enum        NOT NULL                -- Inbound|Outbound
  partner_id          uuid        NULL                    -- no FK across the partner if archived
  connector_id        uuid        NULL
  portal_name         string      NOT NULL                -- ExternalRef component
  external_job_id     string      NOT NULL                -- ExternalRef component
  internal_job_id     uuid        NULL                    -- BC-4 posting id, NO FK
  status              enum        NOT NULL                -- Accepted|Quarantined|Synced|Failed|Archived
  raw_payload         json        NOT NULL
  normalised_snapshot json        NULL
  error_code          string      NULL
  error_message       string      NULL
  last_sync_on_utc    datetime    NOT NULL
  created_on_utc      datetime    NOT NULL
  version_token       (optimistic-concurrency token)
  UNIQUE (portal_name, external_job_id) WHERE status <> 'Archived'
  INDEX (status)
  INDEX (internal_job_id)

TABLE sync_attempts
  id                  uuid        PK
  sync_record_id      uuid        NOT NULL                -- FK → sync_records.id ON DELETE CASCADE
  attempt_no          int         NOT NULL
  outcome             enum        NOT NULL                -- Success|TransientFailure|PermanentFailure
  response_code       int         NULL
  detail              string      NULL
  processing_ms       int         NOT NULL
  was_manual_override bool        NOT NULL DEFAULT false
  attempted_on_utc    datetime    NOT NULL
  INDEX (sync_record_id, attempt_no)

TABLE verification_requests
  id                            uuid        PK
  kind                          enum        NOT NULL      -- Identity|Education|Employer
  subject_user_id               uuid        NULL          -- BC-1, NO FK
  subject_job_seeker_profile_id uuid        NULL          -- BC-3, NO FK
  subject_employer_id           uuid        NULL          -- BC-2, NO FK
  registry                      json        NOT NULL      -- Registry VO
  consent                       json        NOT NULL      -- ConsentRecord VO
  request_payload               json        NOT NULL      -- MinimisedRequestPayload (encrypted at rest)
  status                        enum        NOT NULL      -- Pending|InProgress|Verified|Unverified|Error
  result                        json        NULL          -- VerificationResult VO
  failure_reason                string      NULL
  cached_until_utc              datetime    NULL
  created_on_utc                datetime    NOT NULL
  updated_on_utc                datetime    NOT NULL
  version_token                 (optimistic-concurrency token)
  INDEX (kind, subject_user_id, subject_employer_id, subject_job_seeker_profile_id)
  INDEX (status)

TABLE government_audit_entries          -- IMMUTABLE, append-only, 7-year retention
  id                       uuid        PK
  verification_request_id  uuid        NULL
  actor_id                 uuid        NULL
  registry_name            string      NOT NULL
  direction                enum        NOT NULL          -- Query|Response
  query_parameters         json        NOT NULL          -- masked per privacy rules
  result_code              string      NOT NULL
  response_size_bytes      int         NULL
  transformations_applied  string      NULL
  consent_status_at_time   string      NOT NULL
  integrity_hash           string      NOT NULL          -- hash-chain, tamper-evidence
  occurred_on_utc          datetime    NOT NULL
  INDEX (verification_request_id)
  INDEX (occurred_on_utc)

TABLE api_version_registry
  id              uuid        PK                          -- singleton

TABLE api_versions
  id                          uuid        PK
  registry_id                 uuid        NOT NULL        -- FK → api_version_registry.id
  version                     string      NOT NULL UNIQUE -- v1|v2
  status                      enum        NOT NULL        -- Active|Deprecated|Sunset
  released_on_utc             datetime    NOT NULL
  deprecation_announced_on_utc datetime   NULL
  sunset_on_utc               datetime    NULL
  migration_guide_url         string      NULL

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

### 11.2 Module-specific mapping notes

- Child collections (`api_keys`, `webhook_subscriptions`, `field_mappings`, `sync_attempts`, `api_versions`) are **owned** by their root and loaded with it.
- Value objects map to `json` columns, **except** ones needing querying/uniqueness: `status`, `key_hash`, `portal_name` + `external_job_id` are flattened to scalar columns.
- Optimistic-concurrency tokens are required on `partners`, `external_connectors`, `sync_records`, `verification_requests` (all updatable concurrently).
- **Government data fields** (`request_payload`, sensitive parts of `result`) are encrypted at rest — apply a value converter that runs through the `CredentialEncryptionPort`. `government_audit_entries` is configured **without** any update/delete in the repository (append-only).
- General persistence conventions (one persistence context per module, outbox/inbox wiring, value-object and strongly-typed-id mapping) follow [[00-Shared-Foundations]] §3 and §6.

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

`PartnerRepository` (`GetById`, `GetByApiKeyHash`, `Add`, `Update`), `ExternalConnectorRepository` (`GetById`, `ListDueForPull`, `ListWithPushOnPublish`, `Add`, `Update`), `MappingProfileRepository` (`GetById`, `GetActive(portalName, schemaVersion, direction)`, `Add`, `Update`), `SyncRecordRepository` (`GetById`, `GetByExternalRef`, `ListByStatus`, `ListByConnector`, `Add`, `Update`), `VerificationRequestRepository` (`GetById`, `GetLatestForSubject`, `Add`, `Update`), `GovernmentAuditRepository` (`Append`, `GetLastEntryHash`, `Query` — **no update/delete**), `ApiVersionRegistryRepository` (`GetSingleton`, `Update`), `UnitOfWork` (`SaveChanges`).

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. **Two distinct surfaces:**

**A. Internal admin API** — route prefix `/api/integration`. Requires a valid BC-1 access token with an admin/engineer role (validated via the `TokenValidationApi` port). For MoL admins and Integration Engineers.

**B. Public OHS API** — route prefix `/api/v{version}` (path-versioned, US-3.4.3-05). Two auth modes: **partner API key** (`Authorization: Bearer <api-key>`, validated against `api_keys.key_hash`, plus IP-whitelist and rate-limit checks) for portal-to-platform calls; **user access token** (validated via the `TokenValidationApi` port) for integrator user calls. JSON only, UTF-8, ISO-8601 timestamps, RESTful resource URLs, standard envelope `{ status, data | errors, meta:{ timestamp, requestId } }`.

Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

### 12.1 Public OHS API (partner-facing)

| Verb & route | Command/Query | Success | Notable failures |
|---|---|---|---|
| `POST /api/v1/jobs/push` | `PushJobViaApiCommand` | `201` + `{ status:"success", job_id:"jp_..." }` | `400 E-API-VALIDATION-ERROR` (field-level), `401 E-API-UNAUTHORIZED`, `403 E-API-IP-FORBIDDEN`, `409 E-API-DUPLICATE-JOB`, `429 E-API-RATE-LIMITED`, `401 E-API-KEY-EXPIRED` |
| `PATCH /api/v1/jobs/{job_id}` | `UpdateJobViaApiCommand` / `ChangeJobStatusViaApiCommand` | `200` | `401`, `404`, `429` |
| `GET /api/v1/jobs/{job_id}` | `GetSyncRecordDetailQuery` | `200` | `401`, `404` |
| `POST /api/webhooks/external-portal/{portalId}` | `ProcessWebhookUpdateCommand` | `202 Accepted` | `401 E-WEBHOOK-SIGNATURE-INVALID` |
| `GET /api/v1/docs` *(anonymous)* | `GetOpenApiSpecQuery` | `200` interactive API explorer / OpenAPI JSON | |
| `GET /api/v{n}/...` to a sunset version | — | `410 Gone` + migration guide link | |

Deprecated versions add `Deprecation: true` and `Sunset: <date>` response headers (US-3.4.3-05 AC-03).

### 12.2 Internal admin API (MoL admin / Integration Engineer)

| Verb & route | Command/Query | Success | Notable failures |
|---|---|---|---|
| `POST /api/integration/partners` | `RegisterPartnerCommand` | `201` + `PartnerId` | `400` |
| `POST /api/integration/partners/{id}/approve` | `ApprovePartnerCommand` | `200` (key delivered out-of-band) | `409` not pending |
| `POST /api/integration/partners/{id}/api-key/regenerate` | `RegenerateApiKeyCommand` | `200` + plaintext key (shown once) | `404` |
| `DELETE /api/integration/partners/{id}/api-key/{keyId}` | `RevokeApiKeyCommand` | `204` | `404` |
| `PUT /api/integration/partners/{id}/ip-whitelist` | `SetPartnerIpWhitelistCommand` | `200` | `400` invalid IP/CIDR |
| `PUT /api/integration/partners/{id}/rate-limit` | `SetPartnerRateLimitCommand` | `200` | `400` |
| `POST /api/integration/connectors` | `ConfigureExternalConnectorCommand` | `201` + `ConnectorId` | `400` |
| `POST /api/integration/connectors/{id}/test` | `TestConnectorConnectionCommand` | `200` + `{ connected: bool }` | |
| `PUT /api/integration/connectors/{id}/credentials` | `RotateConnectorCredentialsCommand` | `200` | `400` |
| `PUT /api/integration/connectors/{id}/sync-options` | `SetConnectorSyncOptionsCommand` | `200` | `400` |
| `POST /api/integration/connectors/{id}/webhooks` | `AddWebhookSubscriptionCommand` | `201` | `400` |
| `POST /api/integration/mapping-profiles` | `ConfigureMappingProfileCommand` | `201` | `400` |
| `POST /api/integration/mapping-profiles/{id}/field-mappings` | `AddFieldMappingCommand` | `201` | `409` duplicate source path |
| `POST /api/integration/mapping-profiles/{id}/activate` | `ActivateMappingProfileCommand` | `200` | `409` another active profile |
| `GET /api/integration/partners/{id}/submission-logs` | `GetSubmissionLogsQuery` | `200` paged | |
| `GET /api/integration/submission-logs/{logId}` | `GetSubmissionLogDetailQuery` | `200` | `404` |
| `GET /api/integration/partners/{id}/sync-dashboard` | `GetPartnerSyncDashboardQuery` | `200` | |
| `GET /api/integration/partners/{id}/usage-stats` | `GetPartnerUsageStatsQuery` | `200` | |
| `GET /api/integration/dashboard` | `GetIntegrationDashboardQuery` | `200` | |
| `GET /api/integration/connectors/{id}/sync-log` | `GetConnectorSyncLogQuery` | `200` paged | |
| `GET /api/integration/sync-logs/export` | `ExportSyncLogsQuery` | `200` CSV | |
| `GET /api/integration/failed-syncs` | `GetFailedSyncQueueQuery` | `200` | |
| `GET /api/integration/sync-records/{id}` | `GetSyncRecordDetailQuery` | `200` (RCA view) | `404` |
| `POST /api/integration/sync-records/{id}/retry` | `RetrySyncRecordCommand` | `200` | `404`, `409` not in retryable state |
| `POST /api/integration/sync-records/{id}/override` | `ManualOverrideSyncRecordCommand` | `200` | `404` |
| `POST /api/integration/verifications/identity` | `VerifyIdentityViaGovernmentCommand` | `202` (async) + `VerificationRequestId` | `403 E-GOV-CONSENT-REQUIRED` |
| `POST /api/integration/verifications/education` | `VerifyEducationalCredentialCommand` | `202` + id | `403 E-GOV-CONSENT-REQUIRED`, `422 E-GOV-INSTITUTION-NOT-INTEGRATED` |
| `GET /api/integration/verifications/{id}` | `GetVerificationStatusQuery` | `200` | `404` |
| `POST /api/integration/consent` | `RecordConsentDecisionCommand` | `200` | `400` |
| `DELETE /api/integration/government-data/users/{userId}` | `DeleteGovernmentDataForUserCommand` | `202` (honored within 30 days) | `404` |
| `GET /api/integration/government-audit` | `GetGovernmentAuditTrailQuery` | `200` (compliance-officer only; the read is itself audited) | `403` |
| `GET /api/integration/government-audit/export` | `ExportGovernmentAuditTrailCommand` | `200` signed export | `403` |
| `GET /api/integration/api-versions` | `GetApiVersionsQuery` | `200` | |
| `POST /api/integration/api-versions/{version}/deprecate` | `DeprecateApiVersionCommand` | `200` | `400` sunset < 6 months |

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** every VO factory — valid input succeeds, each invalid case returns the right `Error`. Specifically: `EmailAddress` (RFC 5322), `RateLimit` (zero/negative fails), `EncryptedCredentials` (string representation returns `"***"`, never the ciphertext), `SalaryRange` (`min > max` fails, bad currency fails), `NormalisedJobPosting` (missing title/description/location fails — the quarantine trigger), `MinimisedRequestPayload` (a non-whitelisted field fails), `ApiVersion` (`Version` not matching `^v\d+$` fails).
- **Partner aggregate:** status machine — every legal transition succeeds, illegal ones fail; `IssueApiKey`/`RegenerateApiKey` revoke the prior active key (assert at most one `Active` key); `Revoke` revokes all keys; IP whitelist rejects malformed IPs.
- **SyncRecord aggregate:** status machine ordering; `Quarantine` reachable only from `Accepted`; `RecordAttempt` caps at 3 then `Failed`; `SyncAttempt` list is append-only; `MarkSynced` on inbound raises `ExternalJobIngested`, on a re-fetch updates not duplicates.
- **VerificationRequest aggregate:** the consent gate — `StartIdentity/Education/Employer` with `consent.Granted == false` returns `E-GOV-CONSENT-REQUIRED` and raises nothing; status machine; `RecordVerified` by `Kind` raises the correct integration event; `IsCacheValid` boundary at 12 months; `RecordError` is non-blocking.
- **GovernmentAuditEntry aggregate:** write-once — assert there are no mutator methods; `IntegrityHash` changes if any field changes; hash chain links to the previous entry's hash.
- **ApiVersionRegistry aggregate:** `DeprecateVersion` with sunset < 6 months fails; `SunsetVersion` before the sunset date fails; cannot sunset below 2 active majors.
- **Domain services:** `JobDataTransformer` — table-driven: each `TransformKind` produces the right target; a missing required mapping yields `E-SYNC-MISSING-FIELD`; outbound round-trips. `DuplicateJobDetector` — exact `ExternalRef` match and fuzzy `(title,company,location)` match both detected. `WebhookSignatureVerifier` — valid HMAC passes, tampered body fails, constant-time compare. `ConsentEvaluator` — consent + minimisation enforced together. `AuthHashChainer` / `AuditHashChainer` — chain verification detects an altered middle entry. `ApiKeyGenerator` — token ≥ 32 chars, plaintext ≠ hash.

### 13.2 Unit tests — Application (handlers, ports replaced with test doubles)

- `PushJobViaApiCommand`: valid payload → `SyncRecord` synced, `ExternalJobIngested` queued to outbox; missing title → `E-API-VALIDATION-ERROR`, nothing persisted; duplicate `ExternalRef` → `E-API-DUPLICATE-JOB`; expired/revoked key → `E-API-UNAUTHORIZED`.
- `IngestExternalJobsCommand`: a foreign job missing a required field → `SyncRecord` quarantined + `SyncErrorDetected` emitted, the rest still ingest; an already-known external job updates its existing record; network error retried ×3 then `Failed`.
- `ExportJobCommand`: success → outbound `SyncRecord` synced; 3 failures → `Failed` + `SyncErrorDetected` + Integration Engineer alert path.
- `VerifyIdentityViaGovernmentCommand`: consent granted + registry match → `IdentityVerifiedByGovernment` queued, two `GovernmentAuditEntry` rows (query + response) written in the same transaction; consent withheld → `E-GOV-CONSENT-REQUIRED`, registry **never called**, no event; a valid 6-month-old cached `Verified` result → registry not called again.
- `VerifyEducationalCredentialCommand`: institution not integrated → `E-GOV-INSTITUTION-NOT-INTEGRATED`, seeker prompted to upload docs.
- `DeleteGovernmentDataForUserCommand`: all of the user's `VerificationRequest`s are tombstoned and a deletion audit entry is written.
- `ProcessWebhookUpdateCommand`: tampered signature → `E-WEBHOOK-SIGNATURE-INVALID`, HTTP 401, nothing queued; valid → 202 and async processing; a webhook + a poll for the same job-id/timestamp → processed once.
- `DeprecateApiVersionCommand`: sunset < 6 months out → rejected.
- Validation behavior: each validator rejects the documented bad inputs before the handler runs.

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip each aggregate including child collections and `json` VOs; optimistic-concurrency conflict is detected; `SyncRecordRepository.GetByExternalRef` and the partial unique index enforce inbound dedupe; `GovernmentAuditRepository` rejects update/delete.
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `external_job_sync`.
- **Outbox:** an ingestion writes both the `SyncRecord` change and the `ExternalJobIngested` outbox row in one transaction; rolling back leaves neither.
- **Inbox / idempotency:** delivering `JobPostingPublishedIntegrationEvent` twice exports once and is a no-op the second time.
- **Encryption at rest:** `external_connectors.credentials` and `verification_requests.request_payload` are ciphertext on disk; decrypt round-trips through the port.
- **Audit hash chain:** insert N audit entries, then verify the chain; manually alter one row's `query_parameters` and confirm chain verification fails at that row.
- **API:** host-level tests — partner push happy path returns `201` + `job_id`; non-whitelisted IP → `403 E-API-IP-FORBIDDEN`; over rate limit → `429`; a request to a sunset API version → `410`.
- **Consumed events:** `EmployerVerificationRequestedIntegrationEvent` triggers an `Employer` verification and emits `EmployerVerifiedByGovernment`; `TaxonomyUpdatedIntegrationEvent` flags affected `SkillTaxonomyMap` field mappings.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "Partner pushes a job via the public API"

End-to-end, to pattern-match every other command against:

1. **API.** `POST /api/v1/jobs/push` with `Authorization: Bearer <api-key>` and a JSON job payload `{ my_job_title, my_desc, my_location, my_salary_range, ... }`. The **API-key middleware** hashes the presented key (SHA-256), looks it up in `api_keys`, checks the key is `Active` and not expired (`E-API-KEY-EXPIRED`), the calling IP is whitelisted (`E-API-IP-FORBIDDEN`), and the partner is under its rate limit (`E-API-RATE-LIMITED`). It resolves the `PartnerId` and builds `PushJobViaApiCommand { PartnerId, RawPayload }`, dispatches it through the mediator.
2. **Validation step.** `PushJobViaApiCommand`'s validator runs: required fields present, salary min ≤ max. On failure → `Result` with `E-API-VALIDATION-ERROR` carrying field-level messages, mapped to `400`.
3. **Handler.** `PushJobViaApiCommandHandler`:
   a. `SchemaVersionDetector.Detect(rawPayload, knownVersions)` → which foreign schema this is.
   b. `MappingProfileRepository.GetActive(portalName, schemaVersion, Inbound)` → the `MappingProfile`.
   c. `JobDataTransformer.ToNormalised(rawPayload, profile, TaxonomyApi.MapSkillToTaxonomyCode, GeocodingPort.Normalise)` → `Result<NormalisedJobPosting>`. If a required mapping yields nothing → `E-SYNC-MISSING-FIELD`: create the `SyncRecord` and call `Quarantine(...)`, return (no `ExternalJobIngested`).
   d. `DuplicateJobDetector.IsDuplicate(externalRef, normalisedJob, SyncRecordRepository.GetByExternalRef, fuzzyMatch)` → if duplicate, `E-API-DUPLICATE-JOB`.
   e. `SyncRecord.StartInbound(externalRef, rawPayload, partnerId)` → `RecordNormalised(snapshot)` → `MarkSynced(internalJobId: null)` — the aggregate raises `ExternalJobIngestedIntegrationEvent`.
   f. `repository.Add(syncRecord)`; `unitOfWork.SaveChanges()`.
4. **Domain-event / outbox step.** After the unit of work is saved, the mediator pipeline writes `ExternalJobIngestedIntegrationEvent` (payload = the full `NormalisedJobPosting`) into the outbox — same transaction as the `SyncRecord` insert. ([[00-Shared-Foundations]] §6.1–6.2.)
5. **Relay.** The background outbox relay publishes the event; **BC-4 Job Postings** consumes it and creates the mirrored posting (then BC-4's own `JobPostingPublished` fans out to BC-6 search, BC-7 matching). **BC-10 Reporting** also consumes it.
6. **Response.** Handler returns `Result` success with the new `SyncRecordId` rendered as `job_id` (`jp_...`); the endpoint returns `201` + `{ status:"success", job_id:"jp_..." }`.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.1.3-01 Register third-party portal | Portal created in `PendingActivation`; admin approval → `Active` + API key issued out-of-band; key is ≥32-char random, stored hashed; regenerate revokes old key immediately; IP whitelist rejects others (`E-API-IP-FORBIDDEN`); rate limit (`E-API-RATE-LIMITED`); optional expiry → `E-API-KEY-EXPIRED`. |
| US-3.1.3-02 Push jobs via API | `POST /jobs/push` → `201` + `job_id`; field-level validation (`E-API-VALIDATION-ERROR`); automatic source attribution + backlink; `PATCH` update/status-change; duplicate detection (`E-API-DUPLICATE-JOB`); invalid key → `401`; over limit → `429`. |
| US-3.1.3-03 Configure data mapping | OpenAPI schema published with field types/constraints; per-portal mapping config saved and applied; sandbox/test environment isolated from production; test jobs invisible to seekers; OpenAPI spec downloadable. |
| US-3.1.3-04 View integration logs | Paginated submission logs with timestamp/title/status/code; filter/search; log detail (payload, response, processing time); sync dashboard counts; usage stats + trends; immutable audit trail; MoL-admin provenance view. |
| US-3.4.1-01 Ingest jobs from external portal | Scheduled pull fetches new/updated jobs; incomplete jobs logged + quarantined; dedupe by external ID updates not duplicates; sync metadata recorded (source, timestamp, external ID, status). |
| US-3.4.1-02 Export jobs to external portal | Push on publish + scheduled batch; failures retried then marked "export pending" + engineer alerted; export status tracked (portal ID, portal's job ID, timestamp, status). |
| US-3.4.1-03 Map and transform job data | Transformation rules map portal-native → system schema; missing required field → schema-validation error + quarantine; external identifiers preserved; multiple foreign schema versions detected and handled. |
| US-3.4.1-04 Configure external portal credentials | Add connector with encrypted (AES-256) credentials; "Test Connection" health check; configure sync options (interval, push-on-publish, mapping profile); credentials never logged plaintext; rotate credentials. |
| US-3.4.1-05 Monitor integration dashboard | Per-portal health summary (healthy/warning/error per defined thresholds); detailed sync logs (last 100); alert after 3 consecutive failures; CSV log export. |
| US-3.4.1-06 Reconcile sync errors | Failed-sync queue with error code/message; manual retry; manual override with logging; RCA detail view (stack, request/response payload, retry count); quarantine 30-day retention. |
| US-3.4.1-07 Handle real-time job updates | Webhook POST received on registered callback; HMAC-SHA256 signature verified (`E-WEBHOOK-SIGNATURE-INVALID`); async processing → `202`; webhook/polling duplicate reconciled by job-id + timestamp. |
| US-3.4.2-01 Verify educational credentials | Verification initiated against institution DB; result recorded (verified/unverified/error); institution-not-integrated → upload-docs prompt; audit trail (seeker, institution, timestamp, result, source); 12-month cache. |
| US-3.4.2-02 Verify identity via government system | Gov ID verification initiated; result recorded; declined consent → continue as "unverified"; audit trail incl. consent status; system stores result not full ID details. |
| US-3.4.2-03 Integrate MoL/PEF databases | Authenticated connection + schema-compat check; query for verification/enrichment; cached with refresh policy; DB-unavailable → cache fallback or "verification pending". |
| US-3.4.2-04 Maintain audit trail for government data | Every query + response logged (timestamp, actor, params, DB, result code); audit log access restricted + itself logged; ≥7-year tamper-resistant retention; signed compliance export. |
| US-3.4.2-05 Enforce privacy compliance | Consent tracked with version + timestamp; no consent → query skipped; data minimisation (whitelist); right-to-be-forgotten deletion within 30 days; AES-256 at rest. |
| US-3.4.3-01 Provide comprehensive API framework | Core endpoints published (jobs, employers, seekers, applications read-only, health); role-scoped CRUD; standardised envelope (status, data/errors, meta); error responses with code + message + hint. |
| US-3.4.3-02 Implement RESTful APIs with JSON | Correct HTTP verbs; resource-oriented URLs; JSON-only responses; JSON request bodies validated against schema; correct status codes (200/201/400/401/404/500); UTF-8; ISO-8601. |
| US-3.4.3-03 Publish API documentation | OpenAPI 3.0.x spec covers all endpoints; interactive explorer with "Try it"; code examples; sandbox; auth guide; docs versioned alongside API. |
| US-3.4.3-05 Support API versioning | URL-path versioning `/api/v{n}/`; previous version keeps behavior on breaking change; 6-month deprecation notice + response warnings + migration guide + sunset date; version negotiation routes to the right handler; sunset version → `410`. |

---

## Appendix — teaching notes & open questions

- **The ACL is a domain service, not an afterthought.** `JobDataTransformer` + `MappingProfile` *are* the anti-corruption layer. Show the class the alternative: if BC-4 consumed a foreign payload directly, every portal's quirks would leak into BC-4's `JobPosting` model. The ACL's cost (a rulebook to maintain) buys BC-4 a clean, stable language.
- **One BC, three modules — was that right?** This package keeps `PartnerJobSync`, `GovernmentVerification`, and `PublicApiFramework` as folders in one BC. They share *responsibility* ("we are the edge") and *plumbing* (outbox/inbox, encryption) but barely share *vocabulary*. Good debate: is shared responsibility enough to justify one bounded context, or does divergent ubiquitous language demand a split? What does the team-topology / Conway's-law angle say?
- **ACL and OHS in the same BC.** This BC is *downstream* (ACL) of external systems for inbound data and *upstream* (OHS) of integrators for outbound. Two opposite strategic patterns, one context. Worth contrasting: the ACL protects *us* from *them*; the OHS protects *them* from *our* churn (via versioning).
- **Quarantine vs. fail.** A missing required field quarantines (human review); a network blip fails after 3 retries (machine retry). Discuss why conflating these two — treating bad data like a transient error, or vice versa — produces either infinite retry loops or silent data loss.
- **Hash-chained audit.** `GovernmentAuditEntry` is append-only and hash-chained. Ask the class: is an application-level hash chain "tamper-resistant enough," or does compliance demand a WORM store / external notary? Where's the line between *tamper-evident* and *tamper-proof*?
- **Consent as an invariant, not a checkbox.** `ConsentEvaluator` makes "no consent ⇒ no registry call" an enforced domain invariant, not UI etiquette. This is privacy-by-design in code. Contrast with consent being checked only at the API layer.
- **`ExternalJobIngested` payload ownership.** This BC emits a *fully normalised* posting; BC-4 mirrors it. Discuss: should the event carry the whole normalised posting (fat event, BC-4 needs nothing else) or just an ID + ref (thin event, BC-4 calls back)? We chose fat — fewer round-trips, but the payload is now a wider contract.
- **`SyncRecord` as both audit log and work item.** It's the reconciliation work item *and* the immutable-ish audit record. Ask whether those two jobs should be one aggregate or two — a classic "is this one concept or two?" modeling question.
- **Localization & jurisdiction.** Audit retention defaults to 7 years but is "configurable per jurisdiction"; consent versions are explicit. The defaults here follow the `US-3.4.2` stories and are configuration, not hard-coded policy.
