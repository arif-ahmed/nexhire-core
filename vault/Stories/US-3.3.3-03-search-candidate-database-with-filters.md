---
story_id: "US-3.3.3-03"
title: "Search Candidate Database with Advanced Filtering"
section_id: "3.3.3"
related_requirements: ["FR-93"]
related_stories: []
role: "Employer Recruiter"
status: draft
priority: should
tags:
  - story
  - bc/matching
---

# US-3.3.3-03 — Search Candidate Database with Advanced Filtering

## Story
As an **Employer Recruiter**, I want to **search the entire candidate database using advanced filters (skills, experience, location, salary, education, certifications)**, so that **I can find and evaluate candidate pools even for new postings without pre-existing recommendations**.

## Acceptance Criteria

**AC-01 — Advanced search interface**
- Given an employer accesses the "Candidate Search" or "Talent Pool" section
- When they view the search interface
- Then they see advanced filter options: skills (multi-select with autocomplete), experience level, experience years (range), location/radius, salary range, education level, certifications

**AC-02 — Skill-based search**
- Given a recruiter wants to find candidates with specific skills
- When they type "Python" in the skill filter
- Then the system autocompletes matching skills from the skill taxonomy and shows candidates with Python proficiency

**AC-03 — Location-based search**
- Given a recruiter enters a location and radius (e.g., "New York, 30 km")
- When results load
- Then candidates within the specified radius are returned; remote candidates shown separately or included based on a toggle

**AC-04 — Experience and education filters**
- Given a recruiter sets filters (e.g., "5+ years experience", "Bachelor's degree or higher")
- When results load
- Then candidates matching all selected criteria are returned

**AC-05 — Salary range filter**
- Given a recruiter specifies a salary budget (e.g., 50K–70K)
- When results load
- Then candidates with salary expectations within or near the range are prioritized/included based on a toggle

**AC-06 — Results display and sorting**
- Given a candidate search returns results
- When the recruiter views the list
- Then results display candidate name, key skills, experience, location, match % to the implicit job profile (if inferrable from filters); can sort by relevance, experience, location, etc.

**AC-07 — Filter persistence**
- Given a recruiter sets multiple filters
- When they run a search
- Then filter values persist across pagination; recruiter can save the filter combination as a "Saved Search" for future re-use

**AC-08 — Privacy-respecting results**
- Given a candidate search is performed
- When results are displayed
- Then candidates with "hidden" privacy settings are not shown unless they have applied to one of the recruiter's postings

## Assumptions
- **Search performance**: Full-text search on candidate profiles (name, skills, experience description) + faceted search on categorical fields (experience level, education, location); typical query < 500ms for 100K+ candidates via Elasticsearch or similar.
- **Skill matching**: Skills in search autocomplete sourced from canonical skill taxonomy; fuzzy matching (Levenshtein distance) enabled to handle misspellings.
- **Location search**: Candidates indexed by geographic coordinates; radius search via Elasticsearch geo queries; remote candidates tagged and returned separately or based on toggle.
- **Salary inference**: Candidate salary expectation extracted from profile or resume; salary range filter matches against this field; jobs with no stated salary are included/excluded based on recruiter preference.
- **Privacy filtering**: Candidate privacy_level checked before results returned; only public or applied-to candidates shown unless recruiter is explicitly viewing own job posting candidates.
- **Saved searches**: Stored in recruiter profile; max 20 saved searches; reusable with one-click "Run search" button.

## Source Requirements
- [[3_3_3_Candidate_Recommendation_for_Employers|3.3.3]] — FR-93

## Related Stories
- [[US-3.3.3-01-see-ranked-candidate-recommendations|US-3.3.3-01]]
