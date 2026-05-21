---
story_id: "US-3.1.1-05"
title: "View profile completeness and recommendations"
section_id: "3.1.1"
related_requirements: ["FR-08"]
related_stories: ["US-3.1.1-03-complete-level-2-profile", "US-3.1.1-06-manage-supplementary-documents"]
role: "Job Seeker"
status: draft
priority: should
tags:
  - story
  - bc/seeker-profile
---

# US-3.1.1-05 — View profile completeness and recommendations

## Story
As a **Job Seeker**, I want **to see my profile completeness score and receive recommendations on what to complete**, so that **I know how complete my profile is and what additional information would improve my job matching**.

## Acceptance Criteria

**AC-01 — Completeness score displayed**
- Given I visit my profile dashboard
- When the page loads
- Then I see a profile completeness indicator showing a percentage (0–100%)

**AC-02 — Score calculation**
- Given my profile has been updated
- When the completeness score is computed
- Then it follows the weighted formula: L1 = 30%, L2 = 50%, L3 = 10%, resume = 10%

**AC-03 — Recompute on profile update**
- Given I update any profile field
- When the update is saved
- Then the completeness score is recalculated and refreshed within 2 seconds

**AC-04 — Contextual recommendations**
- Given my profile is incomplete (< 100%)
- When I view my profile
- Then I see recommendations for missing sections (e.g., "Add your work experience to improve matching")

**AC-05 — Recommendations encourage exploration**
- Given I have limited education or training data
- When I complete my profile
- Then recommendations suggest exploring adjacent opportunities beyond formal credentials

**AC-06 — Profile completeness impact**
- Given my profile completeness is below threshold
- When I attempt to access job recommendations or apply for jobs
- Then I see a message encouraging me to complete my profile for better matches

**AC-07 — Milestone notifications**
- Given my completeness score crosses a milestone (e.g., 50%, 75%)
- When the milestone is reached
- Then I receive a notification celebrating the progress

**AC-08 — Level-specific progress view**
- Given I visit the profile edit page
- When I view each level section (L1, L2, L3)
- Then I see which specific fields within that level are complete vs. missing

## Assumptions
- Completeness is computed as: sum(weighted fields) where L1 fields count 30%, L2 count 50%, L3 count 10%, resume presence counts 10%.
- L1 requires all 6 fields; L2 requires at least education OR experience; L3 is optional; resume is optional.
- Score is recomputed every time a profile field changes.
- Recommendations are contextual and refreshed on each page load.
- Milestone notifications are sent at 50%, 75%, and 100%.

## Source Requirements
- [[3_1_1_Job_Seeker_Registration_and_Profile_Management|3.1.1]] — FR-08

## Related Stories
- [[US-3.1.1-03-complete-level-2-profile|US-3.1.1-03 Complete Level 2 profile]]
- [[US-3.1.1-06-manage-supplementary-documents|US-3.1.1-06 Manage supplementary documents]]
