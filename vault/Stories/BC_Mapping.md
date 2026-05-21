---
title: "Story → Bounded Context Mapping"
type: matrix
total_stories: 117
generated: 2026-05-14
tags:
  - matrix
  - bc-mapping
---

# Story → Bounded Context Mapping

Each user story has one **Owning BC** plus zero or more **Collaborating BCs**. Stories crossing more than two BCs are **saga candidates** and reference a named saga from the catalog.

This mapping reflects the **simplified 12-BC course model** agreed for the DDD short course.

## Summary

| BC | Name | Class | Stories owned |
|---|---|---|---|
| BC-1 | IAM and UAM | generic | 6 |
| BC-2 | Employer Profile Management | supporting | 4 |
| BC-3 | JobSeeker Profile | supporting | 10 |
| BC-4 | Job Postings | supporting | 7 |
| BC-5 | Job Application | supporting | 3 |
| BC-6 | Search & Discovery | supporting | 3 |
| BC-7 | Recommendation Engine | CORE | 15 |
| BC-8 | External Job Synchronization | supporting (ACL) | 20 |
| BC-9 | Notification | generic | 18 |
| BC-10 | Reporting | CORE for LMIS | 16 |
| BC-11 | Administrators Configuration | generic | 1 |
| BC-12 | Content Management | supporting | 14 |
| **Total** | | | **117** |

## Saga distribution

| Saga | Stories invoking it | Notes |
|---|---|---|
| AccountDeactivationCascade | 0 | No explicit deactivation story yet; inferred from BC-1 domain model. Propose to add. |
| EmployerVerificationSaga | 1 | [[US-3.1.2-02-verify-employer-account\|US-3.1.2-02]] triggers gov verification (BC-8) + audit (BC-10). |
| ApplicationSubmissionSaga | 1 | [[US-3.2.3-02-apply-to-jobs\|US-3.2.3-02]] captures snapshot + notifications. |
| SavedSearchAlertSaga | 4 | Saved-search match → recommendation compute → digest dispatch. |
| ResumeIntakeSaga | 1 | [[US-3.1.1-04-upload-and-parse-resume\|US-3.1.1-04]] chains resume parse + skill standardization + match refresh. |
| ModerationActionSaga | 2 | [[US-3.1.4-01-manage-user-accounts\|US-3.1.4-01]] (BC-1) and [[US-3.1.4-03-manage-job-postings\|US-3.1.4-03]] (BC-4). |
| PartnerJobIngestSaga | 1 | [[US-3.4.1-01-ingest-jobs-from-external-portal\|US-3.4.1-01]] coordinates sync + posting creation + match refresh. |

## By Bounded Context

### BC-1 · IAM and UAM

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.1.4-01-manage-user-accounts\|US-3.1.4-01]] | Manage user accounts | BC-2, BC-3, BC-10 | ModerationActionSaga |
| [[US-3.1.5-01-login-with-credentials\|US-3.1.5-01]] | Login with credentials | — | — |
| [[US-3.1.5-02-multi-factor-authentication\|US-3.1.5-02]] | Multi-factor authentication | — | — |
| [[US-3.1.5-03-password-reset\|US-3.1.5-03]] | Password reset | — | — |
| [[US-3.1.5-04-session-and-access-control\|US-3.1.5-04]] | Session and access control | — | — |
| [[US-3.4.3-04-implement-oauth2-authentication\|US-3.4.3-04]] | Implement OAuth 2.0 authentication | BC-8 | — |

### BC-2 · Employer Profile Management

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.1.2-01-register-employer-account\|US-3.1.2-01]] | Register employer account | BC-1 | — |
| [[US-3.1.2-02-verify-employer-account\|US-3.1.2-02]] | Verify employer account | BC-8, BC-10 | EmployerVerificationSaga |
| [[US-3.1.2-03-manage-employer-profile\|US-3.1.2-03]] | Manage employer profile | — | — |
| [[US-3.1.2-04-employer-dashboard\|US-3.1.2-04]] | Employer dashboard | BC-7, BC-5 | — |

