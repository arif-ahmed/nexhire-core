# BC-03 Job Seeker Profile — Sequential E2E Test Plan

## Preconditions

| Item | What you need |
|---|---|
| Host running | `dotnet run --project src/Host/Nexhire.Api` |
| Database | PostgreSQL with migrations applied (automatic on first run) |
| Auth token | BC-1 issues tokens after login. All endpoints except `GET p/{slug}` require `Authorization: Bearer <token>`. |
| Seed data | IdentityAccess + EmployerProfiles + JobSeekerProfile seeds must have run (automatic via `Program.cs`) |

## State machine

```
PendingActivation ──[Activate, via UserAccountActivated consumer]──→ Active
       │                                                              │
       │                                                        [Deactivate,
       │                                                         via AccountDeactivated
       │                                                         or UserAccountSuspended]
       │                                                              │
       │                                                              v
       │                                                        Deactivated
       │                                                              │
       │                                                        [Reactivate,
       │                                                         via UserAccountReinstated]
       │                                                              │
       └──────────────────────────────────────────────────────────────→ Active
```

## Phase 1 — Registration Journey

### 1. Register a new job seeker

```
POST /api/job-seekers
Auth: none (public registration)
Body: {
  "email": "newseeker@example.com",
  "mobile": "+8801700000001",
  "password": "Str0ng!Pass",
  "firstName": "Rahim",
  "lastName": "Miah",
  "gender": "Male"
}
```
**Expected:** `201 Created` + `{ "id": "<guid>" }`
**Verifies:** Registration provisions BC-1 credential via `IIdentityProvisioningApi`, creates profile in `PendingActivation`. Emits `JobSeekerRegisteredIntegrationEvent` through outbox.

**Error cases to test:**
- Duplicate email → `409 E-JOBSEEKER-DUPLICATE-EMAIL`
- Invalid email → `400 E-VALIDATION-ERROR`
- Weak password → `400`

### 2. Check profile status after registration

```
GET /api/job-seekers/me
Auth: job-seeker token
```
**Expected:** `200 OK` + `status: "PendingActivation"`, visibility `"Private"`, `completeness.percentage: 15`
**Verifies:** Profile read-back; status reflects registration state.

### 3. Account activated (simulated BC-1 event)

```
Mock: publish UserAccountActivatedIntegrationEvent(UserId: ..., ActivatedAt: ...)
```
**Expected:** Profile transitions to `Active`.
**Verifies:** Consumption of `UserAccountActivatedIntegrationEvent`. Idempotent on duplicate delivery.

**Check:**
```
GET /api/job-seekers/me
```
status now `"Active"`

## Phase 2 — Profile Enrichment

### 4. Update job preferences

```
PUT /api/job-seekers/me/preferences
Auth: job-seeker token
Body: {
  "jobTypes": ["FullTime", "Remote"],
  "industries": ["Technology", "Finance"],
  "locations": ["Dhaka", "Chattogram"],
  "workArrangements": ["Office", "Hybrid"],
  "minSalaryExpectation": 50000,
  "maxSalaryExpectation": 150000,
  "salaryCurrency": "BDT"
}
```
**Expected:** `204 No Content`
**Verifies:** Profile updated.

**Error cases:**
- Empty industries → `400`
- Negative salary → `400`

### 5. Update addresses

```
PUT /api/job-seekers/me/addresses
Auth: job-seeker token
Body: {
  "currentAddress": { "line1": "42 Test Road", "city": "Dhaka", "district": "Dhaka", "postcode": "1212", "country": "Bangladesh" },
  "permanentAddress": { "line1": "10 Village Lane", "city": "Chattogram", "district": "Chattogram", "postcode": "4000", "country": "Bangladesh" }
}
```
**Expected:** `204 No Content`

### 6. Update recent salary

```
PUT /api/job-seekers/me/salary
Auth: job-seeker token
Body: { "amount": 80000, "currency": "BDT" }
```
**Expected:** `204 No Content`

### 7. Set profile visibility to RecruitersOnly

```
PUT /api/job-seekers/me/visibility
Auth: job-seeker token
Body: { "visibility": "RecruitersOnly" }
```
**Expected:** `204 No Content`
**Verifies:** Visibility update. Emits `ProfileVisibilityChangedIntegrationEvent` through outbox.

