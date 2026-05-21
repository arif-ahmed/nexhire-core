---
story_id: "US-3.1.5-03"
title: "Reset password"
section_id: "3.1.5"
related_requirements: ["FR-49", "FR-50"]
related_stories: ["US-3.1.5-01-login-with-credentials"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/identity
  - topic/security
---

# US-3.1.5-03 — Reset password

## Story
As a **User**, I want **to reset my password if I forget it**, so that **I can regain access to my account**.

## Acceptance Criteria

**AC-01 — Initiate password reset**
- Given I click "Forgot Password" on the login page
- When I submit my email or mobile
- Then an SMS OTP is dispatched to my registered mobile within 10 seconds

**AC-02 — OTP verification**
- Given I receive the password reset OTP
- When I submit the correct 6-digit OTP within 5 minutes
- Then the OTP is verified and I proceed to password change screen

**AC-03 — OTP expiration and retry**
- Given my password reset OTP expires after 5 minutes
- When I attempt to submit an expired OTP
- Then I see error `E-OTP-EXPIRED` and am offered to request a new OTP

**AC-04 — Failed OTP attempts**
- Given I submit an incorrect OTP
- When the 3rd incorrect attempt occurs
- Then my password reset session is locked for 15 minutes

**AC-05 — New password validation**
- Given I submit a new password after OTP verification
- When the password does not meet policy (< 8 chars, missing uppercase/lowercase/digit)
- Then validation fails with error `E-RESET-INVALID-PASSWORD` and policy is displayed

**AC-06 — Breached password rejection**
- Given I submit a new password in the HIBP breached list
- When validation runs
- Then I see error `E-RESET-PASSWORD-BREACHED` and must choose a different password

**AC-07 — Password change confirmation**
- Given I submit valid new password
- When the change is confirmed
- Then I see success message and am redirected to login page

**AC-08 — Session invalidation**
- Given my password has been changed
- When the change is saved
- Then all existing sessions are invalidated; I must log in again with the new password

**AC-09 — Rate limiting on reset requests**
- Given I request a password reset
- When I request a 4th reset from the same email/mobile in one hour
- Then the request is throttled with error `E-RESET-RATE-LIMITED`

**AC-10 — Security audit log**
- Given I complete a password reset
- When the reset is confirmed
- Then a security event is logged with timestamp and IP address

## Assumptions
- Password reset flow: request → SMS OTP → verify OTP → set new password → invalidate sessions.
- OTP validity: 5 minutes; max 3 failed attempts, then 15-minute lockout.
- New password must not match the previous 3 passwords (prevent reuse).
- Rate limit: 3 password reset requests per email/mobile per hour.
- All sessions are invalidated after password change (user must re-login).
- Password reset does not require prior authentication (important for account recovery).

## Source Requirements
- [[3_1_5_Authentication_and_Authorization|3.1.5]] — FR-49, FR-50

## Related Stories
- [[US-3.1.5-01-login-with-credentials|US-3.1.5-01 Login with credentials]]
