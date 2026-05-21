---
story_id: "US-3.6.3-01"
title: "Send SMS for critical updates and time-sensitive information"
section_id: "3.6.3"
related_requirements: ["FR-156"]
related_stories: ["US-3.6.3-02", "US-3.6.3-04"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.3-01 — Send SMS for critical updates and time-sensitive information

## Story
As a **System**, I want to send SMS notifications for critical and time-sensitive events (registration verification, password reset, account security alerts), so that users receive immediate alerts even if they don't have email access.

## Acceptance Criteria

**AC-01 — SMS sent for registration verification**
- Given a new user registers an account
- When email confirmation is sent
- Then a simultaneous SMS with a 6-digit verification code is sent to the registered phone number
- And the SMS expires after 10 minutes or 3 incorrect attempts

**AC-02 — SMS sent for password reset**
- Given a user requests password reset
- When the reset flow is initiated
- Then an SMS with a one-time code is sent to the phone number on file
- And the code is valid for 15 minutes

**AC-03 — SMS sent for account security alerts**
- Given suspicious activity is detected on an account
- When an unauthorized login or unusual activity is confirmed
- Then an immediate SMS alert is sent to the user's phone

**AC-04 — SMS sent for employer registration verification**
- Given an employer or recruiter registers
- When employer account verification is required
- Then an SMS with a verification code is sent similar to job seeker registration

**AC-05 — SMS message format**
- Given an SMS is sent
- When the user receives it
- Then it includes: clear purpose, code or action link, expiration time, and support contact info
- And total length does not exceed 160 characters (standard SMS length)

**AC-06 — SMS service provider integration**
- Given the system needs to send SMS
- When SMS is triggered
- Then the SMS service provider (e.g., Twilio, AWS SNS) handles delivery through their infrastructure

## Assumptions
- SMS provider: Twilio, AWS SNS, or similar (supports OTP, delivery tracking)
- Supported countries: US, Canada, UK, EU (regional compliance)
- SMS cost: user account debited per SMS sent (~$0.01 per message)
- Character limit: 160 characters for single SMS (concatenation supported for longer messages)
- Delivery timeout: 30 seconds SLA
- Opt-in required for SMS (collected at registration, stored in database)
- Compliance: GDPR, TCPA (US), PIPEDA (Canada), Ofcom (UK)

## Source Requirements
- [[3_6_3_SMS_Notifications|3.6.3]] — FR-156
- [[3_1_5_Authentication_and_Authorization|3.1.5. Authentication and Authorization]]

## Related Stories
- [[US-3.6.3-02-allow-users-to-opt-in-sms|US-3.6.3-02]]
- [[US-3.6.3-04-track-sms-delivery-status|US-3.6.3-04]]
