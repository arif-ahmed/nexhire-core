---
story_id: "US-3.2.4-03"
title: "View and manage job applications"
section_id: "3.2.4"
related_requirements: ["FR-67"]
related_stories: ["US-3.2.3-02-apply-to-jobs", "US-3.2.4-01-view-manage-job-status", "US-3.3.3-01-see-ranked-candidate-recommendations", "US-3.3.3-06-save-promising-candidates-to-talent-pool"]
role: "Employer Recruiter"
status: ready
priority: must
owning_bc: "BC-14"
collaborating_bcs: ["BC-2", "BC-3", "BC-5", "BC-8", "BC-10", "BC-11", "BC-12"]
saga: "ApplicationSubmissionSaga, ModerationActionSaga"
tags:
  - story
  - bc/applications
  - topic/recruiter-workflow
---

# US-3.2.4-03 — View and manage job applications

## Story
As an **Employer Recruiter**, I want to view all applications received for the postings I manage, advance candidates through hiring stages, capture internal notes and decisions, and extend offers, so that the team can run a coordinated, auditable hiring pipeline against one source of truth per application.

## Bounded-Context Position
- **Owns:** BC-14 — manages the `Application` aggregate state machine, `ApplicationStage` history, `EmployerDecision`, `ApplicationNote`, and `Offer`.
- **Reads from:**
  - BC-2 (`JobSeekerProfile`) — for live profile drill-down (where visibility allows; otherwise candidate snapshot only).
  - BC-5 (`JobPosting`) — to scope applications to postings the recruiter has access to.
  - BC-3 (`EmployerProfile`, `Role`) — for recruiter access control and team-wide views.
  - BC-8 (`MatchScore`) — for fit insights on each application.
- **Publishes:** `ApplicationStageChanged`, `EmployerDecisionRecorded`, `OfferExtended`, `OfferAccepted`, `OfferRescinded`, `ApplicationRejected`.
- **Sagas:** participates in `ApplicationSubmissionSaga` (consumer side) and triggers stage-driven sub-flows that fan out to BC-10 (candidate notifications) and BC-12 (analytics).

## Acceptance Criteria

**AC-01 — List applications for a posting**
- Given I have recruiter access to a posting with applications
- When I open its **Applications** tab
- Then I see a paginated list with columns: applicant name, applied_at, current stage, match score (BC-8), resume preview link, time-in-stage; default sort `applied_at desc`; default filter excludes `withdrawn` and `rejected`

**AC-02 — Cross-posting view**
- Given I have access to multiple postings under one employer
- When I open **All Applications**
- Then the same list is scoped to all postings I have permission to view, with a posting-name column added

**AC-03 — Application detail**
- Given I open an application
- When the detail loads
- Then I see the candidate snapshot (frozen at apply-time), live profile link (if visibility permits), resume + attachments, match-score breakdown, full stage history, internal notes, decisions, offer state

**AC-04 — Stage transition (single)**
- Given an application is in stage `submitted`
- When I select a new stage from the allowed transitions menu
- Then the change is persisted, an `ApplicationStage` history row is appended, `ApplicationStageChanged` is published, and the candidate is notified per their channel preferences within 60 s

**AC-05 — Allowed transitions are constrained**
- Given an application is in any stage
- When I open the stage menu
- Then only valid forward / sideways transitions are offered (e.g., from `submitted` → `under_review`, `rejected`); terminal stages (`hired`, `rejected`, `withdrawn`, `expired`) are not transitionable from the UI; reopen requires admin override

**AC-06 — Bulk stage transition**
- Given I have selected ≥2 applications via checkboxes
- When I apply a bulk stage change
- Then each application transitions independently; failures are reported per row; one toast summarizes "N changed, M failed (see details)"

**AC-07 — Decision capture**
- Given I am moving an application to `rejected` or beyond
- When the stage menu requires a reason
- Then I must select a reason code (lookup) and may add a free-text comment ≤ 1 000 chars; an `EmployerDecision` is recorded

