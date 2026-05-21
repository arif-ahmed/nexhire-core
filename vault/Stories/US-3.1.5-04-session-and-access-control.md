---
story_id: "US-3.1.5-04"
title: "Manage session and role-based access control"
section_id: "3.1.5"
related_requirements: ["FR-51", "FR-52", "FR-53"]
related_stories: ["US-3.1.5-01-login-with-credentials"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/identity
  - topic/security
  - topic/identity
---

# US-3.1.5-04 — Manage session and role-based access control

## Story
As a **System**, I want **to enforce role-based access control (RBAC) and manage user sessions with configurable timeout**, so that **I can restrict access to features based on user roles and protect against unauthorized access**.

## Acceptance Criteria

**AC-01 — Role-based access enforcement**
- Given a user with role "job seeker"
- When they attempt to access an employer-only feature (e.g., post job)
- Then access is denied with error `E-UNAUTHORIZED-ROLE` (403)

**AC-02 — Feature-level access control**
- Given I define feature permissions per role
- When a user with an insufficient role attempts the feature
- Then the feature is hidden in UI and API access is blocked

**AC-03 — Admin-only access**
- Given an endpoint is reserved for admin role (e.g., /admin/users)
- When a non-admin user attempts access
- Then they receive error `E-FORBIDDEN` (403)

**AC-04 — Session creation**
- Given a user successfully logs in
- When the session is established
- Then a session ID is created, stored server-side, and a session cookie is issued with HTTP-only, Secure flags

**AC-05 — Session timeout (inactivity)**
- Given a session has no activity for 1 hour (configurable)
- When the next request arrives
- Then the session is expired and user is redirected to login

**AC-06 — Explicit logout**
- Given a user clicks "Logout"
- When logout is processed
- Then the session is destroyed, session cookie is cleared, and JWT is invalidated

**AC-07 — Concurrent session management**
- Given a user logs in from two different devices simultaneously
- When both sessions are active
- Then both are allowed (unless a "single session" policy is enforced per role)

**AC-08 — Access log entry**
- Given a user accesses a resource
- When the request is processed
- Then the access is logged with: user ID, resource, action, timestamp, IP address, user agent

**AC-09 — Audit trail for sensitive operations**
- Given an admin performs a sensitive action (ban user, approve employer, delete job)
- When the action completes
- Then a detailed audit entry is created with: admin ID, action type, target entity, timestamp, reason

**AC-10 — Configurable timeout settings**
- Given I am a system admin
- When I set session timeout configuration
- Then timeout values are applied across the platform (default: 1 hour)

**AC-11 — Token refresh mechanism**
- Given a JWT token is expiring in 5 minutes
- When I request a token refresh with a refresh token
- Then a new JWT is issued with updated expiration

## Assumptions
- User roles: Job Seeker, Employer Owner, Employer Recruiter, Employer Admin, MoL Administrator, Third-Party Portal, System.
- RBAC is enforced at API and UI levels.
- Session timeout is configurable per environment (default: 1 hour inactivity).
- Sessions are stored in secure session store (Redis or similar).
- Access logs are retained for 90 days minimum.
- All sensitive operations are logged in immutable audit trail.
- JWT tokens use RS256 signing; keys are rotated periodically.

## Source Requirements
- [[3_1_5_Authentication_and_Authorization|3.1.5]] — FR-51, FR-52, FR-53

## Related Stories
- [[US-3.1.5-01-login-with-credentials|US-3.1.5-01 Login with credentials]]
