---
title: "Shared Foundations — Handover Packages"
type: shared-foundations
generated: 2026-05-15
applies_to: "All bounded-context handover packages (BC-1 … BC-12)"
related:
  - "[[BC_Mapping]]"
  - "[[Context_Map]]"
  - "[[Event_Catalog]]"
tags:
  - handover-package
  - shared-foundations
  - target-stack
---

# Shared Foundations — Handover Packages

Every bounded-context handover package in this folder (`BC-NN-*.md`) is **language-, framework-, and database-neutral**. This file holds everything that is **identical across all 12 packages**, so it lives in exactly one place:

1. the **Target Stack** declaration — the one block the implementer fills in
2. the neutral **notation** used in every package
3. the neutral **type vocabulary**
4. the **module structure & layering** model
5. the **shared-kernel** building blocks
6. **cross-cutting conventions** — outbox/inbox, domain events, error handling, persistence rules, the standard infrastructure tables
7. **testing conventions**

**How packages use this file.** Each `BC-NN-*.md` package is self-contained for its *domain design* — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, the test cases. For everything **stack-related and cross-cutting**, the package points here by section number (e.g. "see [[00-Shared-Foundations]] §5"). When you hand a package to a coding agent, **hand it this file too** — the two together are the complete brief for one module.

---

## 1. Target Stack — *declared by the implementer*

> This is the one block the instructor/implementer fills in. It applies to **all 12 modules** — they form a single modular monolith and share one stack. Fill it in once here; every package inherits it.

```
TARGET STACK
  Language:              C#
  Application framework: ASP.NET Core
  Persistence / ORM:     EF Core
  Database:              SQL Server
  In-process mediator:   MediatR
  Validation library:    FluentValidation
  Unit test stack:       NUnit, Moq, FluentAssertion
  Integration testing:   I won't use any real persistance so consider in-memory database concept.
```

If this block is still blank when you receive a package, **ask the implementer to fill it in before writing any code** — do not assume a stack.

Everything below is written against the **roles** above ("the chosen ORM", "the in-process mediator", "the validation library"), never against a specific product. Translate each role into its idiomatic equivalent in the chosen stack.

---

## 2. Notation

Used in every handover package:

- Type names in `PascalCase` (`JobSeekerProfile`, `EmailAddress`, …) are **ubiquitous-language names** — keep them as-is in code.
- Primitive types are neutral: `uuid`, `string`, `int`, `int64`, `decimal`, `bool`, `datetime` (always UTC), `enum`, `list<T>`, `map<K,V>`, `bytes`, and `T?` (nullable/optional).
- Contract and method shapes are written as **specifications, not code**:

```
MethodName(param: type, param: type) -> ReturnType
TypeName { field: type, field: type }
Port: PortName
  Operation(param: type) -> ReturnType
```

Implement these idiomatically in the target language — as interfaces, classes, records, structs, protocols, etc.

---

## 3. Neutral type vocabulary

Each handover package's relational schema is written in neutral types. Map each to your database's native type:

