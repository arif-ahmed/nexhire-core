# BC-02 Employer Profile Management — Sequential E2E Test Plan

## Preconditions

| Item | What you need |
|---|---|
| Host running | `dotnet run --project src/Host/Nexhire.Api` |
| Database | PostgreSQL with migrations applied (automatic on first run) |
| Auth token | BC-1 issues tokens after login. All endpoints except `/public` require `Authorization: Bearer <token>`. |
| Admin token | MoL Administrator token required for approve/reject. Permission claim: `users:manage` |
| Seed data | IdentityAccess + EmployerProfiles seeds must have run (automatic via `Program.cs`) |

## State machine

```
PendingActivation ──[Activate]──→ PendingVerification ──[BeginAutoVerify]──→ AutoPending
       │                                    │                                  │
       │                              [L2 Complete]                     [Pass] │ [Fail]
       │                                    │                                  │
       v                                    v                                  v
  (OTP via BC-1)                    (ready for verify)                  Verified  PendingManualVerification
                                                                                   │              │
                                                                             [Approve]       [Reject]
                                                                                  │              │
                                                                                  v              v
                                                                            Verified        Rejected ──[Resubmit]──→ PendingManualVerification
```

## Phase 1 — Registration Journey (US-3.1.2-01)

### 1. Register a new employer

```
POST /api/employers/register
Auth: none (public registration)
Body: {
  "email": "testcompany@example.com",
  "mobile": "+8801700000001",
  "password": "Str0ng!Pass",
  "companyName": "Test Company Ltd.",
  "companyIdentifier": "REG-TEST-001"
}
```
**Expected:** `201 Created` + `{ "employerProfileId": "<guid>" }`
**Verifies:** US-3.1.2-01 — Registration with L1 fields. Calls BC-1 `IdentityProvisioningApi` synchronously, creates profile in `PendingActivation`.

**Error cases to test:**
- Duplicate `companyIdentifier` → `409 E-REG-DUPLICATE`
- Invalid email → `400 E-REG-INVALID-EMAIL`
- Invalid mobile → `400 E-REG-INVALID-MOBILE`

### 2. Register with duplicate company identifier

```
POST /api/employers/register
Body: { ... same companyIdentifier: "REG-TEST-001" ... }
```
**Expected:** `409 Conflict` — `E-REG-DUPLICATE`
**Verifies:** AC-02 (unique company ID enforced pre-provisioning).

### 3. Check profile is PendingActivation

```
GET /api/employers/me
Auth: employer token
```
**Expected:** `200 OK` + `status: "PendingActivation"`
**Verifies:** US-3.1.2-03 — Profile read-back; status reflects registration state.

### 4. Employer account activated (simulated via BC-1 event)

> In production, BC-1 emits `UserAccountActivatedIntegrationEvent` when user clicks OTP link.
> For testing, trigger the consumer directly or use the integration test `UserAccountActivatedConsumer_ShouldActivateProfile`.

```
Mock: publish UserAccountActivatedIntegrationEvent(UserId: ..., ActivatedAt: ...)
```
**Expected:** Profile transitions to `PendingVerification`
**Verifies:** §9.1 — Consumption of `UserAccountActivatedIntegrationEvent`. Idempotent on duplicate delivery.

**Check:**
```
GET /api/employers/me
```
status now `"PendingVerification"`

## Phase 2 — Level 2 Details (US-3.1.2-01 AC-07)

### 5. Complete Level 2 details

```
PUT /api/employers/me/level2
Auth: employer token
Body: {
  "website": "https://testcompany.com",
  "industry": "Technology",
  "companySize": "Medium",
  "address": {
    "line1": "42 Test Street",
    "city": "Dhaka",
    "district": "Dhaka",
    "postcode": "1212",
    "country": "Bangladesh"
  },
  "description": "A test company for e2e testing."
}
```
**Expected:** `200 OK`
**Verifies:** US-3.1.2-01 AC-07 — Level 2 completion. Profile becomes ready for verification.

**Error cases:**
- Invalid website URL → `400`
- Missing required address fields → `400`

## Phase 3 — Verification Flow (US-3.1.2-02)

### 6. Request automatic verification

```
POST /api/employers/me/verification/request
Auth: employer token
Body: { "registryRef": "gov-reg-ref-001" }
```
**Expected:** `202 Accepted`
**Verifies:** US-3.1.2-02 AC-01 — Begins auto-verification, emits `EmployerVerificationRequestedIntegrationEvent`.