### BC-3 · JobSeeker Profile

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.1.1-01-self-register-as-job-seeker\|US-3.1.1-01]] | Self-register as a job seeker | BC-1 | — |
| [[US-3.1.1-02-activate-account-via-otp\|US-3.1.1-02]] | Activate account via OTP | BC-1, BC-9 | — |
| [[US-3.1.1-03-complete-level-2-profile\|US-3.1.1-03]] | Complete Level 2 profile | — | — |
| [[US-3.1.1-04-upload-and-parse-resume\|US-3.1.1-04]] | Upload and parse resume | BC-7 | ResumeIntakeSaga |
| [[US-3.1.1-05-view-profile-completeness\|US-3.1.1-05]] | View profile completeness | — | — |
| [[US-3.1.1-06-manage-supplementary-documents\|US-3.1.1-06]] | Manage supplementary documents | — | — |
| [[US-3.1.1-07-manage-profile-visibility-and-sharing\|US-3.1.1-07]] | Manage profile visibility & sharing | — | — |
| [[US-3.1.1-08-edit-profile-history\|US-3.1.1-08]] | Edit profile history | BC-10 | — |
| [[US-3.3.1-03-parse-resume-and-extract-skills\|US-3.3.1-03]] | Parse resume and extract skills | BC-7, BC-11 | — |
| [[US-3.3.1-04-review-and-correct-parsed-resume\|US-3.3.1-04]] | Review and correct parsed resume | BC-7 | — |

### BC-4 · Job Postings

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.1.4-03-manage-job-postings\|US-3.1.4-03]] | Manage job postings (admin moderation) | BC-10 | ModerationActionSaga |
| [[US-3.2.1-01-create-job-posting\|US-3.2.1-01]] | Create job posting | BC-2, BC-11 | — |
| [[US-3.2.1-02-standardize-job-data\|US-3.2.1-02]] | Standardize job data | BC-11 | — |
| [[US-3.2.1-03-manage-job-posting-visibility\|US-3.2.1-03]] | Manage job posting visibility | — | — |
| [[US-3.2.1-04-manage-posting-expiration\|US-3.2.1-04]] | Manage posting expiration | BC-9 | — |
| [[US-3.2.4-01-view-manage-job-status\|US-3.2.4-01]] | View / manage job status | — | — |
| [[US-3.2.4-02-track-status-changes\|US-3.2.4-02]] | Track status changes | BC-10 | — |

### BC-5 · Job Application

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.2.3-01-bookmark-and-save-jobs\|US-3.2.3-01]] | Bookmark and save jobs | BC-4 | — |
| [[US-3.2.3-02-apply-to-jobs\|US-3.2.3-02]] | Apply to jobs and initiate application interaction | BC-3, BC-2, BC-4, BC-9 | ApplicationSubmissionSaga |
| [[US-3.2.4-03-view-manage-applications\|US-3.2.4-03]] | View / manage applications | BC-2, BC-3 | — |

### BC-6 · Search & Discovery

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.2.2-01-search-jobs\|US-3.2.2-01]] | Search jobs | BC-4, BC-7 | — |
| [[US-3.2.2-02-filter-and-rank-results\|US-3.2.2-02]] | Filter and rank results | BC-7 | — |
| [[US-3.2.2-03-save-favorites-and-searches\|US-3.2.2-03]] | Save favorites and searches | BC-9 | — |

### BC-7 · Recommendation Engine

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.3.1-01-implement-ai-driven-matching-algorithm\|US-3.3.1-01]] | Implement AI-driven matching algorithm | — | — |
| [[US-3.3.1-02-perform-nlp-semantic-analysis\|US-3.3.1-02]] | Perform NLP semantic analysis | — | — |
| [[US-3.3.1-05-generate-shortlist-top-matching-candidates\|US-3.3.1-05]] | Generate shortlist of top-matching candidates | BC-3 | — |
| [[US-3.3.1-06-manage-match-threshold-configuration\|US-3.3.1-06]] | Manage match threshold configuration | — | — |
| [[US-3.3.1-07-rank-jobs-by-match-percentage\|US-3.3.1-07]] | Rank jobs by match percentage | BC-4 | — |
| [[US-3.3.1-08-configure-matching-parameters\|US-3.3.1-08]] | Configure matching parameters | — | — |
| [[US-3.3.2-01-see-personalized-job-recommendations\|US-3.3.2-01]] | See personalized job recommendations | BC-3, BC-4, BC-9, BC-10 | SavedSearchAlertSaga |
| [[US-3.3.2-02-compute-recommendation-embeddings-nightly\|US-3.3.2-02]] | Compute recommendation embeddings nightly | BC-3, BC-4, BC-10 | SavedSearchAlertSaga |
| [[US-3.3.2-03-recommend-jobs-based-on-preferences\|US-3.3.2-03]] | Recommend jobs based on preferences | BC-3, BC-4, BC-9 | SavedSearchAlertSaga |
| [[US-3.3.3-01-see-ranked-candidate-recommendations\|US-3.3.3-01]] | See ranked candidate recommendations | BC-2 | — |
| [[US-3.3.3-02-set-candidate-qualification-thresholds\|US-3.3.3-02]] | Set candidate qualification thresholds | BC-2 | — |
| [[US-3.3.3-03-search-candidate-database-with-filters\|US-3.3.3-03]] | Search candidate database with filters | BC-2, BC-3 | — |
| [[US-3.3.3-04-respect-candidate-privacy-in-recommendations\|US-3.3.3-04]] | Respect candidate privacy in recommendations | BC-3 | — |
| [[US-3.3.3-05-provide-candidate-insights-and-fit-analysis\|US-3.3.3-05]] | Provide candidate insights and fit analysis | BC-2, BC-3 | — |
| [[US-3.3.3-06-save-promising-candidates-to-talent-pool\|US-3.3.3-06]] | Save promising candidates to talent pool (shortlist) | BC-2 | — |