**Check:**
```
GET /api/job-seekers/me
```
visibility now `"RecruitersOnly"`

### 8. Add education entry

```
POST /api/job-seekers/me/education
Auth: job-seeker token
Body: {
  "degree": "B.Sc. in Computer Science",
  "institution": "University of Dhaka",
  "startDate": "2018-01-01",
  "endDate": "2022-12-31",
  "gpa": 3.75
}
```
**Expected:** `201 Created`
**Verifies:** Education entry added.

**Error cases:**
- End date before start date → `400`
- Missing institution → `400`

### 9. Update education entry

```
PUT /api/job-seekers/me/education/{educationId}
Auth: job-seeker token
Body: { "degree": "M.Sc. in Computer Science", "institution": "University of Dhaka", "startDate": "2023-01-01", "endDate": null, "gpa": null }
```
**Expected:** `204 No Content`
**Verifies:** Education entry updated.

**Error cases:**
- Non-existent educationId → `400`

### 10. Add second education entry

```
POST /api/job-seekers/me/education
Body: { "degree": "Higher Secondary", "institution": "Dhaka College", "startDate": "2016-01-01", "endDate": "2017-12-31" }
```
**Expected:** `201 Created`

### 11. Remove education entry

```
DELETE /api/job-seekers/me/education/{educationId}
Auth: job-seeker token
```
**Expected:** `204 No Content`
**Verifies:** Education entry removed.

### 12. Add experience entry

```
POST /api/job-seekers/me/experience
Auth: job-seeker token
Body: {
  "company": "Tech Corp Ltd.",
  "role": "Software Engineer",
  "startDate": "2022-01-01",
  "endDate": "2023-12-31",
  "isCurrent": false,
  "responsibilities": "Full-stack development with .NET and React"
}
```
**Expected:** `201 Created`
**Verifies:** Experience entry added.

### 13. Update experience entry

```
PUT /api/job-seekers/me/experience/{experienceId}
Auth: job-seeker token
Body: { "company": "Tech Corp Ltd.", "role": "Senior Software Engineer", "startDate": "2022-01-01", "endDate": "2024-06-01", "isCurrent": true, "responsibilities": "Led team of 5 engineers" }
```
**Expected:** `204 No Content`

### 14. Remove experience entry

```
DELETE /api/job-seekers/me/experience/{experienceId}
Auth: job-seeker token
```
**Expected:** `204 No Content`

## Phase 3 — Skills Management

### 15. Add skill

```
POST /api/job-seekers/me/skills
Auth: job-seeker token
Body: { "rawSkillLabel": "C#", "category": "Hard", "tier": "Primary", "proficiency": 5 }
```
**Expected:** `201 Created`
**Verifies:** Skill added. If taxonomy mapping fails, returns fallback raw label.

**Error cases:**
- Invalid proficiency (0 or 6) → `400`
- Duplicate skill code → `409 E-PROFILE-DUPLICATE-SKILL`

### 16. Remove skill

```
DELETE /api/job-seekers/me/skills/{skillId}
Auth: job-seeker token
```
**Expected:** `204 No Content`

## Phase 4 — Self Attestation & Resume

### 17. Self-attest profile

```
POST /api/job-seekers/me/self-attest
Auth: job-seeker token
```
**Expected:** `204 No Content`
**Verifies:** `verification.selfAttested: true`, emits `ProfileVerificationChangedEvent` (internal).

**Check:**
```
GET /api/job-seekers/me
```
`verification.selfAttested` now `true`

### 18. Upload resume (async operation)

```
POST /api/job-seekers/me/resume
Auth: job-seeker token
Content-Type: multipart/form-data
File: resume.pdf (valid PDF, ≤10 MB)
```
**Expected:** `202 Accepted` + `{ "resumeId": "<guid>" }`
**Verifies:** Resume uploaded, virus scanned, parsing begins. Emits `ResumeUploadedIntegrationEvent` through outbox.

**Error cases:**
- Upload .exe → `400 E-UPLOAD-INVALID-FORMAT`
- Upload 15 MB file → `400` (size limit)
- Infected file (stub returns infected) → `422`

