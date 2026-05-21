---
story_id: "US-3.6.2-05"
title: "Configure in-app notification preferences"
section_id: "3.6.2"
related_requirements: ["FR-153"]
related_stories: ["US-3.6.2-01", "US-3.6.1-02"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.2-05 — Configure in-app notification preferences

## Story
As a **Job Seeker**, I want to configure which types of in-app notifications I receive, so that I control what alerts appear and avoid notification fatigue.

## Acceptance Criteria

**AC-01 — Toggle notification types**
- Given I am in notification preferences
- When I see toggles for in-app notification types (job recommendations, application updates, messages, profile views, announcements)
- Then I can enable or disable each type independently

**AC-02 — Separate from email preferences**
- Given I configure in-app notification settings
- When I enable/disable a notification type
- Then this preference is independent from email notification settings

**AC-03 — Real-time vs. periodic notifications**
- Given notification settings are available
- When I configure preferences
- Then I can choose: always show toast, only show in notification center (no toast), or disable entirely

**AC-04 — Sound and visual indicators**
- Given I want to customize notifications
- When I access preferences
- Then I can enable/disable: sound alert (ding), visual badge, animated icons

**AC-05 — Notification volume control**
- Given I receive many notifications
- When I configure preferences
- Then I can set a "do not disturb" window (e.g., 9 PM - 9 AM) where toasts don't appear (but are logged to center)

**AC-06 — Persist preferences**
- Given I save notification preferences
- When I close the settings
- Then preferences are stored and apply immediately

## Assumptions
- Settings stored per user in database
- Notification types: job recommendations, application updates, messages, profile views, recruiter activity, announcements
- Default: all enabled with toast + notification center
- Sound disabled by default (user can enable)
- Do not disturb respects user's timezone
- Preferences accessible from account settings or notification center

## Source Requirements
- [[3_6_2_In_App_Notifications|3.6.2]] — FR-153

## Related Stories
- [[US-3.6.2-01-access-in-app-notification-center|US-3.6.2-01]]
- [[US-3.6.1-02-configure-email-notification-preferences|US-3.6.1-02]]
