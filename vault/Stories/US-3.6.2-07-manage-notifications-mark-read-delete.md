---
story_id: "US-3.6.2-07"
title: "Manage notifications: mark as read, delete, or take action"
section_id: "3.6.2"
related_requirements: ["FR-155"]
related_stories: ["US-3.6.2-01", "US-3.6.2-04"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.2-07 — Manage notifications: mark as read, delete, or take action

## Story
As a **Job Seeker**, I want to manage notifications by marking them as read, deleting them, or taking action directly from the notification, so that I can organize my notification center and act on important items quickly.

## Acceptance Criteria

**AC-01 — Mark as read**
- Given I have unread notifications
- When I click on a notification or select "Mark as read"
- Then the notification is marked as read and removed from the unread count

**AC-02 — Mark all as read**
- Given I have multiple unread notifications
- When I click "Mark all as read"
- Then all notifications in the current view are marked as read

**AC-03 — Delete notification**
- Given I have a notification I no longer need
- When I click the delete icon or "Delete"
- Then the notification is removed from the notification center (soft delete to archive)

**AC-04 — Delete multiple notifications**
- Given I select multiple notifications
- When I click "Delete selected"
- Then all selected notifications are deleted at once

**AC-05 — Take action from notification**
- Given I have a notification with an action (e.g., "Apply to job", "Reply to message", "View application")
- When I click the action button in the notification
- Then I am taken to the relevant page and the action is initiated

**AC-06 — Notification context menu**
- Given I right-click on a notification
- When a context menu appears
- Then I see options: Mark as read, Delete, Reply (if applicable), Forward, or Archive

**AC-07 — Undo delete**
- Given I delete a notification
- When I immediately undo (Ctrl+Z or "Undo" appears briefly)
- Then the notification is restored

## Assumptions
- Soft delete: notifications moved to archive, not permanently removed
- Archive retention: notifications available for 90 days after deletion
- Batch operations: up to 50 notifications at a time
- Action buttons contextual (not all notifications have actions)
- Undo available for 10 seconds after delete
- Mobile UI: swipe to delete, context menu accessible via long-press

## Source Requirements
- [[3_6_2_In_App_Notifications|3.6.2]] — FR-155

## Related Stories
- [[US-3.6.2-01-access-in-app-notification-center|US-3.6.2-01]]
- [[US-3.6.2-04-maintain-notification-history|US-3.6.2-04]]
