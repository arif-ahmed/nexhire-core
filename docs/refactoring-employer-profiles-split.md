# Plan: Split EmployerProfiles from 3 Projects to 5 Projects

## Context

The EmployerProfiles module currently has 3 projects: **Contracts**, **Core**, **Infrastructure**. The `Core` project bundles both Domain and Application concerns together. The `Infrastructure` project bundles both Infrastructure and Presentation concerns. Per the project's Clean Architecture guidelines (and the IdentityAccess reference implementation), each module should have separate **Domain**, **Application**, **Infrastructure**, and **Presentation** projects. This is a pure restructuring — no logic changes.

**Current:**
```
Contracts → Shared.Core
Core → Shared.Core + Contracts             (Domain + Application combined)
Infrastructure → Core + Contracts + Shared.Infrastructure + Npgsql  (Infrastructure + Presentation combined)
```

**Target:**
```
Contracts → Shared.Core
Domain → Shared.Core + Contracts
Application → Domain + Shared.Core + Contracts + FluentValidation
Infrastructure → Domain + Application + Shared.Infrastructure + Npgsql
Presentation → Application + FrameworkReference(Microsoft.AspNetCore.App)
```

---

## Phase 1: Rename Core → Domain

This phase is purely a namespace/rename operation. The bulk of files stay in place.

### Step 1.1 — Rename directory and .csproj
- Rename `Nexhire.Modules.EmployerProfiles.Core/` → `Nexhire.Modules.EmployerProfiles.Domain/`
- Rename `.csproj` file inside accordingly

### Step 1.2 — Update namespaces in all files in the renamed Domain project
Global find-replace `Nexhire.Modules.EmployerProfiles.Core` → `Nexhire.Modules.EmployerProfiles.Domain` across all `.cs` files in the project.

This covers:
- **21 Domain files** (aggregates, VOs, events, ports, repositories, services, projections) — update `namespace` declarations
- **25 Application files** (commands/handlers/queries/DTOs) — update both `namespace` declarations AND `using` statements for Domain types
- `Domain/Events/IntegrationEvents.cs` — the global using alias already points to Contracts namespace, no change needed

### Step 1.3 — Update Infrastructure project
- In `.csproj`: change project reference path from `Core` → `Domain`
- In all `.cs` files: find-replace `Nexhire.Modules.EmployerProfiles.Core` → `Nexhire.Modules.EmployerProfiles.Domain`

Affected files: `EmployerProfilesModule.cs`, `EmployerProfilesDbContext.cs`, all 6 EF config files, all 4 repo files, all 3 adapters, `EventConsumers.cs`, `EmployerProfilePublicApiAdapter.cs`, `EmployerEndpoints.cs`

### Step 1.4 — Update solution file (`Nexhire.slnx`)
- Replace the Core project path with the Domain project path

### Step 1.5 — Update test project
- In `.csproj`: change project reference path from `Core` → `Domain`
- In all 14 test `.cs` files: find-replace `Nexhire.Modules.EmployerProfiles.Core` → `Nexhire.Modules.EmployerProfiles.Domain`

### Step 1.6 — Update Host
- `Program.cs`: `using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates` → `using Nexhire.Modules.EmployerProfiles.Domain.Aggregates`

### Step 1.7 — Verify
`dotnet build && dotnet test` — commit as atomic "rename Core → Domain"

---

## Phase 2: Create Application Project + Move Files

### Step 2.1 — Create project directory and .csproj
Create `Nexhire.Modules.EmployerProfiles.Application/` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Nexhire.Modules.EmployerProfiles.Domain\Nexhire.Modules.EmployerProfiles.Domain.csproj" />
    <ProjectReference Include="..\..\..\Shared\Nexhire.Shared.Core\Nexhire.Shared.Core.csproj" />
    <ProjectReference Include="..\Nexhire.Modules.EmployerProfiles.Contracts\Nexhire.Modules.EmployerProfiles.Contracts.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
  </ItemGroup>
