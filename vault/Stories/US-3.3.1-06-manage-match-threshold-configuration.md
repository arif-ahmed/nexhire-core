---
story_id: "US-3.3.1-06"
title: "Manage Match Threshold Configuration"
section_id: "3.3.1"
related_requirements: ["FR-75"]
related_stories: []
role: "MoL Administrator"
status: draft
priority: should
tags:
  - story
  - bc/matching
---

# US-3.3.1-06 — Manage Match Threshold Configuration

## Story
As a **MoL Administrator**, I want to **define and adjust the minimum match threshold (e.g., 60%) that determines which job matches are displayed to users**, so that **I can control match quality and ensure only relevant opportunities reach job seekers and employers**.

## Acceptance Criteria

**AC-01 — View current threshold**
- Given an administrator accesses matching configuration
- When they view the dashboard
- Then they see the current global minimum match threshold and any posting-specific overrides

**AC-02 — Update threshold**
- Given an administrator decides to change the match threshold
- When they update it (e.g., 60% → 65%)
- Then the change takes effect immediately; all future matches below the new threshold are filtered from display

**AC-03 — Threshold range validation**
- Given an administrator enters a new threshold value
- When they submit it
- Then the system validates it is between 0% and 100%; values outside this range are rejected with a clear error message

**AC-04 — Per-posting override**
- Given some job postings may have unique quality requirements
- When an administrator configures a posting-specific threshold
- Then matches for that posting use the posting-specific value instead of the global default

**AC-05 — Audit logging**
- Given a threshold change is made
- When it is applied
- Then the change is logged with timestamp, admin user, old value, and new value for compliance and troubleshooting

**AC-06 — Impact preview**
- Given an administrator wants to understand the effect of a threshold change
- When they adjust the value in a preview mode
- Then the system shows estimate of how many matches would be filtered (e.g., "changing to 70% would hide 15% of current matches")

## Assumptions
- **Global default**: 60% used as baseline; can be adjusted 0–100%.
- **Per-posting override**: Allowed if recruiter explicitly requests it during job creation; default inherits global setting.
- **Persistence**: Threshold configuration stored in a dedicated config table; changed atomically to ensure consistency.
- **Caching invalidation**: Changes trigger cache invalidation for all active shortlists; re-filtering happens on next retrieval.
- **Impact calculation**: Statistical sampling used for preview to avoid re-filtering all matches; shown as percentage estimate.
- **Audit table**: All threshold changes logged with admin ID, timestamp, before/after values; queryable for compliance audits.

## Source Requirements
- [[3_3_1_A_Vector_Based_Skills_Based_and_Behavior_Matching_Algorithms|3.3.1]] — FR-75

## Related Stories
- [[US-3.3.1-01-implement-ai-driven-matching-algorithm|US-3.3.1-01]]
