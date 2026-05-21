---
story_id: "US-3.7.2-06"
title: "Create Multimedia Help Content"
section_id: "3.7.2"
related_requirements: ["FR-174"]
related_stories: ["US-3.7.2-01", "US-3.7.2-07"]
role: "MoL Administrator"
status: draft
priority: could
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.2-06 — Create Multimedia Help Content

## Story
As a **MoL Administrator**, I want **to create and manage multimedia help content including videos and interactive guides**, so that **I can accommodate different learning styles and make complex topics easier to understand**.

## Acceptance Criteria

**AC-01 — Embed video in help entry**
- Given I am creating a help article
- When I click the video insertion button
- Then I can embed a video from YouTube or upload a video file

**AC-02 — Interactive guide support**
- Given I am creating a help article
- When I choose the interactive guide option
- Then I can compose a step-by-step guide with images and annotations

**AC-03 — Video transcription**
- Given I have embedded a video in a help article
- When an administrator manages video settings
- Then they can upload or provide a link to a video transcript

**AC-04 — Accessibility support**
- Given a video or interactive guide has been created
- When the content is published
- Then captions/transcripts are available for accessibility compliance

**AC-05 — Storage and CDN delivery**
- Given I have uploaded a video file
- When the video is published
- Then it is stored on a CDN for efficient delivery

**AC-06 — Multimedia in both languages**
- Given I am creating multimedia content
- When I assign language
- Then I can upload language-specific versions (e.g., videos with EN or Bangla narration)

## Assumptions
- Video uploads are limited to reasonable file sizes (max 500MB per file)
- Supported video formats: MP4, WebM; supported image formats: JPEG, PNG, GIF
- Interactive guides are authored in a simple builder UI (no code required)
- Transcripts are optional but recommended; system enforces no requirement
- CDN integration is automatic (upload triggers async transcoding and CDN distribution)

## Source Requirements
- [[3_7_2_FAQ_and_Help_Center|3.7.2]] — FR-174

## Related Stories
- [[US-3.7.2-01-create-faq-entry|US-3.7.2-01 Create and Edit FAQ Entries]]
- [[US-3.7.2-07-create-guided-tours|US-3.7.2-07 Create Guided Tours and Tutorials]]