### BC-8 · External Job Synchronization

Now also owns government-interface stories (formerly a separate BC) per the agreed simplification.

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.1.3-01-register-third-party-portal\|US-3.1.3-01]] | Register third-party portal | BC-1 | — |
| [[US-3.1.3-02-push-jobs-via-api\|US-3.1.3-02]] | Push jobs via API | BC-4 | — |
| [[US-3.1.3-03-configure-data-mapping\|US-3.1.3-03]] | Configure data mapping | BC-11 | — |
| [[US-3.1.3-04-view-integration-logs\|US-3.1.3-04]] | View integration logs | BC-10 | — |
| [[US-3.4.1-01-ingest-jobs-from-external-portal\|US-3.4.1-01]] | Ingest jobs from external portal | BC-4, BC-7, BC-11 | PartnerJobIngestSaga |
| [[US-3.4.1-02-export-jobs-to-external-portal\|US-3.4.1-02]] | Export jobs to external portal | BC-4 | — |
| [[US-3.4.1-03-map-and-transform-job-data\|US-3.4.1-03]] | Map and transform job data | BC-11 | — |
| [[US-3.4.1-04-configure-external-portal-credentials\|US-3.4.1-04]] | Configure external portal credentials | — | — |
| [[US-3.4.1-05-monitor-integration-dashboard\|US-3.4.1-05]] | Monitor integration dashboard | BC-10 | — |
| [[US-3.4.1-06-reconcile-sync-errors\|US-3.4.1-06]] | Reconcile sync errors | BC-10 | — |
| [[US-3.4.1-07-handle-real-time-job-updates\|US-3.4.1-07]] | Handle real-time job updates | BC-4 | — |
| [[US-3.4.2-01-verify-educational-credentials\|US-3.4.2-01]] | Verify educational credentials | BC-3, BC-10 | — |
| [[US-3.4.2-02-verify-identity-via-government-system\|US-3.4.2-02]] | Verify identity via government system | BC-1, BC-10 | — |
| [[US-3.4.2-03-integrate-mol-pef-databases\|US-3.4.2-03]] | Integrate MoL/PEF databases | — | — |
| [[US-3.4.2-04-maintain-audit-trail-for-government-data\|US-3.4.2-04]] | Maintain audit trail for government data | BC-10 | — |
| [[US-3.4.2-05-enforce-privacy-compliance\|US-3.4.2-05]] | Enforce privacy compliance | BC-10 | — |
| [[US-3.4.3-01-provide-comprehensive-api-framework\|US-3.4.3-01]] | Provide comprehensive API framework | BC-1 | — |
| [[US-3.4.3-02-implement-restful-apis-with-json\|US-3.4.3-02]] | Implement RESTful APIs with JSON | — | — |
| [[US-3.4.3-03-publish-api-documentation\|US-3.4.3-03]] | Publish API documentation | BC-12 | — |
| [[US-3.4.3-05-support-api-versioning\|US-3.4.3-05]] | Support API versioning | — | — |

