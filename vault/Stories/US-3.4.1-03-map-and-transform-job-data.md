---
story_id: "US-3.4.1-03"
title: "Map and Transform Job Data"
section_id: "3.4.1"
related_requirements: ["FR-99", "FR-100"]
related_stories: ["US-3.4.1-01", "US-3.4.1-02"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/partner-integration
---

# US-3.4.1-03 — Map and Transform Job Data

## Story
As a **System**, I want to **normalize job data from heterogeneous external sources into a standardized internal schema**, so that **the platform can reliably process jobs from different portals without downstream errors**.

## Acceptance Criteria

**AC-01 — Apply transformation rules**
- Given a job arrives from an external portal in portal-native format
- When the transformation pipeline processes it
- Then fields are mapped to system schema: job title → standardized title, location → normalized location, salary → range object, etc.

**AC-02 — Handle missing required fields**
- Given a job lacks a required field (e.g., job title)
- When transformation is attempted
- Then the system logs a schema validation error and quarantines the job for manual review

**AC-03 — Preserve external identifiers**
- Given a job is transformed
- When the process completes
- Then the system records the external portal ID and external job ID for future deduplication and sync tracking

**AC-04 — Support multiple schema versions**
- Given portals may use different data formats (old vs. new API versions)
- When a job is ingested
- Then the system detects the source schema version and applies the correct transformation rule

## Assumptions
- Transformation rules are defined in a configuration file (YAML/JSON) per external portal
- Mapping is bidirectional: inbound (portal→system) and outbound (system→portal)
- Salary fields are converted to a normalized range (min/max currency/currency_code)
- Location normalization uses a geocoding service or predefined location database
- Transformation errors are logged with job ID and field details for debugging
- Skills/qualifications are mapped to a canonical skill taxonomy (assumption: taxonomy exists in system)

## Source Requirements
- [[3_4_1_External_Job_Site_Integration|3.4.1]] — FR-99, FR-100

## Related Stories
- [[US-3.4.1-01-ingest-jobs-from-external-portal|US-3.4.1-01 — Ingest Jobs from External Portal]]
- [[US-3.4.1-02-export-jobs-to-external-portal|US-3.4.1-02 — Export Jobs to External Portal]]
