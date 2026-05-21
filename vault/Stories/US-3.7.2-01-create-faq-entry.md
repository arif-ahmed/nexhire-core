---
story_id: "US-3.7.2-01"
title: "Create and Edit FAQ Entries"
section_id: "3.7.2"
related_requirements: ["FR-167", "FR-171"]
related_stories: ["US-3.7.2-02", "US-3.7.2-03"]
role: "MoL Administrator"
status: draft
priority: must
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.2-01 — Create and Edit FAQ Entries

## Story
As a **MoL Administrator**, I want **to create, edit, and manage FAQ entries and help center articles**, so that **I can provide comprehensive self-service support covering laws, regulations, and contract types**.

## Acceptance Criteria

**AC-01 — Create FAQ entry**
- Given I am logged in as an administrator
- When I navigate to the FAQ management interface
- Then I can create a new FAQ entry with a question and answer

**AC-02 — Rich text support**
- Given I am creating a FAQ entry
- When I edit the answer field
- Then I can use rich text formatting (bold, italic, links, lists)

**AC-03 — Edit existing entry**
- Given I have an existing FAQ entry
- When I click edit
- Then I can modify the question, answer, and metadata

**AC-04 — Save draft**
- Given I am creating or editing a FAQ entry
- When I click save as draft
- Then the entry is saved but not published to the help center

**AC-05 — Publish entry**
- Given I have a draft FAQ entry
- When I click publish
- Then the entry immediately appears in the help center

**AC-06 — Bilingual entries**
- Given I am creating a FAQ entry
- When I toggle language selection
- Then I can create independent EN and Bangla versions

## Assumptions
- FAQ editor uses the same rich text editor as news content
- Publish/draft states are language-independent (both EN and Bangla must be published separately)
- FAQ entries have no built-in versioning (overwrite on edit); version history is not tracked in MVP
- Administrators can edit any FAQ; no approval workflow in MVP

## Source Requirements
- [[3_7_2_FAQ_and_Help_Center|3.7.2]] — FR-167, FR-171

## Related Stories
- [[US-3.7.2-02-organize-help-by-topic-and-role|US-3.7.2-02 Organize Help Content by Topic and Role]]
- [[US-3.7.2-03-search-help-content|US-3.7.2-03 Search Help Content]]
