---
story_id: "US-3.3.1-05"
title: "Generate Shortlist of Top-Matching Candidates"
section_id: "3.3.1"
related_requirements: ["FR-72"]
related_stories: ["US-3.3.1-01", "US-3.3.3-01"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/matching
---

# US-3.3.1-05 — Generate Shortlist of Top-Matching Candidates

## Story
As a **System**, I want to **generate and maintain a ranked shortlist of the top-matching candidates (e.g., top 100) for each published job posting**, so that **employers can quickly identify the most suitable candidates without manual review of thousands of profiles**.

## Acceptance Criteria

**AC-01 — Initial shortlist generation**
- Given a job posting is published
- When the system completes matching
- Then it generates a ranked list of top candidates (default: top 100, configurable) sorted by match percentage descending

**AC-02 — Shortlist refresh**
- Given new job seekers join or existing profiles are updated
- When a job posting is active
- Then the shortlist is re-computed daily (or on-demand) to include recently qualified candidates

**AC-03 — Shortlist retrieval by employer**
- Given an employer views a job posting detail
- When they request the candidate shortlist
- Then they receive the ranked list with match scores, candidate names, and key qualifications visible (respecting privacy settings)

**AC-04 — Configurable shortlist size**
- Given administrators need to adjust shortlist scope
- When they configure parameters
- Then the system respects the new top-N value on next computation

**AC-05 — Performance at scale**
- Given there are 10,000+ candidates and 100+ active postings
- When the system generates shortlists
- Then computation completes within 5 minutes and results are cached for sub-second retrieval

## Assumptions
- **Shortlist size**: Default 100; configurable per posting or globally; excludes candidates below match threshold.
- **Ranking stability**: Shortlist re-computed daily at off-peak hours (2 AM UTC); explicit "refresh" button available for recruiters if needed.
- **Caching**: Shortlists stored in cache layer (Redis/Memcached) with TTL = 24 hours; invalidated on profile updates.
- **Privacy respect**: Shortlist respects candidate privacy settings; candidates with "hidden profile" excluded unless they applied.
- **Performance**: Vectorized batch computation using matrix operations; shortlisting scales to 100K+ profiles with <5 min latency.
- **Deduplication**: Only active, non-duplicate profiles included (blocks job seeker re-registrations with same email).

## Source Requirements
- [[3_3_1_A_Vector_Based_Skills_Based_and_Behavior_Matching_Algorithms|3.3.1]] — FR-72

## Related Stories
- [[US-3.3.1-01-implement-ai-driven-matching-algorithm|US-3.3.1-01]]
- [[US-3.3.3-01-see-ranked-candidate-recommendations|US-3.3.3-01]]
