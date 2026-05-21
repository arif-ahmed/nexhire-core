---
story_id: "US-3.2.4-02"
title: "Track job posting status change history and audit trail"
section_id: "3.2.4"
related_requirements: ["FR-69"]
related_stories: ["US-3.2.4-01", "US-3.2.1-03"]
role: "Employer Admin"
status: draft
priority: should
tags:
  - story
  - bc/job-posting
---

# US-3.2.4-02 — Track job posting status change history and audit trail

## Story
As an **Employer Admin**, I want to view a history of status changes for all job postings, so that I can audit the lifecycle of postings and ensure compliance with internal policies.

## Acceptance Criteria

**AC-01 — Status change history is visible per job**
- Given I have a job posting
- When I navigate to "View History" or similar option on the job detail page
- Then I see a chronological log of all status changes: date, time, previous status, new status, and user who made the change

**AC-02 — Status change history shows who made changes**
- Given I view the status history
- When I see each entry
- Then it includes the name or user ID of the employer or admin who triggered the change

**AC-03 — Automatic status changes are recorded**
- Given a job posting expires automatically
- When the deadline passes
- Then the status change to "expired" is recorded with a "System" or "Automated" notation and the expiration timestamp

**AC-04 — Can export audit trail**
- Given I want to report on job posting activity
- When I click "Export History" or similar
- Then I can download the status change log in CSV or PDF format

**AC-05 — Audit trail is immutable**
- Given historical status changes are recorded
- When the audit trail is created
- Then no user can edit or delete historical entries; the audit trail is append-only

**AC-06 — Timestamp is precise**
- Given a status change is recorded
- When I view the history
- Then each entry includes date, time (to seconds), and timezone information

**AC-07 — Full history is retained**
- Given a job posting is archived or deleted (soft delete)
- When I access historical records
- Then the complete status history remains accessible for audit purposes

## Assumptions
- Status change history is retained indefinitely (or per regulatory requirements—not specified in SRS).
- "User who made change" refers to the employer account or admin account; no deeper role/permission tracking is included.
- Automatic status changes (expiration) are attributed to "System" or a service account.
- Timestamp granularity is seconds (not milliseconds); timezone is stored (assuming UTC or employer timezone).
- Export format is CSV or PDF; specific fields in export not detailed—assuming: job ID, job title, timestamp, previous status, new status, user.
- Audit trail is internal to the employer/admin dashboard; it is not shared with job seekers.

## Source Requirements
- [[3_2_4_Job_Status_Tracking|3.2.4]] — FR-69

## Related Stories
- [[US-3.2.4-01-...|US-3.2.4-01 View and manage job status]]
- [[US-3.2.1-03-...|US-3.2.1-03 Manage posting visibility]]