**AC-08 — Internal notes**
- Given I am viewing an application
- When I add a note (≤ 5 000 chars, markdown subset)
- Then the note is persisted with author, timestamp, and `is_internal = true`; not visible to the candidate; visible to all recruiters with access to the posting

**AC-09 — Filtering & sorting**
- Given the application list
- When I apply filters (stage, applied_at range, match-score min, posting, recruiter assignee, has-notes) or click any sortable column header
- Then results refresh client-side (≤ 500 ms p95); URL reflects the filter set so it can be shared / bookmarked

**AC-10 — Search by candidate**
- Given the application list
- When I type a name, email, or skill in the search box
- Then results match against candidate snapshot (and live profile, where indexed)

**AC-11 — Saved views**
- Given I have a filter set I use repeatedly
- When I click **Save view** and name it
- Then the view is saved to my account and appears in a sidebar; views are private by default with an option to share with team

**AC-12 — Notifications on new applications**
- Given a new application is submitted to a posting I am subscribed to
- When the system processes `ApplicationSubmitted`
- Then I receive a notification (email + in-app per my channel preferences); subscription is per-recruiter and configurable per posting (default: opt-in for the posting owner, opt-out for other recruiters)

**AC-13 — Extend an offer**
- Given an application is in `offered`-eligible state (typically `interview` or `shortlisted`)
- When I click **Extend offer** and fill in terms (position, salary, currency, start_date, expiry_at, optional benefits text)
- Then an `Offer` aggregate is created in `extended`, the application stage moves to `offered`, candidate is notified, and a 7-day default `expiry_at` is applied if I leave it blank

**AC-14 — Rescind / amend offer**
- Given an offer is in `extended`
- When I click **Rescind**
- Then the offer transitions to `rescinded`, the application reverts to `interview`; **Amend** instead creates a new `Offer` superseding the previous and notifies the candidate of the change

**AC-15 — Concurrency on stage changes**
- Given two recruiters open the same application
- When both attempt different stage transitions simultaneously
- Then the second-to-commit receives `409 E-APP-STALE` with the latest state and is asked to retry; the first commit wins

**AC-16 — Permission scoping**
- Given I am a recruiter with access to postings A and B but not C
- When I attempt to view or mutate an application against posting C
- Then I receive `403 E-APP-FORBIDDEN`; the moderation log captures the attempt

**AC-17 — Candidate-side visibility of stage**
- Given an application stage changes
- When the candidate-facing flag for that stage is `true` (e.g., `shortlisted`, `interview`, `offered`, `hired`, `rejected`)
- Then the candidate's **My Applications** view updates and they receive a notification; stages flagged `internal_only` (e.g., a recruiter-internal sub-stage) do not notify

**AC-18 — Audit trail**
- Given any mutation on an application
- When the action commits
- Then BC-11 records an `AdminAuditEntry` with actor, action, before/after stage, and (for notes) the note ID; PII is hashed in the audit table per retention policy

## Data Model Contribution

```
EmployerDecision (BC-14)
  id, application_id, decision (advance | reject | hold | offer | rescind_offer),
  reason_code, recruiter_id, decided_at, comment (text, nullable)

ApplicationNote
  id, application_id, author_id, body (text, ≤5000 chars),
  is_internal (bool default true), created_at, edited_at

Offer
  id, application_id,
  terms (jsonb: position_title, salary, currency, start_date, expiry_at,
         benefits_text, custom_fields),
  status (extended | accepted | declined | rescinded | expired),
  extended_by_recruiter_id,
  extended_at, responded_at, responded_by_seeker_id

SavedRecruiterView
  id, recruiter_id, name, scope (employer | posting), filter_set (jsonb),
  is_shared (bool), created_at, updated_at

ApplicationStageDefinition (config, employer-scoped or platform-default)
  code, label_en, label_ar, ordinal, is_terminal, is_candidate_visible,
  requires_reason (bool)
```

