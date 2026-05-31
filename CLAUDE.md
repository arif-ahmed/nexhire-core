# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

| Action | Command |
| ------ | ------ |
| Restore | `dotnet restore` |
| Build | `dotnet build` |
| Test all | `dotnet test` |
| Test single module | `dotnet test tests/Modules/IdentityAccess/Nexhire.Modules.IdentityAccess.Tests.Unit` |
| Test single method | `dotnet test --filter "FullyQualifiedName.TestMethodName"` |
| Architecture tests | `dotnet test tests/Nexhire.ArchitectureTests` |
| Run API | `dotnet run --project src/Host/Nexhire.Api` |
| API docs | <http://localhost:5001/scalar/v1> |
| Docker full stack | `docker compose up -d --build` |
| Docker DB only | `docker compose up -d nexhire-db` |
| Docker down | `docker compose down` |

## Architecture

Modular monolith, .NET 10 / C# 14. Each business module follows Clean Architecture with 4–5 layers.

### Dependency flow

```text
Host (Nexhire.Api)
  -> all Module.Infrastructure + Module.Presentation
  -> Nexhire.Shared.Infrastructure

Module.Presentation     -> Module.Application
Module.Infrastructure   -> Module.Application + Module.Domain + Shared.Infrastructure
Module.Application      -> Module.Domain + Nexhire.Shared.Core
Module.Domain           -> Nexhire.Shared.Core (only)
Module.Contracts        -> Nexhire.Shared.Core (only; integration events + public APIs)
Shared.Infrastructure   -> Shared.Core
Shared.Core             -> MediatR, FluentValidation (only)
```

Core/Domain projects must never reference EF Core, routing, or host types. Architecture tests in `tests/Nexhire.ArchitectureTests` enforce this per module.

### Module structure

Each module (IdentityAccess, JobPostings, JobSeekerProfile, etc.) follows:

- **`.Contracts`** (optional): Integration-event definitions and public API interfaces (`IIdentityProvisioningApi`, `ITokenValidationApi`). Depends on nothing but `Shared.Core`.
- **`.Domain`**: Aggregates, Entities, Value Objects, Domain Events, Domain Services. Persistence-ignorant.
- **`.Application`**: CQRS commands/queries + handlers, DTOs, FluentValidation validators, port interfaces. References `Domain` + `Shared.Core`.
- **`.Infrastructure`**: EF Core DbContext, repository implementations, port adapters (argon2id, JWT, SMS stub, etc.), background services, data seeding. References `Application` + `Domain` + `Shared.Infrastructure`.
- **`.Presentation`**: Minimal API endpoints, route groups, error-to-HTTP mapping. References `Application`.

Module registration uses two static extension methods on the module's `*Module.cs`:

- `Add[Module]Module(IServiceCollection, IConfiguration)` — registers DbContext, repos, domain services, port adapters, background services.
- `Map[Module]Endpoints(IEndpointRouteBuilder)` — maps route groups.

Some modules with a `Contracts` layer register their public APIs as DI services here.

The Host's `Program.cs` collects all module assemblies, passes them to `AddSharedInfrastructure()` (for MediatR/FluentValidation scanning), then calls each module's registration methods. Optional seed calls follow `app.Build()`:

```csharp
var app = builder.Build();
await app.Services.SeedIdentityAccessDataAsync();
app.Run();
```

### Shared abstractions (Nexhire.Shared.Core)

| Type | Purpose |
| ---- | ------- |
| `Result` / `Result<T>` | Monadic error handling. No exceptions for validation/business rules. |
| `Error` | `record Error(string Code, string Message)`. Sentinel: `Error.None`, `Error.NullValue`, `Error.ValidationError`. |
| `AggregateRoot<TId>` | Holds `List<IDomainEvent>`, exposes `RaiseDomainEvent()` / `ClearDomainEvents()`. |
| `Entity<TId>` | Base with identity-based equality. |
| `ValueObject` | Base with structural equality via `GetEqualityComponents()`. |
| `ICommand` / `IQuery<T>` | Extend MediatR `IRequest<Result>` / `IRequest<Result<T>>`. |
| `IDomainEvent` | Extends MediatR `INotification`. Adds `EventId` + `OccurredOnUtc`. |

