---
story_id: "US-3.1.1-02"
title: "Activate account via OTP"
section_id: "3.1.1"
related_requirements: ["FR-02"]
related_stories: ["US-3.1.1-01-self-register-as-job-seeker", "US-3.1.5-01-login-with-credentials"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/seeker-profile
---

# US-3.1.1-02 — Activate account via OTP

## Story
As a **Job Seeker** with a pending activation account, I want **to activate my account using the SMS OTP sent to my mobile**, so that **I can log in and start using the platform**.

## Acceptance Criteria

**AC-01 — Happy path**
- Given I have a `pending_activation` account and have received an SMS OTP
- When I submit the correct 6-digit OTP within 5 minutes
- Then my account transitions to `active` status and I receive a JWT access token

**AC-02 — OTP expired**
- Given my OTP was sent more than 5 minutes ago
- When I attempt to submit it
- Then I see error `E-OTP-EXPIRED` and am offered to request a new OTP

**AC-03 — Incorrect OTP (first or second attempt)**
- Given I submit an incorrect OTP
- When the system validates it (attempt 1 or 2)
- Then I see error `E-OTP-INVALID` with remaining attempts displayed

**AC-04 — Account locked after 3 failed attempts**
- Given I have made 3 failed OTP submission attempts
- When I attempt to submit a 4th OTP
- Then the account is locked for 15 minutes with error `E-OTP-LOCKED`

**AC-05 — Resend OTP with cooldown**
- Given I have just requested an OTP
- When I immediately request another OTP (before 60 seconds)
- Then I see error `E-OTP-COOLDOWN` with time remaining

**AC-06 — Resend OTP allowed after cooldown**
- Given I have waited 60 seconds since my last OTP request
- When I request a new OTP
- Then a fresh 6-digit OTP is generated and sent via SMS within 10 seconds

**AC-07 — Account locked; unlock after 15 minutes**
- Given my account is locked after 3 failed attempts
- When 15 minutes have passed
- Then the account is automatically unlocked and I can request a new OTP

**AC-08 — Rate limiting on OTP send**
- Given I request OTP 4 times in one hour for the same mobile number
- When I attempt the 4th send request
- Then the request is throttled with error `E-OTP-RATE-LIMITED`

## Assumptions
- OTP is 6 digits, numeric only.
- OTP validity window is 5 minutes; attempt lockout is 15 minutes.
- Each OTP request resets the attempt counter.
- Rate limit on OTP send is 3 per hour per mobile number.
- JWT token is issued immediately upon successful activation.
- Failed attempts are logged for security audit.

## Source Requirements
- [[3_1_1_Job_Seeker_Registration_and_Profile_Management|3.1.1]] — FR-02

## Related Stories
- [[US-3.1.1-01-self-register-as-job-seeker|US-3.1.1-01 Self-register as a job seeker]]
- [[US-3.1.5-01-login-with-credentials|US-3.1.5-01 Login with credentials]]
