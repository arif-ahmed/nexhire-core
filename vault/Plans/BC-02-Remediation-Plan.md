# BC-02 Gap Remediation — Implementation Plan

**Plan date:** 2026-05-31
**Branch:** `fix/bc-02-gap-remediation`
**Based on review:** `vault/Reviews/BC-02-Review-2026-05-31.md`
**Estimated gaps:** 9 (4 blockers, 3 major, 2 minor)

---

## Guiding principles

1. Work gap-by-gap in the order below — each gap is independently shippable.
2. Write or update tests for every change before committing it. Tests first, then production code.
3. Never touch another module's internals. All cross-module coupling goes through the composition-root adapters or the `Contracts` surface.
4. The shared-foundation conventions (Result/Error, AggregateRoot, outbox/inbox, MediatR pipeline) are non-negotiable — align to them, do not work around them.

---

## Work order

Blockers (GAP-1 to GAP-4) must be completed before major and minor gaps. Within blockers, GAP-3 must be done before GAP-1 because the `IEmployerProfilePublicApi` shapes the Contracts project that the rest of the module registers.

```
GAP-3 → GAP-4 → GAP-1 → GAP-2 → GAP-5 → GAP-6 → GAP-7 → GAP-8 → GAP-9
```

---

## GAP-3 — Create `EmployerProfilePublicApi` contract surface

**Goal:** Expose `IEmployerProfilePublicApi` so BC-4 can enforce the verified-employer gate without importing internal types.

### Step 1 — Add Contracts project

Create `src/Modules/EmployerProfiles/Nexhire.Modules.EmployerProfiles.Contracts/`.

Add a `csproj` referencing only `Nexhire.Shared.Core`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\Shared\Nexhire.Shared.Core\Nexhire.Shared.Core.csproj" />
  </ItemGroup>
</Project>
```

### Step 2 — Define the public API interface

File: `Contracts/IEmployerProfilePublicApi.cs`

```csharp
namespace Nexhire.Modules.EmployerProfiles.Contracts;

public interface IEmployerProfilePublicApi
{
    Task<bool> IsVerifiedAsync(Guid employerUserId, CancellationToken ct = default);
    Task<EmployerProfileSummaryDto?> GetSummaryAsync(Guid employerUserId, CancellationToken ct = default);
}

public record EmployerProfileSummaryDto(
    Guid EmployerProfileId,
    Guid UserId,
    string CompanyName,
    string Status,
    bool IsVerified,
    string? LogoStorageKey,
    string? Industry);
```

### Step 3 — Move integration events to Contracts

Move `IntegrationEvents.cs` from `Core/Domain/Events/` into `Contracts/Events/IntegrationEvents.cs` so downstream modules can reference the event types without depending on `Core`.

Update all internal references to the new namespace.

### Step 4 — Implement the adapter in Infrastructure

File: `Infrastructure/PublicApi/EmployerProfilePublicApiAdapter.cs`

```csharp
public class EmployerProfilePublicApiAdapter : IEmployerProfilePublicApi
{
    private readonly IEmployerProfileRepository _repository;

    public EmployerProfilePublicApiAdapter(IEmployerProfileRepository repository)
        => _repository = repository;

    public async Task<bool> IsVerifiedAsync(Guid employerUserId, CancellationToken ct)
    {
        var profile = await _repository.GetByUserIdAsync(employerUserId, ct);
        return profile?.IsVerified ?? false;
    }

