---
story_id: "US-3.1.5-01"
title: "Login with credentials"
section_id: "3.1.5"
related_requirements: ["FR-49", "FR-50"]
related_stories: ["US-3.1.5-02-multi-factor-authentication", "US-3.1.5-03-password-reset"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/identity
  - topic/security
  - topic/identity
---

# US-3.1.5-01 — Login with credentials

## Story
As a **User** (Job Seeker, Employer, or Admin), I want **to log in using my email/mobile and password**, so that **I can access my account and use the platform**.

## Acceptance Criteria

**AC-01 — Successful login**
- Given I am on the login page
- When I submit valid email/mobile and password
- Then I am authenticated and receive a JWT access token with 1-hour validity

**AC-02 — Invalid password**
- Given I submit incorrect password
- When I attempt login
- Then I see error `E-LOGIN-INVALID-CREDENTIALS` without specifying if email or password is wrong

**AC-03 — Non-existent user**
- Given I submit an email/mobile that does not exist
- When I attempt login
- Then I see error `E-LOGIN-INVALID-CREDENTIALS` (same as wrong password)

**AC-04 — Account not activated**
- Given my account is in pending_activation status
- When I attempt login
- Then I see error `E-LOGIN-ACCOUNT-NOT-ACTIVATED` and prompt to activate via OTP

**AC-05 — Account deactivated**
- Given my account is deactivated
- When I attempt login
- Then I see error `E-LOGIN-ACCOUNT-DEACTIVATED` with option to reactivate via OTP

**AC-06 — Account banned**
- Given my account is banned
- When I attempt login
- Then I see error `E-LOGIN-ACCOUNT-BANNED` without reactivation option

**AC-07 — Rate limiting on failed attempts**
- Given I fail to log in 10 times in 1 minute from the same IP
- When I attempt the 11th login
- Then the IP is temporarily throttled with error `E-LOGIN-RATE-LIMITED` for 5 minutes

**AC-08 — Session creation**
- Given I successfully log in
- When the session is created
- Then a HTTP-only, secure cookie is set with session ID; JWT is also returned in response body

**AC-09 — Redirect to home/dashboard**
- Given I successfully log in
- When authentication completes
- Then I am redirected to my role-appropriate home page (job seeker dashboard, employer dashboard, admin panel)

**AC-10 — Remember me option**
- Given I check "Remember me" before login (optional)
- When I log in
- Then my session cookie is set with extended TTL (14 days) instead of default session timeout

## Assumptions
- Login identifier can be email or mobile number (system auto-detects).
- Passwords are hashed using Argon2id; comparison is constant-time.
- JWT token validity: 1 hour; can be refreshed with refresh token.
- Rate limiting is per IP, not per user (prevents account enumeration).
- Failed login attempts are logged for security audit.
- Session timeout is configurable (default: 1 hour of inactivity).
- "Remember me" extends session to 14 days.

## Source Requirements
- [[3_1_5_Authentication_and_Authorization|3.1.5]] — FR-49, FR-50

## Related Stories
- [[US-3.1.5-02-multi-factor-authentication|US-3.1.5-02 Enable multi-factor authentication]]
- [[US-3.1.5-03-password-reset|US-3.1.5-03 Reset password]]
