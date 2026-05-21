---
title: "Domain Event Catalog"
type: strategic-design
total_bcs: 12
generated: 2026-05-14
related:
  - "[[BC_Mapping]]"
  - "[[Context_Map]]"
tags:
  - strategic-design
  - events
  - ddd
---

# Domain Event Catalog

This catalog is the **shared vocabulary** between all bounded contexts. Every BC publishes events when something happens inside it, and subscribes to events from other BCs to react. Sagas (defined separately) compose these events into multi-step processes.

## Conventions

- **Events are facts in the past tense.** `JobPostingPublished`, not `PublishJobPosting` (that would be a command).
- **PascalCase**, named subject-first: `<Aggregate><WhatHappened>`.
- **Owned by exactly one BC** — only the owning BC can emit a given event. Other BCs subscribe.
- **Payload sketch** lists the salient fields, not a complete schema. Treat as the **published language** at the boundary; once published, the shape is a contract.
- **Choreography by default.** Most BC-to-BC interactions are event subscriptions. Sagas (orchestration) are reserved for flows that need explicit failure handling or compensation — see [[Saga_Catalog]].

### Pattern: events vs. commands

| | Direction | Tense | Recipients |
|---|---|---|---|
| Command | sender → one aggregate | imperative | one |
| Event | aggregate → world | past | many (any subscriber) |

This catalog only lists **events**. Commands are internal to each BC.

## Summary

| BC | Events emitted | Notable consumers |
|---|---|---|
| BC-1 IAM and UAM | 9 | Everyone (auth + lifecycle) |
| BC-2 Employer Profile | 6 | BC-4, BC-7, BC-9, BC-10 |
| BC-3 JobSeeker Profile | 8 | BC-7, BC-9, BC-10 |
| BC-4 Job Postings | 8 | BC-5, BC-6, BC-7, BC-8, BC-9, BC-10 |
| BC-5 Job Application | 6 | BC-2, BC-3, BC-9, BC-10 |
| BC-6 Search & Discovery | 3 | BC-10 |
| BC-7 Recommendation Engine | 5 | BC-2, BC-3, BC-9, BC-10 |
| BC-8 External Job Sync | 9 | BC-2, BC-3, BC-4, BC-10 |
| BC-9 Notification | 5 | BC-10 |
| BC-10 Reporting | 3 | (mostly internal) |
| BC-11 Administrators Configuration | 3 | BC-3, BC-4, BC-7, BC-8 |
| BC-12 Content Management | 5 | BC-3, BC-9 |
| **Total events** | **70** | |

---

## BC-1 · IAM and UAM

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `UserRegistered` | New account created (any role) | userId, role, email, createdAt | BC-2, BC-3, BC-9, BC-10, BC-12 |
| `UserAccountActivated` | OTP/email confirmation succeeded | userId, activatedAt | BC-2, BC-3, BC-9, BC-10 |
| `UserAccountSuspended` | Admin moderation suspends account | userId, reason, by, at | BC-2, BC-3, BC-4, BC-5, BC-9, BC-10 |
| `UserAccountReinstated` | Admin reinstates suspended account | userId, by, at | BC-2, BC-3, BC-9, BC-10 |
| `AccountDeactivated` | User self-deactivates | userId, deactivatedAt | BC-2, BC-3, BC-4, BC-5, BC-7, BC-9, BC-10 |
| `UserLoggedIn` | Successful login | userId, sessionId, channel, at | BC-10 |
| `UserLoginFailed` | Failed login (audit signal) | identifier, reason, at | BC-10 |
| `PasswordReset` | User completed password reset | userId, at | BC-9, BC-10 |
| `RoleAssigned` | Role/permission grant | userId, role, by, at | BC-10 |

### Consumes

| Event | Source | Reaction |
|---|---|---|
| `IdentityVerifiedByGovernment` | BC-8 | Mark identity-verified flag on user record |
| `IdentityVerificationFailed` | BC-8 | Record verification failure for audit |

---

