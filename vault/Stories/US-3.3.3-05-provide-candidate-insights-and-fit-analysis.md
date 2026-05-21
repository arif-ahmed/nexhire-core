---
story_id: "US-3.3.3-05"
title: "Provide Candidate Insights and Fit Analysis"
section_id: "3.3.3"
related_requirements: ["FR-95"]
related_stories: ["US-3.3.3-01"]
role: "System"
status: draft
priority: should
tags:
  - story
  - bc/matching
---

# US-3.3.3-05 — Provide Candidate Insights and Fit Analysis

## Story
As a **System**, I want to **provide recruiters with insights on candidate availability, salary expectations, and potential fit**, so that **employers can make informed decisions about candidate suitability and avoid misaligned pursuits**.

## Acceptance Criteria

**AC-01 — Availability indicator**
- Given a candidate's profile is viewed by a recruiter
- When they see candidate details
- Then they see an "Availability" section showing: job search status (actively looking / open to opportunities / passive), available start date (e.g., "immediately" or "2 weeks notice"), and last profile update date

**AC-02 — Salary expectation display**
- Given a recruiter views a candidate
- When they check candidate details
- Then they see the candidate's stated salary expectation range (min–max); if no range provided, system shows "Not disclosed"

**AC-03 — Salary alignment scoring**
- Given a job posting has a posted salary range
- When a recruiter views a candidate
- Then they see a "Salary Fit" indicator: green (within range), yellow (slightly above range), red (significantly above range); salary_match % also shown

**AC-04 — Fit analysis summary**
- Given a candidate is being reviewed
- When the recruiter views candidate details
- Then they see a "Fit Analysis" section summarizing: overall match %, key strengths (top matching factors), potential gaps (mismatches), time-to-productivity estimate (inferred from experience/match %), and a "Contact likelihood" indicator (e.g., likelihood candidate will respond given availability status)

**AC-05 — Motivation and engagement signals**
- Given a recruiter wants to understand candidate engagement
- When they view insights
- Then they see signals: profile completeness %, days since last profile update, number of applications submitted in past month (inferred engagement level)

**AC-06 — Work arrangement compatibility**
- Given a job has specific work arrangement requirements
- When a recruiter views a candidate
- Then they see the candidate's work arrangement preference (on-site / hybrid / remote) and a compatibility indicator (compatible / mismatch)

## Assumptions
- **Availability data**: Extracted from job_seeker profile (job_search_status, available_start_date); updated manually by job seeker or inferred from application patterns (e.g., passive if no applications in 60 days).
- **Salary expectation**: Stored in job_seeker profile; can be a range or not disclosed; displayed to recruiters only if candidate is public or applied (privacy respected).
- **Salary fit**: Calculated as salary_match % (overlap between candidate expectation and job posting salary range); green if > 80% overlap, yellow 50–80%, red < 50%.
- **Motivation score**: Inferred from (profile_completeness * 0.4) + (recent_activity * 0.4) + (applications_sent * 0.2); scale 0–100; shown as percentage.
- **Time-to-productivity**: Estimated from match % and experience level; quick heuristic: high match + senior level = 1 week; medium match + mid = 2–3 weeks; low match or junior = 4+ weeks.
- **Contact likelihood**: Binary inferred from (job_search_status == "actively_looking") && (days_since_update < 30); shown as "High" / "Medium" / "Low".
- **Insights refresh**: Calculated on-demand when recruiter views candidate; cached with 24-hour TTL.

## Source Requirements
- [[3_3_3_Candidate_Recommendation_for_Employers|3.3.3]] — FR-95

## Related Stories
- [[US-3.3.3-01-see-ranked-candidate-recommendations|US-3.3.3-01]]