</Project>
```

### Step 2.2 — Move DTOs (9 files)
Move from `Domain/DTOs/` → `Application/DTOs/`:
- `AddressDto.cs`, `DashboardPostingDto.cs`, `EmployerDashboardDto.cs`, `EmployerProfileDto.cs`, `MatchedCandidateDto.cs`, `PublicEmployerProfileDto.cs`, `ShortlistDetailDto.cs`, `ShortlistDto.cs`, `VerificationStateDto.cs`

Update namespace: `.Domain.DTOs` → `.Application.DTOs`

### Step 2.3 — Move Commands (17 folders)
Move from `Domain/EmployerProfiles/Commands/` → `Application/EmployerProfiles/Commands/`:
- `AddCandidateToShortlist`, `ApproveEmployerVerification`, `CompleteEmployerLevel2`, `CreateShortlist`, `DeleteShortlist`, `RegisterEmployer`, `RejectEmployerVerification`, `RemoveCandidateFromShortlist`, `RemoveCompanyImage`, `RemoveEmployerDocument`, `RenameShortlist`, `RequestEmployerVerification`, `ResubmitEmployerVerification`, `UpdateEmployerProfile`, `UploadCompanyImage`, `UploadEmployerDocument`, `UploadEmployerLogo`

Update namespace: `.Domain.EmployerProfiles.Commands.*` → `.Application.EmployerProfiles.Commands.*`

### Step 2.4 — Move Queries (8 folders)
Move from `Domain/EmployerProfiles/Queries/` → `Application/EmployerProfiles/Queries/`:
- `GetEmployerDashboard`, `GetEmployerJobPostings`, `GetEmployerVerificationStatus`, `GetMatchedCandidates`, `GetMyEmployerProfile`, `GetPublicEmployerProfile`, `GetShortlist`, `GetShortlists`

Update namespace: `.Domain.EmployerProfiles.Queries.*` → `.Application.EmployerProfiles.Queries.*`

### Step 2.5 — Update Infrastructure
- `.csproj`: add Application project reference
- All `.cs` files referencing moved types: update `using` from `.Domain.DTOs` → `.Application.DTOs`, `.Domain.EmployerProfiles.Commands` → `.Application.EmployerProfiles.Commands`, `.Domain.EmployerProfiles.Queries` → `.Application.EmployerProfiles.Queries`

### Step 2.6 — Remove `MapEmployerProfilesEndpoints` from `EmployerProfilesModule.cs`
Keep only `AddEmployerProfilesModule()`. Remove the endpoint mapping method and its related usings.

### Step 2.7 — Update Host Program.cs
- Add Application assembly marker for MediatR scanning
- Add `using Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.RegisterEmployer;`

### Step 2.8 — Update test project
- `.csproj`: add Application project reference, keep Domain reference
- All test files: update `using` statements for moved types

### Step 2.9 — Update solution file
Add the Application project entry.

### Step 2.10 — Verify
`dotnet build && dotnet test` — commit as atomic "extract Application layer"

---

## Phase 3: Create Presentation Project + Move Endpoints

### Step 3.1 — Create project directory and .csproj
Create `Nexhire.Modules.EmployerProfiles.Presentation/` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Nexhire.Modules.EmployerProfiles.Application\Nexhire.Modules.EmployerProfiles.Application.csproj" />
  </ItemGroup>
</Project>
```

### Step 3.2 — Move endpoints
Move `Infrastructure/Endpoints/EmployerEndpoints.cs` → `Presentation/Endpoints/EmployerEndpoints.cs`

Update namespace: `.Infrastructure.Endpoints` → `.Presentation.Endpoints`

### Step 3.3 — Create Presentation module entry point
New file `Presentation/EmployerProfilesPresentationModule.cs`:
```csharp
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.EmployerProfiles.Presentation.Endpoints;

namespace Nexhire.Modules.EmployerProfiles.Presentation;

public static class EmployerProfilesPresentationModule
{
    public static IEndpointRouteBuilder MapEmployerProfilesEndpoints(this IEndpointRouteBuilder app)
    {
        EmployerEndpoints.MapEndpoints(app);
        return app;
    }
}
```

### Step 3.4 — Update Host
- `Nexhire.Api.csproj`: add Presentation project reference
- `Program.cs`: add Presentation using and assembly marker
- `Program.cs`: the call `app.MapEmployerProfilesEndpoints()` now resolves from Presentation

### Step 3.5 — Delete empty `Infrastructure/Endpoints/` directory

### Step 3.6 — Update solution file
Add the Presentation project entry.

### Step 3.7 — Verify
`dotnet build && dotnet test` — commit as atomic "extract Presentation layer"

---

## Phase 4: Final Cleanup

- Delete stale `bin/`/`obj/` directories
- `dotnet clean && dotnet build && dotnet test`
- Grep for any remaining `Nexhire.Modules.EmployerProfiles.Core` references → should be zero
- Verify dependency graph is correct

---

## Files Modified (Summary)

| Area | Files |
|------|-------|
| **New projects** | `Application.csproj`, `Presentation.csproj`, `EmployerProfilesPresentationModule.cs` |
| **Renamed project** | `Core/` → `Domain/` (directory + .csproj) |
| **Domain (stays, namespace change)** | 21 files: aggregates, VOs, events, ports, repos, services, projections |
| **Domain → Application (move)** | 9 DTOs + 17 command folders + 8 query folders (~50 files) |
| **Infrastructure (update usings + csproj)** | `EmployerProfilesModule.cs`, `EmployerProfilesDbContext.cs`, 6 EF configs, 4 repos, 3 adapters, `EventConsumers.cs`, `PublicApiAdapter.cs` |
| **Infrastructure → Presentation (move)** | `EmployerEndpoints.cs` |
| **Host** | `Program.cs`, `Nexhire.Api.csproj` |
| **Tests** | `.csproj` + all 14 test files |
| **Solution** | `Nexhire.slnx` |

## Verification

1. `dotnet build` — compiles with zero errors
2. `dotnet test` — all existing tests pass
3. `grep -r "EmployerProfiles\.Core" --include="*.cs" --include="*.csproj"` — returns zero results
4. Architecture test ready: Domain has no EF Core / ASP.NET references; Presentation has no Infrastructure references
