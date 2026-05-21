---
story_id: "US-3.6.1-04"
title: "Support immediate and digest email notifications"
section_id: "3.6.1"
related_requirements: ["FR-146"]
related_stories: ["US-3.6.1-02", "US-3.6.1-01"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.1-04 — Support immediate and digest email notifications

## Story
As a **System**, I want to support both immediate email notifications and batched digest emails, so that users can choose between real-time alerts and less-frequent consolidated messages based on their preference.

## Acceptance Criteria

**AC-01 — Send immediate email on event**
- Given a user has "immediate" frequency selected for a notification type
- When an event occurs (job recommendation, application update)
- Then an email is sent to the user within 5 minutes

**AC-02 — Batch digest notifications**
- Given a user has "daily digest" or "weekly digest" selected
- When events occur
- Then the system collects them in a queue and sends a consolidated email at the scheduled time (e.g., 8 AM, Monday morning)

**AC-03 — Digest contains all batched events**
- Given events are queued for digest delivery
- When the digest email is sent
- Then it contains all events from the batch period with summaries and action links

**AC-04 — Empty digest not sent**
- Given no events occurred during a digest period
- When the digest send time arrives
- Then no email is sent to avoid clutter

**AC-05 — Frequency change applies to future notifications**
- Given a user changes frequency preference from immediate to digest
- When the next event occurs
- Then it is queued for digest delivery, not sent immediately

## Assumptions
- Digest schedule: daily (8 AM user timezone) and weekly (Monday 8 AM user timezone)
- Events expire from queue after 30 days if not sent
- Digest template differs from immediate email (summary format)
- Timezone: user's profile timezone or system default
- Batch size: unlimited (all events during period included)
- Digest delivery SLA: within 1 hour of scheduled time

## Source Requirements
- [[3_6_1_Email_Notifications|3.6.1]] — FR-146

## Related Stories
- [[US-3.6.1-02-configure-email-notification-preferences|US-3.6.1-02]]
- [[US-3.6.1-01-receive-important-event-email|US-3.6.1-01]]
