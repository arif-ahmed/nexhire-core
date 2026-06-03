# Demo: Employer Registration Flow

## Business Story

A new company wants to join the Nexhire platform to post jobs and hire talent. The employer fills out a registration form on the website. Behind the scenes, three systems work together:

1. **Identity & Access (BC-1)** — creates the employer's login credential, checks password strength, ensures no duplicate email or mobile
2. **Employer Profile (BC-2)** — builds the company's profile record
3. **Notification (BC-9)** — gets alerted that a new employer registered so it can prepare a welcome message

The interesting part: when BC-1 finishes creating the credential, it doesn't call BC-9 directly. It just **announces** the registration happened. BC-9 picks up that announcement on its own schedule and logs the welcome action. This keeps the systems loosely coupled.

---

## Flow

```
1. Employer submits registration form
         │
         ▼
2. BC-2 receives the form, calls BC-1 to create the credential
   (synchronous — BC-2 needs the new UserId back right away)
         │
         ▼
3. BC-1 validates everything, creates the account, returns UserId
         │
         ▼
4. BC-2 saves the employer profile with that UserId
   (user gets a 201 Created response)
         │         │
         │         └──► BC-1 writes "UserRegistered" event to its outbox
         │                      (async — happens in same DB transaction)
         │
         ▼
5. BC-1's outbox relay reads the event (every 15s)
         │
         ▼
6. BC-9 receives the event, logs "would send welcome notification"
```

---

## How to Test

### Step 1: Start the database

```bash
docker compose up -d nexhire-db
```

This starts PostgreSQL 16 on `localhost:5432`. Wait a few seconds for the health check to pass.

### Step 2: Start the API

```bash
dotnet run --project src/Host/Nexhire.Api
```

The server starts at `http://localhost:5001`.

### Step 3: Register an employer

```bash
curl -s -X POST http://localhost:5001/api/employers/register \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "Tech Corp",
    "email": "admin@techcorp.com",
    "mobile": "+8801712345678",
    "password": "SecureP@ss123",
    "companyIdentifier": "TC-2024-001"
  }'
```

**Expected result** — `201 Created` with a response body like:

```json
{
  "employerProfileId": "b7e4a1c2-...",
  "userId": "d8f5b2e3-..."
}
```

This confirms the employer profile was created in BC-2 and the user credential was provisioned in BC-1.

### Step 4: Watch the event cross to BC-9

Look at the API console output. Within 15 seconds you'll see two log lines:

```
[IdentityAccessOutboxRelayBackgroundService]
  Processing outbox message ... of type UserRegisteredIntegrationEvent

[UserRegisteredConsumer]
  Received UserRegistered event for UserId=..., Role=Employer,
  Email=admin@techcorp.com — would send welcome notification
```

This is the proof that:
- BC-1 saved the event to its outbox when the account was created
- BC-1's outbox relay picked it up and published it
- BC-9 received and handled it without BC-1 ever knowing about BC-9

### Step 5: Prove idempotency (try registering again)

```bash
curl -s -X POST http://localhost:5001/api/employers/register \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "Tech Corp",
    "email": "admin@techcorp.com",
    "mobile": "+8801712345678",
    "password": "SecureP@ss123",
    "companyIdentifier": "TC-2024-001"
  }'
```

**Expected result** — `409 Conflict` with error code `E-REG-DUPLICATE` or similar. The same event is **not** published again.

---

## What This Proves

| Capability | Evidence |
|------------|----------|
| Sync cross-BC call works | BC-2 gets `UserId` from BC-1 in the same request |
| Async event delivery works | BC-1's event reaches BC-9 via outbox relay |
| Events survive transaction rollback | Event is written in same DB transaction as the account |
| Idempotent consumers | Duplicate event delivery is a no-op (inbox dedup) |
| Loose coupling | BC-1 doesn't reference BC-9; BC-9 references BC-1's Contracts only |
