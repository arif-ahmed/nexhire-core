---
story_id: "US-3.4.3-02"
title: "Implement RESTful APIs with JSON"
section_id: "3.4.3"
related_requirements: ["FR-111"]
related_stories: ["US-3.4.3-01", "US-3.4.3-03"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/identity
---

# US-3.4.3-02 — Implement RESTful APIs with JSON

## Story
As a **System**, I want to **enforce RESTful conventions and JSON data format across all APIs**, so that **third-party developers have a predictable, standard interface**.

## Acceptance Criteria

**AC-01 — Use HTTP verbs correctly**
- Given API endpoints are designed
- When they are implemented
- Then they follow HTTP verb semantics: GET (read), POST (create), PUT/PATCH (update), DELETE (delete), HEAD (metadata)

**AC-02 — Use resource-oriented URLs**
- Given API endpoints are defined
- When they are named
- Then they follow resource-oriented patterns: `/jobs`, `/jobs/{id}`, `/jobs/{id}/applications` (avoid action-based: `/getJob`)

**AC-03 — Return JSON responses**
- Given an API endpoint is called
- When it returns data
- Then the response body is valid JSON (no XML, CSV, or other formats)

**AC-04 — Support JSON request bodies**
- Given a POST/PUT request is made
- When the request contains a body
- Then the body is valid JSON and matches the API schema

**AC-05 — Use appropriate HTTP status codes**
- Given an API request is processed
- When a response is returned
- Then the HTTP status code reflects the outcome: 200 (success), 201 (created), 400 (bad request), 401 (unauthorized), 404 (not found), 500 (server error)

## Assumptions
- JSON schema is defined for all request/response bodies
- Request/response payloads are validated against schema before processing
- Content-Type header: `application/json` for all JSON endpoints
- Character encoding: UTF-8
- Timestamp format: ISO 8601 (e.g., "2026-04-26T10:30:00Z")
- Number precision: floating-point numbers are used for currency (assumption: may require decimal type for financial accuracy)

## Source Requirements
- [[3_4_3_API_Framework|3.4.3]] — FR-111

## Related Stories
- [[US-3.4.3-01-provide-comprehensive-api-framework|US-3.4.3-01 — Provide Comprehensive API Framework]]
- [[US-3.4.3-03-publish-api-documentation|US-3.4.3-03 — Publish API Documentation]]
