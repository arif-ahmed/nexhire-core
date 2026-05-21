---
story_id: "US-3.7.2-02"
title: "Organize Help Content by Topic and Role"
section_id: "3.7.2"
related_requirements: ["FR-168"]
related_stories: ["US-3.7.2-01", "US-3.7.2-03"]
role: "MoL Administrator"
status: draft
priority: must
tags:
  - story
  - bc/content
  - topic/cms
---

# US-3.7.2-02 — Organize Help Content by Topic and Role

## Story
As a **MoL Administrator**, I want **to organize help content by topics (e.g., laws, regulations, contracts) and target it to specific user roles**, so that **users can quickly find relevant guidance for their role**.

## Acceptance Criteria

**AC-01 — Assign topic to FAQ entry**
- Given I am creating or editing a FAQ entry
- When I open the topic assignment panel
- Then I can select one or more predefined topics (Laws, Regulations, Contract Types, etc.)

**AC-02 — Assign user role**
- Given I am creating a FAQ entry
- When I open the role assignment panel
- Then I can select which user roles can see this content (Job Seeker, Employer, Administrator, All)

**AC-03 — Topic hierarchy navigation**
- Given I am viewing the help center
- When I browse by topic
- Then topics are organized hierarchically (e.g., Laws > Labor Laws > Wage Regulations)

**AC-04 — Role-based filtering**
- Given I am a Job Seeker viewing the help center
- When I navigate to the FAQ section
- Then I see entries filtered to my role and also entries marked as "All"

**AC-05 — Cross-role visibility control**
- Given an entry is marked "Employer Recruiter" only
- When a Job Seeker searches the help center
- Then that entry does not appear in results

**AC-06 — Manage topic list**
- Given I am an administrator
- When I access topic management
- Then I can add, edit, or delete topics (if no entries reference them)

## Assumptions
- Topics are admin-defined and centrally managed; no user-created topics
- Role assignment uses multi-select (one entry can target multiple roles)
- Help center UI dynamically renders navigation based on assigned topics
- Topic hierarchy is flat in MVP (no nested subcategories in first release)
- Topic/role assignments are bilingual (applied per language independently)

## Source Requirements
- [[3_7_2_FAQ_and_Help_Center|3.7.2]] — FR-168

## Related Stories
- [[US-3.7.2-01-create-faq-entry|US-3.7.2-01 Create and Edit FAQ Entries]]
- [[US-3.7.2-03-search-help-content|US-3.7.2-03 Search Help Content]]
