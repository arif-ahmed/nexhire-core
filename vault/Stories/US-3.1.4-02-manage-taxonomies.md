---
story_id: "US-3.1.4-02"
title: "Manage skills and taxonomies"
section_id: "3.1.4"
related_requirements: ["FR-42", "FR-43", "FR-44"]
related_stories: ["US-3.1.4-01-manage-user-accounts"]
role: "MoL Administrator"
status: draft
priority: should
tags:
  - story
  - bc/skill-taxonomy
  - topic/admin
  - topic/taxonomy
---

# US-3.1.4-02 — Manage skills and taxonomies

## Story
As a **MoL Administrator**, I want **to view, create, update, and manage system reference data and taxonomies (skills, occupations, training programs)**, so that **the platform maintains up-to-date, standardized data for job matching and reporting**.

## Acceptance Criteria

**AC-01 — View skills taxonomy**
- Given I navigate to the Taxonomies section
- When I select "Skills"
- Then I see a hierarchical list of all skills (name, category, proficiency levels, usage count)

**AC-02 — Add new skill**
- Given I click "Add Skill"
- When I fill in skill name, category (hard/soft), and save
- Then the skill is added to the taxonomy and becomes available for job seekers and employers

**AC-03 — Edit skill**
- Given I click "Edit" on a skill
- When I update the skill name or category
- Then the change is saved and reflected system-wide

**AC-04 — Disable skill**
- Given I want to deprecate a skill without deleting it
- When I click "Disable"
- Then the skill is marked inactive; new profiles cannot select it, but existing profiles retain it

**AC-05 — View occupations/jobs taxonomy**
- Given I select "Occupations" from taxonomies
- When the page loads
- Then I see a hierarchical list of job categories, job titles, and occupations

**AC-06 — Add or update occupation**
- Given I click "Add Occupation" or "Edit"
- When I fill in occupation name and category
- Then the entry is added or updated in the taxonomy

**AC-07 — View training programs taxonomy**
- Given I select "Training Programs" from taxonomies
- When the page loads
- Then I see all training programs and certifications available on the platform

**AC-08 — Manage training programs**
- Given I view the training programs list
- When I click "Add" or "Edit"
- Then I can create or update training program entries

**AC-09 — View reference data usage**
- Given I view any taxonomy entry
- When I look at the usage statistics
- Then I see how many profiles reference this skill, occupation, or training program

**AC-10 — Bulk import taxonomies**
- Given I have a CSV file of skills or occupations
- When I upload the file
- Then the system validates and imports all entries, reporting success/failure for each row

## Assumptions
- Taxonomies are reference data and affect the entire system.
- Skills have categories: hard (technical) or soft (interpersonal).
- Occupations follow a hierarchy: category → job group → specific job title.
- Training programs include certifications and formal training courses.
- Disabling (not deleting) allows historical data to remain valid.
- Bulk import supports CSV format with validation error reporting.
- Changes to taxonomies do not retroactively affect existing profile data.

## Source Requirements
- [[3_1_4_Administrator_User_Management|3.1.4]] — FR-42, FR-43, FR-44

## Related Stories
- [[US-3.1.4-01-manage-user-accounts|US-3.1.4-01 Manage user accounts]]
