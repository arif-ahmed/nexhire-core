---
story_id: "US-3.4.2-01"
title: "Verify Educational Credentials"
section_id: "3.4.2"
related_requirements: ["FR-106"]
related_stories: ["US-3.4.2-02"]
role: "System"
status: draft
priority: should
tags:
  - story
  - bc/government
---

# US-3.4.2-01 — Verify Educational Credentials

## Story
As a **System**, I want to **verify job seeker educational credentials against government/institutional databases**, so that **employers and the platform can trust the authenticity of claimed qualifications**.

## Acceptance Criteria

**AC-01 — Initiate credential verification**
- Given a job seeker submits educational credentials (institution, degree, graduation date)
- When they request verification
- Then the system initiates a background check with the linked educational institution database

**AC-02 — Receive verification result**
- Given a verification request is sent to an institution database
- When the institution responds
- Then the system records the result: verified (match found), unverified (no match), or error (database unavailable)

**AC-03 — Handle missing institutional integration**
- Given an institution is not in the integrated database list
- When a seeker provides credentials from that institution
- Then the system returns "Unable to verify" and prompts the seeker to upload official documents as evidence

**AC-04 — Track verification audit trail**
- Given a verification occurs
- When the process completes
- Then the system logs: seeker ID, institution ID, verification timestamp, result, and database source

## Assumptions
- Educational institutions provide API endpoints for credential verification (or manual verification process)
- Verification requests include: institution name, degree type, graduation date, seeker name
- Institutions return: verified/unverified (no candidate details beyond verification status)
- Verification takes 1–5 business days (synchronous API calls may timeout; async polling required)
- Verification results are cached for 12 months (credential does not change frequently)
- Privacy regulations (GDPR, FERPA) restrict what data can be shared; system respects consent

## Source Requirements
- [[3_4_2_Government_Database_Integration|3.4.2]] — FR-106

## Related Stories
- [[US-3.4.2-02-verify-identity-via-government-system|US-3.4.2-02 — Verify Identity via Government System]]
