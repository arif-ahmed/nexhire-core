---
story_id: "US-3.2.1-01"
title: "Create a job posting with structured form"
section_id: "3.2.1"
related_requirements: ["FR-54", "FR-55"]
related_stories: ["US-3.2.1-02", "US-3.2.1-03", "US-3.2.4-01"]
role: "Employer Recruiter"
status: draft
priority: must
tags:
  - story
  - bc/job-posting
---

# US-3.2.1-01 — Create a job posting with structured form

## Story
As an **Employer Recruiter**, I want to create a job posting using a structured form with predefined fields, so that I can efficiently list new job opportunities with consistent, complete information.

## Acceptance Criteria

**AC-01 — Form displays all required fields**
- Given I am on the job creation page
- When the form loads
- Then I see all required fields: job title, summary, skills, contract type, education level, application deadline, work format, required languages, and optional job link

**AC-02 — Can input job title and summary**
- Given I am filling out the job creation form
- When I enter text in title and summary fields
- Then the text is accepted and retained on form submission

**AC-03 — Can add required skills with multiple input methods**
- Given I am on the job creation form
- When I add skills via file upload or copy/paste
- Then the system accepts and stores the skills list

**AC-04 — Can select contract type**
- Given I am filling the form
- When I click the contract type dropdown
- Then I see options: full-time, part-time, training, project-based; I can select one

**AC-05 — Can set application deadline with optional auto-close**
- Given I am setting the deadline
- When I select a date and optionally enable auto-close
- Then the deadline is saved with auto-close preference

**AC-06 — Can specify work format and location**
- Given I am on the work format section
- When I select Physical, Online, or Hybrid
- Then I can specify employment location if Physical is selected

**AC-07 — Can specify required languages and proficiency**
- Given I am filling language requirements
- When I add languages and proficiency levels
- Then the languages are saved for job matching

**AC-08 — Form validation prevents submission with incomplete required fields**
- Given I attempt to submit an incomplete form
- When required fields are missing
- Then the form displays validation errors and prevents submission

## Assumptions
- Contract type options are fixed: full-time, part-time, training, project-based.
- Work format is a single selection (not multi-select).
- Application deadline is a date field; auto-close is a boolean toggle.
- Language proficiency uses a standard scale (not defined in SRS—assuming Common European Framework or similar).
- File upload for skills has a reasonable file size limit (not specified—assuming 5MB).
- Gender and employee count fields mentioned in FR-55 are treated as optional custom fields; implementation details deferred.

## Source Requirements
- [[3_2_1_Job_Creation_and_Publishing|3.2.1]] — FR-54, FR-55

## Related Stories
- [[US-3.2.1-02-...|US-3.2.1-02 Standardize job data]]
- [[US-3.2.1-03-...|US-3.2.1-03 Manage job posting visibility]]