### 19. Get resume parse status

```
GET /api/job-seekers/me/resume/status
Auth: job-seeker token
```
**Expected:** `200 OK` + `{ resumeId, parseStatus: "Parsed"|"Parsing"|"Failed", parsedData?: { ... } }`
**Verifies:** US-8.1 — Parse status including structured skills with taxonomy codes.

### 20. Confirm parsed resume fields

```
POST /api/job-seekers/me/resume/confirm
Auth: job-seeker token
Body: { "resumeId": "<guid>", "selectedFieldKeys": ["education", "experience"] }
```
**Expected:** `204 No Content`
**Verifies:** Merged confirmed fields into profile. Emits `ResumeFieldsConfirmedEvent` (internal).

## Phase 5 — Supplementary Documents

### 21. Upload supplementary document

```
POST /api/job-seekers/me/documents?kind=Certificate
Auth: job-seeker token
Content-Type: multipart/form-data
File: certificate.pdf (PDF/PNG/JPG, ≤10 MB)
```
**Expected:** `201 Created`
**Verifies:** US-8.1 — Document uploaded, virus scanned. Emits `SupplementaryDocumentUploadedIntegrationEvent` through outbox.

**Upload 10 documents** — the 11th should fail with `409 E-UPLOAD-LIMIT-EXCEEDED`

### 22. Delete supplementary document

```
DELETE /api/job-seekers/me/documents/{documentId}
Auth: job-seeker token
```
**Expected:** `204 No Content`

## Phase 6 — Public Profile Sharing

### 23. Enable public sharing

```
POST /api/job-seekers/me/sharing/enable
Auth: job-seeker token
```
**Expected:** `200 OK` + `{ enabled: true, slug: "rahim-miah-abc123", qrCodeStorageKey: "qr/..." }`
**Verifies:** Enables sharing, generates slug, stores QR. Emits `PublicSharingEnabledEvent` (internal) + `ProfileCompletenessChangedIntegrationEvent` through outbox.

### 24. Access public profile (anonymous)

```
GET /p/rahim-miah-abc123
Auth: none
```
**Expected:** `200 OK` + `{ firstName, lastName, skills, experience, education, currentCity, currentCountry }`
**Verifies:** PII-secured public profile returned. No email, mobile, or salary exposed.

**Error cases:**
- Non-existent slug → `404`
- Deactivated profile slug → `404`

### 25. Regenerate public slug

```
POST /api/job-seekers/me/sharing/regenerate-slug
Auth: job-seeker token
```
**Expected:** `200 OK` + `{ slug: "rahim-miah-xyz789" }`
**Verifies:** Old slug invalidated, new slug active. Emits `PublicSharingSlugRegeneratedEvent` (internal).

### 26. Disable public sharing

```
DELETE /api/job-seekers/me/sharing
Auth: job-seeker token
```
**Expected:** `204 No Content`
**Verifies:** Disables sharing. Emits `PublicSharingDisabledEvent` (internal).

**Check old slug returns 404:**
```
GET /p/rahim-miah-xyz789
```
→ `404 Not Found`

## Phase 7 — Completeness & History

### 27. Get completeness score

```
GET /api/job-seekers/me/completeness
Auth: job-seeker token
```
**Expected:** `200 OK` + `{ percentage: <int>, missingSections: [...] }`
**Verifies:** Dynamically computed from present fields (education, experience, skills, resume, preferences, addresses, salary, self-attestation).

### 28. Get edit history

```
GET /api/job-seekers/me/history
Auth: job-seeker token
```
**Expected:** `200 OK` + `{ id, jobSeekerProfileId, versions: [ { id, action, changedFields, createdOnUtc }, ... ] }`
**Verifies:** US-8.6 — Snapshot-based audit trail.

### 29. Restore profile version

```
POST /api/job-seekers/me/history/restore/{versionId}
Auth: job-seeker token
```
**Expected:** `204 No Content`
**Verifies:** Profile state restored from snapshot. Emits `ProfileRestoredEvent` (internal).

## Phase 8 — Cross-Module Event Consumption

### 30. Account deactivation (BC-1 event)