## BC-2 · Employer Profile Management

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `EmployerRegistered` | Employer account/profile created | employerId, userId, companyName, at | BC-4, BC-9, BC-10 |
| `EmployerProfileUpdated` | Profile fields changed | employerId, changedFields, at | BC-7, BC-10 |
| `EmployerVerificationRequested` | Verification submitted | employerId, registryRef, at | BC-8 |
| `EmployerVerified` | Verification badge granted | employerId, verifiedAt, evidenceRef | BC-4, BC-9, BC-10 |
| `EmployerVerificationFailed` | Verification rejected | employerId, reason, at | BC-9, BC-10 |
| `CandidateSavedToTalentPool` | Employer shortlists a candidate | employerId, jobSeekerId, poolId, at | BC-3, BC-9, BC-10 |

### Consumes

| Event | Source | Reaction |
|---|---|---|
| `UserRegistered` (role=employer) | BC-1 | Create employer profile shell |
| `UserAccountActivated` | BC-1 | Allow profile completion |
| `EmployerVerifiedByGovernment` | BC-8 | Update verification status, emit `EmployerVerified` |
| `AccountDeactivated` / `UserAccountSuspended` | BC-1 | Disable profile, hide from search |
| `ApplicationSubmitted` | BC-5 | Surface in employer dashboard |

---

## BC-3 · JobSeeker Profile

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `JobSeekerRegistered` | Profile shell created | jobSeekerId, userId, at | BC-9, BC-10 |
| `ProfileLevel2Completed` | Extended profile filled | jobSeekerId, completenessScore, at | BC-7, BC-10 |
| `ResumeUploaded` | Resume file submitted | jobSeekerId, resumeId, mime, at | BC-10 |
| `ResumeParsed` | Skills/experience extracted from resume | jobSeekerId, resumeId, skills, education, experience, at | BC-7, BC-10 |
| `ProfileSkillsUpdated` | Seeker edits skills directly | jobSeekerId, addedSkills, removedSkills, at | BC-7, BC-10 |
| `ProfileVisibilityChanged` | Seeker toggles visibility/privacy | jobSeekerId, visibility, at | BC-7, BC-10 |
| `SupplementaryDocumentUploaded` | Supporting doc added | jobSeekerId, docId, type, at | BC-10 |
| `ProfileCompletenessChanged` | Completeness score recomputed | jobSeekerId, score, at | BC-9 (nudges), BC-10 |

### Consumes

| Event | Source | Reaction |
|---|---|---|
| `UserRegistered` (role=jobseeker) | BC-1 | Create profile shell |
| `UserAccountActivated` | BC-1 | Unlock profile features |
| `IdentityVerifiedByGovernment` | BC-8 | Set identity-verified flag |
| `EducationVerified` | BC-8 | Mark education credential as verified |
| `AccountDeactivated` / `UserAccountSuspended` | BC-1 | Hide profile, halt recommendations |
| `TaxonomyUpdated` | BC-11 | Re-validate stored skill tags |

---

## BC-4 · Job Postings

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `JobPostingCreated` | Posting drafted | postingId, employerId, draft, at | BC-10 |
| `JobPostingPublished` | Posting goes live | postingId, employerId, title, requirements, at | BC-5, BC-6, BC-7, BC-8, BC-9, BC-10 |
| `JobPostingUpdated` | Posting edited after publish | postingId, changedFields, at | BC-6, BC-7, BC-8, BC-10 |
| `JobPostingExpired` | Reached end date | postingId, expiredAt | BC-5, BC-6, BC-9, BC-10 |
| `JobPostingClosed` | Employer closes posting | postingId, reason, at | BC-5, BC-6, BC-9, BC-10 |
| `JobPostingSuspended` | Admin moderation removes posting | postingId, by, reason, at | BC-5, BC-6, BC-9, BC-10 |
| `JobPostingReinstated` | Admin restores posting | postingId, by, at | BC-6, BC-9, BC-10 |
| `JobPostingStatusChanged` | Generic state transition signal | postingId, fromStatus, toStatus, at | BC-10 |

### Consumes

