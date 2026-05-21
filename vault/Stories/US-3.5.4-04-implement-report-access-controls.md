---
story_id: "US-3.5.4-04"
title: "Implement Report Access Controls"
section_id: "3.5.4"
related_requirements: ["FR-142"]
related_stories: ["US-3.5.4-02", "US-3.5.4-03"]
role: "System Administrator"
status: draft
priority: must
tags:
  - story
  - bc/analytics
---

# US-3.5.4-04 — Implement Report Access Controls

## Story
As a **System Administrator**, I want **to enforce role-based access controls so users can only view, create, and schedule reports appropriate to their role**, so that **sensitive employment and activity data is protected and organizational boundaries are maintained**.

## Acceptance Criteria

**AC-01 — Role-based report visibility**
- Given different roles need different reports
- When a user accesses the reporting interface
- Then only reports appropriate to their role are visible in the library

**AC-02 — Role-based report template access**
- Given report templates vary in sensitivity
- When a user attempts to use a template
- Then access is granted only if their role is authorized for that template

**AC-03 — Data-level filtering by role**
- Given users have different authority scopes
- When a report is generated
- Then data is filtered based on user role (e.g., Employer Owner sees only their data; MoL Admin sees all)

**AC-04 — Report creation restrictions**
- Given only certain roles should create reports
- When a user tries to create a new report
- Then access is granted only to roles with report creation permission

**AC-05 — Schedule management restrictions**
- Given scheduling is sensitive
- When a user tries to create/edit a schedule
- Then access is granted only to System Administrator and MoL Administrator roles

**AC-06 — Audit trail of report access**
- Given security and compliance are important
- When a user views or downloads a report
- Then the action is logged with timestamp, user, and report details

**AC-07 — Data masking for sensitive fields**
- Given some users shouldn't see all data
- When a report is generated
- Then sensitive fields (e.g., salary ranges, specific applicant names) can be masked based on role

## Assumptions
- Roles are: System Administrator, MoL Administrator, Data Analyst, Employer Owner, Auditor
- Report creation permission limited to: System Administrator, MoL Administrator, Data Analyst
- Schedule management limited to: System Administrator, MoL Administrator
- Employer Owner can view reports filtered to their organization only
- Auditor role has read-only access to all reports
- Data filtering is applied at generation time (not stored separately)
- Audit logs retained for 2 years; immutable (write-once)

## Source Requirements
- [[3_5_4_Custom_Report_Generation|3.5.4]] — FR-142

## Related Stories
- [[US-3.5.4-02|Define and Configure Report Templates]]
- [[US-3.5.4-03|Schedule Recurring Reports with Automated Distribution]]
