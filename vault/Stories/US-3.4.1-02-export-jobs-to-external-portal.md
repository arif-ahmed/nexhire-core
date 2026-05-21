---
story_id: "US-3.4.1-02"
title: "Export Jobs to External Portal"
section_id: "3.4.1"
related_requirements: ["FR-97", "FR-100"]
related_stories: ["US-3.4.1-01", "US-3.4.1-04"]
role: "System"
status: draft
priority: should
tags:
  - story
  - bc/partner-integration
---

# US-3.4.1-02 — Export Jobs to External Portal

## Story
As a **System**, I want to **push newly created jobs to external portals based on configuration**, so that **employers can reach candidates across multiple job platforms**.

## Acceptance Criteria

**AC-01 — Push on-demand**
- Given an employer publishes a job and the portal is configured for push
- When the job is published
- Then the system attempts to export it to the external portal immediately

**AC-02 — Push on schedule**
- Given batch export is configured for an external portal
- When the scheduled push trigger fires
- Then all eligible (unpushed or updated) jobs are exported in a single batch request

**AC-03 — Handle push failures**
- Given a job push fails due to network error
- When the retry limit is reached
- Then the system logs the failure, marks the job as "export pending," and alerts the Integration Engineer

**AC-04 — Track export status**
- Given a job is exported to an external portal
- When the push succeeds
- Then the system records: external portal ID, portal's assigned job ID, export timestamp, and status (success/pending/failed)

## Assumptions
- Push model uses HTTP POST/PUT to external portal APIs (RESTful)
- External portals return a confirmation ID or URI on successful push
- Only jobs with complete required fields are exported
- Export retries occur up to 3 times with exponential backoff
- Job data is transformed to portal-specific schema before push (FR-99)

## Source Requirements
- [[3_4_1_External_Job_Site_Integration|3.4.1]] — FR-97, FR-100, FR-101

## Related Stories
- [[US-3.4.1-01-ingest-jobs-from-external-portal|US-3.4.1-01 — Ingest Jobs from External Portal]]
- [[US-3.4.1-04-configure-external-portal-credentials|US-3.4.1-04 — Configure External Portal Credentials]]
