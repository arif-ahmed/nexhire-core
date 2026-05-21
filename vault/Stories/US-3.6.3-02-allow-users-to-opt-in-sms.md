---
story_id: "US-3.6.3-02"
title: "Allow users to opt-in to SMS notifications and provide phone number"
section_id: "3.6.3"
related_requirements: ["FR-157"]
related_stories: ["US-3.6.3-01", "US-3.6.3-03"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.3-02 — Allow users to opt-in to SMS notifications and provide phone number

## Story
As a **Job Seeker**, I want to opt-in to SMS notifications for critical updates and provide my phone number, so that I can receive time-sensitive alerts via SMS in addition to email and in-app notifications.

## Acceptance Criteria

**AC-01 — SMS opt-in during registration**
- Given I am creating a new account
- When I reach the phone number step
- Then I see a checkbox to opt-in to SMS notifications with explanation
- And I can enter my phone number (with international dialing support)

**AC-02 — SMS opt-in in account settings**
- Given I have an existing account
- When I navigate to notification preferences or phone settings
- Then I can enable SMS notifications and add/update my phone number

**AC-03 — Phone number validation**
- Given I provide a phone number
- When I submit it
- Then the system validates the format and country code
- And sends a verification SMS with a code to confirm the number

**AC-04 — Confirmation of SMS opt-in**
- Given I receive the verification SMS
- When I enter the verification code
- Then my phone number is confirmed and SMS notifications are enabled
- And I see a success message

**AC-05 — Opt-out option**
- Given I have SMS notifications enabled
- When I uncheck "Receive SMS notifications" in settings
- Then SMS notifications are disabled and no further SMS are sent (except critical security alerts)

**AC-06 — Update phone number**
- Given I want to change my phone number
- When I update it in settings
- Then the new number is verified with an SMS code before taking effect

**AC-07 — SMS-only critical alerts**
- Given I disable SMS notifications
- When critical security or account alerts occur
- Then I still receive SMS for these (cannot be opted out, per TCPA)

## Assumptions
- Phone number validation: E.164 format (international)
- Supported countries: US, Canada, UK, EU (with local compliance)
- Verification code valid for 10 minutes, 3 attempt limit
- Default: SMS opt-in is optional (not forced)
- Phone number PII: encrypted at rest, not shared
- Multiple phone numbers per account not supported initially
- SMS-only critical alerts: login attempt, account locked, suspicious activity

## Source Requirements
- [[3_6_3_SMS_Notifications|3.6.3]] — FR-157
- [[3_1_5_Authentication_and_Authorization|3.1.5. Authentication and Authorization]]

## Related Stories
- [[US-3.6.3-01-send-sms-for-critical-updates|US-3.6.3-01]]
- [[US-3.6.3-03-limit-sms-frequency|US-3.6.3-03]]
