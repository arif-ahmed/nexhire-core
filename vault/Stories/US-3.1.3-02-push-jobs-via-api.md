---
story_id: "US-3.1.3-02"
title: "Push jobs via API"
section_id: "3.1.3"
related_requirements: ["FR-24", "FR-25", "FR-29", "FR-30", "FR-31"]
related_stories: ["US-3.1.3-01-register-third-party-portal", "US-3.1.3-03-configure-data-mapping"]
role: "Third-Party Portal"
status: draft
priority: should
tags:
  - story
  - bc/partner-integration
---

# US-3.1.3-02 — Push jobs via API

## Story
As a **Third-Party Job Portal**, I want **to submit job postings via a secure REST API and receive confirmation with unique job IDs**, so that **my jobs are synced to the platform and visible to job seekers**.

## Acceptance Criteria

**AC-01 — Job push via POST endpoint**
- Given I have a valid API key and properly formatted job payload
- When I POST to `/api/v1/jobs/push` with job details (title, description, location, salary, requirements)
- Then the job is accepted and I receive a 201 response with a unique job ID

**AC-02 — Job ID confirmation**
- Given a job is successfully pushed
- When the response is returned
- Then I receive: `{ "status": "success", "job_id": "jp_..." }`

**AC-03 — Data validation on push**
- Given I submit a job with missing or invalid fields (e.g., missing title)
- When the API validates the payload
- Then I receive error `E-API-VALIDATION-ERROR` with field-level error messages

**AC-04 — Automatic platform tagging**
- Given I push a job successfully
- When the job is created on the platform
- Then it is automatically tagged with source attribution (e.g., "Source: samplejobsite.ps") and includes a backlink to the original job

**AC-05 — Source attribution option**
- Given I have configured my integration
- When a job is posted
- Then the source platform name appears publicly (if I enabled public attribution) or only in admin logs (if disabled)

**AC-06 — Job update via API**
- Given I have posted a job with job_id "jp_123"
- When I PATCH `/api/v1/jobs/{job_id}` with updated details (deadline, description, status)
- Then the job is updated on the platform and I receive a 200 response

**AC-07 — Job status changes via API**
- Given I want to close or deactivate a job early
- When I PATCH the job with status="closed" or status="deactivated"
- Then the job is immediately hidden from search and applications are blocked

**AC-08 — Duplicate job handling**
- Given I attempt to push a job with identical details twice
- When the second push occurs
- Then the system detects the duplicate and returns error `E-API-DUPLICATE-JOB` with guidance

**AC-09 — Invalid API key**
- Given I use an expired, revoked, or invalid API key
- When I submit a job push request
- Then I receive error `E-API-UNAUTHORIZED` (401)

**AC-10 — Rate limit exceeded**
- Given I have exceeded my configured rate limit
- When I submit a push request
- Then I receive error `E-API-RATE-LIMITED` (429)

## Assumptions
- Job push API uses REST POST/PATCH endpoints.
- Job payload schema is defined and validated server-side.
- Unique job_id is generated and returned immediately upon successful push.
- Source tagging is automatic; platform name is stored in job metadata.
- Job updates are synced within 30 seconds of PATCH request.
- Duplicate detection is based on: source portal, job title, company, location (configurable).
- API uses Bearer token authentication via Authorization header.

## Source Requirements
- [[3_1_3_Third_Party_Job_Portals_Registration_and_Integration|3.1.3]] — FR-24, FR-25, FR-29, FR-30, FR-31

## Related Stories
- [[US-3.1.3-01-register-third-party-portal|US-3.1.3-01 Register third-party job portal]]
- [[US-3.1.3-03-configure-data-mapping|US-3.1.3-03 Configure data mapping]]