**Error cases:**
- Request with L2 incomplete (skip step 5) → `409`
- Request from `PendingActivation` (skip activation) → `409`

### 7. Auto-verification succeeds (simulated via saga stub)

> In production, BC-8 (via `EmployerVerificationSaga`) emits `EmployerVerifiedByGovernmentIntegrationEvent`.
> For testing, use the stub saga adapter or publish the event directly.

```
Mock: publish EmployerVerifiedByGovernmentIntegrationEvent(
  EmployerProfileId: ..., EvidenceRef: "auto-pass-001", At: ...)
```
**Expected:** Profile transitions to `Verified` (status `Verified`, outcome `AutoPassed`)

**Check:**
```
GET /api/employers/me
```
status now `"Verified"`

```
GET /api/employers/me/verification
```
**Expected:** `200 OK` + `{ status: "Verified", outcome: "AutoPassed", isVerified: true }`
**Verifies:** US-3.1.2-02 AC-07 — Verification status query.

### 8. Check public profile (anonymous)

```
GET /api/employers/{profileId}/public
Auth: none
```
**Expected:** `200 OK` + `{ companyName: "Test Company Ltd.", isVerified: true, logo: null, ... }`
**Verifies:** US-3.1.2-03 AC-05 — Public profile returns name, verified badge; images/documents omitted.

### 9. Public profile for suspended/deactivated → 404

```
GET /api/employers/suspended-profile-id/public
```
**Expected:** `404 Not Found`
**Verifies:** Suspended/Deactivated/Rejected profiles return 404 on public endpoint.

## Phase 3b — Alternative: Manual Verification Branch

> Only test this branch if you want to cover the reject→resubmit→approve path.

### 10. Reject manual verification (Admin only)

```
POST /api/employers/{profileId}/verification/reject
Auth: admin token (users:manage permission)
Body: { "reason": "Documentation incomplete — missing VAT certificate." }
```
**Expected:** `200 OK`
**Verifies:** US-3.1.2-02 AC-04 — MoL rejection with required reason.
**Error cases:**
- Call without admin token → `403 Forbidden`
- Missing reason → `400`

**Check:**
```
GET /api/employers/me/verification
```
status now `"Rejected"`, outcome `ManualRejected`, `rejectionReason` set.

### 11. Resubmit without editing → fails

```
POST /api/employers/me/verification/resubmit
Auth: employer token
```
**Expected:** `409 Conflict` — `E-VERIFY-NO-CHANGES`
**Verifies:** US-3.1.2-02 AC-08 — Company info must be edited since rejection.

### 12. Edit company info, then resubmit

```
PUT /api/employers/me
Body: { "description": "Updated description with VAT certificate details." }
```
→ `200 OK`

Then:
```
POST /api/employers/me/verification/resubmit
```
→ `202 Accepted`
**Verifies:** US-3.1.2-02 AC-08 — Resubmit succeeds after edit. Profile moves to `PendingManualVerification`.

### 13. Approve manual verification (Admin only)

```
POST /api/employers/{profileId}/verification/approve
Auth: admin token (users:manage permission)
Body: { "evidenceRef": "manual-evidence-001" }
```
**Expected:** `200 OK`
**Verifies:** US-3.1.2-02 AC-04 — MoL approval. Status → `Verified`, outcome `ManualPassed`.

## Phase 4 — Profile Management (US-3.1.2-03)

### 14. Update company information

```
PUT /api/employers/me
Auth: employer token
Body: {
  "companyName": "Test Company Ltd. (Updated)",
  "website": "https://testcompany-updated.com",
  "description": "Updated company description."
}
```
**Expected:** `200 OK`
**Verifies:** US-3.1.2-03 AC-08 — Profile editing. Emits `EmployerProfileUpdatedIntegrationEvent`.

### 15. Upload logo

```
POST /api/employers/me/logo
Auth: employer token
Content-Type: multipart/form-data
File: logo.png (valid PNG, ≤5 MB)
```
**Expected:** `200 OK` + `{ "storageKey": "...", "originalFileName": "logo.png", ... }`
**Verifies:** US-3.1.2-03 AC-01 — Logo upload, virus scan, aggregate attachment.

**Error cases:**
- Upload BMP/GIF → `400 E-UPLOAD-INVALID-FORMAT`
- Upload 6 MB file → `413 E-UPLOAD-SIZE-EXCEEDED`

### 16. Upload company images

