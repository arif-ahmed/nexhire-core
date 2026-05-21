---
story_id: "US-3.5.1-01"
title: "View User Activity Dashboard"
section_id: "3.5.1"
related_requirements: ["FR-115", "FR-119"]
related_stories: ["US-3.5.1-02", "US-3.5.3-01"]
role: "System Administrator"
status: draft
priority: must
tags:
  - story
  - bc/audit
---

# US-3.5.1-01 — View User Activity Dashboard

## Story
As a **System Administrator**, I want **to see a real-time dashboard of current user logins, login counts, and last session details with links to user profiles**, so that **I can monitor system usage and quickly identify active users or potential security concerns**.

## Acceptance Criteria

**AC-01 — Dashboard displays current logins**
- Given I have access to the User Activity Monitoring dashboard
- When I load the dashboard
- Then I see the number of currently active logins displayed prominently

**AC-02 — Dashboard shows last session information**
- Given the dashboard is loaded
- When I view the last sessions section
- Then I see last login timestamp and session duration for each active user

**AC-03 — User profile navigation links**
- Given a user is listed on the dashboard
- When I click on the user's name or profile link
- Then I am navigated to that user's profile page with full details

**AC-04 — Dashboard auto-refresh**
- Given the dashboard is open
- When time passes (configurable interval)
- Then the data refreshes automatically to show current state

**AC-05 — Login count persistence**
- Given a user has multiple concurrent sessions
- When I view the dashboard
- Then the dashboard accurately reflects the number of concurrent logins for each user

## Assumptions
- Auto-refresh interval defaults to 30 seconds but is configurable by admins
- Dashboard only shows active sessions; inactive sessions are excluded
- User profile links respect role-based access controls
- Timestamps use system timezone
- No PII is displayed except what's required for identification (name, user ID)

## Source Requirements
- [[3_5_1_User_Activity_Monitoring|3.5.1]] — FR-115, FR-119

## Related Stories
- [[US-3.5.1-02|Job Seeker Activity Tracking]]
- [[US-3.5.1-03|Employer Activity Tracking]]
- [[US-3.5.3-01|View System Performance Dashboard]]
