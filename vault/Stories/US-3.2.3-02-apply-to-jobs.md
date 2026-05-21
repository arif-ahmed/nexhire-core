---
story_id: "US-3.2.3-02"
title: "Apply to jobs and initiate application interaction"
section_id: "3.2.3"
related_requirements: ["FR-66"]
related_stories: ["US-3.2.3-01-bookmark-and-save-jobs", "US-3.2.4-03-view-manage-applications", "US-3.1.1-03-complete-level-2-profile", "US-3.1.1-04-upload-and-parse-resume"]
role: "Job Seeker"
status: ready
priority: must
owning_bc: "BC-14"
collaborating_bcs: ["BC-2", "BC-5", "BC-3", "BC-8", "BC-10", "BC-12"]
saga: "ApplicationSubmissionSaga"
tags:
  - story
  - bc/applications
  - topic/lifecycle
---

# US-3.2.3-02 — Apply to jobs and initiate application interaction

## Story
As a **Job Seeker**, I want to apply to a job from the job listing or my Interested List, so that I can formally express interest, capture an immutable snapshot of my profile at apply-time, and begin the hiring interaction process tracked under one application identifier.

## Bounded-Context Position
- **Owns:** BC-14 — creates the `Application` aggregate (root) and the initial `ApplicationStage` entry.
- **Reads from:**
  - BC-5 (`JobPosting`) — to validate that the posting is in `active` status and not past its deadline.
  - BC-2 (`JobSeekerProfile`) — to take an immutable candidate snapshot and verify Level 2 completeness.
  - BC-8 (`MatchScore`) — to record the match-score-at-apply (informational, not blocking).
- **Publishes:** `ApplicationSubmitted` (consumed by BC-3 dashboards, BC-10 notifications, BC-12 analytics).
- **Saga:** initiator of `ApplicationSubmissionSaga` (see Domains_and_Bounded_Contexts §5.7).

## Acceptance Criteria

**AC-01 — Apply from job detail page (happy path)**
- Given I am viewing a job detail page for a posting in `active` status
- When I click **Apply Now**
- Then the application form opens pre-populated with my Level 1 + Level 2 profile fields, my latest resume, and (if shown) my computed match score for this posting

**AC-02 — Apply from Interested List**
- Given a posting is in my Interested List
- When I click **Apply** on the list item
- Then the same application flow as AC-01 is initiated

**AC-03 — Profile pre-fill**
- Given I have a Level 2-complete profile
- When the application form loads
- Then name, email, phone, current location, and the most recent resume are pre-filled and editable; edits affect only this application's snapshot, not my master profile

**AC-04 — Resume swap**
- Given I have multiple resumes uploaded
- When I open the form
- Then I can pick which resume to attach (default: latest); I can also upload a new resume in-line

**AC-05 — Optional cover letter**
- Given I am submitting an application
- When I add a cover letter (≤4 000 characters, plain text or markdown subset)
- Then the cover letter is stored against this application only

**AC-06 — Submit application**
- Given the form passes client-side validation
- When I press **Submit**
- Then within 3 seconds (p95) the system creates an `Application` in status `submitted`, returns the application ID, and shows a confirmation screen with: application ID, expected first-response window (configurable per employer; default 7 days), and a **View status** link

**AC-07 — Duplicate prevention**
- Given I have a non-terminal application (`submitted`, `under_review`, `shortlisted`, `interview`, `offered`) for this posting
- When I try to apply again
- Then the system returns `E-APP-DUPLICATE` and surfaces the existing application's status with a **Continue tracking** link instead of creating a new application

**AC-08 — Re-apply after withdrawal / rejection**
- Given my prior application for this posting is in a terminal state (`withdrawn`, `rejected`, `expired`) AND the posting is still `active`
- When I apply again
- Then a new `Application` is created and linked to the previous one via `replaces_application_id` so the employer sees the history

**AC-09 — Posting no longer accepting applications**
- Given the posting has been `expired`, `archived`, `paused`, or its deadline has passed at the moment of submission
- When I press **Submit**
- Then submission is rejected with `E-APP-POSTING-CLOSED` and the form offers similar active postings (recommendations from BC-8)

**AC-10 — Profile not eligible to apply**
- Given my profile is below Level 2 completeness
- When I try to submit
- Then submission is blocked with `E-APP-PROFILE-INCOMPLETE` and an inline link to complete the missing profile fields ([[US-3.1.1-03-complete-level-2-profile|US-3.1.1-03]])

**AC-11 — Confirmation, audit, and notifications**
- Given an application is successfully submitted
- When the transaction commits
- Then `ApplicationSubmitted` is published; the seeker receives an in-app + email confirmation (per their channel preferences); the employer's dashboard increments the new-applicants count; the application becomes visible in **My Applications**

