---
story_id: "US-3.6.1-01"
title: "Receive email notifications for important events"
section_id: "3.6.1"
related_requirements: ["FR-143"]
related_stories: ["US-3.6.1-02", "US-3.6.1-03"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.1-01 — Receive email notifications for important events

## Story
As a **Job Seeker**, I want to receive email notifications for important events (job recommendations, application status changes, new messages), so that I stay informed about relevant activity on the platform.

## Acceptance Criteria

**AC-01 — Email delivered on important event**
- Given I have an active account and email notifications enabled
- When a job recommendation, application status change, or message is created
- Then an email is delivered to my registered email address within 5 minutes

**AC-02 — Email contains relevant event details**
- Given an event notification is sent
- When I receive the email
- Then it includes a clear subject, event details, and action link to the platform

**AC-03 — Event types trigger notifications**
- Given the system processes events
- When the following occur: new job match, application status change, recruiter message, profile view
- Then each event generates a notification for the user (if enabled)

## Assumptions
- Email provider: SendGrid or equivalent (supports templates, tracking, bounce handling)
- Important events defined: job recommendations, application updates, messages, profile views
- Delivery timeout: 5 minutes SLA
- Email address validation occurs at registration/profile update
- GDPR/CAN-SPAM compliance: users can opt-out globally
- Maximum retry: 3 attempts over 24 hours on bounce

## Source Requirements
- [[3_6_1_Email_Notifications|3.6.1]] — FR-143

## Related Stories
- [[US-3.6.1-02-configure-email-notification-preferences|US-3.6.1-02]]
- [[US-3.6.1-03-manage-email-template|US-3.6.1-03]]