### BC-9 · Notification

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.6.1-01-receive-important-event-email\|US-3.6.1-01]] | Receive important event email | BC-10 | — |
| [[US-3.6.1-02-configure-email-notification-preferences\|US-3.6.1-02]] | Configure email notification preferences | — | — |
| [[US-3.6.1-03-manage-email-template\|US-3.6.1-03]] | Manage email template | — | — |
| [[US-3.6.1-04-support-immediate-and-digest-emails\|US-3.6.1-04]] | Support immediate and digest emails | — | — |
| [[US-3.6.1-05-log-all-email-notifications\|US-3.6.1-05]] | Log all email notifications | BC-10 | — |
| [[US-3.6.1-06-ensure-spam-compliance\|US-3.6.1-06]] | Ensure spam compliance | — | — |
| [[US-3.6.2-01-access-in-app-notification-center\|US-3.6.2-01]] | Access in-app notification center | — | — |
| [[US-3.6.2-02-receive-real-time-notifications\|US-3.6.2-02]] | Receive real-time notifications | BC-10 | — |
| [[US-3.6.2-03-receive-weekly-job-recommendations\|US-3.6.2-03]] | Receive weekly job recommendations in-app | BC-7, BC-10 | SavedSearchAlertSaga |
| [[US-3.6.2-04-maintain-notification-history\|US-3.6.2-04]] | Maintain notification history | BC-10 | — |
| [[US-3.6.2-05-configure-in-app-notification-preferences\|US-3.6.2-05]] | Configure in-app notification preferences | — | — |
| [[US-3.6.2-06-support-notification-types-with-visual-indicators\|US-3.6.2-06]] | Support notification types with visual indicators | — | — |
| [[US-3.6.2-07-manage-notifications-mark-read-delete\|US-3.6.2-07]] | Manage notifications (mark read/delete) | — | — |
| [[US-3.6.3-01-send-sms-for-critical-updates\|US-3.6.3-01]] | Send SMS for critical updates | BC-10 | — |
| [[US-3.6.3-02-allow-users-to-opt-in-sms\|US-3.6.3-02]] | Allow users to opt in to SMS | — | — |
| [[US-3.6.3-03-limit-sms-notification-frequency\|US-3.6.3-03]] | Limit SMS notification frequency | — | — |
| [[US-3.6.3-04-track-sms-delivery-status\|US-3.6.3-04]] | Track SMS delivery status | BC-10 | — |
| [[US-3.6.3-05-ensure-sms-compliance\|US-3.6.3-05]] | Ensure SMS compliance | — | — |

### BC-10 · Reporting

Now also owns user-activity / audit trails (formerly a separate BC). Holds both operational read-models and LMIS analytics.

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.1.4-04-view-admin-reports\|US-3.1.4-04]] | View admin reports | BC-1 | — |
| [[US-3.5.1-01-view-user-activity-dashboard\|US-3.5.1-01]] | View user activity dashboard | — | — |
| [[US-3.5.1-02-track-job-seeker-activities\|US-3.5.1-02]] | Track job seeker activities | BC-3 | — |
| [[US-3.5.1-03-track-employer-activities\|US-3.5.1-03]] | Track employer activities | BC-2 | — |
| [[US-3.5.1-04-manage-activity-retention-policy\|US-3.5.1-04]] | Manage activity retention policy | — | — |
| [[US-3.5.2-01-view-employment-stats-dashboard\|US-3.5.2-01]] | View employment stats dashboard | — | — |
| [[US-3.5.2-02-view-industry-analytics\|US-3.5.2-02]] | View industry analytics | — | — |
| [[US-3.5.2-03-view-skill-demand-trends\|US-3.5.2-03]] | View skill demand trends | — | — |
| [[US-3.5.2-04-view-employment-outcomes\|US-3.5.2-04]] | View employment outcomes | — | — |
| [[US-3.5.3-01-view-system-performance-dashboard\|US-3.5.3-01]] | View system performance dashboard | — | — |
| [[US-3.5.3-02-monitor-matching-performance\|US-3.5.3-02]] | Monitor matching performance | BC-7 | — |
| [[US-3.5.3-03-configure-performance-alerts\|US-3.5.3-03]] | Configure performance alerts | BC-9 | — |
| [[US-3.5.4-01-generate-custom-report\|US-3.5.4-01]] | Generate custom report | — | — |
| [[US-3.5.4-02-define-report-templates\|US-3.5.4-02]] | Define report templates | — | — |
| [[US-3.5.4-03-schedule-recurring-reports\|US-3.5.4-03]] | Schedule recurring reports | BC-9 | — |
| [[US-3.5.4-04-implement-report-access-controls\|US-3.5.4-04]] | Implement report access controls | BC-1 | — |

