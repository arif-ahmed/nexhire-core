---
story_id: "US-3.1.2-01"
title: "Register employer account"
section_id: "3.1.2"
related_requirements: ["FR-13", "FR-14"]
related_stories: ["US-3.1.2-02-verify-employer-account", "US-3.1.5-01-login-with-credentials"]
role: "Employer Owner"
status: draft
priority: must
tags:
  - story
  - bc/employer-profile
---

# US-3.1.2-01 — Register employer account

## Story
As an **Employer Owner**, I want **to register my company through a stepwise process**, so that **I can access the job posting and recruitment features**.

## Acceptance Criteria

**AC-01 — Level 1 registration**
- Given I am a new employer on the registration page
- When I submit Level 1 fields (company name, email, mobile number, company ID/registration number)
- Then my employer account is created in `pending_activation` state and an SMS OTP is dispatched within 10 seconds

**AC-02 — Duplicate company identifier**
- Given an employer account already exists with my company ID or registration number
- When I attempt to register
- Then I see error `E-REG-DUPLICATE` with a prompt to recover the existing account

**AC-03 — Duplicate email or mobile**
- Given an account already exists with my email or mobile number
- When I attempt to register
- Then I see error `E-REG-DUPLICATE`

**AC-04 — Invalid mobile format**
- Given I submit an invalid mobile number
- When I attempt to register
- Then I see error `E-REG-INVALID-MOBILE` with formatting guidance

**AC-05 — Invalid email format**
- Given I submit an invalid email
- When I attempt to register
- Then I see error `E-REG-INVALID-EMAIL`

**AC-06 — Account activation via OTP**
- Given I submit Level 1 and receive an OTP
- When I submit the correct 6-digit OTP within 5 minutes
- Then my account transitions to `pending_verification` and I can proceed to Level 2

**AC-07 — Level 2: company details**
- Given my account is activated
- When I submit Level 2 (website, industry, company size, address, company description)
- Then the details are saved

**AC-08 — Level 2: supplementary documents**
- Given I am completing Level 2
- When I upload registration certificate or other documents (PDF, JPG, PNG ≤ 10 MB)
- Then files are scanned and stored with my account

**AC-09 — Rate limiting**
- Given I attempt to register 6 times from the same IP in one hour
- When I submit the 6th registration
- Then the request is throttled with error `E-REG-RATE-LIMITED`

## Assumptions
- Level 1 is required; Level 2 is gated until after OTP activation.
- Employer registration uses stepwise process (Level 1 → Level 2).
- Registration number and company ID are validated for uniqueness.
- Mobile OTP is sent via SMS gateway (default: Bangladesh +880).
- Account transitions from pending_activation → pending_verification after OTP success.
- Document upload supports PDF, JPG, PNG, max 10 MB per file.

## Source Requirements
- [[3_1_2_Employer_Registration_and_Profile_Management|3.1.2]] — FR-13, FR-14

## Related Stories
- [[US-3.1.2-02-verify-employer-account|US-3.1.2-02 Verify employer account]]
- [[US-3.1.5-01-login-with-credentials|US-3.1.5-01 Login with credentials]]
