---
section_id: "3.1.2"
title: "Employer Registration and Profile Management"
srs_page: 14
requirement_type: FR
subdomain: supporting
bounded_contexts: ["BC-3"]
tags:
  - requirement/functional
  - subdomain/supporting
  - bc/employer-profile
  - topic/verification
---
# 3.1.2. Employer Registration and Profile Management

*(SRS page 14)*

3.1.2. Employer Registration and Profile Management
ID Requirement
FR-13. The system SHALL provide a stepwise registration process for employers:
 Level 1: Company name, email, mobile number, company ID, and
registration number (if registered)
 Level 2: Website, industry, company size, address, company
description, and attachments for supplementary documents
FR-14. The system SHALL require account activation through a one-time password
(OTP) sent to the registered mobile number.
FR-15. The system SHALL provide an employer verification process that includes
automatic verification using official information such as the employer’s
registration number, VAT number, and registered mobile number as recorded
in government databases.
FR-16. If automatic verification fails, the Ministry of Labor (MoL) SHALL be able to
manually verify the employer’s account based on submitted registration
details and/or direct contact.
FR-17. The system SHALL display a (cid:33061)(cid:57281) Verified Employer badge next to the names
of verified companies to enhance trust for job seekers and provide added
value to employers who complete the verification process.
FR-18. The system SHALL provide a company profile page that showcases the
employer's information, job openings, and company background.
FR-19. The system SHALL allow employers to upload company logo, and images to
enhance their profile. In addition to documents such as company
registration and manage uploaded documents
FR-20. The system SHALL provide a dashboard for employers to manage job
postings, view matched job seekers, create shortlists of interested or qualified
candidates, and track key metrics.
FR-21. The system SHALL allow the employer to upload supplementary documents
(i.e. company registration certificate)

## Related

- [[3_1_User_Management_and_Authentication|3.1. User Management and Authentication]]
- [[3_1_5_Authentication_and_Authorization|3.1.5. Authentication and Authorization]]
- [[3_3_3_Candidate_Recommendation_for_Employers|3.3.3. Candidate Recommendation for Employers]]
- [[3_4_2_Government_Database_Integration|3.4.2. Government Database Integration]]
