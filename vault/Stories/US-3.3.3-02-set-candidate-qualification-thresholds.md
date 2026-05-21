---
story_id: "US-3.3.3-02"
title: "Set Candidate Qualification Thresholds"
section_id: "3.3.3"
related_requirements: ["FR-92"]
related_stories: ["US-3.3.3-01"]
role: "Employer Recruiter"
status: draft
priority: should
tags:
  - story
  - bc/matching
---

# US-3.3.3-02 — Set Candidate Qualification Thresholds

## Story
As an **Employer Recruiter**, I want to **define minimum qualification thresholds (overall match %, skill match %, experience level, education requirements, etc.) for my job posting**, so that **I can automatically filter the candidate pool to only those meeting my quality standards, reducing screening time**.

## Acceptance Criteria

**AC-01 — Threshold configuration UI**
- Given an employer is creating or editing a job posting
- When they scroll to "Advanced Filtering" or "Candidate Thresholds"
- Then they see a form to set: minimum overall match %, minimum skill match %, required education level, required experience level, required certifications

**AC-02 — Set overall match threshold**
- Given a recruiter sets a minimum overall match of 75%
- When candidates are recommended
- Then only candidates with overall match >= 75% are displayed; below-threshold candidates are hidden by default

**AC-03 — Set factor-specific thresholds**
- Given a recruiter needs to enforce strict skill requirements
- When they set "Minimum skill match: 85%"
- Then candidates are automatically filtered to those with skill match >= 85%, regardless of overall score

**AC-04 — Required education level**
- Given a job posting requires a Bachelor's degree
- When a recruiter sets "Education threshold: Bachelor's"
- Then candidates without at least a Bachelor's (or higher) are excluded from recommendations

**AC-05 — Required experience level**
- Given a job posting requires 5+ years of experience
- When a recruiter sets "Minimum experience: 5 years"
- Then candidates with experience < 5 years are excluded

**AC-06 — Threshold persistence**
- Given thresholds are set on a job posting
- When the posting is saved
- Then thresholds are stored and applied to all candidate recommendations for this posting
- When the recruiter returns to edit the posting, thresholds are visible and editable

**AC-07 — Visual feedback**
- Given thresholds are applied
- When a recruiter views the candidate list
- Then they see a summary banner (e.g., "Showing 42 of 315 candidates matching thresholds") and can toggle "Show all candidates" to bypass filtering

## Assumptions
- **Threshold options**: Overall match %, skill %, experience level (dropdown: entry/mid/senior, or years 0–50), education level (dropdown: high school/diploma/bachelor/master/phd), certifications (multi-select from job's required certifications list).
- **Default thresholds**: If not set by recruiter, global defaults used (e.g., 60% overall match, no education/experience minimum).
- **Filtering logic**: Thresholds applied at recommendation retrieval time; candidates cached with metadata allowing real-time filtering without re-computation.
- **Toggle mechanism**: "Show all candidates" button in UI allows recruiter to bypass thresholds to see full candidate list; toggle persists for this posting in session.
- **Feedback banner**: Shows "Showing X of Y matching your thresholds"; "X of Y" calculated via pre-aggregated counts in cache.
- **Threshold updates**: Changes take effect immediately on next candidate list load; no re-computation required (filtering done at retrieval).

## Source Requirements
- [[3_3_3_Candidate_Recommendation_for_Employers|3.3.3]] — FR-92

## Related Stories
- [[US-3.3.3-01-see-ranked-candidate-recommendations|US-3.3.3-01]]
