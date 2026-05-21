---
story_id: "US-3.7.2-05"
title: "Collect Help Content Feedback"
section_id: "3.7.2"
related_requirements: ["FR-172"]
related_stories: ["US-3.7.2-04", "US-3.7.2-07"]
role: "Visitor"
status: draft
priority: should
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.2-05 — Collect Help Content Feedback

## Story
As a **Visitor**, I want **to provide feedback on the helpfulness of FAQ entries and help content**, so that **administrators can identify and improve inadequate or confusing documentation**.

## Acceptance Criteria

**AC-01 — Was this helpful prompt**
- Given I have viewed a FAQ entry or help article
- When I scroll to the bottom of the content
- Then I see a "Was this helpful?" prompt with Yes/No buttons

**AC-02 — Collect feedback reason**
- Given I clicked "No" on the helpful prompt
- When the prompt expands
- Then I can select a reason (unclear, incomplete, incorrect, other)

**AC-03 — Optional comment**
- Given I have selected a feedback reason
- When the feedback form opens
- Then I can optionally enter a comment with suggestions

**AC-04 — Store feedback**
- Given I have submitted feedback
- When the form is submitted
- Then the feedback is stored with a timestamp and user context (role, language)

**AC-05 — No identification required**
- Given I am providing feedback
- When I submit the form
- Then I am not required to log in or identify myself

**AC-06 — Feedback dashboard**
- Given I am an administrator
- When I access the feedback dashboard
- Then I can view aggregated feedback by entry (helpful count, reasons, comments)

## Assumptions
- Feedback collection is anonymous (no user ID stored unless user is logged in)
- Feedback is associated with specific FAQ entry by ID
- No email follow-up or confirmation is sent (fire-and-forget)
- Feedback dashboard is admin-only; aggregation is by entry and reason
- Feedback can be exported for analysis but not deleted by users

## Source Requirements
- [[3_7_2_FAQ_and_Help_Center|3.7.2]] — FR-172

## Related Stories
- [[US-3.7.2-04-context-sensitive-help|US-3.7.2-04 Provide Context-Sensitive Help]]
- [[US-3.7.2-07-create-guided-tours|US-3.7.2-07 Create Guided Tours and Tutorials]]