    public async Task<EmployerProfileSummaryDto?> GetSummaryAsync(Guid employerUserId, CancellationToken ct)
    {
        var profile = await _repository.GetByUserIdAsync(employerUserId, ct);
        if (profile is null) return null;
        return new EmployerProfileSummaryDto(
            profile.Id, profile.UserId, profile.CompanyName.Value,
            profile.Status.ToString(), profile.IsVerified,
            profile.Logo?.StorageKey, profile.Industry);
    }
}
```

### Step 5 — Register in DI

In `EmployerProfilesModule.cs`:

```csharp
services.AddScoped<IEmployerProfilePublicApi, EmployerProfilePublicApiAdapter>();
```

### Step 6 — Tests

Unit test `EmployerProfilePublicApiAdapter`:
- `IsVerifiedAsync` returns `true` when `Status == Verified`.
- `IsVerifiedAsync` returns `false` when profile not found.
- `GetSummaryAsync` returns `null` when profile not found.
- `GetSummaryAsync` maps all fields correctly.

---

## GAP-4 — Implement Outbox + Inbox infrastructure

**Goal:** Make integration-event publishing transactional (outbox) and consumed-event handling idempotent (inbox).

### Step 1 — Implement `IOutboxInboxDbContext` on `EmployerProfilesDbContext`

Add `DbSet<OutboxMessage>` and `DbSet<InboxMessage>`. These types already exist in `Nexhire.Shared.Infrastructure`. Confirm the interface shape in `Shared.Infrastructure` and implement it.

```csharp
public class EmployerProfilesDbContext : DbContext, IOutboxInboxDbContext
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    // ... existing DbSets
}
```

Add table configurations (use the same pattern as `IdentityAccess` module):

```
TABLE employer_profile.outbox_messages  (id, type, payload, occurred_on_utc, processed_on_utc)
TABLE employer_profile.inbox_messages   (id, consumer, occurred_on_utc)
```

### Step 2 — Switch domain event dispatch to outbox mode

Change `PublishDomainEventsInterceptor` registration (or the DbContext's interceptor wiring) to use the **outbox dispatch** path — serialize integration events to `OutboxMessage` rows in the same transaction rather than publishing in-process.

Refer to the IdentityAccess module's `OutboxRelayBackgroundService` pattern. Add an `EmployerProfilesOutboxRelayBackgroundService` (15 s `PeriodicTimer`) that reads unprocessed outbox messages and dispatches them via `IPublisher`.

### Step 3 — Add inbox idempotency to all event consumers

Wrap every consumer handler body in an inbox check:

```csharp
public async Task Handle(UserAccountActivatedIntegrationEvent notification, CancellationToken ct)
{
    if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, ct))
        return; // already processed

    // ... handler logic ...

    _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(UserAccountActivatedIntegrationEvent), DateTime.UtcNow));
    await _unitOfWork.SaveChangesAsync(ct);
}
```

Apply to all 10 consumers in `EventConsumers.cs`.

### Step 4 — Add migration

```
dotnet ef migrations add AddOutboxInbox --project src/Modules/EmployerProfiles/Nexhire.Modules.EmployerProfiles.Infrastructure
```

### Step 5 — Register background service

```csharp
services.AddHostedService<EmployerProfilesOutboxRelayBackgroundService>();
```

### Step 6 — Tests

- Unit: delivering `UserAccountActivatedIntegrationEvent` with the same `EventId` twice activates the profile exactly once.
- Unit: delivering `JobPostingPublishedIntegrationEvent` with the same `EventId` twice writes one `dashboard_postings` row.
- Integration (adjust when real-DB tests are added in GAP-9): outbox row is written in the same transaction as the profile mutation; rolling back leaves neither.

---

## GAP-1 — Implement `UpdateEmployerProfileCommand`

**Goal:** Allow an activated employer to edit their company information.

### Step 1 — Command + Validator

File: `Core/EmployerProfiles/Commands/UpdateEmployerProfile/UpdateEmployerProfileCommand.cs`

```csharp
public record UpdateEmployerProfileCommand(
    Guid UserId,
    string? CompanyName,
    string? Website,
    string? Industry,
    string? CompanySize,
    AddressDto? Address,
    string? Description) : ICommand;
