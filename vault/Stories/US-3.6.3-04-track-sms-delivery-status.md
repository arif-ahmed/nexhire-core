---
story_id: "US-3.6.3-04"
title: "Track SMS delivery status for monitoring and troubleshooting"
section_id: "3.6.3"
related_requirements: ["FR-159"]
related_stories: ["US-3.6.3-01", "US-3.6.3-05"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.3-04 — Track SMS delivery status for monitoring and troubleshooting

## Story
As a **System**, I want to track SMS delivery status (sent, delivered, failed, invalid number) and log all SMS messages, so that I can monitor health, troubleshoot delivery issues, and maintain compliance records.

## Acceptance Criteria

**AC-01 — SMS sent event logged**
- Given an SMS is sent
- When the send is initiated
- Then a log entry is created with: user ID, phone number (masked), message content (redacted), template ID, timestamp (UTC), status "pending"

**AC-02 — Delivery status updated**
- Given an SMS was sent
- When the SMS provider reports delivery (delivered, failed, bounced)
- Then the log entry is updated with delivery status and timestamp

**AC-03 — Bounce handling logged**
- Given an SMS fails to deliver (invalid number, carrier rejection)
- When the bounce is received
- Then the log records: bounce type (hard bounce, soft bounce, invalid), carrier message, and phone number is flagged

**AC-04 — Retry on soft bounce**
- Given an SMS experiences a soft bounce (temporary error)
- When the system retries
- Then the retry attempt is logged with attempt number and outcome
- And maximum 3 retries are performed over 24 hours

**AC-05 — Hard bounce handling**
- Given an SMS hard bounces (invalid number, carrier blacklist)
- When the bounce is processed
- Then the user's phone number is marked invalid and SMS is disabled for this user
- And a flag is set for manual review

**AC-06 — SMS delivery audit trail**
- Given I need to verify SMS delivery
- When I query the SMS log by user ID, phone number (masked), date range, or message type
- Then I retrieve all SMS records with delivery status and timestamps

**AC-07 — Aggregate metrics dashboard**
- Given SMS are sent regularly
- When I view the SMS admin dashboard
- Then I see: total SMS sent, delivery rate, bounce rate, retry count, cost tracking

**AC-08 — Log retention policy**
- Given logs accumulate over time
- When logs reach retention threshold (e.g., 1 year old)
- Then they are archived to cold storage for compliance (not deleted)

## Assumptions
- SMS provider: Twilio, AWS SNS (supports webhook callbacks for delivery status)
- Log storage: database or data warehouse
- Retention: 3 years (GDPR/TCPA compliance)
- Soft bounce: network error, carrier congestion, temporary rejection
- Hard bounce: invalid number, carrier blacklist, number ported
- Retry policy: 1 hour, 6 hours, 24 hours after initial send
- Masking: phone numbers masked as +1****5678 in logs
- Metrics tracked: sent, delivered, failed, bounced, retry_count, cost

## Source Requirements
- [[3_6_3_SMS_Notifications|3.6.3]] — FR-159

## Related Stories
- [[US-3.6.3-01-send-sms-for-critical-updates|US-3.6.3-01]]
- [[US-3.6.3-05-ensure-sms-compliance|US-3.6.3-05]]
