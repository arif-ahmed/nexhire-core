---
title: "User Stories Index"
type: index
total_stories: 117
generated: 2026-04-26
tags:
  - stories
  - index
---

# User Stories Index

This vault contains **117 user stories** derived from the SRS Section 3 (Functional Requirements). Each story carries:

- A unique ID `US-{section}-{NN}` traceable to the SRS subsection
- Acceptance Criteria in Given/When/Then format
- Documented assumptions for any gaps
- Backlinks to source SRS requirements and related stories

Open a section to see the stories grouped by SRS subsection. Filenames omit the `US-` prefix in the link text for readability.

## 3.1 — User Management and Authentication

### 3.1.1 Job Seeker Registration and Profile Management — *(8 stories)*

- [[US-3.1.1-01-self-register-as-job-seeker|US-3.1.1-01 Self-register as a job seeker]]
- [[US-3.1.1-02-activate-account-via-otp|US-3.1.1-02 Activate account via OTP]]
- [[US-3.1.1-03-complete-level-2-profile|US-3.1.1-03 Complete Level 2 profile]]
- [[US-3.1.1-04-upload-and-parse-resume|US-3.1.1-04 Upload and parse resume]]
- [[US-3.1.1-05-view-profile-completeness|US-3.1.1-05 View profile completeness]]
- [[US-3.1.1-06-manage-supplementary-documents|US-3.1.1-06 Manage supplementary documents]]
- [[US-3.1.1-07-manage-profile-visibility-and-sharing|US-3.1.1-07 Manage profile visibility & sharing]]
- [[US-3.1.1-08-edit-profile-history|US-3.1.1-08 Edit profile history]]

### 3.1.2 Employer Registration and Profile Management — *(4 stories)*

- [[US-3.1.2-01-register-employer-account|US-3.1.2-01 Register employer account]]
- [[US-3.1.2-02-verify-employer-account|US-3.1.2-02 Verify employer account]]
- [[US-3.1.2-03-manage-employer-profile|US-3.1.2-03 Manage employer profile]]
- [[US-3.1.2-04-employer-dashboard|US-3.1.2-04 Employer dashboard]]

### 3.1.3 Third-Party Job Portals Registration and Integration — *(4 stories)*

- [[US-3.1.3-01-register-third-party-portal|US-3.1.3-01 Register third-party portal]]
- [[US-3.1.3-02-push-jobs-via-api|US-3.1.3-02 Push jobs via API]]
- [[US-3.1.3-03-configure-data-mapping|US-3.1.3-03 Configure data mapping]]
- [[US-3.1.3-04-view-integration-logs|US-3.1.3-04 View integration logs]]

### 3.1.4 Administrator User Management — *(4 stories)*

- [[US-3.1.4-01-manage-user-accounts|US-3.1.4-01 Manage user accounts]]
- [[US-3.1.4-02-manage-taxonomies|US-3.1.4-02 Manage taxonomies]]
- [[US-3.1.4-03-manage-job-postings|US-3.1.4-03 Manage job postings]]
- [[US-3.1.4-04-view-admin-reports|US-3.1.4-04 View admin reports]]

### 3.1.5 Authentication and Authorization — *(4 stories)*

- [[US-3.1.5-01-login-with-credentials|US-3.1.5-01 Login with credentials]]
- [[US-3.1.5-02-multi-factor-authentication|US-3.1.5-02 Multi-factor authentication]]
- [[US-3.1.5-03-password-reset|US-3.1.5-03 Password reset]]
- [[US-3.1.5-04-session-and-access-control|US-3.1.5-04 Session and access control]]

## 3.2 — Job Posting and Management

### 3.2.1 Job Creation and Publishing — *(4 stories)*

- [[US-3.2.1-01-create-job-posting|US-3.2.1-01 Create job posting]]
- [[US-3.2.1-02-standardize-job-data|US-3.2.1-02 Standardize job data]]
- [[US-3.2.1-03-manage-job-posting-visibility|US-3.2.1-03 Manage job posting visibility]]
- [[US-3.2.1-04-manage-posting-expiration|US-3.2.1-04 Manage posting expiration]]

### 3.2.2 Job Search and Filtering — *(3 stories)*

- [[US-3.2.2-01-search-jobs|US-3.2.2-01 Search jobs]]
- [[US-3.2.2-02-filter-and-rank-results|US-3.2.2-02 Filter and rank results]]
- [[US-3.2.2-03-save-favorites-and-searches|US-3.2.2-03 Save favorites and searches]]

