---
story_id: "US-3.3.2-03"
title: "Recommend Jobs Respecting Location, Salary, and Work Arrangement Preferences"
section_id: "3.3.2"
related_requirements: ["FR-89"]
related_stories: ["US-3.3.2-01"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/matching
---

# US-3.3.2-03 — Recommend Jobs Respecting Location, Salary, and Work Arrangement Preferences

## Story
As a **System**, I want to **filter and rank job recommendations to respect job seeker location, salary expectations, and work arrangement preferences**, so that **all recommendations are practical and reduce friction in the application process**.

## Acceptance Criteria

**AC-01 — Preference retrieval**
- Given a recommendation request for a job seeker
- When the system processes it
- Then it retrieves the job seeker's saved preferences: preferred locations (city/region), salary range (min/max), work arrangement (on-site/hybrid/remote)

**AC-02 — Location filtering**
- Given a job seeker prefers locations within 50 km of their city
- When recommendations are generated
- Then only jobs in that geographic range are considered; remote jobs are always included if work_arrangement preference allows

**AC-03 — Salary alignment**
- Given a job seeker has a salary expectation of 40K–60K
- When job recommendations are scored
- Then jobs outside their range are deprioritized; jobs within range are boosted; jobs slightly above range (within 10%) are included with a note

**AC-04 — Work arrangement matching**
- Given a job seeker prefers hybrid work
- When recommendations are generated
- Then on-site-only jobs are excluded; hybrid and remote jobs are prioritized; job seeker can choose to include/exclude work arrangements

**AC-05 — Preference updates**
- Given a job seeker changes their preferences (e.g., salary range)
- When they update their profile
- Then future recommendation queries reflect the new preferences immediately; cached recommendations are invalidated

**AC-06 — Preference confidence**
- Given a job seeker has not explicitly set preferences
- When the system generates recommendations
- Then it infers preferences from profile and historical behavior (average applied-to salary, most common location interest, etc.); explicit preferences override inferred

## Assumptions
- **Location matching**: Uses job_seeker_location + job_location geographic coordinates; haversine distance algorithm; default radius 50 km, user-configurable.
- **Salary tolerance**: Jobs within range included as exact match; jobs within 10% above range soft-included with lower score; jobs 10%+ above range excluded (unless user explicitly "open to higher").
- **Work arrangement logic**: job_seeker.work_arrangement_preference (list: [on-site, hybrid, remote]) matched against job_posting.work_arrangements (enum); intersection determines inclusion.
- **Inference logic**: On first sign-up before preferences set, system analyzes job_seeker's profile to infer likely preferences; recalculated if job seeker remains inactive 30+ days.
- **Preference UI**: Simple toggles in job seeker profile; "Edit Preferences" modal allows specifying location (map picker), salary range (sliders), work arrangement (checkboxes).
- **Caching**: Recommendation results cached per job seeker + job set; cache invalidated on preference change or nightly refresh.

## Source Requirements
- [[3_3_2_Job_Recommendation_Engine|3.3.2]] — FR-89

## Related Stories
- [[US-3.3.2-01-see-personalized-job-recommendations|US-3.3.2-01]]