```
Mock: publish AccountDeactivatedIntegrationEvent(UserId: ..., DeactivatedAt: ...)
```
**Expected:** Profile status → `Deactivated`.
**Verifies:** `AccountDeactivatedConsumer`. All mutations blocked.

**Check:**
```
GET /api/job-seekers/me
```
status now `"Deactivated"`

### 31. Try mutations while deactivated

```
PUT /api/job-seekers/me/preferences
Body: { ... }
```
**Expected:** `400 E-PROFILE-DEACTIVATED`

### 32. Account reinstatement (BC-1 event)

```
Mock: publish UserAccountReinstatedIntegrationEvent(UserId: ..., At: ...)
```
**Expected:** Profile status → `Active` (via `Reactivate()`).
**Verifies:** `UserAccountReinstatedConsumer`.

### 33. Account suspension (BC-1 event)

```
Mock: publish UserAccountSuspendedIntegrationEvent(UserId: ..., Reason: "...", At: ...)
```
**Expected:** Profile deactivated (via `Deactivate()`).
**Verifies:** `UserAccountSuspendedConsumer`.

### 34. Identity verified by government (BC-8 event)

```
Mock: publish ExternalJobSync.Core.Domain.Events.IdentityVerifiedByGovernmentIntegrationEvent(
  UserId: ..., Registry: "NID", VerifiedOnUtc: ...)
```
**Expected:** `verification.identityVerified: true`
**Verifies:** `IdentityVerifiedByGovernmentConsumer`.

### 35. Education verified by government (BC-8 event)

```
Mock: publish EducationVerifiedIntegrationEvent(JobSeekerProfileId: ..., CredentialRef: "...", VerifiedOnUtc: ...)
```
**Expected:** `verification.educationVerified: true`
**Verifies:** `EducationVerifiedConsumer`.

### 36. Taxonomy cache invalidation (BC-11 event)

```
Mock: publish TaxonomyUpdatedIntegrationEvent(TaxonomyId: ..., Kind: "Skill", Version: 3, ...)
```
**Expected:** Log entry; stub cache eviction signal.
**Verifies:** `TaxonomyUpdatedConsumer`.

## Phase 9 — Contract API (In-Process)

### 37. Check `IJobSeekerProfilePublicApi.GetCompletenessScoreAsync`

```csharp
var api = serviceProvider.GetRequiredService<IJobSeekerProfilePublicApi>();
var result = await api.GetCompletenessScoreAsync(userId);
Assert.True(result.IsSuccess);
Assert.InRange(result.Value.Percentage, 0, 100);
```
**Verifies:** §9.3 — Public API contract.

### 38. Check `IJobSeekerProfilePublicApi.GetVerificationStatusAsync`

```csharp
var result = await api.GetVerificationStatusAsync(userId);
Assert.True(result.IsSuccess);
Assert.True(result.Value.SelfAttested);
```
**Verifies:** Verification flag exposure.

### 39. Check `IJobSeekerProfilePublicApi.GetPublicProfileAsync`

```csharp
var result = await api.GetPublicProfileAsync("rahim-miah-abc123");
Assert.True(result.IsSuccess);
Assert.NotNull(result.Value);
Assert.Equal("Rahim", result.Value.FirstName);
```
**Verifies:** Public profile projection.

### 40. Check `IJobSeekerProfilePublicApi.ExistsAsync`

```csharp
var exists = await api.ExistsAsync(userId);
Assert.True(exists);

var notExists = await api.ExistsAsync(Guid.NewGuid());
Assert.False(notExists);
```
**Verifies:** Existence check used by other modules (e.g., BC-5 validation).

## Produced Integration Events (Outbox)

| Event | Trigger | Consumer(s) |
|---|---|---|
| `JobSeekerRegisteredIntegrationEvent` | Registration | BC-6, BC-7, BC-10 |
| `ProfileLevel2CompletedIntegrationEvent` | Completeness ≥ 80% | BC-6, BC-7 |
| `ResumeUploadedIntegrationEvent` | Resume upload | BC-6 |
| `ResumeParsedIntegrationEvent` | Parse completes | BC-6 |
| `ProfileSkillsUpdatedIntegrationEvent` | Skill add/remove | BC-7 |
| `ProfileVisibilityChangedIntegrationEvent` | Visibility change | BC-10 |
| `SupplementaryDocumentUploadedIntegrationEvent` | Doc upload | BC-6, BC-10 |
| `ProfileCompletenessChangedIntegrationEvent` | Score changes | BC-7, BC-10 |

