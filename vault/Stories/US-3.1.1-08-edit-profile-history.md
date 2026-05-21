---
story_id: "US-3.1.1-08"
title: "View and restore profile edit history"
section_id: "3.1.1"
related_requirements: ["FR-07"]
related_stories: ["US-3.1.1-03-complete-level-2-profile"]
role: "Job Seeker"
status: draft
priority: could
tags:
  - story
  - bc/seeker-profile
---

# US-3.1.1-08 — View and restore profile edit history

## Story
As a **Job Seeker**, I want **to view my profile edit history and optionally restore previous versions**, so that **I can track changes and revert to an earlier version if needed**.

## Acceptance Criteria

**AC-01 — View edit history timeline**
- Given I am on the profile history page
- When the page loads
- Then I see a chronological list of edits made to my profile with timestamps and field names changed

**AC-02 — History retention period**
- Given I edit my profile
- When the edit is made
- Then the previous version is retained in history for 12 months

**AC-03 — History purge after 12 months**
- Given an edit was made more than 12 months ago
- When the system runs the cleanup job
- Then the historical record is permanently deleted

**AC-04 — Per-field edit tracking**
- Given my profile has been edited multiple times
- When I view the history timeline
- Then each edit entry shows which specific fields were changed (e.g., "Updated skills and experience")

**AC-05 — Restore previous version**
- Given I view a historical version
- When I click "Restore to this version"
- Then I see a confirmation dialog

**AC-06 — Confirm restore action**
- Given I click "Restore" on a confirmation dialog
- When I confirm the action
- Then the selected historical version is restored as the current profile version

**AC-07 — Restore creates new history entry**
- Given I restore a previous version
- When the restore completes
- Then a new history entry is created showing "Restored to version from [date]"

**AC-08 — View side-by-side diff (optional)**
- Given I select two versions from history
- When I request a comparison
- Then I see a side-by-side view of changed fields (nice-to-have)

## Assumptions
- Edit history is tracked at the profile-level (not at individual field level for storage).
- History records include: timestamp, user action, fields changed, previous values.
- Retention period is 12 months; older records are purged via scheduled job.
- Restore action creates a new history entry (audit trail).
- History is read-only; users cannot manually edit or delete historical records.
- Deactivated accounts retain history but it is not accessible until reactivation.

## Source Requirements
- [[3_1_1_Job_Seeker_Registration_and_Profile_Management|3.1.1]] — FR-07

## Related Stories
- [[US-3.1.1-03-complete-level-2-profile|US-3.1.1-03 Complete Level 2 profile]]
