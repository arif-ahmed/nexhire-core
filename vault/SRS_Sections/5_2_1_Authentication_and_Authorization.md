---
section_id: "5.2.1"
title: "Authentication and Authorization"
srs_page: 28
requirement_type: SR
subdomain: generic
bounded_contexts: ["BC-1"]
tags:
  - requirement/security
  - subdomain/generic
  - bc/identity
  - topic/security
  - topic/identity
---
# 5.2.1. Authentication and Authorization

*(SRS page 28)*

5.2.1. Authentication and Authorization
ID Requirement
NFR-22. The system SHALL implement multi-factor authentication for administrative
accounts and as an option for all users.
NFR-23. The system SHALL enforce strong password policies, including minimum
length, complexity, and regular password changes.
NFR-24. The system SHALL implement role-based access control (RBAC) to restrict
access to features and data based on user roles.
NFR-25. The system SHALL maintain detailed access logs for all authentication and
authorization events.
NFR-26. The system SHALL automatically lock accounts after a specified number of
failed login attempts.
NFR-27. The system SHALL implement secure session management with appropriate
timeout settings.
NFR-28. The system SHALL support OAuth 2.0 and OpenID Connect for third-party
authentication where applicable.

## Related

- [[3_1_5_Authentication_and_Authorization|3.1.5. Authentication and Authorization]]
- [[5_2_Security_Requirements|5.2. Security Requirements]]
