---
story_id: "US-3.7.2-03"
title: "Search Help Content"
section_id: "3.7.2"
related_requirements: ["FR-169"]
related_stories: ["US-3.7.2-02", "US-3.7.2-04"]
role: "Visitor"
status: draft
priority: must
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.2-03 — Search Help Content

## Story
As a **Visitor**, I want **to search the FAQ and help center by keyword**, so that **I can quickly find answers without browsing the full category hierarchy**.

## Acceptance Criteria

**AC-01 — Keyword search**
- Given I am on the help center
- When I enter a search term in the search box
- Then the system returns FAQ entries and help articles whose title or content matches the term

**AC-02 — Search respects role filtering**
- Given I am logged in as a Job Seeker
- When I search the help center
- Then results include only entries visible to my role

**AC-03 — Search highlights matches**
- Given I have performed a search
- When I open a result
- Then search terms are highlighted in the question and answer text

**AC-04 — Result ranking by relevance**
- Given I have performed a search
- When results are displayed
- Then entries matching in the title appear above entries matching only in the body

**AC-05 — Case-insensitive search**
- Given I search for "Wage"
- When results are returned
- Then entries with "wage", "WAGE", or "Wage" are all included

**AC-06 — Bilingual search**
- Given I search in Bangla
- When I perform the search
- Then results are limited to Bangla-language entries only

## Assumptions
- Search is simple full-text matching (not fuzzy or Levenshtein distance)
- Relevance ranking is based on field type (title > body); no popularity/click-based ranking in MVP
- Search is case-insensitive by default
- Search results include role-filtered content automatically
- Search excludes draft entries

## Source Requirements
- [[3_7_2_FAQ_and_Help_Center|3.7.2]] — FR-169

## Related Stories
- [[US-3.7.2-02-organize-help-by-topic-and-role|US-3.7.2-02 Organize Help Content by Topic and Role]]
- [[US-3.7.2-04-context-sensitive-help|US-3.7.2-04 Provide Context-Sensitive Help]]
