---
story_id: "US-3.5.2-02"
title: "View Industry-Specific Analytics"
section_id: "3.5.2"
related_requirements: ["FR-122", "FR-125"]
related_stories: ["US-3.5.2-01", "US-3.5.2-03", "US-3.5.4-03"]
role: "Data Analyst"
status: draft
priority: should
tags:
  - story
  - bc/analytics
---

# US-3.5.2-02 — View Industry-Specific Analytics

## Story
As a **Data Analyst**, I want **to analyze industry-specific labor market demand and supply, including job market demand metrics and salary range analytics by industry and position**, so that **I can identify trends in specific sectors and support targeted market analysis**.

## Acceptance Criteria

**AC-01 — Industry selection filter**
- Given I access the Industry Analytics view
- When I select one or more industries from a filterable list
- Then the dashboard filters all metrics to show only selected industries

**AC-02 — Demand vs. supply visualization**
- Given an industry is selected
- When I view the analytics
- Then I see a comparison of job postings (demand) vs. active candidates (supply) with trend lines

**AC-03 — Position-level drill-down**
- Given an industry is displayed
- When I click on an industry
- Then I see the top positions by posting volume with individual metrics for each

**AC-04 — Salary range analytics by position**
- Given a position is selected
- When I view salary details
- Then I see salary range (min/max), median, and 25th/75th percentiles

**AC-05 — Location-based salary variation**
- Given position salary data is available
- When I compare by location
- Then salary ranges are broken down by geographic region within the industry

**AC-06 — Historical trend comparison**
- Given I need to track industry shifts
- When I select a time-range comparison
- Then I see year-over-year or quarter-over-quarter demand/supply trends

## Assumptions
- Industries are classified using a standard taxonomy (e.g., NAICS); mapping assumed to exist in data model
- Salary data comes from job postings and assumed to be non-mandatory (many postings may lack salary ranges)
- Supply metric is estimated from candidate job searches and saved jobs in industry categories
- "Position" refers to job titles; standardization/clustering of job titles assumed available
- Geographic granularity defaults to state/province level; city-level breakdown optional

## Source Requirements
- [[3_5_2_Employment_Statistics|3.5.2]] — FR-122, FR-125

## Related Stories
- [[US-3.5.2-01|View Employment Statistics Dashboard]]
- [[US-3.5.2-03|View Skill Demand Analysis]]
- [[US-3.5.4-03|Create Custom Employment Report]]
