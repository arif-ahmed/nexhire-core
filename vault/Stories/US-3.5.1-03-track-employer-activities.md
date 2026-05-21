---
story_id: "US-3.5.1-03"
title: "Track Employer Activities"
section_id: "3.5.1"
related_requirements: ["FR-117"]
related_stories: ["US-3.5.1-02", "US-3.5.1-04", "US-3.5.4-02"]
role: "Data Analyst"
status: draft
priority: must
tags:
  - story
  - bc/audit
---

# US-3.5.1-03 — Track Employer Activities

## Story
As a **Data Analyst**, I want **to monitor employer platform activities including job postings, candidate searches, and application management**, so that **I can understand employer engagement patterns and market activity**.

## Acceptance Criteria

**AC-01 — Job posting tracking**
- Given an employer posts a job
- When I access the employer activity log
- Then the posting is recorded with timestamp, job title, and posting status

**AC-02 — Candidate search tracking**
- Given an employer searches for candidates
- When I view their activity history
- Then search criteria and result counts are logged

**AC-03 — Employer filtering**
- Given I need to analyze a specific employer's activity
- When I filter by employer ID or company name
- Then only that employer's activities are shown

**AC-04 — Activity timeline view**
- Given I have selected an employer
- When I choose a time range
- Then a chronological timeline of all their activities is displayed

**AC-05 — Activity volume metrics**
- Given a date range is selected
- When I view the summary dashboard
- Then I see counts of job postings, searches, and other actions performed

## Assumptions
- Employer activities include: job creation, editing, deletion, publish/unpublish, candidate search, saved candidates, application reviews, rejections, offers
- Activities are tracked at the action level (not page view level) to reduce noise
- Employer aggregation respects company hierarchy (parent/subsidiary relationships are assumed but not detailed in FR-117)
- Data retention follows FR-118 compliance requirements
- "Candidate search" includes saved searches and advanced filters

## Source Requirements
- [[3_5_1_User_Activity_Monitoring|3.5.1]] — FR-117

## Related Stories
- [[US-3.5.1-02|Track Job Seeker Activities]]
- [[US-3.5.1-04|Manage Activity Log Retention]]
- [[US-3.5.4-02|Export Activity Reports]]
