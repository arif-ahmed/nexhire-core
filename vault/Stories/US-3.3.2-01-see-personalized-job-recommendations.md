---
story_id: "US-3.3.2-01"
title: "See Personalized Job Recommendations"
section_id: "3.3.2"
related_requirements: ["FR-85", "FR-87", "FR-88", "FR-89"]
related_stories: ["US-3.3.1-01", "US-3.3.1-07"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/matching
---

# US-3.3.2-01 — See Personalized Job Recommendations

## Story
As a **Job Seeker**, I want to **receive personalized job recommendations based on my profile, preferences, activity, and similar users' behavior**, so that **I can discover opportunities without manually searching, improving my chances of finding a good match**.

## Acceptance Criteria

**AC-01 — Recommendation feed**
- Given a job seeker logs in
- When they view the dashboard or "Recommended for You" tab
- Then they see a feed of 5–10 personalized job recommendations ranked by relevance

**AC-02 — Collaborative filtering**
- Given job seekers with similar profiles have applied to or favorited certain jobs
- When the system generates recommendations for the current job seeker
- Then it includes jobs similar to those interactions by similar users (collaborative filtering)

**AC-03 — Content-based filtering**
- Given a job seeker has shown interest in specific jobs or job categories
- When recommendations are generated
- Then they include other postings with similar skills, industry, or role level (content-based filtering)

**AC-04 — Preference consideration**
- Given a job seeker has set location, salary, and work arrangement preferences
- When recommendations are calculated
- Then all matching recommendations respect these preferences; non-matching jobs are deprioritized or hidden

**AC-05 — Daily refresh**
- Given a job seeker has recommendations displayed
- When they return the next day
- Then recommendations are refreshed; new matching jobs are included; previously declined jobs are not re-shown

**AC-06 — Feedback loop**
- Given a job seeker sees a recommendation
- When they click "Not interested", apply, or view details
- Then the system records this feedback and improves future recommendations

## Assumptions
- **Collaborative filtering**: User-item interactions (applies, favorites, clicks, view duration) used to compute user similarity; recommendations from top 50 similar users; update daily batch.
- **Content-based filtering**: Job embeddings computed nightly; similarity measured via cosine distance in skill/requirement vector space.
- **Hybrid ranking**: Collaborative score (40%) + content score (40%) + match score from 3.3.1 (20%); threshold applied (≥60% overall).
- **Freshness**: Recommendations computed nightly (2 AM UTC); new jobs included same day; refresh triggered manually via "Get fresh recommendations" button.
- **Decay**: Declining jobs stored in job_seeker_feedback table; re-shown only if 14+ days elapse or job updated.
- **Cold-start**: New job seekers receive content-based recommendations only (no collaborative history) until 5+ interactions recorded.

## Source Requirements
- [[3_3_2_Job_Recommendation_Engine|3.3.2]] — FR-85, FR-87, FR-88, FR-89

## Related Stories
- [[US-3.3.1-01-implement-ai-driven-matching-algorithm|US-3.3.1-01]]
- [[US-3.3.1-07-rank-jobs-by-match-percentage|US-3.3.1-07]]
- [[3_6_2_In_App_Notifications|3.6.2]] (for notification of new recommendations)
