---
story_id: "US-3.7.2-04"
title: "Provide Context-Sensitive Help"
section_id: "3.7.2"
related_requirements: ["FR-170"]
related_stories: ["US-3.7.2-03", "US-3.7.2-05"]
role: "System"
status: draft
priority: should
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.2-04 — Provide Context-Sensitive Help

## Story
As a **System**, I want **to display context-sensitive help (tooltips, inline help, help panels) relevant to the current page or form**, so that **users get assistance without leaving their current context**.

## Acceptance Criteria

**AC-01 — Help icon on forms**
- Given I am filling out a form
- When I hover over or click a form field label
- Then a tooltip or help panel appears explaining what the field is for

**AC-02 — Help drawer on pages**
- Given I am on a page with complex functionality
- When I click the help icon (?) in the header
- Then a side panel opens showing relevant FAQ entries and guides

**AC-03 — Help content is dynamic**
- Given I am on the job listing creation page
- When the help drawer opens
- Then it displays help entries tagged with "Job Creation" or "Listings"

**AC-04 — Help respects language preference**
- Given I have set my language to Bangla
- When context-sensitive help appears
- Then it is displayed in Bangla

**AC-05 — Dismiss help without navigation**
- Given a help tooltip or panel is displayed
- When I click the close button or click outside the help element
- Then it closes without navigating away or losing form data

**AC-06 — Help content updates with page**
- Given I navigate from one page to another
- When I open the help drawer on the new page
- Then the help content updates to match the new page context

## Assumptions
- Context-sensitive help is mapped via page/form identifier (data-help-id attribute or routing context)
- Help content is pre-curated by administrators; no dynamic mapping based on page structure
- Tooltips are rendered client-side from a context help map
- Help drawer is non-modal; users can continue working while it's open
- Context mapping is flexible and can be extended without code changes (admin-configurable in future)

## Source Requirements
- [[3_7_2_FAQ_and_Help_Center|3.7.2]] — FR-170

## Related Stories
- [[US-3.7.2-03-search-help-content|US-3.7.2-03 Search Help Content]]
- [[US-3.7.2-05-collect-help-feedback|US-3.7.2-05 Collect Help Content Feedback]]
