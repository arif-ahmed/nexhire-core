---
story_id: "US-3.2.2-01"
title: "Search jobs with basic and advanced modes"
section_id: "3.2.2"
related_requirements: ["FR-59", "FR-60", "FR-61"]
related_stories: ["US-3.2.2-02", "US-3.2.3-01"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/search
---

# US-3.2.2-01 — Search jobs with basic and advanced modes

## Story
As a **Job Seeker**, I want to search for jobs using both a simple keyword search and advanced filters, so that I can quickly find relevant opportunities or conduct detailed job searches.

## Acceptance Criteria

**AC-01 — Basic search with single keyword**
- Given I am on the job search page
- When I enter a single keyword in the search bar (e.g., "Python developer")
- Then the system returns jobs matching that keyword in title, summary, or skills; results load within 2 seconds

**AC-02 — Basic search returns results ranked by relevance**
- Given I perform a basic keyword search
- When results are displayed
- Then they are ordered by relevance (e.g., title matches ranked higher than skill matches)

**AC-03 — Advanced search mode is available**
- Given I am searching for jobs
- When I click "Advanced Search" or equivalent
- Then I see a form with additional filter options (detailed in AC-05 below)

**AC-04 — Semantic search understands intent**
- Given I search for "remote work opportunities in tech"
- When the system processes this query
- Then it interprets "remote" as work format, "tech" as industry/skills, and returns matching results even if those exact terms aren't in job titles

**AC-05 — Advanced filters available**
- Given I am in advanced search mode
- When I view the filter panel
- Then I see options: keywords, location, salary range, employment type, date posted, application deadline, job requirements (skills, education, experience), and company sector/industry

**AC-06 — Can combine multiple filters**
- Given I have selected multiple filters (e.g., "Python", location "New York", employment type "full-time")
- When I apply filters
- Then results are narrowed to jobs matching all selected criteria (AND logic)

**AC-07 — Filters persist in session**
- Given I apply filters and view results
- When I navigate back to the search form
- Then the previously applied filters remain selected

## Assumptions
- Semantic search is powered by NLP; exact implementation deferred.
- "Relevance" ranking logic is not specified—assuming title match > skill match > summary match.
- Salary range filtering may be optional if salary is not disclosed; jobs without salary are shown/hidden based on user preference.
- "Date posted" filter supports relative ranges (e.g., "last 7 days") and absolute date ranges.
- "Location" supports geographic radius search; exact mapping of "location" to work format deferred (e.g., "New York" may return remote jobs if employer permits or hybrid roles).

## Source Requirements
- [[3_2_2_Job_Search_and_Filtering|3.2.2]] — FR-59, FR-60, FR-61

## Related Stories
- [[US-3.2.2-02-...|US-3.2.2-02 Filter and rank search results]]
- [[US-3.2.3-01-...|US-3.2.3-01 Save favorite jobs]]
