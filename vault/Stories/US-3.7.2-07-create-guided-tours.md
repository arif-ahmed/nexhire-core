---
story_id: "US-3.7.2-07"
title: "Create Guided Tours and Tutorials"
section_id: "3.7.2"
related_requirements: ["FR-173"]
related_stories: ["US-3.7.2-05", "US-3.7.2-06"]
role: "MoL Administrator"
status: draft
priority: could
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.2-07 — Create Guided Tours and Tutorials

## Story
As a **MoL Administrator**, I want **to create interactive guided tours and onboarding tutorials for new users**, so that **new users can learn platform features through step-by-step walkthroughs**.

## Acceptance Criteria

**AC-01 — Create new tour**
- Given I am logged in as an administrator
- When I navigate to the tour builder
- Then I can create a new guided tour with a name, description, and target audience

**AC-02 — Define tour steps**
- Given I have created a tour
- When I edit the tour
- Then I can add multiple steps, each with a target element (CSS selector), tooltip text, and optional action

**AC-03 — Target element highlighting**
- Given a tour step is active
- When the user views the step
- Then the target element is highlighted and a tooltip appears with instructions

**AC-04 — Step navigation**
- Given I am viewing a tour step
- When I click next/previous buttons
- Then the tour navigates to the next/previous step, highlighting the new target

**AC-05 — Skip and exit**
- Given I am in a guided tour
- When I click skip or close
- Then the tour is dismissed and I return to normal platform interaction

**AC-06 — Tour targeting**
- Given I am creating a tour
- When I set the target audience
- Then I can specify which user roles (new users, job seekers, recruiters) see this tour

**AC-07 — Bilingual tours**
- Given I am creating a tour
- When I toggle language
- Then I can create separate EN and Bangla versions with independent content

## Assumptions
- Tours are built without code using a visual builder UI
- Target elements are specified via CSS selectors (requires basic technical knowledge for setup)
- Tours are optional for users; automatic trigger only on first login (can be dismissed anytime)
- Tour progress is not persisted across sessions (restart from step 1 on next login)
- Tour creation/editing is admin-only; no user-created tours in MVP

## Source Requirements
- [[3_7_2_FAQ_and_Help_Center|3.7.2]] — FR-173

## Related Stories
- [[US-3.7.2-05-collect-help-feedback|US-3.7.2-05 Collect Help Content Feedback]]
- [[US-3.7.2-06-multimedia-help-content|US-3.7.2-06 Create Multimedia Help Content]]