### 3.2.3 Job Interaction Process — *(2 stories)*

- [[US-3.2.3-01-bookmark-and-save-jobs|US-3.2.3-01 Bookmark and save jobs]]
- [[US-3.2.3-02-apply-to-jobs|US-3.2.3-02 Apply to jobs]]

### 3.2.4 Job Status Tracking — *(3 stories)*

- [[US-3.2.4-01-view-manage-job-status|US-3.2.4-01 View / manage job status]]
- [[US-3.2.4-02-track-status-changes|US-3.2.4-02 Track status changes]]
- [[US-3.2.4-03-view-manage-applications|US-3.2.4-03 View / manage applications]]

## 3.3 — AI-Driven Matching System

### 3.3.1 Vector-Based Matching Algorithms — *(8 stories)*

- [[US-3.3.1-01-implement-ai-driven-matching-algorithm|US-3.3.1-01 Implement AI-driven matching algorithm]]
- [[US-3.3.1-02-perform-nlp-semantic-analysis|US-3.3.1-02 Perform NLP semantic analysis]]
- [[US-3.3.1-03-parse-resume-and-extract-skills|US-3.3.1-03 Parse resume and extract skills]]
- [[US-3.3.1-04-review-and-correct-parsed-resume|US-3.3.1-04 Review and correct parsed resume]]
- [[US-3.3.1-05-generate-shortlist-top-matching-candidates|US-3.3.1-05 Generate shortlist of top-matching candidates]]
- [[US-3.3.1-06-manage-match-threshold-configuration|US-3.3.1-06 Manage match threshold configuration]]
- [[US-3.3.1-07-rank-jobs-by-match-percentage|US-3.3.1-07 Rank jobs by match percentage]]
- [[US-3.3.1-08-configure-matching-parameters|US-3.3.1-08 Configure matching parameters]]

### 3.3.2 Job Recommendation Engine — *(3 stories)*

- [[US-3.3.2-01-see-personalized-job-recommendations|US-3.3.2-01 See personalized job recommendations]]
- [[US-3.3.2-02-compute-recommendation-embeddings-nightly|US-3.3.2-02 Compute recommendation embeddings nightly]]
- [[US-3.3.2-03-recommend-jobs-based-on-preferences|US-3.3.2-03 Recommend jobs based on preferences]]

### 3.3.3 Candidate Recommendation for Employers — *(6 stories)*

- [[US-3.3.3-01-see-ranked-candidate-recommendations|US-3.3.3-01 See ranked candidate recommendations]]
- [[US-3.3.3-02-set-candidate-qualification-thresholds|US-3.3.3-02 Set candidate qualification thresholds]]
- [[US-3.3.3-03-search-candidate-database-with-filters|US-3.3.3-03 Search candidate database with filters]]
- [[US-3.3.3-04-respect-candidate-privacy-in-recommendations|US-3.3.3-04 Respect candidate privacy in recommendations]]
- [[US-3.3.3-05-provide-candidate-insights-and-fit-analysis|US-3.3.3-05 Provide candidate insights and fit analysis]]
- [[US-3.3.3-06-save-promising-candidates-to-talent-pool|US-3.3.3-06 Save promising candidates to talent pool]]

## 3.4 — External System Integration

### 3.4.1 External Job Site Integration — *(7 stories)*

- [[US-3.4.1-01-ingest-jobs-from-external-portal|US-3.4.1-01 Ingest jobs from external portal]]
- [[US-3.4.1-02-export-jobs-to-external-portal|US-3.4.1-02 Export jobs to external portal]]
- [[US-3.4.1-03-map-and-transform-job-data|US-3.4.1-03 Map and transform job data]]
- [[US-3.4.1-04-configure-external-portal-credentials|US-3.4.1-04 Configure external portal credentials]]
- [[US-3.4.1-05-monitor-integration-dashboard|US-3.4.1-05 Monitor integration dashboard]]
- [[US-3.4.1-06-reconcile-sync-errors|US-3.4.1-06 Reconcile sync errors]]
- [[US-3.4.1-07-handle-real-time-job-updates|US-3.4.1-07 Handle real-time job updates]]

### 3.4.2 Government Database Integration — *(5 stories)*

