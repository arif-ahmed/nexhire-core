---
story_id: "US-3.5.2-01"
title: "View Employment Statistics Dashboard"
section_id: "3.5.2"
related_requirements: ["FR-120", "FR-121", "FR-122"]
related_stories: ["US-3.5.2-02", "US-3.5.2-03", "US-3.5.4-01"]
role: "MoL Administrator"
status: draft
priority: should
tags:
  - story
  - bc/analytics
---

# US-3.5.2-01 — View Employment Statistics Dashboard

## Story
As a **MoL Administrator**, I want **to view a comprehensive employment statistics dashboard showing job posting trends, application rates, hiring rates, and time-to-fill metrics**, so that **I can monitor labor market health and support evidence-based policy decisions**.

## Acceptance Criteria

**AC-01 — Job posting trends display**
- Given I access the Employment Statistics dashboard
- When I view the dashboard
- Then I see a time-series chart of job posting volume (daily/weekly/monthly)

**AC-02 — Application rate metrics**
- Given the dashboard is loaded
- When I view the metrics section
- Then I see total applications, applications per posting, and application trend

**AC-03 — Hiring rate tracking**
- Given time-to-fill is a key metric
- When I view the dashboard
- Then I see average time-to-fill, filled positions, and hiring velocity metrics

**AC-04 — Time-range selection**
- Given I need to analyze different periods
- When I select a date range or preset period (last 30 days, quarter, year)
- Then all dashboard metrics update to reflect that period

**AC-05 — Drill-down capability**
- Given aggregate metrics are displayed
- When I click on a metric or chart
- Then I can drill down to industry-specific, location-specific, or position-specific views

**AC-06 — Comparison mode**
- Given I need to compare periods
- When I enable year-over-year or period-over-period comparison
- Then the dashboard displays side-by-side metrics with variance

## Assumptions
- Time-to-fill is calculated from job posting date to job closure/filled status
- "Hiring rate" is interpreted as filled positions / total posted positions
- Dashboard updates nightly in batch; real-time granularity assumed not required
- Industry and location data sourced from job postings; classification defaults assumed available
- Metrics exclude test/spam postings (filtered by default)

## Source Requirements
- [[3_5_2_Employment_Statistics|3.5.2]] — FR-120, FR-121

## Related Stories
- [[US-3.5.2-02|View Industry-Specific Analytics]]
- [[US-3.5.2-03|View Skill Demand Analysis]]
- [[US-3.5.4-01|Generate Employment Report]]
