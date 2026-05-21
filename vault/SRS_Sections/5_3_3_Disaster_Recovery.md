---
section_id: "5.3.3"
title: "Disaster Recovery"
srs_page: 30
requirement_type: NFR
subdomain: cross-cutting
bounded_contexts: []
tags:
  - requirement/non-functional
  - subdomain/cross-cutting
  - topic/reliability
  - topic/backup
---
# 5.3.3. Disaster Recovery

*(SRS page 30)*

5.3.3. Disaster Recovery
ID Requirement
NFR-60. The system SHALL maintain regular backups of all data, with full backups at
least weekly and incremental backups daily.
NFR-61. The system SHALL store backups in geographically separate locations from
the primary system.
NFR-62. The system SHALL define and document Recovery Time Objective (RTO) of 4
hours for critical functions and 24 hours for non-critical functions.
NFR-63. The system SHALL define and document Recovery Point Objective (RPO) of 1
hour, meaning no more than 1 hour of data loss in a disaster scenario.
NFR-64. The system SHALL have a documented and tested disaster recovery plan.
NFR-65. The system SHALL conduct disaster recovery drills at least twice per year.

## Related

- [[5_3_Reliability_and_Availability|5.3. Reliability and Availability]]
- [[5_7_2_Backup_and_Recovery|5.7.2. Backup and Recovery]]
