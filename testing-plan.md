# Nexhire Core — Testing Plan

## Goal
Test each Bounded Context step-by-step in dependency order, from foundation to edge modules, ensuring every module compiles and passes its tests before moving to the next dependency level.

## Constraints
- Begin from modules with zero dependency on other modules
- Progress only when current level compiles & passes its tests
- Prefer small, targeted commits per module level

---

## Module Dependency Graph

```
Phase 1: Shared Layer (zero module deps)
  Nexhire.Shared.Core → Nexhire.Shared.Infrastructure

Phase 2: Foundation Modules (no cross-module deps)
  IdentityAccess
  AdministratorsConfiguration

Phase 3: Core Business Modules (consume from foundation)
  EmployerProfiles
  JobSeekerProfile
  JobPostings

Phase 4: Transaction Modules (consume from core)
  JobApplication
  ExternalJobSync

Phase 5: Intelligence Modules (consume from core + transaction)
  RecommendationEngine
  SearchDiscovery

Phase 6: Generic/Supporting Modules (read-side, port adapters)
  Reporting
  ContentManagement
  Notification
```

---

## Phase 1 — Shared Layer

### Step 1.1 — Nexhire.Shared.Core
| Focus | Test Areas |
|-------|-----------|
| `Result`/`Result<T>` monad | Success, failure, implicit conversion, error combining |
| `Error` sentinels | `None`, `NullValue`, `ValidationError`, equality |
| `Entity<TId>` | Identity-based equality, `GetHashCode`, `ToString` |
| `ValueObject` | Structural equality via `GetEqualityComponents()` |
| `AggregateRoot<TId>` | `RaiseDomainEvent()`, `ClearDomainEvents()`, event list management |
| `IDomainEvent` | `EventId` generation, `OccurredOnUtc` timing |
| `ICommand`/`IQuery<T>` | Contract adherence |

### Step 1.2 — Nexhire.Shared.Infrastructure
| Focus | Test Areas |
|-------|-----------|
| `LoggingBehavior` | Log output on success, log output on failure, error detail propagation |
| `ValidationBehavior` | Validator pass-through, validator failure → `Result.Failure`, multiple error combining |
| `PublishDomainEventsInterceptor` | Direct dispatch path, outbox dispatch path, no-event-no-op |
| `OutboxMessage`/`InboxMessage` | Entity config, serialization round-trip |
| `ResultExtensions` | HTTP mapping from `Result<T>` to `IResult` |

---

## Phase 2 — Foundation Modules

### Step 2.1 — IdentityAccess (4-project Clean Architecture)
| Layer | Focus | Key Tests |
|-------|-------|-----------|
| **Domain** | `FullName` VO, `EmailAddress` VO, `UserAccount` aggregate factory (success + error paths), `UserAccount.Activate()`/`Deactivate()`/`Suspend()`/`Reinstate()`, `UserCreatedEvent`, `UserAccountActivatedIntegrationEvent` | Unit |
| **Application** | `CreateUserCommand` + handler (with mocked repo), `GetUserByIdQuery` + handler, `UpdateProfileCommand` + handler, validation via FluentValidation | Unit |
| **Infrastructure** | `IdentityAccessDbContext` entity config, `UserAccountConfiguration` (EF mapping), `UserAccountRepository` impl (in-memory), pending migration smoke test | Integration |
| **Presentation** | Minimal API endpoint contract tests with `WebApplicationFactory` | Functional |

### Step 2.2 — AdministratorsConfiguration
| Focus | Key Tests |
|-------|-----------|
| `Taxonomy` aggregate (hierarchy management) | Create, add term, deprecate term, update term |
| `TaxonomyImportService` | CSV/JSON import, duplicate detection, rollback on error |
| `TaxonomyTermAdded`/`Deprecated`/`Updated` domain events | Raising, serialization |
| `IntegrationEventPublisher` | Outbox serialization round-trip |
| Outbox relay | `AdministratorsConfigurationOutboxRelayBackgroundService` publishes correctly |
| `ITaxonomyApi` impl | Cache-invalidation behavior, query by code/path |

---

## Phase 3 — Core Business Modules

### Step 3.1 — EmployerProfiles
| Focus | Key Tests |
|-------|-----------|
| `EmployerProfile` aggregate | Creation, verification, deactivation lifecycle |
| `Shortlist` VO | Equality, creation rules |
| Dashboard projections | Read-side projection updates |
| Integration event consumers (7 handlers) | `UserAccountActivatedConsumer`, `AccountDeactivatedConsumer`, `UserAccountSuspendedConsumer`, `UserAccountReinstatedConsumer`, `EmployerVerifiedByGovernmentConsumer`, `EmployerVerificationFailedByGovernmentConsumer` |
| Cross-module event consumption (3 handlers) | `JobPostingPublishedConsumer`, `JobPostingClosedConsumer`, `ApplicationSubmittedConsumer`, `CandidateRecommendationGeneratedConsumer` |

