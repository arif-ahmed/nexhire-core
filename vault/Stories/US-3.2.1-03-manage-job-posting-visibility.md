---
story_id: "US-3.2.1-03"
title: "Manage job posting visibility and details"
section_id: "3.2.1"
related_requirements: ["FR-57"]
related_stories: ["US-3.2.1-01", "US-3.2.1-04", "US-3.2.4-02"]
role: "Employer Recruiter"
status: draft
priority: must
tags:
  - story
  - bc/job-posting
---

# US-3.2.1-03 — Manage job posting visibility and details

## Story
As an **Employer Recruiter**, I want to edit job postings, extend application deadlines, and control visibility (public, private, or targeted), so that I can adjust job listings to match changing business needs and recruit effectively.

## Acceptance Criteria

**AC-01 — Can edit job posting details**
- Given I have created a job posting
- When I navigate to edit it
- Then I can modify job title, summary, skills, contract type, location, and other editable fields; changes are saved and take effect immediately

**AC-02 — Can extend application deadline**
- Given a job posting has an existing deadline
- When I edit the posting
- Then I can extend the deadline to a later date; the new deadline is persisted and communicated to candidates (if applicable)

**AC-03 — Can set visibility to public**
- Given I am editing visibility settings
- When I select "Public"
- Then the job is searchable and visible to all job seekers on the platform

**AC-04 — Can set visibility to private**
- Given I am editing visibility settings
- When I select "Private"
- Then the job is not listed in search results or public job boards; only invited candidates can see it

**AC-05 — Can set visibility to targeted**
- Given I am editing visibility settings
- When I select "Targeted"
- Then I can specify which job seekers, groups, or criteria can access this job; targeted users see it in search results

**AC-06 — Visibility changes are reflected immediately**
- Given I change visibility
- When the change is saved
- Then the job's searchability and discoverability update instantly

**AC-07 — Edit history is maintained**
- Given I make edits to a job posting
- When I view the posting details
- Then I can see when and what fields were last modified (for audit/compliance)

## Assumptions
- "Targeted" visibility requires a targeting mechanism (criteria/user groups); exact targeting logic is deferred to implementation.
- Visibility changes do not affect historical data or candidate interactions already made.
- "Edit" covers changes to job details but does NOT allow removal or permanent deletion (per prohibited actions).
- Deadline extensions do not retroactively notify candidates; notifications are part of a separate system feature.

## Source Requirements
- [[3_2_1_Job_Creation_and_Publishing|3.2.1]] — FR-57

## Related Stories
- [[US-3.2.1-01-...|US-3.2.1-01 Create job posting]]
- [[US-3.2.1-04-...|US-3.2.1-04 Manage posting expiration and renewal]]
- [[US-3.2.4-02-...|US-3.2.4-02 Track job status changes]]
