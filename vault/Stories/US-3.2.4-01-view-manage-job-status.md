---
story_id: "US-3.2.4-01"
title: "View and manage job posting status"
section_id: "3.2.4"
related_requirements: ["FR-67", "FR-68"]
related_stories: ["US-3.2.4-02", "US-3.2.1-04"]
role: "Employer Recruiter"
status: draft
priority: must
tags:
  - story
  - bc/job-posting
---

# US-3.2.4-01 — View and manage job posting status

## Story
As an **Employer Recruiter**, I want to view and manage the status of my job postings (draft, active, paused, expired, archived), so that I can control which jobs are recruiting and track the lifecycle of my postings.

## Acceptance Criteria

**AC-01 — Dashboard displays job postings with status**
- Given I am on my job postings dashboard
- When the page loads
- Then I see all my job postings listed with their current status: draft, active, paused, expired, or archived

**AC-02 — Can transition job from draft to active**
- Given I have a draft job posting
- When I click "Publish" or "Activate"
- Then the job status changes to "active" and the job becomes searchable by job seekers

**AC-03 — Can pause an active job**
- Given I have an active job posting
- When I click "Pause"
- Then the job status becomes "paused"; it is no longer visible in search results but is not deleted

**AC-04 — Can resume a paused job**
- Given I have a paused job posting
- When I click "Resume" or "Activate"
- Then the job status returns to "active" and is again searchable

**AC-05 — Expired jobs are automatically marked**
- Given a job posting has reached its application deadline
- When the deadline passes
- Then the status is automatically set to "expired"

**AC-06 — Can archive a job posting**
- Given I have an expired or completed job posting
- When I click "Archive"
- Then the job is moved to "archived" status; it is no longer visible in my active list but remains in records

**AC-07 — Can filter postings by status**
- Given I am viewing my job postings
- When I use the status filter
- Then I can view only jobs with a specific status (e.g., "Show only active jobs")

**AC-08 — Status changes are immediately reflected**
- Given I change a job status
- When the action is saved
- Then the UI updates immediately and the job's visibility on the job seeker side is updated (or updated within seconds)

## Assumptions
- Status transitions follow the lifecycle: draft → active ⇄ paused → expired → archived. Reverse transitions (e.g., archived back to active) are not supported; renewal creates a new posting.
- "Paused" is a temporary hold; pause duration is indefinite until explicitly resumed or expired.
- Status changes are triggered by the employer; only expiration is automatic.
- Archiving is a soft delete (data retained for audit); jobs cannot be un-archived.

## Source Requirements
- [[3_2_4_Job_Status_Tracking|3.2.4]] — FR-67, FR-68

## Related Stories
- [[US-3.2.4-02-...|US-3.2.4-02 Track status changes]]
- [[US-3.2.1-04-...|US-3.2.1-04 Manage expiration and renewal]]
