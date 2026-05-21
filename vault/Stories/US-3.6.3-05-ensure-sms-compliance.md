---
story_id: "US-3.6.3-05"
title: "Ensure SMS compliance with telecommunications regulations"
section_id: "3.6.3"
related_requirements: ["FR-160"]
related_stories: ["US-3.6.3-03", "US-3.6.3-04"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.3-05 — Ensure SMS compliance with telecommunications regulations

## Story
As a **System**, I want to ensure SMS notifications comply with telecommunications regulations (TCPA, GDPR, PIPEDA, Ofcom), so that the platform avoids fines and maintains user trust.

## Acceptance Criteria

**AC-01 — TCPA compliance (US)**
- Given SMS is sent to US numbers
- When SMS are dispatched
- Then the system complies with: prior express written consent (PEWC) required, no SMS before 8 AM or after 9 PM recipient timezone, unique identifier in SMS (e.g., "+1-555-1234"), clear opt-out instructions

**AC-02 — Explicit opt-in required**
- Given a user provides a phone number
- When SMS opt-in is enabled
- Then the system obtains explicit written consent (checkbox + confirmation SMS or email)
- And the consent is logged with timestamp and IP address

**AC-03 — Honor opt-out requests**
- Given a user sends "STOP" or "OPT OUT" via SMS reply
- When the message is received
- Then the user is immediately unsubscribed from SMS and a confirmation message is sent

**AC-04 — Opt-out confirmation**
- Given a user unsubscribes from SMS
- When the opt-out is processed
- Then a confirmation message is sent (via email) acknowledging the opt-out
- And no further SMS are sent to that number (except critical security alerts with TCPA carve-out)

**AC-05 — Identity and support info**
- Given an SMS is sent
- When the message is formatted
- Then it includes: company identity (e.g., "JobPlatform"), reason for sending (e.g., "Account Verification"), and customer support phone number or website

**AC-06 — GDPR compliance (EU)**
- Given SMS is sent to EU numbers
- When SMS opt-in/opt-out occurs
- Then the system complies with: unambiguous consent, right to withdraw consent, data minimization, retention limits

**AC-07 — PIPEDA compliance (Canada)**
- Given SMS is sent to Canadian numbers
- When SMS are dispatched
- Then the system complies with: express consent required, CRTC DNC list checked before sending, identify sender with business name and contact info

**AC-08 — Ofcom compliance (UK)**
- Given SMS is sent to UK numbers
- When SMS are dispatched
- Then the system complies with: prior consent required, suppress numbers on preference services, clear identification, prohibition on spoofing

**AC-09 — DNC list checking**
- Given SMS are sent
- When a number is about to receive SMS
- Then the system checks national Do-Not-Call/Do-Not-Contact registries (CRTC, FTC, ICO, etc.)
- And skips sending if the number is registered

**AC-10 — Consent audit trail**
- Given SMS consent is collected
- When a user opts in or out
- Then the consent decision is logged with: user ID, timestamp, timezone, IP address, consent method (form, SMS confirmation)
- And retained for 3 years for regulatory audit

## Assumptions
- Compliance frameworks: TCPA (US), GDPR (EU), PIPEDA (Canada), Ofcom (UK/WCAG)
- Timezone awareness: SMS timing respects user's registered timezone
- Consent method: checkbox with clear language + confirmation SMS/email
- DNC list integration: third-party service for automated checking
- Critical security SMS exemptions: account compromise, unauthorized access (TCPA carve-out)
- Opt-in method: explicit checkboxes, no pre-checked boxes, separate from email opt-in
- Opt-out response: immediate (within 1 hour), no further marketing SMS within 24 hours
- Documentation: consent logs, audit trail, SMS content archive for 3 years

## Source Requirements
- [[3_6_3_SMS_Notifications|3.6.3]] — FR-160

## Related Stories
- [[US-3.6.3-03-limit-sms-notification-frequency|US-3.6.3-03]]
- [[US-3.6.3-04-track-sms-delivery-status|US-3.6.3-04]]
