---
story_id: "US-3.6.2-02"
title: "Receive real-time in-app notifications for important events"
section_id: "3.6.2"
related_requirements: ["FR-150"]
related_stories: ["US-3.6.2-01", "US-3.6.2-04"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.2-02 — Receive real-time in-app notifications for important events

## Story
As a **Job Seeker**, I want to receive real-time in-app notifications for important events (job matches, application updates, recruiter messages), so that I am immediately informed of time-sensitive activity.

## Acceptance Criteria

**AC-01 — Real-time notification toast**
- Given an important event occurs (new job match, application update)
- When I am logged in and viewing the app
- Then a toast notification appears at the top of the screen for 5 seconds

**AC-02 — Notification detail**
- Given a notification appears
- When I click on it
- Then I am navigated to the relevant page (job details, application, message) with context preserved

**AC-03 — Event types trigger notifications**
- Given important events occur
- When the following happen: new job recommendation, application status change, recruiter message, profile view
- Then a real-time notification is shown to the user (if they are active on the platform)

**AC-04 — Notification persistence in center**
- Given a toast notification appears
- When it dismisses
- Then the notification is also added to the notification center for later review

**AC-05 — Batch multiple events**
- Given multiple events occur in quick succession (< 10 seconds)
- When notifications would be shown
- Then they are batched into a single toast (e.g., "You have 3 new notifications")

## Assumptions
- Real-time delivery: WebSocket connection or Server-Sent Events (SSE)
- Toast duration: 5 seconds (user can dismiss early)
- Only sent if user is active (has app open, not away for > 5 minutes)
- Notification types: job recommendation, application update, message, profile view, job removed
- Priority levels: high (urgent action required), normal (informational)
- Maximum batch size: 10 events per toast

## Source Requirements
- [[3_6_2_In_App_Notifications|3.6.2]] — FR-150

## Related Stories
- [[US-3.6.2-01-access-in-app-notification-center|US-3.6.2-01]]
- [[US-3.6.2-04-manage-notifications-mark-read-delete|US-3.6.2-04]]
