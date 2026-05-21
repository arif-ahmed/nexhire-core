---
story_id: "US-3.4.1-01"
title: "Ingest Jobs from External Portal"
section_id: "3.4.1"
related_requirements: ["FR-97", "FR-100"]
related_stories: ["US-3.4.1-02", "US-3.4.1-05"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/partner-integration
---

# US-3.4.1-01 — Ingest Jobs from External Portal

## Story
As a **System**, I want to **pull job postings from MoL/PEF-recommended external job portals**, so that **the platform aggregates job data from multiple sources and provides a comprehensive job catalog**.

## Acceptance Criteria

**AC-01 — Pull jobs on schedule**
- Given an external portal connector is configured
- When the scheduled sync trigger fires
- Then all new/updated job postings are fetched and ingested into the system

**AC-02 — Handle missing/partial data**
- Given jobs with incomplete required fields arrive
- When ingestion is attempted
- Then the system logs a data validation error and skips the job (or quarantines it for manual review)

**AC-03 — Avoid duplicate ingestion**
- Given a job already exists in the system
- When the same job is fetched again
- Then the system identifies it (by external ID) and updates it instead of creating a duplicate

**AC-04 — Track sync metadata**
- Given jobs are ingested from an external portal
- When the process completes
- Then the system records: source portal, ingestion timestamp, external ID, and ingestion status

## Assumptions
- Pull model uses HTTP GET (REST API) to fetch jobs; polling interval is configurable (default: hourly)
- External portals provide job IDs/URIs for deduplication
- Job data arrives in JSON format; schema mapping occurs at transformation stage (FR-99)
- Network timeouts are retried up to 3 times with exponential backoff
- MoL/PEF provide list of approved portals and their API endpoints

## Source Requirements
- [[3_4_1_External_Job_Site_Integration|3.4.1]] — FR-97, FR-100, FR-101

## Related Stories
- [[US-3.4.1-02-export-jobs-to-external-portal|US-3.4.1-02 — Export Jobs to External Portal]]
- [[US-3.4.1-05-monitor-integration-dashboard|US-3.4.1-05 — Monitor Integration Dashboard]]
