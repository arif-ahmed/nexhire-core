---
story_id: "US-3.7.1-02"
title: "Schedule Article Publication"
section_id: "3.7.1"
related_requirements: ["FR-161", "FR-162"]
related_stories: ["US-3.7.1-01", "US-3.7.1-04"]
role: "Content Editor"
status: draft
priority: should
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.1-02 — Schedule Article Publication

## Story
As a **Content Editor**, I want **to schedule news articles for publication at a future date and time**, so that **I can plan content releases in advance without manual intervention**.

## Acceptance Criteria

**AC-01 — Set publication date and time**
- Given I am composing a news article
- When I toggle the scheduling option
- Then I can select a future date and time for automatic publication

**AC-02 — Schedule persists across sessions**
- Given I have scheduled an article
- When I close the editor and log out
- Then the article remains scheduled and publishes at the designated time

**AC-03 — Scheduled article in draft state**
- Given I have scheduled an article
- When the current time is before the scheduled time
- Then the article remains in draft/unpublished status

**AC-04 — Auto-publish at scheduled time**
- Given an article is scheduled for time T
- When the system time reaches T
- Then the article automatically publishes without editor intervention

**AC-05 — Edit or cancel scheduled article**
- Given I have a scheduled article
- When I open the article editor
- Then I can modify the scheduled time or cancel the schedule before publication

## Assumptions
- Scheduling uses server-side job queue (e.g., Celery, Bull, or similar)
- Scheduled articles default to same visibility rules as immediate publications
- Time zone defaults to system/user locale; no explicit tz configuration in MVP
- Cancelled schedules do not create database debris (soft delete or cleanup)

## Source Requirements
- [[3_7_1_News_and_Updates|3.7.1]] — FR-161, FR-162

## Related Stories
- [[US-3.7.1-01-publish-news-article|US-3.7.1-01 Publish News Article with Rich Content]]
- [[US-3.7.1-04-archive-and-unpublish-articles|US-3.7.1-04 Archive and Unpublish Articles]]