```

File: `UpdateEmployerProfileCommandValidator.cs`

Rules (spec §10.3):
- `CompanyName` when present: non-empty, ≤ 200 chars.
- `Website` when present: valid http/https URL.
- `CompanySize` when present: must parse to enum (`Micro`/`Small`/`Medium`/`Large`).
- `Address` when present: `Line1`, `City`, `District`, `Country` required.
- `Description` when present: ≤ 5000 chars.

### Step 2 — Handler

File: `UpdateEmployerProfileCommandHandler.cs`

```
Load profile by UserId (404 if not found)
Build typed VOs from nullable command fields (skip null fields)
Call profile.UpdateCompanyInformation(companyName?, website?, industry?, companySize?, address?, description?)
On failure → return Result.Failure
persist → SaveChanges
```

### Step 3 — Endpoint

In `EmployerEndpoints.cs`, add:

```csharp
group.MapPut("me", async (UpdateEmployerProfileRequest request, ClaimsPrincipal principal, ISender sender) =>
{
    var userId = GetUserId(principal);
    if (userId == null) return Results.Unauthorized();

    var command = new UpdateEmployerProfileCommand(
        userId.Value, request.CompanyName, request.Website,
        request.Industry, request.CompanySize, request.Address, request.Description);

    var result = await sender.Send(command);
    return result.IsSuccess ? Results.Ok() : MapError(result.Error);
})
.WithName("UpdateEmployerProfile")
.RequireAuthorization();
```

Add `UpdateEmployerProfileRequest` record at the bottom of the endpoints file.

### Step 4 — Tests

- Handler: happy path updates fields and emits `EmployerProfileUpdated`.
- Handler: returns 404 when profile not found.
- Handler: returns failure when `Status == PendingActivation` (blocked by aggregate).
- Handler: returns failure when `Status == Suspended`.
- Validator: rejects invalid website URL.
- Validator: rejects description > 5000 chars.
- Validator: rejects unknown CompanySize value.

---

## GAP-2 — Implement `GetEmployerJobPostingsQuery`

**Goal:** Return the employer's posting list from the `dashboard_postings` read-model projection.

### Step 1 — Query + Handler

File: `Core/EmployerProfiles/Queries/GetEmployerJobPostings/GetEmployerJobPostingsQuery.cs`

```csharp
public record GetEmployerJobPostingsQuery(Guid UserId) : IQuery<List<DashboardPostingDto>>;
```

File: `GetEmployerJobPostingsQueryHandler.cs`

```
var postings = await _dashboardStore.GetPostingsAsync(request.UserId, ct);
return Result.Success(postings.Select(p => new DashboardPostingDto(p.PostingId, p.Title, p.Status, p.LastEventOnUtc)).ToList());
```

Add `DashboardPostingDto` to `Core/DTOs/` if not already present.

### Step 2 — Endpoint

In `EmployerEndpoints.cs`, add under the dashboard section:

```csharp
group.MapGet("me/dashboard/postings", async (ClaimsPrincipal principal, ISender sender) =>
{
    var userId = GetUserId(principal);
    if (userId == null) return Results.Unauthorized();
    var result = await sender.Send(new GetEmployerJobPostingsQuery(userId.Value));
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
})
.WithName("GetEmployerJobPostings")
.RequireAuthorization();
```

### Step 3 — Tests

- Returns empty list when no postings exist for the employer.
- Returns projected postings filtered to the requesting employer's `UserId`.

---

## GAP-5 — Add optimistic concurrency tokens

**Goal:** Prevent lost-update races on `employer_profiles` and `shortlists`.

### Step 1 — Add `Version` property to aggregates

In `EmployerProfile.cs`:

```csharp
public uint Version { get; private set; }
```

In `Shortlist.cs`:

```csharp
public uint Version { get; private set; }
```

### Step 2 — Configure in EF

In `EmployerProfileConfiguration.cs`:

```csharp
builder.Property(ep => ep.Version)
    .IsRowVersion()
    .IsConcurrencyToken();
```

In `ShortlistConfiguration.cs`:

```csharp
builder.Property(s => s.Version)
    .IsRowVersion()
    .IsConcurrencyToken();
```

> **Note for PostgreSQL:** `IsRowVersion()` maps to `xmin` in Npgsql. Use `.UseXminAsConcurrencyToken()` on the entity builder instead, which maps the built-in Postgres row version without adding a column.

```csharp
builder.UseXminAsConcurrencyToken();
```

### Step 3 — Handle `DbUpdateConcurrencyException` in repositories

In `EmployerProfileRepository.UpdateAsync` and `ShortlistRepository.UpdateAsync`, catch `DbUpdateConcurrencyException` and return / throw a mapped `Result.Failure` with error code `E-CONCURRENCY-CONFLICT`.

### Step 4 — Tests

- Persistence test: two concurrent in-memory modifications to the same profile detect the conflict (this requires a real-DB test in GAP-9 to be truly meaningful, but add the shape now).

---

## GAP-6 — Enforce `users:manage` permission on admin endpoints

**Goal:** Only MoL Administrators can approve or reject verification.

### Step 1 — Add permission policy

In `EmployerProfilesModule.cs` (or the host's auth setup), ensure a policy named `"RequireUsersManage"` exists:

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireUsersManage", policy =>
        policy.RequireClaim("permission", "users:manage"));
});
```