| Neutral | Typical mappings |
|---|---|
| `uuid` | UUID / GUID / 16-byte id |
| `string` | TEXT / VARCHAR / NVARCHAR |
| `int` / `int64` | INT / BIGINT |
| `decimal` | NUMERIC / DECIMAL |
| `bool` | BOOLEAN / BIT |
| `datetime` | TIMESTAMP WITH TIME ZONE (stored UTC) / DATETIME2 |
| `date` | DATE |
| `json` | native JSON/JSONB column; **if the database has none**, use serialized text or a child table — the domain model does not change either way |
| `enum` | stored as `string` (or the database's native enum) |

---

## 4. Module structure & layering

Every module follows Clean Architecture / Hexagonal layering. Five logical layers — express each as the target stack's idiomatic unit of modularity (a project/assembly, a Gradle/Maven module, a package or workspace, or at minimum a top-level folder with **enforced** dependency rules).

| Layer | Contains | May depend on |
|---|---|---|
| **Domain** | Entities, value objects, domain events, domain services, invariants. Framework-free and persistence-free. | nothing |
| **Application** | Use cases (commands & queries) and their handlers, DTOs, validation rules, **port interfaces**. | Domain |
| **Infrastructure** | Persistence (ORM mappings, repositories), the outbox/inbox, port adapters, background workers. | Application, Domain |
| **API / Presentation** | HTTP endpoints. | Application |
| **Contracts** *(public surface)* | Integration-event definitions + the module's public API interface. | nothing |

**Dependency rule:** `Domain ← Application ← {Infrastructure, API}`. `Contracts` depends on nothing. Only `Infrastructure` may reference the persistence/ORM library. The Domain layer is persistence-ignorant.

**Module registration:** each module exposes **one composition entry point** (an `Add<ModuleName>Module` function, a DI configuration class, a framework module — whatever the stack uses) that registers command/query/event handlers, validators, the persistence context, repositories, and port adapters, and wires the module's inbound integration-event subscriptions. The host composition root calls every module's entry point.

**Public surface:** other modules may reference **only** a module's `Contracts` layer. Everything else in a module is internal / non-exported wherever the language allows. No module reads another module's tables or internal types.

---

## 5. Shared-kernel building blocks

A shared building-blocks library is referenced by every module's `Domain` layer. Implement it once for the target stack; every package assumes these concepts exist:

```
Entity<Id>
  - Id: Id                              // strongly-typed identifier
  - equality is by Id

AggregateRoot<Id>  (is an Entity<Id>)
  - DomainEvents: list<DomainEvent>      // read-only
  - raise(event: DomainEvent)            // protected; appends to DomainEvents
  - clearDomainEvents()

ValueObject
  - immutable; equality is structural (by component values)

DomainEvent          // an in-process fact raised by an aggregate; handled within the module
IntegrationEvent     // a cross-module fact: { EventId: uuid, OccurredOnUtc: datetime }

Result               // an expected success-or-failure: { IsSuccess: bool, Error: Error }
Result<T>            // { IsSuccess: bool, Value: T, Error: Error }
Error                // { Code: string, Message: string }
```

**Conventions:**

- Each aggregate has its **own** strongly-typed id type wrapping a `uuid` (not a bare `uuid`), so ids of different aggregates cannot be mixed.
- Aggregates raise `DomainEvent`s via `raise(...)`.
- Expected failures (validation, invariant violations, not-found) return `Result` / `Result<T>`. Throw/raise exceptions only for truly exceptional or programmer errors.
- All timestamps are UTC.
- Value objects are created through validating factories: `Create(...) -> Result<T>`.

---

## 6. Cross-cutting conventions

These apply to **every** module. Packages do not repeat them.

### 6.1 Domain-event dispatch

After a unit of work is saved, a mediator pipeline step (or equivalent middleware) collects the `DomainEvents` from all changed aggregates and dispatches them in-process, then calls `clearDomainEvents()`.

### 6.2 Integration events — the outbox

Integration events (cross-module facts) are **never** published directly. They are written to the module's `outbox_messages` table **in the same transaction** as the aggregate change. A background relay reads unprocessed rows, publishes them, and stamps `processed_on_utc`. This guarantees an integration event is emitted if and only if the state change committed.

### 6.3 Consuming integration events — the inbox

Every inbound integration-event handler must be **idempotent**. Before processing, check the `inbox_messages` table for the event's `EventId`; if present, skip. Otherwise process and record the `EventId`. (Or make the operation naturally idempotent — but the inbox is the default.)

### 6.4 Persistence rules

- Each module owns its **own schema/namespace** in the chosen relational database. **No foreign key crosses a module boundary** — references to another module's ids are plain `uuid` columns with no FK constraint.
- Use the chosen ORM with code-first or migration-based schema management. The Domain layer stays persistence-ignorant.
- Value objects map to `json` columns via the ORM's type-conversion mechanism — **except** value objects (or their fields) that must be queried, indexed, or uniquely constrained, which are flattened to scalar columns alongside (or instead of) the `json` form.
- Strongly-typed ids map to `uuid` columns via the ORM's value-conversion mechanism.
- **Concurrency:** aggregates that can be updated concurrently carry an optimistic-concurrency token — a `version` integer the ORM increments, a `rowversion`/`timestamp` column, or the database's native mechanism.
- **One persistence context / unit-of-work per module**; its default schema/namespace is the module's schema.
- Child collections owned by an aggregate are loaded with it.

### 6.5 Standard infrastructure tables

Every module's schema includes these two tables (packages reference them here rather than redefining them):

```
TABLE outbox_messages
  id uuid PK, type string NOT NULL, content json NOT NULL,
  occurred_on_utc datetime NOT NULL, processed_on_utc datetime NULL, error string NULL

TABLE inbox_messages
  event_id uuid PK, processed_on_utc datetime NOT NULL
```

---

## 7. Testing conventions

Apply to every module. Packages list only their **module-specific test cases**; the strategy below is assumed.

- Use the target stack's **unit-test framework**, **assertion library**, and **test-double/mocking library**. For integration tests, run a **real instance of the chosen database in a container**.
- **Coverage target:** ≥ 85% line coverage on `Domain` and `Application`.
- **Three test layers:**
  1. **Domain unit tests** — pure, no test doubles. Cover every value-object factory (valid + each invalid case), every aggregate behavior and invariant (especially state-machine transitions — legal *and* illegal), and every domain service.
  2. **Application unit tests** — handlers with ports replaced by test doubles. Cover happy paths, each documented failure path, and the validation step.
  3. **Integration tests** — against the real containerized database. Cover: repository round-trips (including child collections and `json` value objects), schema migration applies cleanly, the outbox writes in the same transaction as the state change, inbox idempotency (delivering the same event twice is a no-op), API host-level happy paths, and reactions to consumed integration events.
- **Every acceptance criterion** in a package's §14 maps to at least one test. Treat that table as the definition-of-done checklist.

---

## How a handover package references this file

A package's stack-related sections are thin pointers, e.g.:

- §3 *Module structure & layering* → "Follows [[00-Shared-Foundations]] §4. Module-specific notes: …"
- §4 *Shared kernel reference* → "Uses the building blocks in [[00-Shared-Foundations]] §5."
- §11 *Persistence* → "Follows the persistence conventions in [[00-Shared-Foundations]] §6; includes the standard `outbox_messages` / `inbox_messages` tables from §6.5. Module-specific schema below."
- §13 *Test requirements* → "Follows the testing strategy in [[00-Shared-Foundations]] §7. Module-specific test cases below."

The package keeps full ownership of its domain design (§§1–2, 5–10, 12, 14) — only the stack-and-cross-cutting material is centralised here.
