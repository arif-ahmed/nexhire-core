---
story_id: "US-3.2.3-01"
title: "Bookmark jobs and save custom search filters"
section_id: "3.2.3"
related_requirements: ["FR-66"]
related_stories: ["US-3.2.2-03", "US-3.2.3-02"]
role: "Job Seeker"
status: draft
priority: should
tags:
  - story
  - bc/search
---

# US-3.2.3-01 — Bookmark jobs and save custom search filters

## Story
As a **Job Seeker**, I want to bookmark jobs in an "Interested List" and save custom search filter combinations for future reuse, so that I can organize and quickly access job opportunities I want to explore.

## Acceptance Criteria

**AC-01 — Can bookmark a job to Interested List**
- Given I am viewing a job listing
- When I click "Add to Interested List" or a bookmark button
- Then the job is added to my Interested List and is visible in my account dashboard

**AC-02 — Interested List is persistent**
- Given I have bookmarked jobs
- When I log out and return later
- Then my Interested List contains all previously bookmarked jobs

**AC-03 — Can view Interested List with job details**
- Given I access my Interested List
- When the list loads
- Then I see all bookmarked jobs with key details (title, company, location, salary if available); I can sort or filter this list

**AC-04 — Can remove jobs from Interested List**
- Given I have a job in my Interested List
- When I click "remove" or the bookmark button
- Then the job is removed from my list

**AC-05 — Can save custom search filters**
- Given I have created a search with specific filters
- When I click "Save this search" or similar option
- Then I provide a name (e.g., "Remote Python roles") and the filter combination is saved

**AC-06 — Can view and reuse saved searches**
- Given I have saved searches
- When I navigate to "Saved Searches"
- Then I can see all saved searches; clicking one re-runs that search instantly

**AC-07 — Can edit saved searches**
- Given I have a saved search
- When I click "Edit"
- Then I can modify the filter criteria (keywords, location, etc.) and save changes

**AC-08 — Can delete saved searches**
- Given I have saved searches I no longer need
- When I click "Delete"
- Then the saved search is removed (after confirmation)

## Assumptions
- "Interested List" is a simple collection; no priority ranking or tagging within the list.
- Saved searches store the exact filter state (not just a query string); filters are re-editable.
- Bookmark/Interested List is per-user and not shareable.
- Deletion of a saved search does not affect any bookmarked jobs (they are independent).
- List size is unbounded (no stated limit on bookmarks or saved searches); performance at scale deferred.

## Source Requirements
- [[3_2_3_Job_Interaction_Process|3.2.3]] — FR-66

## Related Stories
- [[US-3.2.2-03-...|US-3.2.2-03 Save favorites and notifications]]
- [[US-3.2.3-02-...|US-3.2.3-02 Apply to jobs and track applications]]
