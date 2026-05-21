---
section_id: "5.1.4"
title: "Scalability"
srs_page: 28
requirement_type: NFR
subdomain: cross-cutting
bounded_contexts: []
tags:
  - requirement/non-functional
  - subdomain/cross-cutting
  - topic/performance
  - topic/scalability
---
# 5.1.4. Scalability

*(SRS page 28)*

5.1.4. Scalability
ID Requirement
NFR-16. The system SHALL be designed to scale horizontally by adding more server
instances to handle increased load.
NFR-17. The system SHALL be designed to scale vertically by utilizing additional
resources on existing servers.
NFR-18. The system SHALL support a minimum of 100,000 registered job seekers
without performance degradation.
NFR-19. The system SHALL support a minimum of 10,000 registered employers
without performance degradation.
NFR-20. The system SHALL support a minimum of 50,000 active job postings without
performance degradation.
NFR-21. The system SHALL be designed to accommodate a 100% annual growth in
user base and transaction volume for at least the first three years of
operation.

## Related

- [[5_1_Performance_Requirements|5.1. Performance Requirements]]