| Event | Source | Reaction |
|---|---|---|
| `EmployerVerified` | BC-2 | Allow this employer to publish |
| `EmployerVerificationFailed` / `EmployerSuspended` | BC-2 | Block publishing, suspend active postings |
| `ExternalJobIngested` | BC-8 | Create or update mirrored posting |
| `TaxonomyUpdated` | BC-11 | Re-validate posting skill/occupation tags |
| `AccountDeactivated` (employer) | BC-1 | Auto-close active postings |

---

## BC-5 · Job Application

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `JobBookmarked` | Seeker bookmarks a posting | jobSeekerId, postingId, at | BC-7 (preference signal), BC-10 |
| `JobUnbookmarked` | Seeker removes bookmark | jobSeekerId, postingId, at | BC-10 |
| `ApplicationSubmitted` | Seeker applies | applicationId, jobSeekerId, postingId, snapshot, at | BC-2, BC-3, BC-7, BC-9, BC-10 |
| `ApplicationViewed` | Employer opens application | applicationId, employerId, at | BC-9, BC-10 |
| `ApplicationStatusChanged` | Status moves (shortlisted/rejected/hired/withdrawn) | applicationId, fromStatus, toStatus, by, at | BC-9, BC-10 |
| `ApplicationWithdrawn` | Seeker withdraws application | applicationId, jobSeekerId, at | BC-2, BC-9, BC-10 |

### Consumes

| Event | Source | Reaction |
|---|---|---|
| `JobPostingExpired` / `JobPostingClosed` / `JobPostingSuspended` | BC-4 | Auto-close pending applications, notify seeker |
| `AccountDeactivated` | BC-1 | Withdraw seeker's open applications |

---

## BC-6 · Search & Discovery

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `SearchPerformed` | User runs a search | userId, query, filters, resultCount, at | BC-7 (signal), BC-10 |
| `SavedSearchCreated` | Seeker saves a query | savedSearchId, userId, criteria, at | BC-9, BC-10 |
| `SavedSearchMatchFound` | Saved search hits a new posting | savedSearchId, postingIds, at | BC-9 (digest), BC-10 |

### Consumes

| Event | Source | Reaction |
|---|---|---|
| `JobPostingPublished` / `JobPostingUpdated` | BC-4 | Add/update document in search index |
| `JobPostingExpired` / `JobPostingClosed` / `JobPostingSuspended` | BC-4 | Remove from search index |
| `TaxonomyUpdated` | BC-11 | Update synonym/facet definitions |

---

## BC-7 · Recommendation Engine

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `MatchComputed` | A seeker-posting match is scored | jobSeekerId, postingId, score, at | BC-2 (employer view), BC-3 (seeker view), BC-10 |
| `RecommendationGenerated` | Personalized job list ready for seeker | jobSeekerId, postingIds, computedAt | BC-9 (digest), BC-10 |
| `CandidateRecommendationGenerated` | Ranked candidate list ready for employer | employerId, postingId, jobSeekerIds, at | BC-2, BC-9, BC-10 |
| `EmbeddingsRefreshed` | Nightly batch recompute completed | scope, vectorCount, at | BC-10 |
| `MatchThresholdChanged` | Threshold/parameters reconfigured | scope, oldValue, newValue, by, at | BC-10 |

### Consumes

| Event | Source | Reaction |
|---|---|---|
| `ResumeParsed` | BC-3 | Refresh seeker embeddings, recompute matches |
| `ProfileSkillsUpdated` / `ProfileLevel2Completed` | BC-3 | Recompute seeker embeddings |
| `JobPostingPublished` / `JobPostingUpdated` | BC-4 | Compute embeddings, fan out matches |
| `JobBookmarked` / `ApplicationSubmitted` | BC-5 | Use as preference signal for re-ranking |
| `TaxonomyUpdated` | BC-11 | Rebuild affected embeddings |
| `EmployerProfileUpdated` (qualification thresholds) | BC-2 | Re-rank candidates for that employer |

---

