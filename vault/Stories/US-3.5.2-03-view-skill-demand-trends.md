---
story_id: "US-3.5.2-03"
title: "View Skill Demand Trends"
section_id: "3.5.2"
related_requirements: ["FR-123", "FR-124"]
related_stories: ["US-3.5.2-02", "US-3.5.2-04", "US-3.5.4-03"]
role: "Data Analyst"
status: draft
priority: should
tags:
  - story
  - bc/analytics
---

# US-3.5.2-03 — View Skill Demand Trends

## Story
As a **Data Analyst**, I want **to analyze emerging skill demand trends and skill gaps, including geographic distribution of jobs and candidates**, so that **I can identify workforce development priorities and inform skills training programs**.

## Acceptance Criteria

**AC-01 — Top skills by demand**
- Given I access the Skill Demand analysis view
- When I load the dashboard
- Then I see a ranked list of skills ranked by posting frequency over a selected time period

**AC-02 — Emerging skills identification**
- Given I need to identify new/trending skills
- When I filter by "emerging" or set a growth threshold
- Then skills with the highest growth rate are highlighted with trend indicators

**AC-03 — Skill gap analysis**
- Given skill demand is displayed
- When I view the supply side
- Then I see the number of candidates with each skill and identify gaps (high demand, low supply)

**AC-04 — Geographic skill distribution**
- Given geographic analytics are available
- When I select a region
- Then I see which skills are most in-demand in that region and candidate skill availability

**AC-05 — Industry + Skill cross-reference**
- Given multiple filters are available
- When I select both an industry and time range
- Then I see the top skills required in that industry during that period

**AC-06 — Skill growth trend chart**
- Given skills have historical data
- When I select a skill or skill group
- Then I see a trend chart showing demand growth over time with period-over-period comparison

## Assumptions
- Skills are extracted from job postings (text mining/NLP assumed); candidate skills from profiles
- Skill taxonomy is standardized (likely linked to industry frameworks like O*NET or similar); mapping assumed to exist
- "Emerging" is defined as skills with >25% growth rate YoY; configurable threshold assumed
- Geographic granularity supports state/province and metro-area levels
- Skill gaps measured as posting demand / candidate supply ratio

## Source Requirements
- [[3_5_2_Employment_Statistics|3.5.2]] — FR-123, FR-124

## Related Stories
- [[US-3.5.2-02|View Industry-Specific Analytics]]
- [[US-3.5.2-04|View Employment Outcomes]]
- [[US-3.5.4-03|Create Custom Employment Report]]
