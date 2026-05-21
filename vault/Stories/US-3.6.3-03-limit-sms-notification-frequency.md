---
story_id: "US-3.6.3-03"
title: "Limit SMS notification frequency to essential communications"
section_id: "3.6.3"
related_requirements: ["FR-158"]
related_stories: ["US-3.6.3-02", "US-3.6.3-04"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.3-03 — Limit SMS notification frequency to essential communications

## Story
As a **System**, I want to limit SMS notifications to essential communications only, so that users are not overwhelmed with SMS messages and costs remain reasonable.

## Acceptance Criteria

**AC-01 — SMS sent only for critical events**
- Given SMS notifications are enabled
- When events occur
- Then SMS is sent only for: account verification, password reset, security alerts, emergency notifications
- And not for marketing, job recommendations, or general updates

**AC-02 — De-duplication of SMS**
- Given multiple critical events occur in quick succession
- When SMS would be sent for each
- Then similar events are batched into a single SMS (e.g., multiple failed login attempts = one alert)

**AC-03 — Frequency cap per user**
- Given a user receives multiple SMS in a short period
- When the limit is reached (e.g., 5 SMS per day)
- Then subsequent SMS are queued and sent after the window resets (or consolidated in next batch)

**AC-04 — Do not disturb respect**
- Given a user sets a do-not-disturb window (e.g., 10 PM - 8 AM)
- When non-critical SMS would be sent during that time
- Then it is delayed until after the window (except genuine emergencies like account compromise)

**AC-05 — SMS cost monitoring**
- Given SMS are sent
- When cost thresholds are approached
- Then alerts are triggered to monitor budget and adjust frequency if needed
- And a summary report is provided to admins monthly

**AC-06 — User control over essential SMS**
- Given I have some SMS I might not need
- When I access SMS preferences
- Then I can choose which critical events trigger SMS (e.g., security alerts yes, password reset no)
- But account verification SMS cannot be disabled (required for registration)

## Assumptions
- Essential SMS types: account verification, password reset, security alerts, emergency notifications
- Non-essential: marketing, job recommendations, general updates (use email/in-app instead)
- Frequency cap: maximum 5 SMS per user per day (can be configured)
- Do not disturb: respects user's timezone and configured hours
- Batch window: SMS queued during DND sent within 1 hour after window ends
- Cost estimate: ~$0.01 per SMS, monthly budget tracking available to admins
- Critical security alerts bypass frequency caps and DND windows

## Source Requirements
- [[3_6_3_SMS_Notifications|3.6.3]] — FR-158

## Related Stories
- [[US-3.6.3-02-allow-users-to-opt-in-sms|US-3.6.3-02]]
- [[US-3.6.3-04-track-sms-delivery-status|US-3.6.3-04]]
