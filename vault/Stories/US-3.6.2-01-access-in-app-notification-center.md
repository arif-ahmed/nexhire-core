---
story_id: "US-3.6.2-01"
title: "Access in-app notification center"
section_id: "3.6.2"
related_requirements: ["FR-149"]
related_stories: ["US-3.6.2-02", "US-3.6.2-03"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.2-01 — Access in-app notification center

## Story
As a **Job Seeker**, I want to access a dedicated notification center within the app, so that I can view all in-app notifications in one place without receiving emails for every event.

## Acceptance Criteria

**AC-01 — Notification center UI**
- Given I am logged into the app
- When I click the notification bell icon in the header
- Then a dropdown or side panel opens showing my recent notifications

**AC-02 — Notification list display**
- Given the notification center is open
- When I view the list
- Then I see: notification type (icon), title, description, timestamp, and whether it's read/unread

**AC-03 — Unread count badge**
- Given I have unread notifications
- When I look at the bell icon
- Then a badge displays the count of unread notifications

**AC-04 — Load more functionality**
- Given I have more than 20 notifications
- When I scroll to the bottom of the list
- Then a "Load more" button appears to show additional notifications

**AC-05 — Notification center persistence**
- Given I use the notification center
- When I close it and return later
- Then the notification history is preserved and unread status is maintained

## Assumptions
- Notification center displays last 100 notifications by default
- Real-time updates: WebSocket or polling every 10 seconds
- Unread state persisted in database
- Notification center accessible from all app pages
- Mobile responsive: overlay or bottom sheet on mobile
- Notification retention: 90 days before archival

## Source Requirements
- [[3_6_2_In_App_Notifications|3.6.2]] — FR-149

## Related Stories
- [[US-3.6.2-02-receive-real-time-notifications|US-3.6.2-02]]
- [[US-3.6.2-03-receive-weekly-job-recommendations|US-3.6.2-03]]