**AC-12 — Withdraw an application (terminal-from-seeker side)**
- Given my application is in any non-terminal state
- When I choose **Withdraw application** and select a reason
- Then the application transitions to `withdrawn`, `ApplicationWithdrawn` is published, and the employer is notified per their preferences

**AC-13 — Anonymous-mode application (privacy)**
- Given my profile visibility is set to `private` or `recruiters_only`
- When I apply
- Then the application is still created normally; visibility settings affect search/match exposure but not direct applications I initiate

**AC-14 — Network failure during submit**
- Given my connection drops mid-submission
- When the client retries with the same idempotency key (UUID generated at form open)
- Then the system creates **at most one** application; subsequent retries with the same key return the existing application ID (HTTP 200)

**AC-15 — Locale**
- Given my preferred locale is Arabic
- When I view confirmations and status copy
- Then UI strings render in Arabic (RTL); free-text fields (cover letter) are stored verbatim

## Data Model Contribution

```
Application (root, BC-14)
  id (UUID, PK)
  job_posting_id (FK → BC-5.JobPosting)
  job_seeker_id (FK → BC-2.JobSeeker)
  employer_id (FK → BC-3.EmployerProfile)        # denormalized for fast scoping
  status (enum: submitted | under_review | shortlisted | interview |
                offered | hired | rejected | withdrawn | expired)
  candidate_snapshot (jsonb, immutable)           # full profile at apply-time
  resume_document_id (FK → BC-2.Document)
  cover_letter (text, nullable, max 4000 chars)
  match_score_at_apply (numeric 0..1, nullable)   # snapshot from BC-8
  replaces_application_id (FK → Application, nullable)
  idempotency_key (UUID, unique)
  applied_at (timestamp)
  last_status_change_at (timestamp)
  withdrawn_at, hired_at, rejected_at (timestamp, nullable)

ApplicationStage (history, append-only)
  id, application_id, stage (enum same as Application.status),
  entered_at, entered_by_role (seeker | recruiter | system),
  entered_by_user_id (nullable),
  reason_code (nullable), comment (text, nullable)

WithdrawalReason (lookup)
  code, label_en, label_ar, is_seeker_facing (bool)

ApplicationAttachment (extension)
  id, application_id, kind (cover_letter | portfolio | other),
  document_id, uploaded_at
```

Indexes: `(job_posting_id, job_seeker_id)` for duplicate checks; `(job_seeker_id, status)` for seeker dashboards; `(employer_id, status, applied_at)` for recruiter dashboards.

## API Contract (REST)

```
POST   /api/v1/applications
  Headers:
    Idempotency-Key: <uuid>
  Body:
    {
      "job_posting_id": "uuid",
      "resume_document_id": "uuid",
      "cover_letter": "string|null",
      "snapshot_overrides": { ...editable fields... }
    }
  201 → { "application_id": "uuid", "status": "submitted",
          "match_score_at_apply": 0.78 }
  409 → E-APP-DUPLICATE  { "existing_application_id": "..." }
  410 → E-APP-POSTING-CLOSED
  422 → E-APP-PROFILE-INCOMPLETE  { "missing_fields": [...] }

GET    /api/v1/seekers/{id}/applications
  Query: ?status=&page=&page_size=&sort=applied_at:desc
  200 → { "items": [...], "total": N }

GET    /api/v1/applications/{id}             # seeker or owning employer
POST   /api/v1/applications/{id}/withdraw
  Body: { "reason_code": "string", "comment": "string|null" }
  200 → { "status": "withdrawn", "withdrawn_at": "..." }
```

All endpoints require Bearer JWT. Rate limits: submit 10/hr/seeker, withdraw 5/hr/seeker.

## Sequence Flow — `ApplicationSubmissionSaga`

1. Seeker → `POST /applications` with idempotency key.
2. **BC-14** validates idempotency: if key seen, return existing application (200 idempotent).
3. **BC-14** queries **BC-5**: posting status must be `active` and `deadline > now`.
4. **BC-14** queries **BC-2**: profile completeness ≥ Level 2; resume `document_id` belongs to seeker.
5. **BC-14** requests **BC-8**: snapshot match score for this `(seeker, posting)` pair (best-effort; absence is not blocking).
6. **BC-14** persists `Application` with `status = submitted`, freezes `candidate_snapshot`, records initial `ApplicationStage` row.
7. **BC-14** publishes `ApplicationSubmitted { application_id, job_posting_id, job_seeker_id, employer_id, applied_at, match_score_at_apply }`.
8. **BC-10** consumes the event → seeker confirmation (in-app + email), employer new-applicant notification (per recruiter preferences).
9. **BC-12** consumes the event → increments `posting.application_count`, time-to-first-application metric.
10. **BC-3** projects the event into recruiter dashboards (read model).
11. **BC-11** records audit entry with hashed candidate-snapshot fingerprint.