## Consumed Integration Events (Inbox Dedup)

| Event | Producer | Handler Action |
|---|---|---|
| `UserAccountActivatedIntegrationEvent` | BC-1 (IdentityAccess) | `profile.Activate()` |
| `AccountDeactivatedIntegrationEvent` | BC-1 (IdentityAccess) | `profile.Deactivate()` |
| `UserAccountSuspendedIntegrationEvent` | BC-1 (IdentityAccess) | `profile.Deactivate()` |
| `UserAccountReinstatedIntegrationEvent` | BC-1 (IdentityAccess) | `profile.Reactivate()` |
| `IdentityVerifiedByGovernmentIntegrationEvent` | BC-8 (ExternalJobSync) | `profile.ApplyIdentityVerified()` |
| `EducationVerifiedIntegrationEvent` | BC-8 (ExternalJobSync) | `profile.ApplyEducationVerified()` |
| `TaxonomyUpdatedIntegrationEvent` | BC-11 (AdminConfig) | Cache invalidation (stub) |

## Story → Endpoint Mapping

| Story | Description | Endpoints | Phase |
|---|---|---|---|
| Registration | Seeker registration | `POST /api/job-seekers`, `GET /me` | 1 |
| Profile Enrichment | Education, experience, skills, preferences, addresses, salary, visibility | `PUT /me/preferences`, `PUT /me/addresses`, `PUT /me/salary`, `PUT /me/visibility`, `POST /me/education`, `PUT /me/education/{id}`, `DELETE /me/education/{id}`, `POST /me/experience`, `PUT /me/experience/{id}`, `DELETE /me/experience/{id}`, `POST /me/skills`, `DELETE /me/skills/{id}` | 2–3 |
| Self Attestation & Resume | Self-attest, upload/parse/confirm resume | `POST /me/self-attest`, `POST /me/resume`, `GET /me/resume/status`, `POST /me/resume/confirm` | 4 |
| Documents | Supplementary document management | `POST /me/documents`, `DELETE /me/documents/{id}` | 5 |
| Public Profile | Enable/disable/regenerate sharing, anonymous access | `POST /me/sharing/enable`, `DELETE /me/sharing`, `POST /me/sharing/regenerate-slug`, `GET /p/{slug}` | 6 |
| Completeness & History | Score, version audit, restore | `GET /me/completeness`, `GET /me/history`, `POST /me/history/restore/{versionId}` | 7 |

## Key Invariants

| Transition | Must succeed | Must fail |
|---|---|---|
| `PendingActivation → Active` | `UserAccountActivated` consumer | Profile mutations (blocked) |
| `Active → Deactivated` | `AccountDeactivated` or `UserAccountSuspended` consumer | — |
| `Deactivated → Active` | `UserAccountReinstated` consumer | Profile mutations (blocked) |
| Self-attest while Active | `POST /me/self-attest` | Self-attest while `PendingActivation` (if tested) |
| Public slug regeneration | `POST /me/sharing/regenerate-slug` | Regenerate with sharing disabled |
| Resume upload while Active | `POST /me/resume` → 202 | Upload while deactivated |
| History restore | `POST /me/history/restore/{id}` → 204 | Restore to non-existent version → 400 |

## Running Automated Tests

```bash
# All unit tests (fast, no Docker)
dotnet test tests/Modules/JobSeekerProfile/Nexhire.Modules.JobSeekerProfile.Tests.Unit \
  --filter "FullyQualifiedName~ValueObjectsTests|FullyQualifiedName~DomainServicesTests|FullyQualifiedName~AggregateTests|FullyQualifiedName~ResumeTests|FullyQualifiedName~ProfileHistoryTests" \
  -v n

# All unit tests (simple)
dotnet test tests/Modules/JobSeekerProfile/Nexhire.Modules.JobSeekerProfile.Tests.Unit
```
