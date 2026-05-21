---
story_id: "US-3.5.4-02"
title: "Define and Configure Report Templates"
section_id: "3.5.4"
related_requirements: ["FR-136", "FR-141"]
related_stories: ["US-3.5.4-01", "US-3.5.4-04"]
role: "System Administrator"
status: draft
priority: should
tags:
  - story
  - bc/analytics
---

# US-3.5.4-02 — Define and Configure Report Templates

## Story
As a **System Administrator**, I want **to define reusable report templates with configurable parameters, maintain a library of saved reports, and implement role-based access controls**, so that **users can quickly generate consistent reports without rebuilding them and security is maintained**.

## Acceptance Criteria

**AC-01 — Template creation**
- Given I need to create a standard report template
- When I access the Template Builder
- Then I can define metric selections, visualization types, and default parameters

**AC-02 — Configurable parameters**
- Given templates need flexibility
- When I define a template
- Then I can mark fields as "configurable" (e.g., date range, filters) for end users

**AC-03 — Template library**
- Given multiple templates exist
- When I access the Template Library
- Then I see all available templates with metadata (creator, creation date, usage count)

**AC-04 — Template search and categorization**
- Given the template library grows
- When I search for a template
- Then I can filter by category (Employment Stats, Activity Reports, Performance, etc.) or use keyword search

**AC-05 — Role-based access control**
- Given templates are sensitive
- When I configure a template
- Then I can assign visibility to roles (MoL Admin, Data Analyst, Employer Owner, etc.)

**AC-06 — Template versioning**
- Given templates evolve
- When I modify a template
- Then the old version is archived and a new version is marked current

**AC-07 — Template usage tracking**
- Given I need to understand value
- When I view template details
- Then I see how many times it has been used and by whom

## Assumptions
- Templates are defined by admins and shared with other users (no user-created templates assumed)
- Configurable parameters use a simple parameter system (date ranges, filter dropdowns, metric selection)
- Advanced parameter logic (conditional formatting) deferred to future enhancement
- Template library supports 100+ templates; search/filter assumed performant
- Access control is role-based; no row-level security on templates assumed
- Versioning kept simple: current + previous 5 versions retained; older versions archived

## Source Requirements
- [[3_5_4_Custom_Report_Generation|3.5.4]] — FR-136, FR-141

## Related Stories
- [[US-3.5.4-01|Generate Custom Employment Report]]
- [[US-3.5.4-04|Schedule Recurring Reports]]
