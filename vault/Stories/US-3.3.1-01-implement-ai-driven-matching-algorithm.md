---
story_id: "US-3.3.1-01"
title: "Implement AI-Driven Matching Algorithm"
section_id: "3.3.1"
related_requirements: ["FR-70", "FR-71", "FR-77"]
related_stories: []
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/matching
  - bc/skill-taxonomy
  - topic/ai
---

# US-3.3.1-01 — Implement AI-Driven Matching Algorithm

## Story
As a **System**, I want to **compute a multi-factor match score between job seekers and job postings based on skills, education, training, location, experience, and salary**, so that **I can enable two-way matching for both job seekers and employers**.

## Acceptance Criteria

**AC-01 — Match score computation**
- Given a job seeker profile and a job posting
- When the system computes a match score
- Then the score reflects weighted contributions from: skill overlap, education match, training match, location proximity, experience range alignment, and salary expectation range overlap

**AC-02 — Scoring weights configuration**
- Given system administrators have access to parameter configuration
- When they adjust factor weights
- Then the matching algorithm respects the new weights on subsequent computations

**AC-03 — Bi-directional ranking**
- Given a job posting is published
- When the system completes initial matching
- Then it produces ranked lists of top-matching job seekers for the employer AND ranked lists of top matching jobs for each seeking applicant

**AC-04 — Match threshold enforcement**
- Given administrators have set a minimum match threshold (e.g., 60%)
- When computing matches
- Then only matches at or above the threshold are surfaced to users

## Assumptions
- **Vector embedding approach**: System uses dense vector embeddings (e.g., 768-dim) for skills and job descriptions to enable semantic similarity beyond keyword matching; embedding model refreshed nightly.
- **Scoring method**: Multi-factor scoring uses weighted linear combination (weights configurable 0–100, must sum to 100). Factors: skill (25%), education (15%), training (10%), location (15%), experience (20%), salary (15%).
- **"Top matching"** means highest percentile; default top 100 for employers; configurable.
- **Match threshold**: Default 60%, configurable 0–100% by administrators; applied at display time, not storage.
- **Location matching**: Uses geographic distance (haversine formula) with configurable radius (default 50 km); remote roles flagged separately.
- **Cold-start handling**: New job seekers matched on education + location; new postings matched on skill keywords until sufficient behavioral data.

## Source Requirements
- [[3_3_1_A_Vector_Based_Skills_Based_and_Behavior_Matching_Algorithms|3.3.1]] — FR-70, FR-71, FR-77

## Related Stories
- [[US-3.3.1-02-perform-nlp-semantic-analysis|US-3.3.1-02]]
- [[US-3.3.1-03-parse-resume-and-extract-skills|US-3.3.1-03]]
- [[US-3.3.2-01-see-personalized-job-recommendations|US-3.3.2-01]]
- [[US-3.3.3-01-see-ranked-candidate-recommendations|US-3.3.3-01]]
