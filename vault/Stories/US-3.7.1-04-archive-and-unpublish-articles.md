---
story_id: "US-3.7.1-04"
title: "Archive and Unpublish Articles"
section_id: "3.7.1"
related_requirements: ["FR-166"]
related_stories: ["US-3.7.1-02", "US-3.7.1-06"]
role: "Content Editor"
status: draft
priority: should
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.1-04 — Archive and Unpublish Articles

## Story
As a **Content Editor**, I want **to archive or unpublish news articles**, so that **I can manage the lifecycle of content (remove from active feed while preserving history)**.

## Acceptance Criteria

**AC-01 — Unpublish article**
- Given I have a published article
- When I click the unpublish button
- Then the article is removed from active news feeds and dashboards

**AC-02 — Archive article**
- Given I have an unpublished article
- When I click the archive button
- Then the article is moved to archive and no longer appears in active or unpublished lists

**AC-03 — Archived article is searchable**
- Given an article has been archived
- When a user searches the news archive
- Then the archived article appears in search results with archive status indicator

**AC-04 — Restore from archive**
- Given I have archived an article
- When I access the archive
- Then I can restore the article to published or unpublished state

**AC-05 — Bulk archive operation**
- Given I have multiple articles to archive
- When I select multiple articles and choose archive action
- Then all selected articles are archived in a single operation

## Assumptions
- Unpublish removes from public view but keeps in database; archive soft-deletes with restore capability
- Archived articles are preserved for historical/legal compliance reasons
- Archive search is separate from live article search
- Bulk operations are limited to 50 articles per request to prevent performance degradation
- Restore action restores to previous publication state (published → published, draft → draft)

## Source Requirements
- [[3_7_1_News_and_Updates|3.7.1]] — FR-166

## Related Stories
- [[US-3.7.1-02-schedule-news-publication|US-3.7.1-02 Schedule Article Publication]]
- [[US-3.7.1-06-search-news-archive|US-3.7.1-06 Search News Archive]]