### MediatR pipeline

Two open behaviors registered in order (outermost first):

1. **`LoggingBehavior`** — logs request start, success, and failure with error details.
2. **`ValidationBehavior`** — runs all FluentValidation validators; on failure returns `Result.Failure` with combined errors.

### Domain event dispatching

`PublishDomainEventsInterceptor` (EF Core `SaveChangesInterceptor`) scans the change tracker for `AggregateRoot<>` instances and dispatches their events.

Two dispatch modes:

- **Direct**: publishes via `IPublisher.Publish()` immediately inside the save transaction.
- **Outbox**: serializes events to `OutboxMessage` rows in the DB. A `PeriodicTimer`-based background service (`*OutboxRelayBackgroundService`, 15s interval) picks them up and publishes via MediatR. At-least-once delivery. Modules using outbox implement `IOutboxInboxDbContext` (exposes `DbSet<OutboxMessage>` + `DbSet<InboxMessage>`).

> **Constructor note:** `PublishDomainEventsInterceptor` accepts `IServiceProvider`, not `IPublisher` directly. It calls `CreateScope()` at dispatch time to resolve `IPublisher`. In tests, wrap the mock: `new ServiceCollection().AddSingleton(publisherMock).BuildServiceProvider()`.

### Cross-module communication

Three patterns coexist:

1. **Domain events as integration events**: Events like `UserRegisteredIntegrationEvent` implement `IDomainEvent` directly. Consuming modules handle them via `INotificationHandler<T>`.
2. **Consumed integration event records**: Consuming modules define local `INotification` records (not `IDomainEvent`) with handlers.
3. **Port/adapter at composition root**: A module's Core defines a port interface (e.g. `IJobSeekerProfileQueryApi`). The Host project provides the adapter implementation that uses another module's repository. This is the only place two modules couple — at the composition root.

### EF Core per module

Each module owns its own `DbContext`:

- Calls `modelBuilder.HasDefaultSchema("module_name")` for schema segregation (e.g. `"identity_access"`, `"job_postings"`).
- Implements `IOutboxInboxDbContext` if using outbox dispatch (exposes `DbSet<OutboxMessage>` + `DbSet<InboxMessage>`).
- Entity configurations use `IEntityTypeConfiguration<T>` pattern with ValueObject column conversions and jsonb for complex types.
- Child collections owned by an aggregate are loaded with it (`OwnsMany` for entities, json columns for VOs).
- Value objects that need querying/indexing (email, mobile, role, status) are flattened to scalar columns.
- No FK constraints cross aggregate boundaries, even within the same module schema.

### Endpoints (Minimal API)

No controllers. Each module's `Endpoints/` directory contains static classes with `MapEndpoints(IEndpointRouteBuilder)`:

- Route groups: `app.MapGroup("api/identity").WithTags("Identity Access")`.
- Each endpoint accepts command/query DTO + `ISender`, sends via MediatR, maps `Result<T>` to HTTP responses (`Ok`, `Created`, `BadRequest`).
- Error-to-HTTP mapping uses domain error codes:
  - `E-*INVALID*` / `E-*MISSING*` → 400
  - `E-*UNAUTHORIZED*` → 401
  - `E-*FORBIDDEN*` / `E-*BANNED*` → 403
  - `E-*NOT-FOUND*` → 404
  - `E-*CONFLICT*` / `E-*DUPLICATE*` → 409
  - `E-*LOCKED*` → 423
  - `E-*RATE-LIMITED*` → 429
  - Default → 400
