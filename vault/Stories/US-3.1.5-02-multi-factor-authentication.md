---
story_id: "US-3.1.5-02"
title: "Enable multi-factor authentication"
section_id: "3.1.5"
related_requirements: ["FR-49"]
related_stories: ["US-3.1.5-01-login-with-credentials"]
role: "Job Seeker"
status: draft
priority: should
tags:
  - story
  - bc/identity
  - topic/security
---

# US-3.1.5-02 — Enable multi-factor authentication

## Story
As a **User**, I want **to optionally enable multi-factor authentication (MFA) on my account**, so that **I can add an extra layer of security beyond my password**.

## Acceptance Criteria

**AC-01 — Optional MFA setup**
- Given I am in account security settings
- When I click "Enable MFA"
- Then I see options to enable SMS OTP or TOTP-based MFA (authenticator app)

**AC-02 — SMS OTP MFA setup**
- Given I select SMS OTP as MFA method
- When I confirm
- Then a test OTP is sent to my registered mobile; I must verify it to enable MFA

**AC-03 — TOTP MFA setup**
- Given I select TOTP (Time-based One-Time Password)
- When I initiate setup
- Then I see a QR code to scan with an authenticator app (Google Authenticator, Authy, etc.)

**AC-04 — MFA verification during setup**
- Given I scan the TOTP QR code or receive SMS OTP
- When I submit the code to confirm setup
- Then MFA is enabled and I see backup codes to store securely

**AC-05 — Login with MFA enabled**
- Given MFA is enabled on my account
- When I log in with correct credentials
- Then I am prompted for a second factor (SMS OTP or TOTP code) before issuing JWT

**AC-06 — MFA OTP attempt limit**
- Given I submit an incorrect MFA code
- When I fail 3 times within 5 minutes
- Then my account is locked for 15 minutes

**AC-07 — Disable MFA**
- Given I have MFA enabled
- When I click "Disable MFA" and confirm
- Then MFA is disabled and I can log in with just username and password

**AC-08 — Backup codes for account recovery**
- Given I enable MFA
- When setup completes
- Then I receive 8–10 backup codes; each can be used once if I lose access to the MFA device

**AC-09 — Backup code usage**
- Given I cannot access my MFA device
- When I submit a backup code instead of OTP during login
- Then the code is consumed and login proceeds

**AC-10 — MFA status display**
- Given I view account security settings
- When I check MFA status
- Then I see: enabled/disabled, method (SMS/TOTP), last verified date, and option to reconfigure

## Assumptions
- MFA is optional per user (not mandated system-wide).
- Supported MFA methods: SMS OTP, TOTP (authenticator app).
- SMS OTP sent to registered mobile; TOTP uses standard RFC 6238 algorithm.
- Backup codes are generated during setup and must be securely stored by user.
- MFA verification must occur every login (no "remember this device" option initially).
- MFA setup requires verification before activation (test OTP/code submission).

## Source Requirements
- [[3_1_5_Authentication_and_Authorization|3.1.5]] — FR-49

## Related Stories
- [[US-3.1.5-01-login-with-credentials|US-3.1.5-01 Login with credentials]]
