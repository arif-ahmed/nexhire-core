---
story_id: "US-3.1.1-01"
title: "Self-register as a job seeker"
section_id: "3.1.1"
related_requirements: ["FR-01", "FR-02"]
related_stories: ["US-3.1.1-02-activate-account-via-otp", "US-3.1.5-01-login-with-credentials"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/seeker-profile
---





# US-3.1.1-01 — Self-register as a job seeker

## Story
As a **Job Seeker**, I want **to register an account using my mobile number and email**, so that **I can apply to jobs and build a profile**.

## Acceptance Criteria

**AC-01 — Happy path**
- Given I am a new visitor on the registration page
- When I submit Level 1 fields (first name, last name, email, mobile number, password, gender) with unique email and mobile number
- Then my account is created in `pending_activation` state and an SMS OTP is dispatched within 10 seconds

**AC-02 — Duplicate identifier**
- Given an account already exists with my mobile number
- When I attempt to register
- Then I see error `E-REG-DUPLICATE` with a prompt to recover the existing account

**AC-03 — Duplicate email**
- Given an account already exists with my email
- When I attempt to register
- Then I see error `E-REG-DUPLICATE` and am offered a password reset option

**AC-04 — Invalid password**
- Given I submit a password that does not meet policy (less than 8 chars, missing uppercase/lowercase/digit)
- When I attempt to register
- Then validation fails with error `E-REG-INVALID-PASSWORD` and password policy is displayed inline

**AC-05 — Invalid mobile number format**
- Given I submit an invalid mobile number (not E.164 format or not Bangladesh +880)
- When I attempt to register
- Then I see error `E-REG-INVALID-MOBILE` with formatting guidance

**AC-06 — Invalid email format**
- Given I submit an invalid email (not RFC 5322)
- When I attempt to register
- Then I see error `E-REG-INVALID-EMAIL`

**AC-07 — Rate limiting**
- Given I attempt to register 6 times from the same IP in one hour
- When I submit the 6th registration request
- Then the request is throttled and I see error `E-REG-RATE-LIMITED`

**AC-08 — Breached password rejection**
- Given I submit a password that appears in the HIBP top-10k breached password list
- When I attempt to register
- Then validation fails with error `E-REG-PASSWORD-BREACHED` and I'm prompted to choose a different password

## Assumptions
- SMS OTP is dispatched via a configured SMS gateway (default: Bangladesh +880 region).
- Email validation is RFC 5322 standard.
- Mobile numbers must be in E.164 format and validated against Bangladesh format by default.
- Breached password checking uses HIBP-style list lookup.
- Account creation is transactional; if SMS dispatch fails, account is rolled back.
- Password hashing uses Argon2id.

## Source Requirements
- [[3_1_1_Job_Seeker_Registration_and_Profile_Management|3.1.1]] — FR-01, FR-02

## Related Stories
- [[US-3.1.1-02-activate-account-via-otp|US-3.1.1-02 Activate account via OTP]]
- [[US-3.1.5-01-login-with-credentials|US-3.1.5-01 Login with credentials]]
