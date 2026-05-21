---
story_id: "US-3.4.1-07"
title: "Handle Real-Time Job Updates"
section_id: "3.4.1"
related_requirements: ["FR-98", "FR-102"]
related_stories: ["US-3.4.1-01"]
role: "System"
status: draft
priority: could
tags:
  - story
  - bc/partner-integration
---

# US-3.4.1-07 — Handle Real-Time Job Updates

## Story
As a **System**, I want to **receive and process real-time job updates from external portals via webhook**, so that **the platform stays in sync without waiting for scheduled polling**.

## Acceptance Criteria

**AC-01 — Receive webhook events**
- Given an external portal supports webhook notifications
- When a job is created/updated/closed on the external portal
- Then the system receives a webhook POST to a registered callback URL with the updated job data

**AC-02 — Validate webhook authenticity**
- Given a webhook is received
- When the system processes it
- Then it verifies the webhook signature (HMAC-SHA256 or similar) to ensure it originated from the trusted portal

**AC-03 — Process webhook asynchronously**
- Given a webhook is validated
- When the system receives it
- Then it queues the update for asynchronous processing and immediately returns HTTP 202 Accepted

**AC-04 — Reconcile with polling**
- Given both webhook and scheduled polling are active
- When a job update is received via webhook and then via polling
- Then the system detects the duplicate (by job ID and timestamp) and avoids redundant processing

## Assumptions
- Webhooks are opt-in per external portal
- Webhook payload format is JSON and matches the portal's API schema
- System exposes a public webhook endpoint (e.g., `/api/webhooks/external-portal/{portal_id}`)
- Webhook retry policy: external portal retries up to 5 times with exponential backoff
- Webhook delivery is not guaranteed (eventual consistency model)
- Webhook signing algorithm is configurable per portal (HMAC or bearer token)

## Source Requirements
- [[3_4_1_External_Job_Site_Integration|3.4.1]] — FR-98, FR-102

## Related Stories
- [[US-3.4.1-01-ingest-jobs-from-external-portal|US-3.4.1-01 — Ingest Jobs from External Portal]]
