---
story_id: "US-3.7.1-03"
title: "Categorize and Tag Articles"
section_id: "3.7.1"
related_requirements: ["FR-164"]
related_stories: ["US-3.7.1-01", "US-3.7.1-05"]
role: "Content Editor"
status: draft
priority: must
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.1-03 — Categorize and Tag Articles

## Story
As a **Content Editor**, I want **to assign categories and tags to news articles**, so that **users can discover relevant content through filtering and search**.

## Acceptance Criteria

**AC-01 — Assign category to article**
- Given I am composing a news article
- When I open the categorization panel
- Then I can select one primary category from a predefined list

**AC-02 — Add tags to article**
- Given I am composing a news article
- When I enter the tags field
- Then I can add multiple free-form tags (comma-separated or chip-based)

**AC-03 — Tags are bilingual**
- Given I am tagging an article in English
- When I switch to the Bangla version
- Then I can assign Bangla-language tags independently

**AC-04 — Category/tag validation**
- Given I have assigned tags and category
- When I attempt to publish
- Then the system validates that at least one category is assigned before allowing publication

**AC-05 — Auto-suggest tags**
- Given I am typing a tag
- When the tag matches existing tags in the system
- Then the system auto-suggests matching tags

## Assumptions
- Categories are admin-defined; editors cannot create new categories
- Tags are free-form; no strict vocabulary control in MVP
- Each article has exactly one primary category (multi-select is future enhancement)
- Category/tag assignment is optional during draft but required for publication
- Tags are stored as a normalized array to support case-insensitive search

## Source Requirements
- [[3_7_1_News_and_Updates|3.7.1]] — FR-164

## Related Stories
- [[US-3.7.1-01-publish-news-article|US-3.7.1-01 Publish News Article with Rich Content]]
- [[US-3.7.1-05-browse-and-filter-news|US-3.7.1-05 Browse and Filter News Articles]]