### BC-11 · Administrators Configuration

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.1.4-02-manage-taxonomies\|US-3.1.4-02]] | Manage taxonomies (skills, occupations, industries) | BC-3, BC-4, BC-7, BC-8 | — |

### BC-12 · Content Management

| Story | Title | Collaborates with | Saga |
|---|---|---|---|
| [[US-3.7.1-01-publish-news-article\|US-3.7.1-01]] | Publish news article | — | — |
| [[US-3.7.1-02-schedule-news-publication\|US-3.7.1-02]] | Schedule news publication | — | — |
| [[US-3.7.1-03-categorize-and-tag-articles\|US-3.7.1-03]] | Categorize and tag articles | — | — |
| [[US-3.7.1-04-archive-and-unpublish-articles\|US-3.7.1-04]] | Archive and unpublish articles | — | — |
| [[US-3.7.1-05-browse-and-filter-news\|US-3.7.1-05]] | Browse and filter news | — | — |
| [[US-3.7.1-06-search-news-archive\|US-3.7.1-06]] | Search news archive | — | — |
| [[US-3.7.1-07-dashboard-personalization\|US-3.7.1-07]] | Dashboard personalization | — | — |
| [[US-3.7.2-01-create-faq-entry\|US-3.7.2-01]] | Create FAQ entry | — | — |
| [[US-3.7.2-02-organize-help-by-topic-and-role\|US-3.7.2-02]] | Organize help by topic and role | — | — |
| [[US-3.7.2-03-search-help-content\|US-3.7.2-03]] | Search help content | — | — |
| [[US-3.7.2-04-context-sensitive-help\|US-3.7.2-04]] | Context-sensitive help | — | — |
| [[US-3.7.2-05-collect-help-feedback\|US-3.7.2-05]] | Collect help feedback | — | — |
| [[US-3.7.2-06-multimedia-help-content\|US-3.7.2-06]] | Multimedia help content | — | — |
| [[US-3.7.2-07-create-guided-tours\|US-3.7.2-07]] | Create guided tours | — | — |

## Saga candidates (multi-BC stories)

| Story | Owning BC | Collaborating BCs | Saga |
|---|---|---|---|
| [[US-3.1.1-04-upload-and-parse-resume\|US-3.1.1-04]] | BC-3 | BC-7 | ResumeIntakeSaga |
| [[US-3.1.2-02-verify-employer-account\|US-3.1.2-02]] | BC-2 | BC-8, BC-10 | EmployerVerificationSaga |
| [[US-3.1.4-01-manage-user-accounts\|US-3.1.4-01]] | BC-1 | BC-2, BC-3, BC-10 | ModerationActionSaga |
| [[US-3.1.4-03-manage-job-postings\|US-3.1.4-03]] | BC-4 | BC-10 | ModerationActionSaga |
| [[US-3.2.3-02-apply-to-jobs\|US-3.2.3-02]] | BC-5 | BC-3, BC-2, BC-4, BC-9 | ApplicationSubmissionSaga |
| [[US-3.3.2-01-see-personalized-job-recommendations\|US-3.3.2-01]] | BC-7 | BC-3, BC-4, BC-9, BC-10 | SavedSearchAlertSaga |
| [[US-3.3.2-02-compute-recommendation-embeddings-nightly\|US-3.3.2-02]] | BC-7 | BC-3, BC-4, BC-10 | SavedSearchAlertSaga |
| [[US-3.3.2-03-recommend-jobs-based-on-preferences\|US-3.3.2-03]] | BC-7 | BC-3, BC-4, BC-9 | SavedSearchAlertSaga |
| [[US-3.4.1-01-ingest-jobs-from-external-portal\|US-3.4.1-01]] | BC-8 | BC-4, BC-7, BC-11 | PartnerJobIngestSaga |
| [[US-3.6.2-03-receive-weekly-job-recommendations\|US-3.6.2-03]] | BC-9 | BC-7, BC-10 | SavedSearchAlertSaga |

## Migration notes (vs. previous 15-BC mapping)

The old 15-BC model collapsed into the agreed 12-BC course model as follows:

