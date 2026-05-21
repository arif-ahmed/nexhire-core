---
story_id: "US-3.4.1-05"
title: "Monitor Integration Dashboard"
section_id: "3.4.1"
related_requirements: ["FR-103", "FR-101"]
related_stories: ["US-3.4.1-01", "US-3.4.1-02"]
role: "MoL Administrator"
status: draft
priority: should
tags:
  - story
  - bc/partner-integration
---

# US-3.4.1-05 — Monitor Integration Dashboard

## Story
As a **MoL Administrator**, I want to **view real-time status and metrics for all external portal integrations**, so that **I can quickly identify and respond to sync failures or performance issues**.

## Acceptance Criteria

**AC-01 — View integration health summary**
- Given the admin opens the Integration Dashboard
- When the page loads
- Then they see a summary card per external portal showing: last sync timestamp, status (healthy/warning/error), jobs ingested, jobs exported, and error count

**AC-02 — View detailed sync logs**
- Given the admin clicks on a specific portal
- When the detail view opens
- Then they see a paginated log of the last 100 sync events with: timestamp, type (pull/push), item count, status, error message (if failed)

**AC-03 — Alert on failures**
- Given a sync fails more than 3 times consecutively
- When the threshold is reached
- Then the system sends an alert (email/webhook) to the Integration Engineer with portal name, error summary, and recommended action

**AC-04 — Export logs**
- Given the admin needs audit records
- When they click "Export Logs"
- Then the system downloads a CSV with full sync event history (configurable date range)

## Assumptions
- Dashboard auto-refreshes every 60 seconds (configurable)
- "Healthy" = last sync < 24 hours ago AND success rate > 95%
- "Warning" = last sync > 24 hours OR success rate 85–95%
- "Error" = success rate < 85% OR last sync > 7 days ago
- Logs are retained for 90 days (configurable retention)
- Alert recipients are configured per admin role (RBAC)

## Source Requirements
- [[3_4_1_External_Job_Site_Integration|3.4.1]] — FR-103, FR-101

## Related Stories
- [[US-3.4.1-01-ingest-jobs-from-external-portal|US-3.4.1-01 — Ingest Jobs from External Portal]]
- [[US-3.4.1-02-export-jobs-to-external-portal|US-3.4.1-02 — Export Jobs to External Portal]]
