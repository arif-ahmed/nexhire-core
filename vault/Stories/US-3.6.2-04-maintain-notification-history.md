---
story_id: "US-3.6.2-04"
title: "Maintain notification history for user review"
section_id: "3.6.2"
related_requirements: ["FR-152"]
related_stories: ["US-3.6.2-01"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.2-04 — Maintain notification history for user review

## Story
As a **Job Seeker**, I want to view my complete notification history, so that I can review past notifications and follow up on actions I may have missed.

## Acceptance Criteria

**AC-01 — Notification history display**
- Given I open the notification center
- When I view the history section or scroll through older notifications
- Then I see all notifications from the past 90 days with newest first

**AC-02 — Filter by type**
- Given I have notifications of various types
- When I apply a filter (job recommendations, application updates, messages, etc.)
- Then only notifications of that type are displayed

**AC-03 — Search notifications**
- Given I want to find a specific notification
- When I search by keyword (job title, company name, application status)
- Then matching notifications are returned

**AC-04 — Read/unread state persisted**
- Given I read a notification
- When I close the notification center
- Then the read status is saved and the notification no longer shows as unread on next visit

**AC-05 — Notification detail view**
- Given I click on a notification in history
- When the detail view opens
- Then I see full notification content, timestamp, and relevant action buttons

**AC-06 — Archive old notifications**
- Given notifications are older than 90 days
- When the archival process runs
- Then they are moved to archive and no longer appear in the main history view

## Assumptions
- Notification retention period: 90 days in active history, 1 year in archive
- Search fields: title, description, related job/company name
- Filter categories: job recommendations, application updates, messages, profile activity, announcements
- Notifications are soft-deleted (archived) not permanently removed
- User can still retrieve archived notifications if needed (admin view or export)
- Notification history accessible from notification center UI

## Source Requirements
- [[3_6_2_In_App_Notifications|3.6.2]] — FR-152

## Related Stories
- [[US-3.6.2-01-access-in-app-notification-center|US-3.6.2-01]]