## BC-8 · External Job Synchronization

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `ExternalJobIngested` | Foreign job pulled in & translated | externalRef, partnerId, normalizedPosting, at | BC-4, BC-10 |
| `ExternalJobUpdated` | Partner sent an update | externalRef, partnerId, changedFields, at | BC-4, BC-10 |
| `ExternalJobRetracted` | Partner removed a job | externalRef, partnerId, at | BC-4, BC-10 |
| `IdentityVerifiedByGovernment` | Identity check returned positive | userId, registry, at | BC-1, BC-3, BC-10 |
| `IdentityVerificationFailed` | Identity check returned negative | userId, registry, reason, at | BC-1, BC-9, BC-10 |
| `EducationVerified` | Education credential confirmed | jobSeekerId, credentialRef, at | BC-3, BC-10 |
| `EmployerVerifiedByGovernment` | Employer registry returned positive | employerId, registry, at | BC-2, BC-10 |
| `SyncErrorDetected` | Integration error during sync | partnerId, errorClass, payloadRef, at | BC-9 (ops alert), BC-10 |
| `SyncReconciled` | Manual or automatic reconciliation finished | partnerId, recordsAffected, at | BC-10 |

### Consumes

| Event | Source | Reaction |
|---|---|---|
| `JobPostingPublished` / `JobPostingUpdated` / `JobPostingClosed` | BC-4 | Push to external partners (export flow) |
| `EmployerVerificationRequested` | BC-2 | Call government registry, emit verification result |
| `TaxonomyUpdated` | BC-11 | Update outbound mapping rules |

---

## BC-9 · Notification

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `NotificationDispatched` | Notification handed to channel adapter | notificationId, channel, recipientId, templateId, at | BC-10 |
| `NotificationDelivered` | Channel confirms delivery (email open, SMS receipt) | notificationId, deliveredAt | BC-10 |
| `NotificationFailed` | Delivery failed | notificationId, channel, reason, at | BC-10 |
| `NotificationPreferencesUpdated` | User changed prefs | userId, channel, prefs, at | BC-10 |
| `DigestScheduled` / `DigestSent` | Daily/weekly digest assembled and sent | userId, contents, at | BC-10 |

### Consumes

A long list — Notification subscribes broadly:

| Event | Source | Action |
|---|---|---|
| `UserRegistered`, `UserAccountActivated`, `PasswordReset` | BC-1 | Send welcome / activation / reset emails |
| `EmployerVerified` / `EmployerVerificationFailed` | BC-2 | Notify employer |
| `JobPostingExpired` / `JobPostingClosed` / `JobPostingSuspended` | BC-4 | Notify employer + applicants |
| `ApplicationSubmitted` | BC-5 | Confirm to seeker, alert employer |
| `ApplicationStatusChanged` | BC-5 | Notify seeker |
| `RecommendationGenerated` | BC-7 | Build weekly digest |
| `CandidateRecommendationGenerated` | BC-7 | Notify employer |
| `SavedSearchMatchFound` | BC-6 | Build saved-search alert |
| `SyncErrorDetected` | BC-8 | Page ops |
| `ArticlePublished` | BC-12 | News digest opt-ins |

---

## BC-10 · Reporting

Reporting is a Conformist consumer of essentially every domain event in the platform — that's what makes it the read-model BC. It emits very few events of its own.

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `ReportGenerated` | A report run finishes | reportId, definitionId, byUser, at | BC-9 (notify requester) |
| `ScheduledReportRun` | Cron-triggered report execution | scheduleId, reportId, at | BC-9 |
| `ActivityRetentionApplied` | Retention policy purged old data | scope, recordsPurged, at | BC-10 (audit), BC-9 (admin notice) |

### Consumes

Effectively *all* events from all BCs. Treat the entire catalog above as Reporting's input feed.

---

## BC-11 · Administrators Configuration

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `TaxonomyUpdated` | Skill/occupation/industry vocab changed | taxonomyId, changeSummary, version, at | BC-3, BC-4, BC-6, BC-7, BC-8, BC-10 |
| `TaxonomyTermAdded` | New term introduced | taxonomyId, termId, label, at | BC-3, BC-4, BC-6, BC-7 |
| `TaxonomyTermDeprecated` | Term marked as deprecated | taxonomyId, termId, replacedBy, at | BC-3, BC-4, BC-6, BC-7 |

