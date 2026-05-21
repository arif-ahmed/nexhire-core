---
story_id: "US-3.1.4-01"
title: "Manage user accounts"
section_id: "3.1.4"
related_requirements: ["FR-37", "FR-38", "FR-39", "FR-40"]
related_stories: ["US-3.1.4-02-manage-taxonomies"]
role: "MoL Administrator"
status: draft
priority: must
tags:
  - story
  - bc/identity
  - topic/admin
---

# US-3.1.4-01 — Manage user accounts

## Story
As a **MoL Administrator**, I want **to view, approve, ban, deactivate, and assist users with account management**, so that **I can maintain platform integrity and support user account recovery**.

## Acceptance Criteria

**AC-01 — View all user accounts**
- Given I am logged in as an admin
- When I navigate to User Management
- Then I see a searchable, filterable list of all job seekers, employers, and third-party portals

**AC-02 — Filter users by type and status**
- Given I view the user list
- When I apply filters (user type, status, registration date, verification status)
- Then only matching users are displayed

**AC-03 — Search users by name or email**
- Given I search for a user by name or email
- When I submit the search
- Then matching users are returned

**AC-04 — View user profile as admin**
- Given I click on a user in the list
- When the profile opens
- Then I see all user details (contact, registration info, verification status, account status)

**AC-05 — Approve pending employer**
- Given I view a pending employer account
- When I click "Approve"
- Then the account transitions to `verified` and a confirmation email is sent to the employer

**AC-06 — Reject pending employer**
- Given I view a pending employer account
- When I click "Reject" and provide a reason
- Then the account transitions to `rejected` and the employer is notified

**AC-07 — Deactivate user account**
- Given I view an active user account
- When I click "Deactivate" and confirm
- Then the account transitions to `deactivated` (soft delete) and the user is notified

**AC-08 — Ban user account**
- Given I have evidence of policy violation
- When I click "Ban" on a user account
- Then the account is locked, access is revoked, and a ban reason is logged

**AC-09 — Reset user password**
- Given a user has requested password reset assistance
- When I click "Reset Password"
- Then a password reset link is generated and sent to their registered email

**AC-10 — Unlock account**
- Given a user account is locked (e.g., after OTP failures)
- When I click "Unlock"
- Then the account is unlocked and the user can attempt login again

**AC-11 — Audit trail of admin actions**
- Given I take any admin action (approve, ban, deactivate, etc.)
- When the action completes
- Then it is logged in the audit table with: admin user ID, action type, timestamp, target user, reason

## Assumptions
- User management requires admin role (MoL Administrator).
- Soft delete: deactivated accounts remain in DB for 12 months before hard deletion.
- Hard delete: banned accounts may be purged after legal hold period (configurable).
- All admin actions are logged with timestamp, admin ID, action type, and reason.
- User notifications are sent via email for significant account changes.
- Deactivation is reversible by user login + OTP; bans are not self-reversible.

## Source Requirements
- [[3_1_4_Administrator_User_Management|3.1.4]] — FR-37, FR-38, FR-39, FR-40

## Related Stories
- [[US-3.1.4-02-manage-taxonomies|US-3.1.4-02 Manage skills and taxonomies]]
