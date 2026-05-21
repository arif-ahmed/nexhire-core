---
story_id: "US-3.6.1-02"
title: "Configure email notification preferences"
section_id: "3.6.1"
related_requirements: ["FR-145"]
related_stories: ["US-3.6.1-01", "US-3.6.1-04"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.1-02 — Configure email notification preferences

## Story
As a **Job Seeker**, I want to configure which types of emails I receive and how often, so that I control notification frequency and relevance to my needs.

## Acceptance Criteria

**AC-01 — Toggle notification types on/off**
- Given I am on my notification preferences page
- When I see toggles for: job recommendations, application updates, messages, recruiter activity, platform announcements
- Then I can enable or disable each type independently

**AC-02 — Set frequency preference**
- Given I have notification types enabled
- When I select a frequency option (immediate, daily digest, weekly digest)
- Then notifications are batched and sent according to my choice

**AC-03 — Specify preferred email address**
- Given I have multiple email addresses on file
- When I select preferred email for notifications
- Then future notifications are sent to the selected address

**AC-04 — Persist preferences**
- Given I configure preferences
- When I save changes
- Then preferences are stored and apply immediately to new notifications

**AC-05 — Global opt-out**
- Given I want to stop all email notifications
- When I select "Unsubscribe from all emails"
- Then I no longer receive any email notifications except transactional (password reset, account security)

## Assumptions
- Preferences stored per user account in database
- Transactional emails (password reset, welcome, account verification) are separate from marketing/notification emails
- Default preference: all types enabled, immediate delivery
- Mobile-responsive preferences UI required
- Preferences page accessible from account settings

## Source Requirements
- [[3_6_1_Email_Notifications|3.6.1]] — FR-145

## Related Stories
- [[US-3.6.1-01-receive-important-event-email|US-3.6.1-01]]
- [[US-3.6.1-04-log-all-email-notifications|US-3.6.1-04]]