- Public endpoints (register, login, activate, password-reset) are anonymous.
- Admin endpoints (`/admin/*`) require the `users:manage` permission, return `403 E-FORBIDDEN` otherwise.
- Auth middleware consumes `ITokenValidationApi` (in-process, not an HTTP route) to validate access tokens and extract `ClaimsPrincipal`.

## C# conventions

- C# 14, .NET 10. Implicit usings enabled solution-wide.
- PascalCase public members. `_camelCase` private fields.
- `<Nullable>enable</Nullable>` enforced in all projects.
- Aggregates/Value Objects: private/protected constructors + `public static Result<T>` factory methods.
- Domain events: `record` types implementing `IDomainEvent`, in `Events/` directory.
- Strongly-typed IDs (`record UserAccountId(Guid Value)`) wrapping bare `Guid`s.
- Testing: xUnit + NSubstitute + FluentAssertions. NetArchTest.Rules for architecture tests.
- Three test layers: Domain unit (pure), Application unit (with port doubles), Integration (real DB).

## Data seeding

Seed data scripts live in `Infrastructure/Persistence/SeedData.cs`. Called once at startup before the first request:

```csharp
// In Program.cs
var app = builder.Build();
await app.Services.SeedIdentityAccessDataAsync();
app.Run();
```

Seeds check for existing rows before inserting (idempotent). Development seeds include:
- System Administrator (`MoLAdministrator`, `Active`)
- Test Employer (`Employer`, `Active`)
- Test JobSeeker (`JobSeeker`, `Active`)
- Pending Employer (`Employer`, `PendingActivation`)
- Suspended User (`JobSeeker`, `Suspended`)
- Deactivated User (`JobSeeker`, `Deactivated`)
- Third-Party Portal (`ThirdPartyPortal`, `Active`)

## IdentityAccess module structure

```
Nexhire.Modules.IdentityAccess.Contracts/       # Integration events + public API interfaces
Nexhire.Modules.IdentityAccess.Domain/
  Domain/
    Events/                                       # Domain event records
    Repositories/                                 # Repository interfaces
    Services/                                     # Domain services (PasswordPolicy, PermissionResolver, etc.)
    ValueObjects/                                 # VOs (EmailAddress, MobileNumber, etc.)
    UserAccount.cs                                # Aggregate root
    OtpChallenge.cs                               # Aggregate root
    Session.cs, BackupCode.cs, TrustedDevice.cs,
    PasswordResetToken.cs                         # Child entities
    AdminActionLog.cs                             # Append-only log
    AccountStatus.cs, UserRole.cs, OtpPurpose.cs,
    Channel.cs, AdminActionType.cs                # Enums

Nexhire.Modules.IdentityAccess.Application/
  Accounts/
    Commands/                                     # 25 commands (ProvisionCredential, LoginWithCredentials, etc.)
    Queries/                                      # 7 queries (GetMyAccount, ListUsers, etc.)
  DTOs/                                           # Response DTOs
  Ports/                                          # IPasswordHasher, IJwtSigner, IOtpDeliveryPort, etc.

Nexhire.Modules.IdentityAccess.Infrastructure/
  IdentityAccessModule.cs                         # DI registration + seed entry point
  Persistence/
    IdentityAccessDbContext.cs                    # DbContext with outbox/inbox support
    Configurations/                               # EF entity configurations
    Repositories/                                 # Repository implementations
    SeedData.cs                                   # Data seeding
  PortAdapters/                                   # IPasswordHasher, IJwtSigner, etc. implementations
  BackgroundServices/                             # OTP sweep, session sweep, cleanup jobs

Nexhire.Modules.IdentityAccess.Presentation/
  Endpoints/                                      # Minimal API route definitions
  IdentityAccessPresentationModule.cs             # Route mapping entry point
```

## Registered modules

IdentityAccess (5-layer), EmployerProfiles, JobPostings, JobSeekerProfile, JobApplication, SearchDiscovery, RecommendationEngine, ExternalJobSync, Reporting, AdministratorsConfiguration, ContentManagement, Notification.
