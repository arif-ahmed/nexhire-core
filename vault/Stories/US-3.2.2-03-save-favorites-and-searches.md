---
story_id: "US-3.2.2-03"
title: "Save favorite jobs and search filters with notifications"
section_id: "3.2.2"
related_requirements: ["FR-64", "FR-65"]
related_stories: ["US-3.2.2-02", "US-3.2.3-01"]
role: "Job Seeker"
status: draft
priority: should
tags:
  - story
  - bc/search
---

# US-3.2.2-03 — Save favorite jobs and search filters with notifications

## Story
As a **Job Seeker**, I want to save favorite jobs and save my custom search filters with notification preferences, so that I can quickly revisit interesting opportunities and be notified when new matching jobs are posted.

## Acceptance Criteria

**AC-01 — Can save job as favorite**
- Given I am viewing a job listing
- When I click the "heart" or "favorite" button
- Then the job is added to my "Favorites" or "Interested List" and the button shows a filled/active state

**AC-02 — Can remove job from favorites**
- Given I have a job in my favorites
- When I click the favorite button again (or remove via my favorites list)
- Then the job is removed from favorites and the button returns to inactive state

**AC-03 — Can access favorites list**
- Given I am logged in
- When I navigate to "My Favorites" or "Interested List"
- Then I see all jobs I have saved, with options to view details, remove, or apply

**AC-04 — Can save search filters**
- Given I have applied filters (e.g., location, salary range, skills)
- When I click "Save this search"
- Then the system saves the filter combination with a name I provide (e.g., "Python jobs in NYC")

**AC-05 — Can manage saved searches**
- Given I am in my saved searches
- When I view the list
- Then I can edit filter criteria, rename, or delete saved searches; I can also run a saved search with one click

**AC-06 — Can set notification preferences for saved searches**
- Given I have a saved search
- When I configure notifications
- Then I can choose: no notifications, daily digest, weekly digest, or instant notification for each new matching job

**AC-07 — Receives notifications for new matching jobs**
- Given I have a saved search with notifications enabled
- When a new job is posted matching my saved search criteria
- Then I receive a notification (via in-app, email, or SMS based on preference) with a link to the job

**AC-08 — Can unsubscribe from saved search notifications**
- Given I have notifications enabled for a saved search
- When I click "unsubscribe" or disable notifications
- Then I stop receiving notifications for that search (but the saved search remains)

## Assumptions
- "Favorites" and "Interested List" are synonymous.
- Favorite jobs are visible only to the logged-in user.
- Saved searches use the same filter criteria as advanced search (keywords, location, salary, etc.).
- Notification channel preference (in-app, email, SMS) is set globally or per-saved-search; exact implementation deferred.
- Digest notifications (daily, weekly) are batched and sent at fixed times (e.g., 9 AM); exact schedule deferred.
- Instant notifications may be rate-limited to prevent spam (e.g., max 5 per day per search); thresholds not specified.

## Source Requirements
- [[3_2_2_Job_Search_and_Filtering|3.2.2]] — FR-64, FR-65

## Related Stories
- [[US-3.2.2-02-...|US-3.2.2-02 Filter and rank results]]
- [[US-3.2.3-01-...|US-3.2.3-01 Bookmark and save jobs]]