```
POST /api/employers/me/images
Auth: employer token
Content-Type: multipart/form-data
File: office1.jpg
```
**Expected:** `201 Created` + image id
**Verifies:** US-3.1.2-03 AC-02 — Gallery image upload.

**Upload 5 more images** — the 6th should fail with `409 E-UPLOAD-LIMIT-EXCEEDED`.

### 17. Remove company image

```
DELETE /api/employers/me/images/{imageId}
Auth: employer token
```
**Expected:** `204 No Content`
**Verifies:** US-3.1.2-03 — Image removal (cascading from aggregate, deleting from `ObjectStorage`).

**Error:** non-existent id → `404`

### 18. Upload supplementary document

```
POST /api/employers/me/documents
Auth: employer token
Content-Type: multipart/form-data
File: vat-certificate.pdf
Body: kind: "VatCertificate"
```
**Expected:** `201 Created` + document id
**Verifies:** US-3.1.2-03 AC-06 — Document upload (PDF/PNG/JPG, ≤10 MB).

**Upload 10 documents** — the 11th should fail with `409 E-UPLOAD-LIMIT-EXCEEDED`.

### 19. Remove supplementary document

```
DELETE /api/employers/me/documents/{documentId}
Auth: employer token
```
**Expected:** `204 No Content`
**Verifies:** US-3.1.2-03 AC-07 — Document removal.

## Phase 5 — Dashboard (US-3.1.2-04)

> Dashboard data is projected from consumed integration events (BC-4, BC-5, BC-7). To see populated data, fire those events first.

### 20. Seed dashboard data (simulated)

Publish these events (or use the integration test pattern):
- `JobPostingPublishedIntegrationEvent` → creates `dashboard_postings` row
- `JobPostingClosedIntegrationEvent` → updates posting status
- `ApplicationSubmittedIntegrationEvent` → creates `dashboard_applications` row
- `CandidateRecommendationGeneratedIntegrationEvent` → creates `dashboard_matched_candidates` rows

### 21. Get employer dashboard

```
GET /api/employers/me/dashboard
Auth: employer token
```
**Expected:** `200 OK` + `{ activePostings, totalPostings, totalApplications, matchedCandidates, shortlistCount }`
**Verifies:** US-3.1.2-04 AC-01/07 — Dashboard read model projected from events.

### 22. Get dashboard postings

```
GET /api/employers/me/dashboard/postings
Auth: employer token
```
**Expected:** `200 OK` + `[{ postingId, title, status }, ...]`
**Verifies:** US-3.1.2-04 AC-02 — Posting list from projection.

### 23. Get matched candidates

```
GET /api/employers/me/dashboard/matched-candidates
Auth: employer token
```
**Expected:** `200 OK` + `[{ candidateUserId, matchScore, ... }]`
**Verifies:** US-3.1.2-04 AC-03 — Candidate recommendations from BC-7 events.

## Phase 6 — Shortlists / Talent Pool (US-3.1.2-04)

### 24. Create shortlist

```
POST /api/employers/me/shortlists
Auth: employer token
Body: { "name": "Top Java Developers" }
```
**Expected:** `201 Created` + shortlist id
**Verifies:** US-3.1.2-04 AC-04 — Named talent pool creation.

**Error:** empty name → `400`

### 25. Get all shortlists

```
GET /api/employers/me/shortlists
```
**Expected:** `200 OK` + `[{ id, name, memberCount: 0 }]`

### 26. Get shortlist detail

```
GET /api/employers/me/shortlists/{shortlistId}
```
**Expected:** `200 OK` + `{ id, name, members: [] }`
**Error:** non-existent id → `404`

### 27. Rename shortlist

```
PUT /api/employers/me/shortlists/{shortlistId}
Auth: employer token
Body: { "newName": "Top Backend Engineers" }
```
**Expected:** `200 OK`
**Verifies:** US-3.1.2-04 AC-05 — Shortlist renaming.

### 28. Add candidate to shortlist

```
POST /api/employers/me/shortlists/{shortlistId}/candidates
Auth: employer token
Body: { "candidateUserId": "<guid>", "matchScore": 92 }
```
**Expected:** `201 Created`
**Verifies:** US-3.1.2-04 AC-04 — Candidate added; emits `CandidateSavedToTalentPoolIntegrationEvent` **through outbox**.

**Error:** duplicate candidate → `409`

### 29. Remove candidate from shortlist

