---
story_id: "US-3.7.1-06"
title: "Search News Archive"
section_id: "3.7.1"
related_requirements: ["FR-166"]
related_stories: ["US-3.7.1-04", "US-3.7.1-05"]
role: "Visitor"
status: draft
priority: should
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.1-06 — Search News Archive

## Story
As a **Visitor**, I want **to search past news articles by title, content, and date range**, so that **I can find specific historical updates without manually browsing the archive**.

## Acceptance Criteria

**AC-01 — Full-text search**
- Given I am in the news archive
- When I enter a search term in the search box
- Then the system returns articles whose title or body contains the term (case-insensitive)

**AC-02 — Date range filter**
- Given I am searching the archive
- When I select a date range (start and end date)
- Then search results are limited to articles published within that range

**AC-03 — Search by title**
- Given I am searching the archive
- When I search for a term
- Then articles matching the term in title appear first in results

**AC-04 — Search results pagination**
- Given I have performed a search
- When results exceed 20 items
- Then results are paginated with page navigation controls

**AC-05 — No results message**
- Given I have performed a search
- When no articles match the criteria
- Then the system displays a helpful message suggesting alternative searches

**AC-06 — Search is bilingual**
- Given I am searching in a specific language (EN or Bangla)
- When I perform a search
- Then the search targets articles in that language only

## Assumptions
- Search is simple full-text matching; no advanced Boolean or fuzzy search in MVP
- Indexing is database-driven (LIKE queries or basic full-text search)
- Search is case-insensitive
- Archive includes unpublished and archived articles, not drafts
- Search results are not cached; each query is executed in real-time

## Source Requirements
- [[3_7_1_News_and_Updates|3.7.1]] — FR-166

## Related Stories
- [[US-3.7.1-04-archive-and-unpublish-articles|US-3.7.1-04 Archive and Unpublish Articles]]
- [[US-3.7.1-05-browse-and-filter-news|US-3.7.1-05 Browse and Filter News Articles]]
