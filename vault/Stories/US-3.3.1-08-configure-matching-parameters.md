---
story_id: "US-3.3.1-08"
title: "Configure Matching Algorithm Parameters"
section_id: "3.3.1"
related_requirements: ["FR-78"]
related_stories: ["US-3.3.1-01"]
role: "Data Scientist"
status: draft
priority: should
tags:
  - story
  - bc/matching
---

# US-3.3.1-08 — Configure Matching Algorithm Parameters

## Story
As a **Data Scientist**, I want to **adjust the importance weights of different matching factors (skill, education, location, etc.) to optimize matching quality**, so that **the algorithm can be tuned based on market feedback and platform performance metrics**.

## Acceptance Criteria

**AC-01 — Access configuration interface**
- Given a data scientist needs to tune the matching algorithm
- When they access the admin panel
- Then they can view current factor weights (skill, education, training, location, experience, salary)

**AC-02 — Update factor weights**
- Given the current weights are displayed
- When a data scientist adjusts one or more weights
- Then the new weights are validated (sum to 100% or 1.0), saved, and applied to new matches within 1 minute

**AC-03 — Weight persistence**
- Given weights are updated
- When the system is restarted
- Then previously saved weights are restored; no rollback to defaults occurs unintentionally

**AC-04 — A/B testing support**
- Given two candidate sets need to be tested with different weight profiles
- When a data scientist creates a new weight variant
- Then the system supports assigning a percentage of new users/postings to each variant; metrics are tracked separately

**AC-05 — Performance metrics dashboard**
- Given weights have been applied
- When a data scientist views the performance dashboard
- Then they see metrics: match quality (feedback ratings), placement rate, time-to-hire, and job seeker satisfaction scores by weight variant

**AC-06 — Rollback capability**
- Given a new weight configuration performs poorly
- When a data scientist selects "Rollback to previous"
- Then the system restores the prior weights and resumes using them for new matches; audit log records the action

## Assumptions
- **Weight format**: Stored as JSON object: `{skill: 0.25, education: 0.15, training: 0.10, location: 0.15, experience: 0.20, salary: 0.15}`.
- **Validation**: Weights validated to sum to 1.0 (tolerance 0.01); UI shows running sum as user adjusts.
- **Rollout**: New weights applied to all new matches immediately; in-flight computations may use prior weights (eventual consistency).
- **A/B testing**: Variant assignment done at job-seeker registration or posting creation (stratified random); 50/50 split by default, configurable.
- **Metrics tracking**: Stored in analytics DB; dashboard queries and displays weekly aggregates by variant.
- **Rollback history**: Last 10 weight configurations retained; one-click rollback to any prior version.
- **Monitoring**: System alerts if match quality metrics drop > 10% after weight change.

## Source Requirements
- [[3_3_1_A_Vector_Based_Skills_Based_and_Behavior_Matching_Algorithms|3.3.1]] — FR-78

## Related Stories
- [[US-3.3.1-01-implement-ai-driven-matching-algorithm|US-3.3.1-01]]
