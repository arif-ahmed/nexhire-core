---
story_id: "US-3.5.1-02"
title: "Track Job Seeker Activities"
section_id: "3.5.1"
related_requirements: ["FR-116"]
related_stories: ["US-3.5.1-01", "US-3.5.1-03", "US-3.5.4-02"]
role: "Data Analyst"
status: draft
priority: must
tags:
  - story
  - bc/audit
---

# US-3.5.1-02 — Track Job Seeker Activities

## Story
As a **Data Analyst**, I want **to view aggregated job seeker activities including profile views, job searches, applications, and interaction logs**, so that **I can analyze user behavior patterns and engagement trends on the platform**.

## Acceptance Criteria

**AC-01 — Activity log completeness**
- Given a job seeker performs actions on the platform
- When I access the activity tracking report
- Then I see all tracked activities: profile views, job searches, job applications, and saved jobs

**AC-02 — Time-range filtering**
- Given I am viewing activity logs
- When I select a date range
- Then only activities within that range are displayed

**AC-03 — Activity drill-down**
- Given a user is listed in the activity summary
- When I click on that user
- Then I see a detailed timeline of all their activities with timestamps

**AC-04 — Search functionality**
- Given I need to find a specific user's activities
- When I search by user ID, email, or name
- Then matching records are returned

**AC-05 — Activity metrics aggregation**
- Given I have a time range selected
- When I view the summary section
- Then I see total counts for each activity type (searches, applications, profile views)

## Assumptions
- Activity tracking begins when the system is deployed; historical pre-deployment data is not available
- Activities are recorded with millisecond precision timestamps
- "Profile views" captures views initiated by other users (employers/admins), not self-views
- Data retention follows FR-118 (compliance-based retention policy); assumed 2-year retention minimum
- Export functionality covered separately in US-3.5.4-02

## Source Requirements
- [[3_5_1_User_Activity_Monitoring|3.5.1]] — FR-116

## Related Stories
- [[US-3.5.1-01|View User Activity Dashboard]]
- [[US-3.5.1-03|Track Employer Activities]]
- [[US-3.5.4-02|Export Activity Reports]]