### Step 3.2 — JobSeekerProfile
| Focus | Key Tests |
|-------|-----------|
| `JobSeekerProfile` aggregate | Creation, update, profile completion |
| `Resume` entity | Upload, parse, versioning |
| `ProfileHistory` | Audit trail |
| Port stubs | `IQrCodeGenerator`, `IResumeParser`, `ITaxonomyApi` contract behavior |

### Step 3.3 — JobPostings
| Focus | Key Tests |
|-------|-----------|
| `JobPosting` aggregate | Publish, close, expire, suspend lifecycle |
| Domain services | `SchemaOrgStandardizer`, `PostingExpirationPolicy`, `JobPostingRenewalService` |
| Integration event consumers (5 handlers) | `EmployerStandingProjectionHandlers`, `EmployerAccountClosedHandlers`, `ExternalJobIngestedIntegrationEventHandler`, `TaxonomyUpdatedIntegrationEventHandler`, `PostingMetricsProjectionHandlers` |
| Background services | `PostingExpirationBackgroundService`, `JobPostingsOutboxRelayBackgroundService` |
| Outbox | Serialization, relay, at-least-once delivery |

---

## Phase 4 — Transaction Modules

### Step 4.1 — JobApplication
| Focus | Key Tests |
|-------|-----------|
| `Application` aggregate | Submit, withdraw, status transitions (state machine) |
| Idempotency | `IIdempotencyKeyStore` contract |
| Integration event consumers | `JobPostingClosedConsumer`, `SeekerAccountDeactivatedConsumer` |
| Background service | `IdempotencyKeyPurgeBackgroundService` |

### Step 4.2 — ExternalJobSync
| Focus | Key Tests |
|-------|-----------|
| Aggregates | `Partner`, `ExternalConnector`, `MappingProfile`, `SyncRecord` |
| `CredentialEncryptionAdapter` | Real encryption round-trip |
| Port stubs (6) | Contract behavior for each adapter |
| Integration event consumers | `JobPostingPublishedIntegrationEventHandler`, `EmployerVerificationRequestedIntegrationEventHandler` |
| Background services | `SyncSchedulerWorker`, `StaleRecordArchiverWorker`, `ApiKeyExpirySweepWorker` |

---

## Phase 5 — Intelligence Modules

### Step 5.1 — RecommendationEngine
| Focus | Key Tests |
|-------|-----------|
| Domain services | `MatchScoringService`, `RecommendationRankingService`, `CandidateRankingService`, `FitAnalysisService`, `MatchThresholdResolver` |
| Supporting services | `CandidatePrivacyFilter`, `AbVariantAllocator`, `ImpactPreviewCalculator` |
| AI port stubs (5) | Embedding, vector index, NLP, collaborative filtering contract behavior |
| `EmployerAccessApi` stub | Contract behavior |
| Outbox relay | `RecommendationEngineOutboxRelayBackgroundService` |

### Step 5.2 — SearchDiscovery
| Focus | Key Tests |
|-------|-----------|
| `JobIndexEntry` | Indexing logic, mapping from JobPosting |
| Aggregates | `SavedSearch`, `SearchSession`, `FavoriteJob` |
| `IRecommendationQueryApi` stub | Contract behavior |
| Integration event consumers (6) | JobPosting events (published/updated/expired/closed/suspended/reinstated), `MatchComputed`, `RecommendationGenerated`, `TaxonomyUpdated` |

---

## Phase 6 — Generic/Supporting Modules

### Step 6.1 — Reporting
| Focus | Key Tests |
|-------|-----------|
| Aggregates | `ReportDefinition`, `ReportRun`, `ReportSchedule`, `RetentionPolicy`, `AlertRule` |
| Read stores | `ActivityReadStore`, `AnalyticsReadStore`, `PerformanceReadStore`, `ReportAccessLogStore`, `InboxStore` |
| `ProjectorService` | Projection logic |
| Port stubs (4) | `IReportRenderer`, `IObjectStorage`, `IColdStorageArchive`, `IClock` |
| Outbox relay | `ReportingOutboxRelayBackgroundService` |

### Step 6.2 — ContentManagement
| Focus | Key Tests |
|-------|-----------|
| Aggregates | `Article`, `Category`, `FaqEntry`, `Topic`, `GuidedTour`, `ContentPreference` |
| `IJobSeekerProfileQueryApi` | Composition root adapter test |
| Background service | `ScheduledPublicationWorker` |
| Outbox relay | `ContentManagementOutboxRelayBackgroundService` |