If this policy is already defined by the IdentityAccess module at the host level, skip this step and reference the same policy name.

### Step 2 — Apply `RequireAuthorization` to admin routes

In `EmployerEndpoints.cs`, change the approve and reject endpoint registrations:

```csharp
group.MapPost("{id:guid}/verification/approve", ...)
    .WithName("ApproveEmployerVerification")
    .RequireAuthorization("RequireUsersManage");

group.MapPost("{id:guid}/verification/reject", ...)
    .WithName("RejectEmployerVerification")
    .RequireAuthorization("RequireUsersManage");
```

### Step 3 — Remove the now-redundant `GetAdminId` helper

Once the framework enforces the permission, the handler receives the admin's `UserId` from the `ClaimsPrincipal` exactly like any other authenticated user. Replace `GetAdminId` calls with `GetUserId` in these two handlers.

### Step 4 — Tests

- Integration/endpoint test: calling approve/reject without `users:manage` claim returns `403`.
- Integration/endpoint test: calling approve/reject with `users:manage` claim returns `200`.

---

## GAP-7 — Fix `CandidateSavedToTalentPoolIntegrationEvent.EmployerId`

**Goal:** Emit `EmployerId = UserId` (BC-1 identity) not `EmployerProfileId`.

### Step 1 — Remove `EmployerProfileId` from `Shortlist` event

The `Shortlist` aggregate cannot know `UserId` — it only knows `EmployerProfileId`. Remove the integration event raise from `Shortlist.AddCandidate` and instead raise a **domain-only** event:

```csharp
RaiseDomainEvent(new CandidateAddedToShortlist(Guid.NewGuid(), EmployerProfileId, Id, candidateUserId, matchScore, UpdatedOnUtc));
```

Add `CandidateAddedToShortlist` to `DomainEvents.cs` (internal event, not published outside module).

### Step 2 — Handle in-module in `AddCandidateToShortlistCommandHandler`

In the handler, after `SaveChanges`, construct and publish the integration event with the correct `UserId`:

```csharp
// profile is loaded in the handler to get UserId
var integrationEvent = new CandidateSavedToTalentPoolIntegrationEvent(
    Guid.NewGuid(),
    EmployerId: profile.UserId,         // BC-1 UserId
    JobSeekerId: request.CandidateUserId,
    PoolId: request.ShortlistId,
    At: DateTime.UtcNow,
    OccurredOnUtc: DateTime.UtcNow);

// write to outbox (after GAP-4 is done)
```

### Step 3 — Tests

- `AddCandidateToShortlistCommandHandler` test: verify `CandidateSavedToTalentPoolIntegrationEvent` carries `EmployerId == profile.UserId`, not `profile.Id`.

---

## GAP-8 — Align API routes and HTTP status codes to spec

**Goal:** Make routes and response codes contract-compliant with the spec §12 table.

### Route changes (all in `EmployerEndpoints.cs`)

| Current route | Target route |
|---|---|
| `MapPost("")` | `MapPost("register")` |
| `MapGet("me/verification-status")` | `MapGet("me/verification")` |
| `MapPost("me/verification")` | `MapPost("me/verification/request")` |
| `MapPost("me/resubmit-verification")` | `MapPost("me/verification/resubmit")` |
| `MapPost("{id}/verify/approve")` | `MapPost("{id}/verification/approve")` |
| `MapPost("{id}/verify/reject")` | `MapPost("{id}/verification/reject")` |
| `MapGet("{id}")` | `MapGet("{id}/public")` |

### HTTP status code fixes