Compensation: if step 6 fails, no event is published; client retry with same key is idempotent. If step 7 fails post-commit, an outbox pattern guarantees eventual emission.

## Validation Rules

- `job_posting_id` must reference a posting in `active` status with `deadline > now()`.
- Seeker cannot apply to their own employer's postings (seeker may also be a recruiter — guarded via `ProfileLink`).
- `cover_letter` ≤ 4 000 characters; sanitized HTML / markdown subset.
- `resume_document_id` must belong to the authenticated seeker and be virus-scan-clean.
- One non-terminal application per `(seeker, posting)`; terminal states allow re-apply.
- `idempotency_key` is UUID v4, retained for 24 h.
- Snapshot is computed server-side from `JobSeeker` aggregate at the moment of step 6; client-side `snapshot_overrides` are merged into the snapshot, not applied to the master profile.

## Edge Cases & Error Handling

- **Posting closed mid-flow:** between form-load and submit, posting becomes `expired`. The submission is rejected with `E-APP-POSTING-CLOSED`; seeker is offered alternative active postings.
- **Resume deleted between selection and submit:** if `resume_document_id` is no longer accessible, return `E-APP-RESUME-MISSING`; seeker can re-upload inline.
- **Duplicate submit (same key, two clicks):** server returns the same `application_id` for both — exactly-once semantics from the seeker's view.
- **Re-apply after rejection:** `replaces_application_id` chain visible to employer; seeker re-application is its own first-class `Application`.
- **Account deactivation post-submission:** `AccountDeactivationCascade` transitions the application to `withdrawn` with `WithdrawalReason = AccountDeactivated`.
- **Posting archived after submit:** existing `Application` rows persist; recruiters keep read-only access; new applications blocked.
- **Concurrent withdraw + employer decision:** last-write-wins on `status`, but each transition records an `ApplicationStage` row; `ApplicationWithdrawn` and `ApplicationStageChanged` events both emit and consumers reconcile by `last_status_change_at`.

## Test Scenarios

1. **Happy path:** Level-2 profile + active posting → submission returns `submitted`, seeker sees confirmation, employer dashboard increments by one within 5 s.
2. **Duplicate prevention:** Two consecutive submits to same posting → first returns 201, second returns 409 with the same `application_id`.
3. **Idempotent retry:** Network failure simulated mid-submit; client retries with the same `Idempotency-Key` → exactly one row created.
4. **Posting closed at submit:** Posting expired between page load and submit → 410 with alternative recommendations.
5. **Profile incomplete:** Seeker with Level 1 only → 422 listing missing fields.
6. **Re-apply after rejection:** Prior application rejected → new `Application` created with `replaces_application_id` populated.
7. **Withdraw within 5 minutes:** Submit then withdraw → terminal state `withdrawn`; both events visible in audit log.
8. **Locale check:** Arabic UI; cover letter in Arabic round-trips through save/load without mojibake.
9. **Privacy mode:** Seeker with `private` visibility applies → application visible to that employer only; seeker does not appear in BC-3 candidate search.
10. **Account deactivation:** Seeker deactivates account → all active applications transition to `withdrawn` within 60 s (saga eventual consistency).

## Assumptions

- Application form attachments beyond resume + cover letter (portfolio, references) are out of scope for this story; the `ApplicationAttachment` table leaves room.
- Employer's first-response SLA (default 7 days) is a per-employer setting introduced in BC-3; absence falls back to a platform default.
- "Anonymous applications" (hiding the applicant's name from the recruiter) are NOT supported; visibility settings affect search/discovery, not direct applications.
- Re-application after rejection is allowed by default; per-employer cool-down policies are a future enhancement.
- Application data is retained for the duration of the posting + 24 months after final state, then anonymized for analytics.

## Source Requirements
- [[3_2_3_Job_Interaction_Process|3.2.3]] — FR-66

## Related Stories
- [[US-3.2.3-01-bookmark-and-save-jobs|US-3.2.3-01 Bookmark and save jobs]]
- [[US-3.2.4-03-view-manage-applications|US-3.2.4-03 View and manage applications]] (employer side)
- [[US-3.1.1-03-complete-level-2-profile|US-3.1.1-03 Complete Level 2 profile]] (gate)
- [[US-3.1.1-04-upload-and-parse-resume|US-3.1.1-04 Upload and parse resume]] (resume artifact)
- [[3_3_2_Job_Recommendation_Engine|3.3.2 Job Recommendation Engine]] (alt-posting suggestions on rejection)