- [[US-3.4.2-01-verify-educational-credentials|US-3.4.2-01 Verify educational credentials]]
- [[US-3.4.2-02-verify-identity-via-government-system|US-3.4.2-02 Verify identity via government system]]
- [[US-3.4.2-03-integrate-mol-pef-databases|US-3.4.2-03 Integrate MoL/PEF databases]]
- [[US-3.4.2-04-maintain-audit-trail-for-government-data|US-3.4.2-04 Maintain audit trail for government data]]
- [[US-3.4.2-05-enforce-privacy-compliance|US-3.4.2-05 Enforce privacy compliance]]

### 3.4.3 API Framework — *(5 stories)*

- [[US-3.4.3-01-provide-comprehensive-api-framework|US-3.4.3-01 Provide comprehensive API framework]]
- [[US-3.4.3-02-implement-restful-apis-with-json|US-3.4.3-02 Implement RESTful APIs with JSON]]
- [[US-3.4.3-03-publish-api-documentation|US-3.4.3-03 Publish API documentation]]
- [[US-3.4.3-04-implement-oauth2-authentication|US-3.4.3-04 Implement OAuth 2.0 authentication]]
- [[US-3.4.3-05-support-api-versioning|US-3.4.3-05 Support API versioning]]

## 3.5 — Reporting and Analytics

### 3.5.1 User Activity Monitoring — *(4 stories)*

- [[US-3.5.1-01-view-user-activity-dashboard|US-3.5.1-01 View user activity dashboard]]
- [[US-3.5.1-02-track-job-seeker-activities|US-3.5.1-02 Track job seeker activities]]
- [[US-3.5.1-03-track-employer-activities|US-3.5.1-03 Track employer activities]]
- [[US-3.5.1-04-manage-activity-retention-policy|US-3.5.1-04 Manage activity retention policy]]

### 3.5.2 Employment Statistics — *(4 stories)*

- [[US-3.5.2-01-view-employment-stats-dashboard|US-3.5.2-01 View employment stats dashboard]]
- [[US-3.5.2-02-view-industry-analytics|US-3.5.2-02 View industry analytics]]
- [[US-3.5.2-03-view-skill-demand-trends|US-3.5.2-03 View skill demand trends]]
- [[US-3.5.2-04-view-employment-outcomes|US-3.5.2-04 View employment outcomes]]

### 3.5.3 System Performance Metrics — *(3 stories)*

- [[US-3.5.3-01-view-system-performance-dashboard|US-3.5.3-01 View system performance dashboard]]
- [[US-3.5.3-02-monitor-matching-performance|US-3.5.3-02 Monitor matching performance]]
- [[US-3.5.3-03-configure-performance-alerts|US-3.5.3-03 Configure performance alerts]]

### 3.5.4 Custom Report Generation — *(4 stories)*

- [[US-3.5.4-01-generate-custom-report|US-3.5.4-01 Generate custom report]]
- [[US-3.5.4-02-define-report-templates|US-3.5.4-02 Define report templates]]
- [[US-3.5.4-03-schedule-recurring-reports|US-3.5.4-03 Schedule recurring reports]]
- [[US-3.5.4-04-implement-report-access-controls|US-3.5.4-04 Implement report access controls]]

## 3.6 — Notification System

### 3.6.1 Email Notifications — *(6 stories)*

- [[US-3.6.1-01-receive-important-event-email|US-3.6.1-01 Receive important event email]]
- [[US-3.6.1-02-configure-email-notification-preferences|US-3.6.1-02 Configure email notification preferences]]
- [[US-3.6.1-03-manage-email-template|US-3.6.1-03 Manage email template]]
- [[US-3.6.1-04-support-immediate-and-digest-emails|US-3.6.1-04 Support immediate and digest emails]]
- [[US-3.6.1-05-log-all-email-notifications|US-3.6.1-05 Log all email notifications]]
- [[US-3.6.1-06-ensure-spam-compliance|US-3.6.1-06 Ensure spam compliance]]

### 3.6.2 In-App Notifications — *(7 stories)*

- [[US-3.6.2-01-access-in-app-notification-center|US-3.6.2-01 Access in-app notification center]]
- [[US-3.6.2-02-receive-real-time-notifications|US-3.6.2-02 Receive real-time notifications]]
- [[US-3.6.2-03-receive-weekly-job-recommendations|US-3.6.2-03 Receive weekly job recommendations]]
- [[US-3.6.2-04-maintain-notification-history|US-3.6.2-04 Maintain notification history]]
- [[US-3.6.2-05-configure-in-app-notification-preferences|US-3.6.2-05 Configure in-app notification preferences]]
- [[US-3.6.2-06-support-notification-types-with-visual-indicators|US-3.6.2-06 Support notification types with visual indicators]]
- [[US-3.6.2-07-manage-notifications-mark-read-delete|US-3.6.2-07 Manage notifications (mark read/delete)]]

