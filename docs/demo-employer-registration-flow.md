# Demo: Employer Registration Event Flow

## Goal

Demonstrate a distributed event-driven flow across **three Bounded Contexts** in the Nexhire modular monolith. The flow shows how a single business operation (employer registration) propagates through multiple modules using **two integration patterns** simultaneously:

1. **Synchronous OHS call** — BC-2 Employer Profiles calls BC-1 IAM's `IdentityProvisioningApi.ProvisionCredential()` and receives the `UserId` back in the same request/response cycle.
2. **Asynchronous event choreography** — BC-1 IAM publishes `UserRegisteredIntegrationEvent` via the outbox; BC-9 Notification consumes it asynchronously.

This demonstrates that the architecture supports both request/response (when the caller needs a return value) and fire-and-forget eventing (when reactors don't need to block the caller).

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         HTTP POST /api/employers/register           │
│                              (anonymous)                            │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────────────┐
│  BC-2 Employer Profiles  │  RegisterEmployerCommandHandler           │
│                          │  1. Validate company info & company ID    │
│                          │  2. CALL IIdentityProvisioningApi ────────┼─── SYNC ──┐
│                          │  3. Create EmployerProfile.Register()     │            │
│                          │  4. Return EmployerProfileId              │            │
└──────────────────────────────────────────────────────────────────────┘            │
                                                                                     │
                                                                                     ▼
                                                                     ┌──────────────────────────────┐
                                                                     │ BC-1 IAM (IdentityAccess)    │
                                                                     │                              │
                                                                     │ ProvisionCredentialCommand   │
                                                                     │  • Rate-limit check          │
                                                                     │  • Email/mobile uniqueness   │
                                                                     │  • Password policy + breach  │
                                                                     │  • Argon2id hashing          │
                                                                     │  • Create UserAccount        │
                                                                     │    (PendingActivation)       │
                                                                     │  • Issue OTP Challenge       │
                                                                     │  • Persist + write outbox    │
                                                                     │                              │
                                                                     │ Returns Result<Guid> (UserId)│
                                                                     └──────────────┬───────────────┘
                                                                                    │
                                                                              (async, via outbox)
                                                                                    │
                                                                                    ▼
                                                                     ┌──────────────────────────────┐
                                                                     │ PublishDomainEventsInterceptor│
                                                                     │ Serializes                    │
                                                                     │ UserRegisteredIntegrationEvent│
                                                                     │ to OutboxMessages table       │
                                                                     └──────────────┬───────────────┘
                                                                                    │
                                                                              (every 15s)
                                                                                    │
                                                                     ┌──────────────┴───────────────┐
                                                                     │ IdentityAccessOutboxRelay    │
                                                                     │ Deserializes + publishes     │
                                                                     │ via MediatR IPublisher       │
                                                                     └──────────────┬───────────────┘
                                                                                    │
                                                                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│  BC-9 Notification  │  UserRegisteredConsumer                       │
│                     │                                               │
│   INotificationHandler<UserRegisteredIntegrationEvent>               │
│                     │                                               │
│   1. Inbox dedup check (by EventId)                                 │
│   2. LOG: "Received UserRegistered for UserId={id}, Role={role},    │
│           Email={email} — would send welcome notification"          │
│   3. Record InboxMessage for idempotency                            │
└─────────────────────────────────────────────────────────────────────┘
```

## Bounded Contexts Involved

| BC | Module | Role in flow |
|----|--------|-------------|
| **BC-1** | IdentityAccess | Creates user credential, emits `UserRegisteredIntegrationEvent` |
| **BC-2** | EmployerProfiles | Registers employer, calls BC-1 synchronously for credential provisioning |
| **BC-9** | Notification | Consumes `UserRegisteredIntegrationEvent`, logs welcome notification |

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for PostgreSQL — or use the in-memory database configured for development)
- The solution builds and tests pass

## Running the Demo

### 1. Start the API (with in-memory database — no Docker needed)

```bash
dotnet run --project src/Host/Nexhire.Api
```

The API starts at `http://localhost:5001`.

### 2. Register an employer

Send a `POST` request to the anonymous registration endpoint:

```bash
curl -s -X POST http://localhost:5001/api/employers/register \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "Tech Corp",
    "email": "admin@techcorp.com",
    "mobile": "+8801712345678",
    "password": "SecureP@ss123",
    "companyIdentifier": "TC-2024-001"
  }' | jq
```

**Expected response** (201 Created):

```json
{
  "employerProfileId": "a1b2c3d4-...",
  "userId": "e5f6g7h8-..."
}
```

### 3. Observe the event flow

Check the API console logs. Within 15 seconds (the outbox relay interval), you should see:

```
[IdentityAccessOutboxRelayBackgroundService] Processing outbox message {guid} of type Nexhire.Modules.IdentityAccess.Contracts.Events.UserRegisteredIntegrationEvent, Nexhire.Modules.IdentityAccess.Contracts

[UserRegisteredConsumer] Received UserRegistered event for UserId=e5f6g7h8-..., Role=Employer, Email=admin@techcorp.com — would send welcome notification
```

This confirms:
- BC-1 wrote the event to its outbox (via `PublishDomainEventsInterceptor`)
- `IdentityAccessOutboxRelayBackgroundService` picked it up, deserialized it, and published via MediatR
- BC-9's `UserRegisteredConsumer` handled it (inbox dedup passed, then logged)

### 4. Verify idempotency (optional)

Send the same request again. The second attempt returns a `409 Conflict` (duplicate company identifier or email), confirming no duplicate account or event is created.

---

## Tracing the Code

| Step | File | Line |
|------|------|------|
| BC-2 registration endpoint | `src/Modules/EmployerProfiles/.../Endpoints/EmployerEndpoints.cs` | ~50 |
| BC-2 `RegisterEmployerCommandHandler` | `src/Modules/EmployerProfiles/.../RegisterEmployer/RegisterEmployerCommandHandler.cs` | ~30 |
| BC-2 calls `IIdentityProvisioningApi` | `src/Modules/EmployerProfiles/.../Ports/IIdentityProvisioningApi.cs` | — |
| Host adapter (BC-1 → BC-2) | `src/Host/Nexhire.Api/Adapters/IdentityAccess/IdentityProvisioningApiAdapter.cs` | — |
| BC-1 `ProvisionCredentialCommandHandler` | `src/Modules/IdentityAccess/.../ProvisionCredential/ProvisionCredentialCommandHandler.cs` | ~50 |
| BC-1 interceptor writes outbox | `src/Shared/Nexhire.Shared.Infrastructure/Interceptors/PublishDomainEventsInterceptor.cs` | 78-86 |
| **NEW** BC-1 outbox relay | `src/Modules/IdentityAccess/.../BackgroundServices/IdentityAccessOutboxRelayBackgroundService.cs` | — |
| **NEW** BC-9 consumer | `src/Modules/Notification/.../IntegrationEvents/Consumers/UserRegisteredConsumer.cs` | — |

---

## What to Explain During the Demo

### 1. The Sync Pattern (BC-2 → BC-1)
- "BC-2 needs the `UserId` back *before* it can save the employer profile — it can't fire-and-forget."
- "The `IdentityProvisioningApi` contract is defined in BC-1's `Contracts` layer. BC-2 only depends on the contract interface, not the implementation."
- "At the composition root (Host's `Program.cs`), an adapter wires the contract to BC-1's actual `ProvisionCredentialCommandHandler`."

### 2. The Async Pattern (BC-1 → BC-9)
- "BC-1 doesn't know about BC-9. It just publishes `UserRegisteredIntegrationEvent` to its own outbox table."
- "The `PublishDomainEventsInterceptor` writes the event as JSON during `SaveChangesAsync` — in the same transaction as the user account."
- "`IdentityAccessOutboxRelayBackgroundService` polls the outbox every 15 seconds, deserializes each message, and publishes it through MediatR."
- "BC-9's `UserRegisteredConsumer` is an `INotificationHandler<T>` — MediatR routes the event to it automatically."

### 3. Idempotency / Inbox Pattern
- "BC-9 checks its `InboxMessages` table by `EventId` before processing. If the event was already handled, it's a no-op."
- "This guarantees at-least-once delivery without duplicate side effects."

---

## Architecture Patterns Illustrated

| Pattern | Where |
|---------|-------|
| Open Host Service (OHS) + Published Language (PL) | BC-1's `IIdentityProvisioningApi` + integration events |
| Synchronous OHS call | BC-2 → BC-1 (needs `UserId` return value) |
| Asynchronous event choreography | BC-1 → BC-9 via outbox |
| Outbox pattern (transactional outbox) | `PublishDomainEventsInterceptor` writes to `OutboxMessages` in same transaction |
| Inbox pattern (idempotent consumer) | `UserRegisteredConsumer` checks `InboxMessages` before processing |
| Modular monolith (in-process communication) | All events flow through MediatR — no network calls between modules |
