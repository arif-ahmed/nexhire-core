---
section_id: "3.3.1"
title: "A Vector-Based (Skills-Based and Behavior) Matching Algorithms"
srs_page: 18
requirement_type: FR
subdomain: core
bounded_contexts: ["BC-8", "BC-4"]
tags:
  - requirement/functional
  - subdomain/core
  - bc/matching
  - bc/skill-taxonomy
  - topic/matching
  - topic/ai
---
# 3.3.1. A Vector-Based (Skills-Based and Behavior) Matching Algorithms

*(SRS page 18)*

3.3.1. A Vector-Based (Skills-Based and Behavior) Matching Algorithms
ID Requirement
FR-70. The system SHALL implement AI-driven matching algorithms to connect
job seekers with relevant job opportunities.
FR-71. The system SHALL use a scoring algorithm to calculate a match
score between job seekers and job postings minimally based on the
following criteria:
 Skill overlap
 Education match
 Training match
 Location match
 Experience range
 Salary expectation range
FR-72. The system SHALL generate shortlists of top-matching candidates (e.g.,
top 100) for each job posting to assist employers in the selection process.
FR-73. The system SHALL use natural language processing (NLP) to understand
the semantic meaning of job descriptions and resumes beyond keyword
matching.
FR-74. The system SHALL perform keyword and semantic analysis on posted
job descriptions to identify key attributes such as required skills,
experience levels, and job categories.
FR-75. The system SHALL allow administrators to define and adjust a minimum
match threshold (e.g., 60%) that determines which job matches are
displayed to users.
FR-76. The system SHALL rank/order job postings for each job seeker based on
their individual match percentage, displaying the most relevant
opportunities first.
FR-77. The system SHALL support two-way matching, where job seekers
receive ranked job opportunities based on match percentage, and
employers receive reverse matches with ranked lists of suitable job
seekers for their postings.
FR-78. The system SHALL allow configuration of matching parameters to adjust
the importance of different factors.
FR-79. The system SHALL implement AI-powered resume parsing to extract
structured information from uploaded documents.
FR-80. The system SHALL extract the following information from resumes:
personal details, contact information, education history, work
experience, skills, certifications, and achievements.
FR-81. The system SHALL support multiple languages in resume parsing, with
primary focus on Arabic and English.
FR-82. The system SHALL identify and standardize skills mentioned in resumes
to facilitate matching.
FR-83. The system SHALL provide confidence scores for extracted information
and highlight areas that may need manual verification.
FR-84. The system SHALL allow job seekers to review and correct parsed
information.

## Related

- [[3_1_1_Job_Seeker_Registration_and_Profile_Management|3.1.1. Job Seeker Registration and Profile Management]]
- [[3_1_4_Administrator_User_Management|3.1.4. Administrator User Management]]
- [[3_2_1_Job_Creation_and_Publishing|3.2.1. Job Creation and Publishing]]
- [[3_3_AI_Driven_Matching_System|3.3. AI-Driven Matching System]]
- [[3_3_2_Job_Recommendation_Engine|3.3.2. Job Recommendation Engine]]
- [[3_3_3_Candidate_Recommendation_for_Employers|3.3.3. Candidate Recommendation for Employers]]