### Consumes

Nothing required for the course's scope.

---

## BC-12 · Content Management

### Emits

| Event | Trigger | Payload sketch | Primary consumers |
|---|---|---|---|
| `ArticlePublished` | News article goes live | articleId, title, categories, at | BC-3 (dashboard), BC-9 |
| `ArticleScheduled` | Scheduled publication queued | articleId, publishAt | BC-9 (when fires) |
| `ArticleArchived` | Article unpublished or archived | articleId, at | BC-3, BC-9 |
| `FAQPublished` | FAQ entry added | faqId, topic, at | BC-3, BC-10 |
| `HelpFeedbackReceived` | User left feedback on help content | helpId, rating, at | BC-10 |

### Consumes

| Event | Source | Reaction |
|---|---|---|
| `UserRegistered` | BC-1 | Personalize content delivery (default categories by role) |

---

## How events compose into sagas (preview)

Each named saga from [[BC_Mapping]] threads a sequence of these events together. Sketch:

| Saga | Trigger event | Composes events from |
|---|---|---|
| ResumeIntakeSaga | `ResumeUploaded` (BC-3) | `ResumeParsed` → `EmbeddingsRefreshed` → `RecommendationGenerated` |
| EmployerVerificationSaga | `EmployerVerificationRequested` (BC-2) | `EmployerVerifiedByGovernment` → `EmployerVerified` → `NotificationDispatched` |
| ApplicationSubmissionSaga | `ApplicationSubmitted` (BC-5) | snapshot read of BC-2/3/4 → `NotificationDispatched` (×2) |
| SavedSearchAlertSaga | `JobPostingPublished` (BC-4) | `SavedSearchMatchFound` (BC-6) → `RecommendationGenerated` (BC-7) → `DigestSent` (BC-9) |
| ModerationActionSaga | admin command in BC-1 or BC-4 | `UserAccountSuspended` / `JobPostingSuspended` → cascade `ApplicationWithdrawn`, `JobPostingClosed`, `NotificationDispatched` |
| PartnerJobIngestSaga | `ExternalJobIngested` (BC-8) | `JobPostingCreated`/`Updated` (BC-4) → `EmbeddingsRefreshed` (BC-7) |
| AccountDeactivationCascade | `AccountDeactivated` (BC-1) | profile hide events (BC-2/3) → `JobPostingClosed` / `ApplicationWithdrawn` → `NotificationDispatched` |

Full saga definitions (steps, compensations, orchestration vs. choreography choice) belong in `Saga_Catalog.md` — propose to write next.

## Open design questions for class

1. **Granular vs. unified status events.** `ApplicationStatusChanged` carries a `fromStatus`/`toStatus` payload; the alternative is one event per transition (`ApplicationShortlisted`, `ApplicationRejected`, …). Trade-off: subscriber simplicity vs. catalog explosion.
2. **Should `ResumeParsed` live in BC-3 or BC-7?** Current placement: BC-3 emits it, BC-7 consumes. Defensible the other way (BC-7 owns parsing as part of its ML pipeline). Worth contrasting in lecture.
3. **Where should ops/system events live (sync errors, performance alerts)?** Currently split: BC-8 emits sync errors, BC-7 emits embedding metrics. An argument exists for a separate Telemetry BC (rejected during BC list selection — see history).
4. **Event versioning.** What happens when `JobPostingPublished` adds a new field? Two practical schools — additive-only (deprecate fields, never break) vs. versioned (`JobPostingPublishedV2`). A short-course favorite.
5. **Choreography depth.** With ~70 events flying around, can a new engineer understand the system? When does choreography become spaghetti and demand orchestration via a saga?

## Method note

Events derived by inspecting each story's acceptance criteria for state changes that other BCs would care about. A state change is event-worthy if (a) it crosses a BC boundary, or (b) it triggers a downstream side-effect (notification, indexing, recompute, audit). Internal-only state changes are not catalogued — they're aggregate invariants, not domain events.

**Refresh** when a story is added, an aggregate gains a new lifecycle state, or a subscribing BC needs information not currently in any event payload.