```
DELETE /api/employers/me/shortlists/{shortlistId}/candidates/{memberId}
```
**Expected:** `204 No Content`
**Verifies:** US-3.1.2-04 — Candidate removal.

### 30. Delete shortlist

```
DELETE /api/employers/me/shortlists/{shortlistId}
```
**Expected:** `204 No Content`
**Verifies:** US-3.1.2-04 AC-05 — Soft deletion.

## Phase 7 — Employer Public API (Cross-Module Contract)

### 31. Check `EmployerProfilePublicApi.IsVerified`

> This is an in-process C# API consumed by BC-4, not an HTTP endpoint. Test via integration.

```csharp
var api = serviceProvider.GetRequiredService<IEmployerProfilePublicApi>();
var isVerified = await api.IsVerifiedAsync(employerUserId);
Assert.True(isVerified);
```
**Verifies:** US-3.1.2-02 / Invariant #5 — Only `Verified` profiles answer `true`. BC-4 enforces "must be verified to publish" gate.

### 32. Check `GetSummary`

```csharp
var summary = await api.GetSummaryAsync(employerUserId);
Assert.Equal(employerProfileId, summary.EmployerProfileId);
Assert.Equal("Verified", summary.Status);
Assert.True(summary.IsVerified);
```
**Verifies:** §9.3 — Public API contract for cross-module consumption.

## Story → Endpoint Mapping

| Story | Description | Endpoints | Phase |
|---|---|---|---|
| US-3.1.2-01 | Employer Registration | `POST /register`, `PUT /level2` | 1–2 |
| US-3.1.2-02 | Verification Workflow | `POST /verification/request`, `POST /verification/resubmit`, `POST .../approve`, `POST .../reject`, `GET .../verification` | 3 |
| US-3.1.2-03 | Profile Management | `GET /me`, `PUT /me`, `POST /logo`, `POST /images`, `DELETE /images/{id}`, `POST /documents`, `DELETE /documents/{id}`, `GET /{id}/public` | 4 |
| US-3.1.2-04 | Dashboard & Shortlists | `GET /dashboard`, `GET /dashboard/postings`, `GET /dashboard/matched-candidates`, `POST /shortlists`, `GET /shortlists`, `GET /shortlists/{id}`, `PUT /shortlists/{id}`, `DELETE /shortlists/{id}`, `POST /shortlists/{id}/candidates`, `DELETE /shortlists/{id}/candidates/{memberId}` | 5–6 |

## Key Invariants

| Transition | Must succeed | Must fail |
|---|---|---|
| `PendingActivation → PendingVerification` | Activate via `UserAccountActivated` consumer | `PUT level2` (blocked), `POST verification/request` (blocked) |
| `PendingVerification → Verified` | Auto-pass via `EmployerVerifiedByGovernment` consumer | Direct approve/reject (wrong state) |
| `PendingVerification → PendingManualVerification` | Auto-fail via `EmployerVerificationFailedByGovernment` consumer | Direct approve without status |
| `PendingManualVerification → Verified` | `POST approve` with `users:manage` permission | `POST approve` without admin claim |
| `PendingManualVerification → Rejected` | `POST reject` with valid reason | `POST reject` without reason |
| `Rejected → PendingManualVerification` | `POST resubmit` after profile edit | `POST resubmit` without edits (`E-VERIFY-NO-CHANGES`) |
| Any → `Suspended` | Via `UserAccountSuspended` consumer | `PUT me` (blocked) |
| Any → `Deactivated` | Via `AccountDeactivated` consumer | Any mutation (blocked) |
| `Suspended → prior` | Via `UserAccountReinstated` consumer | — |

## Running Automated Tests

```bash
# All unit tests (fast, no Docker)
dotnet test tests/Modules/EmployerProfiles/Nexhire.Modules.EmployerProfiles.Tests.Unit \
  --filter "FullyQualifiedName~ValueObjectsTests|FullyQualifiedName~DomainServicesTests|FullyQualifiedName~AggregateTests|FullyQualifiedName~ShortlistTests|FullyQualifiedName~RegisterEmployerTests|FullyQualifiedName~UpdateEmployerProfileTests|FullyQualifiedName~MediaUploadTests" \
  -v n

# Persistence + consumer integration tests (requires Docker)
dotnet test tests/Modules/EmployerProfiles/Nexhire.Modules.EmployerProfiles.Tests.Unit \
  --filter "FullyQualifiedName~PersistenceTests" -v n
```
