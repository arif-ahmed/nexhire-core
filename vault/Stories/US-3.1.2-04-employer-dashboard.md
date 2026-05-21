---
story_id: "US-3.1.2-04"
title: "Access employer dashboard"
section_id: "3.1.2"
related_requirements: ["FR-20"]
related_stories: ["US-3.1.2-03-manage-employer-profile"]
role: "Employer Owner"
status: draft
priority: must
tags:
  - story
  - bc/employer-profile
---

# US-3.1.2-04 — Access employer dashboard

## Story
As an **Employer Owner**, I want **to access a dashboard showing my job postings, matched candidates, shortlists, and key metrics**, so that **I can manage my recruitment process and track performance**.

## Acceptance Criteria

**AC-01 — Dashboard main view**
- Given I am logged in as an employer
- When I navigate to the dashboard
- Then I see an overview with key metrics and shortcuts to job management features

**AC-02 — Job postings list**
- Given I am on the dashboard
- When I view the "Job Postings" section
- Then I see all my active and inactive job postings with status (draft, published, closed, archived)

**AC-03 — Matched candidates view**
- Given I have job postings with applications
- When I view the "Matched Candidates" section
- Then I see a list of candidates matched to my jobs with match score and candidate name

**AC-04 — Create shortlist**
- Given I view matched candidates
- When I select candidates and click "Add to Shortlist"
- Then the candidates are added to a named shortlist for tracking

**AC-05 — Manage shortlists**
- Given I have created shortlists
- When I navigate to the Shortlists section
- Then I can view, rename, or delete shortlists

**AC-06 — View candidate profile from dashboard**
- Given I see a matched candidate in my dashboard
- When I click on the candidate name
- Then their public profile is displayed (if visibility allows)

**AC-07 — Key metrics display**
- Given I am on the dashboard
- When I view the metrics section
- Then I see: job postings count, total applications, matched candidates count, shortlist count

**AC-08 — Refresh metrics**
- Given metrics are displayed on the dashboard
- When the page loads or I click "Refresh"
- Then metrics are updated to reflect current data within 10 seconds

## Assumptions
- Dashboard is the primary workspace for employer recruitment activities.
- Metrics are aggregated from job postings, applications, and candidate matching.
- Shortlists are created manually; candidates can be added/removed.
- Metrics refresh is near real-time (up to 10 seconds delay).
- Dashboard only shows data for the logged-in employer and their associated accounts.

## Source Requirements
- [[3_1_2_Employer_Registration_and_Profile_Management|3.1.2]] — FR-20

## Related Stories
- [[US-3.1.2-03-manage-employer-profile|US-3.1.2-03 Manage employer profile]]