### 3.6.3 SMS Notifications — *(5 stories)*

- [[US-3.6.3-01-send-sms-for-critical-updates|US-3.6.3-01 Send SMS for critical updates]]
- [[US-3.6.3-02-allow-users-to-opt-in-sms|US-3.6.3-02 Allow users to opt in to SMS]]
- [[US-3.6.3-03-limit-sms-notification-frequency|US-3.6.3-03 Limit SMS notification frequency]]
- [[US-3.6.3-04-track-sms-delivery-status|US-3.6.3-04 Track SMS delivery status]]
- [[US-3.6.3-05-ensure-sms-compliance|US-3.6.3-05 Ensure SMS compliance]]

## 3.7 — Content Management

### 3.7.1 News and Updates — *(7 stories)*

- [[US-3.7.1-01-publish-news-article|US-3.7.1-01 Publish news article]]
- [[US-3.7.1-02-schedule-news-publication|US-3.7.1-02 Schedule news publication]]
- [[US-3.7.1-03-categorize-and-tag-articles|US-3.7.1-03 Categorize and tag articles]]
- [[US-3.7.1-04-archive-and-unpublish-articles|US-3.7.1-04 Archive and unpublish articles]]
- [[US-3.7.1-05-browse-and-filter-news|US-3.7.1-05 Browse and filter news]]
- [[US-3.7.1-06-search-news-archive|US-3.7.1-06 Search news archive]]
- [[US-3.7.1-07-dashboard-personalization|US-3.7.1-07 Dashboard personalization]]

### 3.7.2 FAQ and Help Center — *(7 stories)*

- [[US-3.7.2-01-create-faq-entry|US-3.7.2-01 Create FAQ entry]]
- [[US-3.7.2-02-organize-help-by-topic-and-role|US-3.7.2-02 Organize help by topic and role]]
- [[US-3.7.2-03-search-help-content|US-3.7.2-03 Search help content]]
- [[US-3.7.2-04-context-sensitive-help|US-3.7.2-04 Context-sensitive help]]
- [[US-3.7.2-05-collect-help-feedback|US-3.7.2-05 Collect help feedback]]
- [[US-3.7.2-06-multimedia-help-content|US-3.7.2-06 Multimedia help content]]
- [[US-3.7.2-07-create-guided-tours|US-3.7.2-07 Create guided tours]]

## Story counts by SRS section

| Section | Title | Stories |
|---|---|---|
| 3.1 | User Management & Authentication | 24 |
| 3.2 | Job Posting & Management | 12 |
| 3.3 | AI-Driven Matching | 17 |
| 3.4 | External System Integration | 17 |
| 3.5 | Reporting & Analytics | 15 |
| 3.6 | Notification System | 18 |
| 3.7 | Content Management | 14 |
| **Total** | | **117** |

## Conventions

- **Story ID:** `US-{section}-{NN}` — `NN` is a 2-digit ordinal, unique within the SRS subsection.
- **Roles:** Job Seeker, Employer Owner, Employer Recruiter, Employer Admin, MoL Administrator, System Administrator, Data Analyst, Data Scientist, Content Editor, Marketing Administrator, Auditor, Visitor, Third-Party Portal, System (automated).
- **Statuses:** `draft` (current default) → `ready` → `in_progress` → `done`.
- **Priorities:** `must` (launch-critical) | `should` (important) | `could` (nice-to-have).
- **Acceptance Criteria:** Given/When/Then. Each story includes happy path + key error/edge cases.
- **Assumptions:** Every story documents the assumptions made where SRS detail was missing — these are the candidates for follow-up clarification.

## Next steps

Stories are in `draft` status. Recommended follow-ups in priority order: (1) review **assumptions** sections — they're the gaps where the SRS underspecifies; (2) reconcile any cross-story inconsistencies (notification cadences, RBAC roles, retention windows); (3) promote stories to `ready` once each subsection's owner signs off; (4) generate stories for SRS Sections 4–6 (interfaces, NFRs, other) when ready.