Indexes: `(employer_id, status, applied_at desc)`, `(application_id, decided_at)` for decision history, GIN on `filter_set` for saved-view lookups.

## API Contract (REST)

```
GET    /api/v1/jobs/{posting_id}/applications
  Query: ?stage=&applied_after=&applied_before=&min_score=&assignee=
         &q=&page=&page_size=&sort=applied_at:desc
  200 → { "items": [...], "total": N, "facets": {...} }

GET    /api/v1/employers/{employer_id}/applications     # cross-posting
  Same query params as above; results scoped to the recruiter's permitted postings.

GET    /api/v1/applications/{id}
  200 → full detail incl. snapshot, live profile (if permitted), stage history,
        notes, decisions, offer

PATCH  /api/v1/applications/{id}/stage
  Body: { "to_stage": "shortlisted",
          "reason_code": "string|null",
          "comment": "string|null",
          "expected_current_stage": "submitted" }   # optimistic concurrency
  200 / 409 E-APP-STALE

POST   /api/v1/applications/bulk/stage
  Body: { "ids": [...], "to_stage": "rejected", "reason_code": "..." }
  207 multi-status → per-row results

POST   /api/v1/applications/{id}/notes
DELETE /api/v1/applications/{id}/notes/{note_id}
PATCH  /api/v1/applications/{id}/notes/{note_id}

POST   /api/v1/applications/{id}/decisions
GET    /api/v1/applications/{id}/decisions

POST   /api/v1/applications/{id}/offers
  Body: { "terms": { ...position, salary, start_date, expiry_at } }
  201 → { "offer_id", "status": "extended" }

POST   /api/v1/applications/{id}/offers/{offer_id}/rescind
POST   /api/v1/applications/{id}/offers/{offer_id}/amend       # supersedes

GET    /api/v1/recruiters/{id}/views
POST   /api/v1/recruiters/{id}/views
PATCH  /api/v1/recruiters/{id}/views/{view_id}
DELETE /api/v1/recruiters/{id}/views/{view_id}
```

All endpoints require Bearer JWT + recruiter scope. Permission middleware checks the requesting account against the posting's employer team and the role permissions in BC-3.

## Sequence Flow — Stage transition triggering candidate notification

1. Recruiter → `PATCH /applications/{id}/stage` with `expected_current_stage`.
2. **BC-14** loads the application, compares `status` against `expected_current_stage`. Mismatch → `409 E-APP-STALE`.
3. **BC-14** validates the transition is allowed by `ApplicationStageDefinition`; reason required if configured.
4. **BC-14** persists: updates `Application.status`, appends `ApplicationStage` row, records `EmployerDecision` if applicable.
5. **BC-14** publishes `ApplicationStageChanged { application_id, from, to, recruiter_id, candidate_visible }`.
6. **BC-10** consumes: if `candidate_visible == true`, dispatch in-app + email + (optionally) SMS to the candidate using their channel preferences and locale; otherwise no candidate notification, only employer-team in-app pings.
7. **BC-12** consumes: increments stage-transition counters; updates time-in-stage metrics for the previous stage.
8. **BC-3** projects to the recruiter dashboard read model.
9. **BC-11** records audit entry with actor, before/after, decision_id (if any).

## Validation Rules

- Recruiter must have `applications:read` (list/detail) and `applications:write` (mutations) for the posting's employer.
- Allowed stage transitions are configured per employer (default: linear with skip-to-reject at any point); cycles forbidden.
- Terminal stages cannot be transitioned away from (admin override path only — separate story).
- Reason code required when transitioning to `rejected`, `withdrawn` (employer-initiated), `interview` skip-back.
- Notes ≤ 5 000 chars; sanitized markdown.
- Offer `salary > 0`, `start_date >= today`, `expiry_at <= start_date`, currency from ISO-4217.
- Saved-view filter sets ≤ 8 KB serialized.

## Edge Cases & Error Handling

