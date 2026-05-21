---
story_id: "US-3.2.1-02"
title: "Standardize job data to Schema.org JobPosting"
section_id: "3.2.1"
related_requirements: ["FR-56"]
related_stories: ["US-3.2.1-01", "US-3.3.1-01"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/job-posting
---

# US-3.2.1-02 — Standardize job data to Schema.org JobPosting

## Story
As a **System**, I want to validate and transform all job posting data to comply with Schema.org JobPosting standard, so that job data is semantically compatible and can be properly indexed and matched.

## Acceptance Criteria

**AC-01 — Input validation ensures compliance**
- Given a job posting is being created or edited
- When form data is submitted
- Then the system validates all fields against Schema.org JobPosting requirements and rejects non-compliant data

**AC-02 — System maps form fields to Schema.org properties**
- Given job posting data is submitted
- When the system processes it
- Then job title maps to baseSalary, summary to description, contract type to employmentType, education level to qualifications, etc.

**AC-03 — Standardized categories and skills**
- Given an employer selects skills or job categories
- When they are stored
- Then they are normalized to a canonical list (e.g., taxonomy of skills and industry categories)

**AC-04 — Structured data integrity maintained**
- Given a job posting is stored
- When it is retrieved or exported
- Then it contains valid Schema.org JobPosting properties with no missing required fields

**AC-05 — Data compatibility for matching**
- Given standardized job data
- When the AI matching engine queries jobs
- Then it receives structured, consistently formatted data for accurate matching

## Assumptions
- Schema.org JobPosting standard is the normative source for property mappings.
- Canonical skill and category lists exist or will be maintained separately (SRS does not detail these).
- Form fields that don't map directly to Schema.org are stored as custom extensions or metadata.
- Validation logic is implemented server-side; client-side hints are provided for UX.

## Source Requirements
- [[3_2_1_Job_Creation_and_Publishing|3.2.1]] — FR-56

## Related Stories
- [[US-3.2.1-01-...|US-3.2.1-01 Create job posting]]
- [[3_3_1_A_Vector_Based_Skills_Based_and_Behavior_Matching_Algorithms|3.3.1 Matching algorithms]]
