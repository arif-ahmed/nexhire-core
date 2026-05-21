---
story_id: "US-3.3.1-07"
title: "Rank Jobs by Match Percentage for Job Seeker"
section_id: "3.3.1"
related_requirements: ["FR-76"]
related_stories: ["US-3.3.2-01"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/matching
---

# US-3.3.1-07 — Rank Jobs by Match Percentage for Job Seeker

## Story
As a **Job Seeker**, I want to **see job postings ranked by their match percentage to my profile**, so that **the most relevant opportunities are displayed first and I can quickly identify the best fits**.

## Acceptance Criteria

**AC-01 — Default ranking**
- Given a job seeker views the job search results
- When results load
- Then jobs are ordered by match percentage descending (highest match first)

**AC-02 — Match score visibility**
- Given a job listing is displayed
- When the job seeker views it
- Then they see the match percentage prominently displayed (e.g., "92% match")

**AC-03 — Match explanation**
- Given a job seeker sees a match percentage
- When they click "View match details" or similar
- Then they see a breakdown of matching factors (skills 90%, location 100%, experience 80%, etc.)

**AC-04 — Sorting flexibility**
- Given a job seeker is on the search results page
- When they click "Sort by relevance" or "Sort by match %"
- Then results are re-sorted by match percentage descending; other sort options (date posted, salary) remain available

**AC-05 — Threshold application**
- Given job postings below the minimum match threshold exist
- When a job seeker views search results
- Then below-threshold jobs are either hidden or shown with a "Low match" label; filtering preference is user-configurable

## Assumptions
- **Default sort**: Jobs ranked by match % descending on initial load; persists across pagination.
- **Match display**: Shown as percentage (0–100) in listing and as detailed breakdown in expanded view.
- **Breakdown**: Factors shown: overall %, skill%, location %, salary %, education %, experience %.
- **Performance**: Match precomputation occurs nightly; results cached; search results returned within 500ms.
- **Below-threshold handling**: Default is hide; job seeker can toggle "Show low matches" in filters; toggle persists in session.
- **Stability**: Match scores do not change during a user session (even if profile updated elsewhere); shown as "last updated X hours ago".

## Source Requirements
- [[3_3_1_A_Vector_Based_Skills_Based_and_Behavior_Matching_Algorithms|3.3.1]] — FR-76

## Related Stories
- [[US-3.3.2-01-see-personalized-job-recommendations|US-3.3.2-01]]