- **Candidate deactivates account** → all their non-terminal applications transition to `withdrawn` (saga); recruiters see a system note on the affected applications.
- **Posting archived mid-pipeline** → existing applications remain mutable for 30 days post-archive (configurable) so recruiters can finish in-flight reviews; new applications blocked.
- **Recruiter loses access** (removed from team) → in-flight saved views become inaccessible; their notes remain visible to remaining team members; their pending decisions are not auto-reverted.
- **Bulk operation partial failure** → 207 multi-status; UI shows per-row outcome; failures grouped by error code.
- **Match-score recompute** during pipeline → score updates do NOT alter `match_score_at_apply`; recruiters see both values clearly labeled (apply-time vs current).
- **Offer expires** without candidate response → automated transition to `offered → expired`; application reverts to `interview` with a system note; `OfferExpired` published.
- **Offer accepted then candidate withdraws** → application transitions to `withdrawn`, `OfferRescinded` is NOT emitted (candidate-initiated); employer dashboard surfaces the candidate-withdrawal reason.
- **Concurrent edits on notes** → notes are append-only; edits versioned with `edited_at`; UI shows latest with a "show history" affordance.

## Test Scenarios

1. **List applications:** posting with 50 applications → first page (page_size=25) returns within 500 ms p95; correct count and facets.
2. **Forward transition:** `submitted` → `under_review` → `shortlisted` chain; each emits `ApplicationStageChanged`; candidate gets one notification per visible stage.
3. **Skip to reject:** transition `submitted` → `rejected` with reason → candidate notified, time-in-stage recorded for `submitted`.
4. **Concurrency:** two recruiters change stage simultaneously → first wins, second gets 409 with current state.
5. **Bulk reject 10 candidates:** one of them already withdrawn → 9 succeed, 1 returns `E-APP-INVALID-TRANSITION`; UI summarizes outcomes.
6. **Offer flow:** extend offer → candidate accepts → application moves to `offered` → `hired`; analytics records time-to-hire.
7. **Offer rescind:** offer rescinded within expiry window → candidate notified, application reverts to `interview`.
8. **Permission boundary:** recruiter on team A attempts to read posting under team B → 403; audit log records attempt.
9. **Saved view:** create filter set, save view, share with team → teammate sees it under "Shared views" sidebar.
10. **Audit completeness:** every mutation in scenarios 1–9 produces a corresponding BC-11 audit entry.

## Assumptions

- Stage definitions ship with a default platform set; per-employer customization (custom stages, reordering) is a follow-on enhancement.
- Recruiter roles (Owner, Admin, Recruiter, Viewer) are owned by BC-3; this story consumes them for permission checks.
- Notifications respect the candidate's channel preferences and quiet-hours / digest settings configured under [[US-3.6.1-02-configure-email-notification-preferences|US-3.6.1-02]] and [[US-3.6.2-05-configure-in-app-notification-preferences|US-3.6.2-05]].
- Interview scheduling, video calls, and assessment integrations are explicitly out of scope here (future BC-14 stories or a sibling BC).
- Match-score breakdown displayed alongside applications uses [[US-3.3.3-05-provide-candidate-insights-and-fit-analysis|US-3.3.3-05]] as the source.
- Offer terms are stored as structured JSON with a small set of common fields plus a free-text `benefits_text`; richer offer-letter generation is deferred.

## Source Requirements
- [[3_2_4_Job_Status_Tracking|3.2.4]] — FR-67

## Related Stories
- [[US-3.2.3-02-apply-to-jobs|US-3.2.3-02 Apply to jobs]] (the seeker-side counterpart)
- [[US-3.2.4-01-view-manage-job-status|US-3.2.4-01 View / manage job status]] (sibling: posting status, not application status)
- [[US-3.3.3-01-see-ranked-candidate-recommendations|US-3.3.3-01 See ranked candidate recommendations]]
- [[US-3.3.3-06-save-promising-candidates-to-talent-pool|US-3.3.3-06 Save to talent pool]]
- [[US-3.6.2-02-receive-real-time-notifications|US-3.6.2-02 Receive real-time notifications]] (recruiter side)
