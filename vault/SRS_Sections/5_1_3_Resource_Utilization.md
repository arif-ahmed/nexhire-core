---
section_id: "5.1.3"
title: "Resource Utilization"
srs_page: 27
requirement_type: NFR
subdomain: cross-cutting
bounded_contexts: []
tags:
  - requirement/non-functional
  - subdomain/cross-cutting
  - topic/performance
---
# 5.1.3. Resource Utilization

*(SRS page 27)*

5.1.3. Resource Utilization
ID Requirement
NFR-11. The system SHALL operate within the allocated server resources, utilizing no
more than 80% of CPU capacity during normal operations.
NFR-12. The system SHALL utilize no more than 80% of available memory during
normal operations.
NFR-13. The system SHALL require no more than 5TB of storage for the first year of
operation, with a growth plan for subsequent years.
NFR-14. The system SHALL optimize database queries to minimize I/O operations and
response times.
NFR-15. The system SHALL implement caching mechanisms to reduce resource
utilization for frequently accessed data.
27

## Related

- [[5_1_Performance_Requirements|5.1. Performance Requirements]]