| Old BC | New BC | Notes |
|---|---|---|
| BC-1 Identity & Access | **BC-1 IAM and UAM** | UAM scope added — admin user-account management (US-3.1.4-01) now owned here, not in a back-office BC. |
| BC-2 Job Seeker Profile | **BC-3 JobSeeker Profile** | Resume parse / skill extraction stories (US-3.3.1-03, -04) folded in — they read into the Profile aggregate, with BC-7 as collaborator. |
| BC-3 Employer Profile & Verification | **BC-2 Employer Profile Management** | Candidate-recommendation viewing stories moved to BC-7 (Recommendation Engine) where the model lives. |
| BC-4 Skill Taxonomy & Resume Intelligence | split | Taxonomy management → **BC-11**; resume parsing → **BC-3**; standardize-job-data → **BC-4**. |
| BC-5 Job Posting | **BC-4 Job Postings** | Admin moderation of postings (US-3.1.4-03) folded in. |
| BC-6 Search & Browse | **BC-6 Search & Discovery** | Bookmarking individual jobs (US-3.2.3-01) moved to **BC-5 Job Application** as pre-application intent. Saved searches stay in S&D. |
| BC-7 Partner Integration | **BC-8 External Job Synchronization** | Renamed; now also owns Government Interfaces. |
| BC-8 Matching & Recommendation | **BC-7 Recommendation Engine** | Renamed; absorbed candidate-recommendation viewing stories from old BC-3. |
| BC-9 Government Interfaces | folded into **BC-8** | Per agreed simplification — single external-integrations context. |
| BC-10 Notifications | **BC-9 Notification** | Unchanged in scope. |
| BC-11 Audit & Activity | folded into **BC-10 Reporting** | Activity tracking is now a Reporting concern in the simplified model. |
| BC-12 Analytics / LMIS Read Model | **BC-10 Reporting** | Renamed; absorbed audit/activity. |
| BC-13 Content | **BC-12 Content Management** | Unchanged in scope. |
| BC-14 Applications & Hiring Pipeline | **BC-5 Job Application** | Renamed; bookmarking added from old BC-6. |
| BC-15 Back-Office & Moderation | dissolved | Moderation responsibilities moved to the BC owning each aggregate (user accounts → BC-1, postings → BC-4). |

## Anomalies & open questions

**BC-8 is now large (20 stories).** Folding Government Interfaces into External Job Synchronization satisfies the simplification goal but produces a heterogeneous BC: outbound job sync, inbound government verification, and the public API framework all live together. For teaching purposes this is a useful discussion point — *when does a BC start asking to be split?* Consider treating "Partner Job Sync," "Government Verification," and "Public API" as **modules within one BC** rather than separate BCs, and use this as a class exercise on bounded-context sizing heuristics.

**BC-11 (Administrators Configuration) is thin (1 story).** Only `manage-taxonomies` clearly belongs here. Other admin-flavored stories (matching parameter config, notification template config, performance-alert config, report access control) live with the aggregate they configure, per DDD conventions. Three options for the course:
1. Keep BC-11 thin — useful counter-example showing not every BC needs many stories.
2. Pull configuration stories from other BCs into BC-11 — good debate material on **shared kernel vs. local config**.
3. Merge BC-11 into BC-1 (IAM/UAM) under a broader "Platform Administration" framing.

**AccountDeactivationCascade saga still has no triggering story.** The BC-1 domain model publishes `AccountDeactivated`, but no story exists for a user requesting deactivation. Recommend adding `US-3.1.1-09-deactivate-account` (BC-3) and `US-3.1.2-05-deactivate-employer-account` (BC-2), both owned by their profile BCs and emitting an event consumed by BC-1.

**SavedSearchAlertSaga spans four stories** across BC-7 (compute) and BC-9 (dispatch). Worth narrating end-to-end in lecture: saved search → match recompute → digest assembly → channel dispatch.

**Cross-story consistency:** All 117 stories are accounted for. Every story is assigned exactly one owning BC. No duplicates, no orphans.

## Method note

Owning BC chosen by the heuristic: *the BC whose ubiquitous language and aggregate state changes most directly when this story is satisfied.* Collaborating BCs derived from domain events implied by each story's acceptance criteria. Saga candidates identified by cross-context event flow: any story whose acceptance criteria require writes or reads to 3+ BCs is flagged and mapped to a named saga.

**Refresh:** Regenerate this matrix whenever stories are added, renumbered, or their acceptance criteria significantly change. Use the `_story_id`, `_section_id`, and `bc/` tags in frontmatter as the authoritative signal.
