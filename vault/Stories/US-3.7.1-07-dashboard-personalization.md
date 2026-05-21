---
story_id: "US-3.7.1-07"
title: "Personalize Dashboard News Display"
section_id: "3.7.1"
related_requirements: ["FR-165"]
related_stories: ["US-3.7.1-05"]
role: "Job Seeker"
status: draft
priority: could
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.1-07 — Personalize Dashboard News Display

## Story
As a **Job Seeker**, I want **to see news and updates on my dashboard that are relevant to my profile (sector, location, job interests)**, so that **I receive targeted information without searching**.

## Acceptance Criteria

**AC-01 — Profile-based filtering**
- Given I have completed my user profile (sector, location, job interests)
- When I view my dashboard
- Then the news widget displays articles tagged with categories matching my profile

**AC-02 — Default fallback**
- Given I have not completed my profile
- When I view my dashboard
- Then the news widget displays the most recent articles globally

**AC-03 — Manual widget customization**
- Given I am on my dashboard
- When I click the widget settings
- Then I can manually select categories to display or hide

**AC-04 — Dismissible articles**
- Given an article appears on my dashboard
- When I click dismiss or hide
- Then that article is removed from my dashboard for the current session

**AC-05 — Preferred language display**
- Given I have set my language preference
- When personalized news appears on my dashboard
- Then articles are displayed in my selected language

## Assumptions
- Profile matching is attribute-based (sector, location, job interests); no collaborative filtering in MVP
- Manual dismissal is session-based (not persistent across sessions)
- Widget customization is per-user and persisted in user preferences
- Dashboard refresh rate is controlled by cache (e.g., 1 hour TTL)
- Role-based filtering is secondary to profile filtering

## Source Requirements
- [[3_7_1_News_and_Updates|3.7.1]] — FR-165

## Related Stories
- [[US-3.7.1-05-browse-and-filter-news|US-3.7.1-05 Browse and Filter News Articles]]
