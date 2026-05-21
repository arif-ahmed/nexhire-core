---
story_id: "US-3.2.2-02"
title: "Filter and rank search results with AI recommendations"
section_id: "3.2.2"
related_requirements: ["FR-62", "FR-63"]
related_stories: ["US-3.2.2-01", "US-3.2.2-03", "US-3.3.2-01"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/search
---

# US-3.2.2-02 — Filter and rank search results with AI recommendations

## Story
As a **Job Seeker**, I want search results ranked by skills-matching relevance and to see AI-recommended jobs when I am logged in, so that I can find the most suitable opportunities efficiently.

## Acceptance Criteria

**AC-01 — Results ranked by skills-matching score**
- Given I perform a search
- When results are displayed
- Then jobs are ranked by a skills-matching score (how well my profile/search criteria align with job requirements); highest-match jobs appear first

**AC-02 — Sorting options available**
- Given I view search results
- When I access the sort menu
- Then I can sort by: relevance (default), match score, date posted, salary (if available), or application deadline

**AC-03 — Can apply additional filters to results**
- Given I am viewing search results
- When I adjust filters (e.g., narrow salary range, change location radius)
- Then results re-filter in real time without requiring a new search

**AC-04 — AI recommendations shown when logged in**
- Given I am a logged-in job seeker
- When I view search results
- Then the system displays AI-recommended jobs based on my profile, past searches, and skills (in addition to search query results)

**AC-05 — Recommendations are clearly distinguished**
- Given I view search results with recommendations
- When I see the page
- Then recommended jobs are visually separated or marked as "Recommended for you" to differentiate from query results

**AC-06 — Recommendations improve over time**
- Given I interact with recommended jobs (e.g., save, apply, view details)
- When the AI matching engine processes this feedback
- Then future recommendations become more accurate and relevant

**AC-07 — Can hide or dismiss recommendations**
- Given I see recommended jobs I don't want
- When I click "dismiss" or "not interested"
- Then that job is removed from my recommendations (at least for this session)

## Assumptions
- Skills-matching score is computed by the AI matching engine (detailed in 3.3.2); implementation deferred.
- Recommendations are generated only for authenticated users with profiles; anonymous job seekers see query results only.
- Real-time filtering assumes database queries perform within acceptable latency (assumed <500ms).
- "Relevance" vs. "match score" may use the same underlying calculation; exact differentiation deferred.
- Dismissing a recommendation does not permanently block that job from all future results.

## Source Requirements
- [[3_2_2_Job_Search_and_Filtering|3.2.2]] — FR-62, FR-63

## Related Stories
- [[US-3.2.2-01-...|US-3.2.2-01 Search with basic/advanced modes]]
- [[US-3.2.2-03-...|US-3.2.2-03 Save favorite jobs and searches]]
- [[3_3_2_Job_Recommendation_Engine|3.3.2 Job Recommendation Engine]]
