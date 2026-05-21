---
story_id: "US-3.1.4-03"
title: "Manage and moderate job postings"
section_id: "3.1.4"
related_requirements: ["FR-45"]
related_stories: ["US-3.1.4-01-manage-user-accounts"]
role: "MoL Administrator"
status: draft
priority: should
tags:
  - story
  - bc/identity
  - topic/admin
---

# US-3.1.4-03 — Manage and moderate job postings

## Story
As a **MoL Administrator**, I want **to view, monitor, suspend, or remove job postings that violate platform policies**, so that **I can maintain a quality job marketplace and protect job seekers**.

## Acceptance Criteria

**AC-01 — View all job postings**
- Given I navigate to Job Moderation
- When the page loads
- Then I see a list of all active and inactive job postings with status (published, draft, suspended, removed, archived)

**AC-02 — Filter job postings**
- Given I view the job list
- When I apply filters (employer, status, date posted, industry, location)
- Then only matching jobs are displayed

**AC-03 — View job details**
- Given I click on a job posting
- When the detail page opens
- Then I see full job details, employer info, posted date, application count, and any reported issues

**AC-04 — View job posting history**
- Given I view a job detail
- When I click "History"
- Then I see all edits made to the posting, including who edited it and when

**AC-05 — Suspend job posting**
- Given I identify a job that violates policy
- When I click "Suspend"
- Then the job is hidden from search and applications are blocked; the employer is notified

**AC-06 — Remove job posting**
- Given a job violates serious platform policies
- When I click "Remove"
- Then the job is deleted from the system and employer is notified with reason

**AC-07 — Log suspension/removal reason**
- Given I suspend or remove a job
- When I provide a reason
- Then it is recorded in the audit log for compliance and employer reference

**AC-08 — Search job postings**
- Given I search for jobs by keyword, employer, or job ID
- When I submit the search
- Then matching jobs are returned

**AC-09 — View job metrics**
- Given I view a job posting
- When I check the metrics section
- Then I see: applications count, matches count, views count, and days posted

**AC-10 — Restore suspended job**
- Given a job was suspended in error
- When I review the suspension and click "Restore"
- Then the job is restored to published status and becomes searchable again

## Assumptions
- Job moderation is a core admin responsibility.
- Suspension hides the job temporarily; removal is permanent.
- All suspension/removal actions are logged with reason and admin ID.
- Employers are notified of suspensions and removals via email.
- Restoration requires explicit admin action; suspended jobs do not auto-restore.
- Metrics include: applications, matches, views, posted date, last edited date.

## Source Requirements
- [[3_1_4_Administrator_User_Management|3.1.4]] — FR-45

## Related Stories
- [[US-3.1.4-01-manage-user-accounts|US-3.1.4-01 Manage user accounts]]
