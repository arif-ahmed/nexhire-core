---
story_id: "US-3.5.2-04"
title: "View Employment Outcomes and Career Progression"
section_id: "3.5.2"
related_requirements: ["FR-126", "FR-127"]
related_stories: ["US-3.5.2-03", "US-3.5.4-04"]
role: "MoL Administrator"
status: draft
priority: could
tags:
  - story
  - bc/analytics
---

# US-3.5.2-04 — View Employment Outcomes and Career Progression

## Story
As a **MoL Administrator**, I want **to track employment outcomes and career progression where available, and generate periodic labor market reports for government stakeholders**, so that **I can demonstrate platform impact and provide evidence-based labor market insights to policy makers**.

## Acceptance Criteria

**AC-01 — Employment outcome tracking**
- Given candidate/employer activity data is available
- When I access the Employment Outcomes dashboard
- Then I see metrics on job placements (applications → offers → acceptances)

**AC-02 — Placement rate calculation**
- Given job seeker applications are tracked
- When I view placement statistics
- Then I see placement rate (acceptances / applications) by time period

**AC-03 — Career progression analysis**
- Given candidates have historical profile data
- When I analyze a cohort
- Then I see career changes (job title progression, skill additions) over time periods

**AC-04 — Outcome filtering**
- Given outcomes are reported
- When I filter by industry, location, or skill
- Then metrics are recalculated for the selected filters

**AC-05 — Government stakeholder report generation**
- Given periodic reporting is required
- When I generate a labor market report
- Then the system compiles key metrics (placements, trend data, skill gaps) into a report

**AC-06 — Report scheduling for distribution**
- Given stakeholders need regular updates
- When I schedule a report
- Then it is generated and distributed on the configured cadence (monthly/quarterly/annually)

## Assumptions
- Employment outcomes are inferred from application status and offer acceptance; actual employment verification not assumed (depends on employer participation)
- Career progression tracking requires candidate consent for historical profile comparisons
- Data quality for outcomes is lower than activity tracking (missing data assumed for 20-40% of placements); conservative metrics assumed
- Government stakeholder reports are anonymized/aggregated (no individual candidate data)
- Periodic report format and structure defined separately; scheduling capability built into US-3.5.4-05

## Source Requirements
- [[3_5_2_Employment_Statistics|3.5.2]] — FR-126, FR-127

## Related Stories
- [[US-3.5.2-03|View Skill Demand Trends]]
- [[US-3.5.4-04|Schedule Recurring Reports]]
