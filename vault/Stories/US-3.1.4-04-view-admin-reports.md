---
story_id: "US-3.1.4-04"
title: "View and download admin reports"
section_id: "3.1.4"
related_requirements: ["FR-46"]
related_stories: ["US-3.1.4-01-manage-user-accounts"]
role: "MoL Administrator"
status: draft
priority: should
tags:
  - story
  - bc/identity
  - topic/admin
---

# US-3.1.4-04 — View and download admin reports

## Story
As a **MoL Administrator**, I want **to view and download comprehensive reports on job postings, user registrations, searches, and system metrics**, so that **I can analyze platform performance and make data-driven decisions**.

## Acceptance Criteria

**AC-01 — Access reports dashboard**
- Given I navigate to the Reports section
- When the page loads
- Then I see a menu of available reports with date range selectors

**AC-02 — Job postings report**
- Given I select the Job Postings report
- When I choose a date range and industry/sector filters
- Then I see: total jobs posted, jobs by sector, jobs by region, active vs. closed, posting trends

**AC-03 — Download job postings report**
- Given I view the job postings report
- When I click "Download"
- Then the report is generated in CSV or XLSX format and downloaded

**AC-04 — User registrations report**
- Given I select the User Registrations report
- When I choose filters (user type, date range, region)
- Then I see: total registrations, job seeker vs. employer, verification status, registration trends

**AC-05 — Search trends report**
- Given I select the Search Trends report
- When I set a date range
- Then I see: top job titles searched, top skills searched, top locations, search frequency trends

**AC-06 — User interactions report**
- Given I select the User Interactions report
- When I set filters
- Then I see: applications submitted, profile views, job views, user activity trends

**AC-07 — System metrics report**
- Given I select the System Metrics report
- When the report loads
- Then I see: total active users, total jobs, match success rate, API usage (for third-party portals)

**AC-08 — Custom report builder**
- Given I want a custom report
- When I select report type and configure filters/metrics
- Then a custom report is generated with selected dimensions

**AC-09 — Schedule automated reports**
- Given I want recurring reports
- When I configure a report to run on a schedule (daily, weekly, monthly)
- Then the report is automatically generated and emailed to me

**AC-10 — Export reports in multiple formats**
- Given I generate a report
- When I select export format
- Then I can download as CSV, XLSX, or PDF

## Assumptions
- Reports are generated on-demand or via scheduled jobs.
- Report data is aggregated from transactional databases (may have 1–2 hour lag).
- Reports support filtering by: date range, industry, sector, region, user type, verification status.
- Export formats: CSV, XLSX, PDF.
- Scheduled reports are emailed to configured admin email.
- Report generation may take 30 seconds to 5 minutes depending on data volume.

## Source Requirements
- [[3_1_4_Administrator_User_Management|3.1.4]] — FR-46

## Related Stories
- [[US-3.1.4-01-manage-user-accounts|US-3.1.4-01 Manage user accounts]]
