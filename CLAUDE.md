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

Modular monolith, .NET 9 / C# 13. Each business module is a pair of `.Core` + `.Infrastructure` projects sharing a Clean Architecture boundary.

### Dependency flow

```text
Host (Nexhire.Api)
  -> all Module.Infrastructure
  -> Nexhire.Shared.Infrastructure

Module.Infrastructure -> Module.Core + Shared.Infrastructure
Module.Core            -> Nexhire.Shared.Core (only)
Shared.Infrastructure  -> Shared.Core
Shared.Core            -> MediatR, FluentValidation (only)
```

Core projects must never reference EF Core, routing, or host types. Architecture tests in `tests/Nexhire.ArchitectureTests` enforce this per module.

### Module structure

Each module (IdentityAccess, JobPostings, JobSeekerProfile, etc.) follows:

- **`.Core`**: Aggregates, Entities, Value Objects, Domain Events, Repository interfaces, CQRS handlers, FluentValidation validators.
- **`.Infrastructure`**: EF Core DbContext, repository implementations, Minimal API endpoints, DI registration.

Module registration uses two static extension methods on the module's `*Module.cs`:

- `Add[Module]Module(IServiceCollection, IConfiguration)` — registers DbContext, repos, domain services.
- `Map[Module]Endpoints(IEndpointRouteBuilder)` — maps route groups.

The Host's `Program.cs` collects all module assemblies, passes them to `AddSharedInfrastructure()` (for MediatR/FluentValidation scanning), then calls each module's registration methods.

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

- **Direct** (e.g. IdentityAccess): publishes via `IPublisher.Publish()` immediately inside the save transaction.
- **Outbox** (e.g. JobPostings): serializes events to `OutboxMessage` rows in the DB. A `PeriodicTimer`-based background service (`*OutboxRelayBackgroundService`, 15s interval) picks them up and publishes via MediatR. At-least-once delivery. Modules using outbox implement `IOutboxInboxDbContext` (exposes `DbSet<OutboxMessage>` + `DbSet<InboxMessage>`).

### Cross-module communication

Three patterns coexist:

1. **Domain events as integration events**: Events like `EmployerVerifiedIntegrationEvent` implement `IDomainEvent` directly. Consuming modules handle them via `INotificationHandler<T>`.
2. **Consumed integration event records**: Consuming modules define local `INotification` records (not `IDomainEvent`) with handlers.
3. **Port/adapter at composition root**: A module's Core defines a port interface (e.g. `IJobSeekerProfileQueryApi`). The Host project provides the adapter implementation that uses another module's repository. This is the only place two modules couple — at the composition root.

### EF Core per module

Each module owns its own `DbContext`:

- Calls `modelBuilder.HasDefaultSchema("module_name")` for schema segregation (e.g. `"users"`, `"job_postings"`).
- Constructor takes `DbContextOptions` + `PublishDomainEventsInterceptor`. Wires interceptor in `OnConfiguring`.
- Entity configurations use `IEntityTypeConfiguration<T>` pattern with ValueObject column conversions and jsonb for complex types.

### Endpoints (Minimal API)

No controllers. Each module's `Endpoints/` directory contains static classes with `MapEndpoints(IEndpointRouteBuilder)`:

- Route groups: `app.MapGroup("api/resource").WithTags("Tag")`.
- Each endpoint accepts command/query DTO + `ISender`, sends via MediatR, maps `Result<T>` to HTTP responses (`Ok`, `Created`, `BadRequest`).
- Error-to-HTTP mapping uses domain error codes (e.g. `"E-POST-NOT-FOUND"` → 404, `"E-POST-FORBIDDEN"` → 403).
- Auth: `ClaimsPrincipal` for extracting `userId` / `employerId` from JWT claims.

## C# conventions

- C# 13, .NET 9. Implicit usings enabled solution-wide.
- PascalCase public members. `_camelCase` private fields.
- `<Nullable>enable</Nullable>` enforced in all projects.
- Aggregates/Value Objects: private/protected constructors + `public static Result<T>` factory methods.
- Domain events: `record` types implementing `IDomainEvent`, in `Events/` directory.
- Testing: xUnit + NSubstitute + FluentAssertions. NetArchTest.Rules for architecture tests.

## Registered modules

IdentityAccess, EmployerProfiles, JobPostings, JobSeekerProfile, JobApplication, SearchDiscovery, RecommendationEngine, ExternalJobSync, Reporting, AdministratorsConfiguration, ContentManagement, Notification.
