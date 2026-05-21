---
story_id: "US-3.1.2-02"
title: "Verify employer account"
section_id: "3.1.2"
related_requirements: ["FR-15", "FR-16", "FR-17"]
related_stories: ["US-3.1.2-01-register-employer-account", "US-3.1.2-03-manage-employer-profile"]
role: "Employer Owner"
status: draft
priority: must
tags:
  - story
  - bc/employer-profile
---

# US-3.1.2-02 — Verify employer account

## Story
As an **Employer Owner**, I want **my employer account to be verified through automatic or manual verification**, so that **I can display a "Verified Employer" badge and build trust with job seekers**.

## Acceptance Criteria

**AC-01 — Automatic verification attempt**
- Given I complete employer registration (Level 1 and Level 2)
- When the verification process runs
- Then the system attempts automatic verification using government database lookup (registration number, VAT number, registered mobile)

**AC-02 — Auto-verify success**
- Given automatic verification checks pass
- When a match is found in the government database
- Then my account transitions to `verified` and I can see the verified badge

**AC-03 — Auto-verify failure**
- Given automatic verification checks fail
- When no match is found or data mismatches
- Then my account transitions to `pending_manual_verification` and MoL is notified

**AC-04 — Manual verification by MoL**
- Given my account is in pending_manual_verification
- When a MoL Administrator reviews my submission and contact information
- Then they can approve (transition to `verified`) or reject (transition to `rejected`)

**AC-05 — Verified badge display**
- Given my account is verified
- When my profile is displayed to job seekers
- Then a "Verified Employer" badge appears next to my company name

**AC-06 — Unverified account limitations**
- Given my account is pending_manual_verification or rejected
- When I attempt to post jobs or appear in search
- Then I see a message that I must complete verification first

**AC-07 — Verification status visibility**
- Given my account is in any verification state
- When I log in to my employer dashboard
- Then I see my current verification status and next steps

**AC-08 — Resubmit for manual verification**
- Given my account was rejected
- When I update my company information
- Then I can resubmit for manual verification

## Assumptions
- Automatic verification uses Bangladesh government database APIs (registration number, VAT number, mobile).
- Manual verification is handled by MoL Administrator role.
- Verification can happen at Level 1 completion or after Level 2 submission.
- Verified status is required for public job posting (but not for account creation).
- Verified badge increases trust and may improve job seeker matching.
- Verification process can take 1–3 days for manual review.

## Source Requirements
- [[3_1_2_Employer_Registration_and_Profile_Management|3.1.2]] — FR-15, FR-16, FR-17

## Related Stories
- [[US-3.1.2-01-register-employer-account|US-3.1.2-01 Register employer account]]
- [[US-3.1.2-03-manage-employer-profile|US-3.1.2-03 Manage employer profile]]
