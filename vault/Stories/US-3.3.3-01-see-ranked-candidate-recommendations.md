---
story_id: "US-3.3.3-01"
title: "See Ranked Candidate Recommendations for Job Posting"
section_id: "3.3.3"
related_requirements: ["FR-90", "FR-91"]
related_stories: ["US-3.3.1-01", "US-3.3.1-05"]
role: "Employer Recruiter"
status: draft
priority: must
tags:
  - story
  - bc/matching
---

# US-3.3.3-01 — See Ranked Candidate Recommendations for Job Posting

## Story
As an **Employer Recruiter**, I want to **view ranked candidate recommendations for my job posting, highlighting each candidate's strengths and gaps**, so that **I can efficiently identify and evaluate the best-fit candidates without manually searching hundreds of profiles**.

## Acceptance Criteria

**AC-01 — Candidate list view**
- Given an employer views their job posting
- When they click "View Candidates" or "Recommended Candidates"
- Then they see a ranked list of recommended candidates (default: top 100, configurable), sorted by match score descending

**AC-02 — Match score and breakdown**
- Given a candidate is shown in the list
- When the recruiter views the candidate row
- Then they see: overall match % (e.g., 92%), match breakdown (skills 90%, experience 85%, education 88%), and a visual indicator (e.g., color-coded)

**AC-03 — Strengths and gaps**
- Given a candidate row is displayed
- When the recruiter clicks to expand or view candidate details
- Then they see highlighted strengths (e.g., "Excellent skill match: Python 10+ years") and potential gaps (e.g., "Location: Overseas, preference on-site")

**AC-04 — Privacy-respecting display**
- Given a candidate has privacy settings enabled
- When the recruiter views the candidate list
- Then sensitive information (phone number, exact location) is hidden; only explicitly public profile details are shown unless the candidate applied directly

**AC-05 — Candidate profile view**
- Given a recruiter clicks on a candidate name
- When the profile opens
- Then they see a summary: extracted resume information, match explanation, application status (if applicable), and an action button (e.g., "Contact", "Shortlist", "Reject")

**AC-06 — Batch actions**
- Given multiple candidates are displayed
- When the recruiter selects candidates using checkboxes
- Then they can perform batch actions: "Send message", "Add to talent pool", "Archive", etc.

## Assumptions
- **Ranking**: Candidates ordered by match % descending; ties broken by most recent profile update.
- **Breakdown detail**: Match % shown in list; expanded view shows per-factor scores (skill%, experience%, education%, location%, salary%, training%).
- **Strengths/gaps**: Auto-extracted from match scoring logic; strengths = factors > 80%; gaps = factors < 60%; generated in recommendation query.
- **Privacy**: Respects candidate's privacy_level setting (public/apply-only/hidden); hidden profiles not shown to employer unless candidate applied.
- **Candidate profile modal**: Shows: parsed resume, match details, messaging/action buttons; no personally identifiable information unless candidate public or applied.
- **Batch size**: UI loads 20 candidates per page with lazy loading; pagination or infinite scroll supported; batch operations on visible candidates only (safety measure).

## Source Requirements
- [[3_3_3_Candidate_Recommendation_for_Employers|3.3.3]] — FR-90, FR-91

## Related Stories
- [[US-3.3.1-01-implement-ai-driven-matching-algorithm|US-3.3.1-01]]
- [[US-3.3.1-05-generate-shortlist-top-matching-candidates|US-3.3.1-05]]
- [[US-3.3.3-02-set-candidate-qualification-thresholds|US-3.3.3-02]]
