---
story_id: "US-3.2.1-04"
title: "Manage job posting expiration and renewal"
section_id: "3.2.1"
related_requirements: ["FR-58"]
related_stories: ["US-3.2.1-03", "US-3.2.4-01", "US-3.2.4-02"]
role: "Employer Recruiter"
status: draft
priority: should
tags:
  - story
  - bc/job-posting
---

# US-3.2.1-04 — Manage job posting expiration and renewal

## Story
As an **Employer Recruiter**, I want to manage job posting expiration and renew expired postings, so that I can keep job listings active with minimal effort and ensure job postings do not remain stale on the platform.

## Acceptance Criteria

**AC-01 — Job postings expire after application deadline**
- Given a job posting has an application deadline
- When the deadline passes
- Then the job posting is automatically marked as "expired" and is no longer visible to job seekers in search results

**AC-02 — Employer receives expiration notification**
- Given a job posting is approaching expiration
- When the deadline is within a configurable threshold (e.g., 7 days)
- Then the employer receives a notification prompting them to renew or extend

**AC-03 — Can renew an expired job posting**
- Given I have an expired job posting
- When I click "Renew" from my job posting dashboard
- Then the system creates a new active posting with the same details; I can optionally edit details before re-publishing

**AC-04 — Renewed posting gets new ID and history**
- Given I renew an expired posting
- When the renewal is processed
- Then the system creates a new job posting record with a new ID; the original posting remains in archive for audit purposes

**AC-05 — Renewal extends deadline and resets status**
- Given I renew a posting
- When renewal is complete
- Then the new posting has a fresh application deadline and status is set to "active" (or "draft" if user prefers to review first)

**AC-06 — Bulk renewal available for multiple postings**
- Given I have multiple expired postings
- When I select them on my dashboard
- Then I can renew all selected postings in one action (with optional detail edits per posting or bulk edit)

## Assumptions
- Expiration is automatic based on application deadline; no manual "mark as expired" action exists.
- Notification threshold is configurable (default: 7 days before deadline). Configuration location not specified—assuming employer settings or admin config.
- Renewal is not automatic; employer must explicitly trigger it.
- Renewed postings can have the same or updated deadline; logic for default renewal period not specified—assuming same duration as original or configurable default.
- "Bulk renewal" applies same deadline calculation to all selected postings unless individually overridden.

## Source Requirements
- [[3_2_1_Job_Creation_and_Publishing|3.2.1]] — FR-58

## Related Stories
- [[US-3.2.1-03-...|US-3.2.1-03 Manage posting visibility]]
- [[US-3.2.4-01-...|US-3.2.4-01 View and manage job status]]
- [[US-3.2.4-02-...|US-3.2.4-02 Track status changes]]
