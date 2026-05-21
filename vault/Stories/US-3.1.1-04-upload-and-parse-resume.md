---
story_id: "US-3.1.1-04"
title: "Upload and parse resume"
section_id: "3.1.1"
related_requirements: ["FR-04", "FR-05"]
related_stories: ["US-3.1.1-03-complete-level-2-profile"]
role: "Job Seeker"
status: draft
priority: should
tags:
  - story
  - bc/seeker-profile
---

# US-3.1.1-04 — Upload and parse resume

## Story
As a **Job Seeker**, I want **to upload my resume/CV and have the system extract information and populate my profile**, so that **I can quickly build my profile without manually entering all details**.

## Acceptance Criteria

**AC-01 — Supported formats and size**
- Given I am on the resume upload page
- When I upload a file in PDF, DOCX, or TXT format with size ≤ 5 MB
- Then the file is accepted for processing

**AC-02 — Rejected formats and oversized files**
- Given I attempt to upload a file in unsupported format (e.g., XLS, PPT) or > 5 MB
- When I submit the upload
- Then I see error `E-UPLOAD-INVALID-FORMAT` or `E-UPLOAD-SIZE-EXCEEDED`

**AC-03 — Virus scan on upload**
- Given I upload a resume file
- When the file is scanned by the virus scanner
- Then infected files are rejected with error `E-UPLOAD-VIRUS` and quarantined

**AC-04 — Parser submission and timeout**
- Given a clean resume file is uploaded
- When the system submits it to the third-party parser API
- Then if no response within 30 seconds, the parser is marked as failed

**AC-05 — Parser failure graceful fallback**
- Given the resume parser fails or times out
- When the system detects the failure
- Then I am notified and offered to manually fill in profile fields instead (no blocking)

**AC-06 — Successful parse and review screen**
- Given the parser succeeds and returns structured JSON
- When the review screen displays extracted fields (education, experience, skills, salary)
- Then I can review and confirm which fields to merge into my profile

**AC-07 — Selective field confirmation**
- Given extracted fields are displayed on the review screen
- When I select/deselect fields and click "Confirm"
- Then only selected fields are merged into my profile entities

**AC-08 — Resume stored in document table**
- Given I upload a resume
- When processing completes (success or fallback)
- Then the file is stored with document kind = "resume" and linked to my profile

## Assumptions
- Third-party parser (e.g., Affinda, Sovren, RChilli) is configured and available.
- Parser returns JSON with structured fields: education, experience, skills, salary.
- Virus scanning is performed before parsing (e.g., ClamAV or cloud antivirus).
- Maximum 5 MB file size per resume upload.
- One resume per profile at a time (new uploads replace old ones).
- Parser timeout is 30 seconds; failures do not block user flow.

## Source Requirements
- [[3_1_1_Job_Seeker_Registration_and_Profile_Management|3.1.1]] — FR-04, FR-05

## Related Stories
- [[US-3.1.1-03-complete-level-2-profile|US-3.1.1-03 Complete Level 2 profile]]
