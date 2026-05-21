---
story_id: "US-3.6.2-06"
title: "Support different notification types with visual indicators"
section_id: "3.6.2"
related_requirements: ["FR-154"]
related_stories: ["US-3.6.2-01", "US-3.6.2-05"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.2-06 — Support different notification types with visual indicators

## Story
As a **System**, I want to categorize in-app notifications with different types and visual indicators (icons, colors, badges), so that users quickly identify the type and urgency of each notification.

## Acceptance Criteria

**AC-01 — Define notification types**
- Given the system generates notifications
- When notifications are created
- Then they are assigned a type: job_recommendation, application_update, message, profile_view, recruiter_activity, announcement

**AC-02 — Icon for each type**
- Given a notification is displayed
- When it appears in the toast or notification center
- Then it shows a distinct icon: briefcase (job), checkmark (application), envelope (message), eye (profile view), megaphone (announcement)

**AC-03 — Color coding**
- Given notifications are displayed
- When I view them in the list
- Then they are colored by type: job (blue), application (green), message (purple), profile (gray), announcement (orange)

**AC-04 — Priority visual cue**
- Given notifications have different priority levels
- When high-priority notifications are shown (e.g., application expiring soon)
- Then they include a visual indicator (red border, star, or "urgent" badge)

**AC-05 — Notification badge text**
- Given notifications appear in the UI
- When they are displayed
- Then the type is also shown as text (e.g., "Job Match", "Application Update") for accessibility

**AC-06 — Consistent visual design**
- Given multiple notification types exist
- When they are displayed together
- Then the visual hierarchy and styling are consistent and accessible (WCAG AA compliant)

## Assumptions
- Visual design follows app's design system (colors, icons, spacing)
- Icon set: consistent with Material Design or custom set
- Color usage: meets WCAG AA contrast requirements
- Mobile and desktop responsive design
- Accessible to users with color blindness (icons + text, not color alone)
- Notification type enum stored in database

## Source Requirements
- [[3_6_2_In_App_Notifications|3.6.2]] — FR-154

## Related Stories
- [[US-3.6.2-01-access-in-app-notification-center|US-3.6.2-01]]
- [[US-3.6.2-05-configure-in-app-notification-preferences|US-3.6.2-05]]
