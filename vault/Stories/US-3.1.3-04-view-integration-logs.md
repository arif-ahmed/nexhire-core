---
story_id: "US-3.1.3-04"
title: "View integration logs and sync dashboard"
section_id: "3.1.3"
related_requirements: ["FR-27", "FR-28", "FR-32", "FR-33", "FR-36"]
related_stories: ["US-3.1.3-02-push-jobs-via-api"]
role: "Third-Party Portal"
status: draft
priority: should
tags:
  - story
  - bc/partner-integration
---

# US-3.1.3-04 — View integration logs and sync dashboard

## Story
As a **Third-Party Job Portal**, I want **to view submission logs, sync status, and integration usage statistics**, so that **I can monitor and troubleshoot my API integration and track performance**.

## Acceptance Criteria

**AC-01 — Submission logs display**
- Given I navigate to the "Submission Logs" section
- When the page loads
- Then I see a paginated list of all job submissions with timestamp, job title, status, and response code

**AC-02 — Log filtering and search**
- Given I view the submission logs
- When I filter by date range or search by job title/ID
- Then only matching logs are displayed

**AC-03 — Log detail view**
- Given I click on a log entry
- When the detail view opens
- Then I see: request payload, response code, error message (if failed), and processing time

**AC-04 — Success vs. failure indicators**
- Given I view submission logs
- When I look at status column
- Then successful submissions show "200 OK", failures show relevant error codes (400, 409, 429, etc.)

**AC-05 — Sync dashboard overview**
- Given I navigate to the "Sync Dashboard"
- When the page loads
- Then I see a summary of job statuses: synced, failed, pending, archived, with counts for each

**AC-06 — Individual job sync status**
- Given I view the sync dashboard
- When I view individual jobs
- Then I see: job ID, title, last sync status, last sync timestamp, and any error messages

**AC-07 — Usage statistics**
- Given I navigate to the "Usage Statistics" section
- When the page loads
- Then I see: total jobs submitted, jobs successfully synced, total applications/matches received, job views count

**AC-08 — Usage trends**
- Given I view usage statistics
- When I select a date range
- Then I see a chart or table showing trends over time (daily/weekly submissions, applications, matches)

**AC-09 — Audit trail for job changes**
- Given I query job history via logs
- When I view a job entry
- Then I see who created it (my portal name), when it was last modified, and all change history

**AC-10 — Admin audit logs (MoL view)**
- Given I am a MoL Administrator
- When I access the integration audit logs
- Then I can see which external portal created/modified each job and display this in the admin dashboard

## Assumptions
- Submission logs are retained for 90 days by default (configurable).
- Usage statistics are aggregated hourly and made available within 1–2 hours of activity.
- Job sync status tracks: accepted, failed, pending, synced, archived.
- Logs include: request payload, response status, processing time, error messages.
- Audit trail is immutable and available to both portal admin and MoL admin.
- Sync dashboard is read-only; status changes happen via API calls, not dashboard UI.

## Source Requirements
- [[3_1_3_Third_Party_Job_Portals_Registration_and_Integration|3.1.3]] — FR-27, FR-28, FR-32, FR-33, FR-36

## Related Stories
- [[US-3.1.3-02-push-jobs-via-api|US-3.1.3-02 Push jobs via API]]