### Step 6.3 — Notification
| Focus | Key Tests |
|-------|-----------|
| `Notification` aggregate | Creation, delivery tracking |
| Entities | `RecipientPreferences`, `NotificationTemplate`, `Digest`, `NotificationLog` |
| Domain services | `TemplateRenderer`, `ChannelFanoutPlanner`, `FrequencyCapEvaluator`, `DndScheduleCalculator`, `DigestAssembler` |
| Channel stubs (4) | Email, SMS, Push, DNC contract behavior |
| Background services | `OutboxRelayWorker` (5s), `DigestSchedulerWorker` (5min), `DndReleaseWorker` (1min), `SoftBounceRetryWorker` (1hr), `RetentionWorker` (24hr) |

---

## Phase 7 — Cross-Module Integration

### Step 7.1 — Composition Root
| Focus | Test |
|-------|------|
| DI container | Spin up `WebApplicationFactory`, verify all 12 modules register without conflicts |
| Module registration order | `AddSharedInfrastructure` + each `Add[Module]Module` |
| MediatR pipeline | Both behaviors registered in correct order |

### Step 7.2 — End-to-End Event Flow
| Flow | Path |
|------|------|
| User → Profile | IdentityAccess creates user → `UserAccountActivatedIntegrationEvent` → EmployerProfiles projection updates |
| Posting → Search | EmployerProfiles creates posting → search index updated |
| Application → Recommendation | Application submitted → recommendation engine triggered |

### Step 7.3 — Direct Dispatch Modules
| Module | Verification |
|---------|-------------|
| IdentityAccess | Events fire within the save transaction |
| EmployerProfiles | Same |
| JobSeekerProfile | Same |
| JobApplication | Same |
| SearchDiscovery | Same |

### Step 7.4 — Outbox Modules
| Module | Verification |
|---------|-------------|
| JobPostings | Events land in `OutboxMessages` table, relay publishes them |
| ExternalJobSync | Same |
| RecommendationEngine | Same |
| Reporting | Same |
| AdministratorsConfiguration | Same |
| ContentManagement | Same |
| Notification | Same |

### Step 7.5 — Port Adapter at Host
| Adapter | Test |
|---------|------|
| `IJobSeekerProfileQueryApi` → `JobSeekerProfileQueryApiAdapter` | Integration test bridging ContentManagement to JobSeekerProfile |

### Step 7.6 — Architecture Tests
| Rule | Modules Covered |
|------|----------------|
| Core must not reference Infrastructure | All 12 modules |
| Core must not reference EF Core | All 12 modules |
| Core must not reference host types | All 12 modules |
| Infrastructure may only reference its own Core + Shared.Infrastructure | All 12 modules |
| Module.Infrastructure must not reference another module.Infrastructure | All 12 modules |

---

## Standard Test Suite Structure (per module)

```
tests/{Module}.UnitTests/
  ├── Domain/          — value objects, entities, domain services, domain events
  ├── Application/     — commands/queries with mocked ports
  └── Infrastructure/  — EF config mapping, repository impl (in-memory)

tests/{Module}.IntegrationTests/
  ├── Database/        — actual EF Core migrations + SQL Server (TestContainers)
  └── Outbox/          — outbox integration event publishing works end-to-end

tests/{Module}.FunctionalTests/
  └── Api/             — Minimal API endpoint calls with WebApplicationFactory + test DB
```

---

## Test Conventions

| Aspect | Convention |
|--------|-----------|
| Framework | xUnit |
| Mocking | NSubstitute |
| Assertions | FluentAssertions |
| Architecture | NetArchTest.Rules |
| DB integration | TestContainers (SQL Server) |
| Naming | `{Method}_Should_{Expected}_When_{Condition}` |
| File placement | One test class per production class, in mirror namespace |
| Categories | `[Trait("Category", "Unit")]`, `[Trait("Category", "Integration")]`, etc. |

---

## Module Registration Verification

Each module must register via:

```csharp
// Infrastructure/ServiceRegistration.cs
public static IServiceCollection Add[Module]Module(
    this IServiceCollection services, IConfiguration configuration)
{
    // Register DbContext, repos, domain services
}

public static IEndpointRouteBuilder Map[Module]Endpoints(
    this IEndpointRouteBuilder endpoints)
{
    // Map route groups
}
```

The Host's `Program.cs` calls these in dependency order. Tests must verify:
1. Module registration succeeds with real config
2. Module registration succeeds with null/in-memory config
3. Endpoints don't conflict with other modules
