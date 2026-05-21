---
story_id: "US-3.6.1-05"
title: "Log all email notifications for audit and troubleshooting"
section_id: "3.6.1"
related_requirements: ["FR-147"]
related_stories: ["US-3.6.1-06"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.1-05 — Log all email notifications for audit and troubleshooting

## Story
As a **System**, I want to log all email notifications sent, including recipient, template, delivery status, and timestamps, so that I can audit communication history, troubleshoot delivery issues, and ensure compliance with regulations.

## Acceptance Criteria

**AC-01 — Log email sent event**
- Given an email is sent to a user
- When the send is initiated
- Then a log entry is created with: user ID, recipient email, template ID, event type, timestamp (UTC), status "pending"

**AC-02 — Update log on delivery confirmation**
- Given an email was sent
- When the email service provider confirms delivery (bounce, open, click)
- Then the log entry is updated with delivery status (delivered, bounced, opened, clicked)

**AC-03 — Log retry attempts**
- Given an email fails to deliver (soft bounce)
- When the system retries
- Then each retry attempt is logged with timestamp and reason (network error, temporary bounce)

**AC-04 — Audit trail queryable**
- Given I need to verify email history for a user
- When I query the notification log by user ID, date range, or email address
- Then I retrieve all sent notifications with delivery statuses and timestamps

**AC-05 — Hard bounce handling logged**
- Given an email bounces permanently (hard bounce)
- When the bounce is received
- Then the log records the bounce type (invalid email, mailbox full) and the user email is flagged for review

**AC-06 — Retention policy**
- Given logs are accumulated over time
- When logs reach archival threshold (e.g., 1 year old)
- Then they are moved to archive storage for compliance, not deleted

## Assumptions
- Log storage: database or data warehouse (e.g., PostgreSQL, BigQuery)
- Retention period: 3 years (GDPR)
- Webhook integration with email service for delivery status updates
- Bounce types: hard (permanent), soft (temporary), complaint (marked spam)
- Queryable fields: user_id, recipient_email, template_id, event_type, status, created_at, updated_at
- Admin access required for log queries

## Source Requirements
- [[3_6_1_Email_Notifications|3.6.1]] — FR-147

## Related Stories
- [[US-3.6.1-06-ensure-spam-compliance|US-3.6.1-06]]