| Endpoint | Current | Target |
|---|---|---|
| `DELETE me/images/{id}` | `Results.Ok()` | `Results.NoContent()` |
| `DELETE me/documents/{id}` | `Results.Ok()` | `Results.NoContent()` |
| `DELETE me/shortlists/{id}` | `Results.Ok()` | `Results.NoContent()` |
| `DELETE me/shortlists/{id}/candidates/{memberId}` | `Results.Ok()` | `Results.NoContent()` |
| `POST me/images` | `Results.Ok()` | `Results.Created(location, imageId)` |
| `POST me/documents` | `Results.Ok()` | `Results.Created(location, documentId)` |
| `POST register` | `Results.Created($"/api/employers/me", id)` | `Results.Created($"/api/employers/{id}", id)` |

For `POST me/images` and `POST me/documents` to return a created ID, the respective command handlers must return `Result<Guid>`. Update:
- `UploadCompanyImageCommand` → returns `Guid` (image id)
- `UploadEmployerDocumentCommand` → returns `Guid` (document id)
- Both handlers return the new entity's `Id`.

### Tests

- Endpoint tests: verify status codes for all changed routes.
- Endpoint tests: verify `POST /api/employers/register` is the registration route (not `POST /api/employers`).

---

## GAP-9 — Upgrade integration tests to real database

**Goal:** Replace `UseInMemoryDatabase` with Testcontainers + real PostgreSQL in `PersistenceTests.cs`.

### Step 1 — Add NuGet package

```
dotnet add tests/Modules/EmployerProfiles/Nexhire.Modules.EmployerProfiles.Tests.Unit \
  package Testcontainers.PostgreSql
```

> Consider renaming the test project to `Nexhire.Modules.EmployerProfiles.Tests.Integration` for clarity, or add a separate integration test project.

### Step 2 — Replace DbContext setup in `PersistenceTests.cs`

```csharp
public class PersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        // run migrations
        var options = new DbContextOptionsBuilder<EmployerProfilesDbContext>()
            .UseNpgsql(_pg.GetConnectionString())
            .Options;
        using var ctx = new EmployerProfilesDbContext(options, ...);
        await ctx.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _pg.DisposeAsync().AsTask();
}
```

### Step 3 — Add missing integration test cases (spec §13.3)

Add tests for:
- **Unique index enforcement:** a second profile with the same `company_identifier` fails at the DB level.
- **Optimistic concurrency:** two concurrent updates detect the conflict (using `xmin`).
- **Outbox transactionality:** profile mutation + outbox write commit together; rollback removes both.
- **Inbox idempotency:** duplicate consumed event is a no-op in the DB.
- **Schema migration:** `MigrateAsync()` on an empty DB lands all tables in schema `employer_profile`.

---

## Acceptance checklist — definition of done

Before merging `fix/bc-02-gap-remediation` to `main`:

- [ ] GAP-3: `IEmployerProfilePublicApi` interface exists in `Contracts` project; adapter registered in DI; unit tests pass.
- [ ] GAP-4: `EmployerProfilesDbContext` implements `IOutboxInboxDbContext`; all 10 consumers have inbox deduplication; outbox relay background service registered; all existing tests still pass.
- [ ] GAP-1: `UpdateEmployerProfileCommand` + handler + validator + `PUT /api/employers/me` endpoint exist; handler unit tests pass.
- [ ] GAP-2: `GetEmployerJobPostingsQuery` + handler + `GET /api/employers/me/dashboard/postings` endpoint exist; handler unit tests pass.
- [ ] GAP-5: `UseXminAsConcurrencyToken()` configured on `employer_profiles` and `shortlists`; EF migration created.
- [ ] GAP-6: `RequireAuthorization("RequireUsersManage")` on approve/reject endpoints; endpoint test for 403 passes.
- [ ] GAP-7: `CandidateSavedToTalentPoolIntegrationEvent.EmployerId` is `profile.UserId`; handler test asserts correct field.
- [ ] GAP-8: All 7 routes corrected; all 4 DELETE return 204; `POST images` and `POST documents` return 201 + id; `POST images`/`POST documents` commands return `Result<Guid>`.
- [ ] GAP-9: `PersistenceTests` (or a new integration test project) uses Testcontainers + real PostgreSQL; unique-index, optimistic-concurrency, outbox, inbox, and migration tests pass.
- [ ] `dotnet build` produces zero warnings.
- [ ] `dotnet test` produces zero failures.
- [ ] `dotnet test tests/Nexhire.ArchitectureTests` passes (no layer violations introduced).
