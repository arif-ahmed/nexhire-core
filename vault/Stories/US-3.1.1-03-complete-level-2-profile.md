---
story_id: "US-3.1.1-03"
title: "Complete Level 2 profile"
section_id: "3.1.1"
related_requirements: ["FR-03", "FR-06", "FR-09"]
related_stories: ["US-3.1.1-04-upload-and-parse-resume", "US-3.1.1-05-view-profile-completeness"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/seeker-profile
---

# US-3.1.1-03 — Complete Level 2 profile

## Story
As a **Job Seeker**, I want **to fill in Level 2 profile information (education, work experience, skills, preferences, and address)**, so that **I can apply to jobs and improve my matching with employers**.

## Acceptance Criteria

**AC-01 — Level 2 gating on job application**
- Given I have a Level 1 complete account but no Level 2 data
- When I attempt to apply to a job
- Then the application is blocked and I see an inline prompt to complete Level 2

**AC-02 — Add education entry**
- Given I am on the profile edit page
- When I submit education details (degree, institution, start date, end date, GPA)
- Then the entry is saved and appears in my education list

**AC-03 — Add work experience entry**
- Given I am on the profile edit page
- When I submit experience details (company, role, start date, end date, is_current, responsibilities)
- Then the entry is saved and appears in my experience list

**AC-04 — Add skill entry**
- Given I am on the profile edit page
- When I add a skill with category (hard/soft), tier (primary/secondary), and proficiency (1–5)
- Then the skill is saved and appears in my skills list

**AC-05 — Set job preferences**
- Given I am on the preferences section
- When I set job type, industry, location, salary expectations (min/max BDT), and work arrangement (on-site / hybrid / remote)
- Then preferences are saved and used for job matching

**AC-06 — Add address information**
- Given I am on the profile edit page
- When I submit current address and permanent address (line1, line2, city, district, postcode, country)
- Then both are saved (permanent address optional)

**AC-07 — Optional salary information**
- Given I am on the profile edit page
- When I optionally enter recent salary (in BDT)
- Then it is saved but not required to proceed

**AC-08 — Contextual form prompts**
- Given I am filling in profile fields
- When I interact with education / experience / skills / preferences sections
- Then I see contextual popup prompts with examples and guidance

## Assumptions
- Level 2 completion is required before first job application.
- Multiple education, experience, and skill entries are allowed.
- Salary information is stored in BDT and converted on display.
- Address fields use a standardized structure (line1, line2, city, district, postcode, country).
- Work arrangements are: on-site, hybrid, remote (checkboxes for multiple selection).
- Date validation: start_date ≤ end_date; is_current=true implies no end_date.

## Source Requirements
- [[3_1_1_Job_Seeker_Registration_and_Profile_Management|3.1.1]] — FR-03, FR-06, FR-09

## Related Stories
- [[US-3.1.1-04-upload-and-parse-resume|US-3.1.1-04 Upload and parse resume]]
- [[US-3.1.1-05-view-profile-completeness|US-3.1.1-05 View profile completeness]]
