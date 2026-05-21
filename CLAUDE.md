# CLAUDE.md — Developer Reference Guide

This document lists standard commands and architectural code style rules for the **Nexhire Core** modular monolith codebase.

---

## 🛠️ Common Commands

### Building and Dependency Management
* **Restore all packages**: `dotnet restore`
* **Build solution**: `dotnet build`
* **Clean build outputs**: `dotnet clean`

### Running the API Host
* **Run API locally**: `dotnet run --project src/Host/Nexhire.Api` (runs on `http://localhost:5001`)
* **Access interactive Scalar API Docs**: Open [http://localhost:5001/scalar/v1](http://localhost:5001/scalar/v1) while the host is running.

### Docker Environment
* **Spin up whole stack (API + DB)**: `docker compose up -d --build`
* **Spin up database only**: `docker compose up -d nexhire-db`
* **Stop Docker stack**: `docker compose down`

### Running Tests
* **Run all tests**: `dotnet test`
* **Run unit tests only**: `dotnet test tests/Modules/Users/Nexhire.Modules.Users.Tests.Unit`
* **Run architecture tests only**: `dotnet test tests/Nexhire.ArchitectureTests`
* **Run a single specific test**:
  ```bash
  dotnet test --filter "FullyQualifiedNameOfTestClass.MethodName"
  ```
  *(Example: `dotnet test --filter "Nexhire.Modules.Users.Tests.Unit.CreateUserCommandHandlerTests.Handle_Should_ReturnSuccessResult_WhenCommandIsValid"`)*

---

## 📐 Code Style & Architecture Guidelines

### Core Architectural Separation
This codebase is a **Modular Monolith** applying **Clean Architecture** within each module:
1. **Domain & Core (`.Core`)**:
   - Must contain **zero** references to EF Core, database libraries, routing frameworks, or host API structures.
   - Contains: Aggregates, Entities, Value Objects, Domain Events, Repository Interfaces, CQRS commands, handlers, and validation schemas.
2. **Infrastructure (`.Infrastructure`)**:
   - Contains: Repository implementations, migrations, DbContexts, mappings, Minimal API controllers (`Endpoints`), and dependency injection setups.
3. **Pluggable Registration**:
   - Modules must hook dynamically via extension methods `Add[ModuleName]Module(IServiceCollection, IConfiguration)` and `Map[ModuleName]Endpoints(IEndpointRouteBuilder)`.

### CQRS & Business Flow Rules
* **No exceptions for validation**: Avoid throwing exceptions for input validation or business rule checks. Use the `Result` and `Result<T>` monad and returning detailed `Error` records.
* **Rich Domain Modeling**:
   - Aggregates and Value Objects must have private/protected constructors (for EF Core compatibility) and expose `public static Result<T>` or `public static T` factory methods to control instantiation.
   - Values should be strongly typed using `ValueObject` (e.g. `Email`, `FullName`).
* **MediatR Pipelines**:
   - All Commands (`ICommand`) and Queries (`IQuery<T>`) pass through global pipeline behaviors:
     - `LoggingBehavior` tracks start, success, and validation failures.
     - `ValidationBehavior` intercepts commands, runs FluentValidation classes, and automatically returns validation failures as `Result.Failure`.

### C# Coding Standards
* **Language version**: C# 13, targeting .NET 9.
* **Formatting**: PascalCase for public classes, methods, and properties; camelCase starting with `_` for private fields (e.g., `_userRepository`).
* **Implicit Usings**: Enabled solution-wide; avoid duplicate `using` directives where global namespaces are active.
* **Nullability**: All projects must enforce `<Nullable>enable</Nullable>`. Design types explicitly around potential null states.
* **Domain Events**: Declare aggregate-emitted domain events as lightweight C# `record` types implementing `IDomainEvent` inside the `Events` directory.
