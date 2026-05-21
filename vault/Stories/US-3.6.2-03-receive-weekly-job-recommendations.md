---
story_id: "US-3.6.2-03"
title: "Receive weekly job recommendations in-app"
section_id: "3.6.2"
related_requirements: ["FR-151"]
related_stories: ["US-3.6.2-01"]
role: "Job Seeker"
status: draft
priority: should
tags:
  - story
  - bc/notifications
---

# US-3.6.2-03 — Receive weekly job recommendations in-app

## Story
As a **Job Seeker**, I want to receive weekly in-app notifications with personalized job recommendations based on my profile and search history, so that I discover new opportunities matched to my preferences without manually searching.

## Acceptance Criteria

**AC-01 — Weekly recommendation notification**
- Given I have saved search criteria or completed my profile
- When the weekly recommendation window arrives (e.g., Monday 8 AM)
- Then an in-app notification is created with a summary of recommended jobs

**AC-02 — Personalized recommendations**
- Given I have search history and profile information
- When recommendations are generated
- Then they are matched on: job title, location, salary range, industry, skills, experience level

**AC-03 — Recommendation notification content**
- Given I receive a weekly recommendation notification
- When I view it
- Then it shows: count of recommendations (e.g., "5 new jobs match your profile"), preview of top jobs, "View all" link

**AC-04 — Navigate to job listing**
- Given I click on a recommended job in the notification
- When the link is activated
- Then I am taken to the job detail page with apply/save options

**AC-05 — Disable weekly recommendations**
- Given I have weekly recommendations enabled
- When I choose to disable in preferences
- Then I no longer receive weekly recommendation notifications

**AC-06 — Recommendation quality metrics**
- Given recommendations are sent weekly
- When I interact with them (click, apply, save)
- Then the system tracks engagement to improve future recommendations

## Assumptions
- Recommendation engine: based on FR-151 and [[3_3_2_Job_Recommendation_Engine|3.3.2. Job Recommendation Engine]]
- Schedule: weekly, Monday 8 AM user timezone
- Minimum recommendations per week: 3 jobs
- Maximum recommendations: 10 jobs (top matches only)
- Job recommendations updated if search criteria change mid-week
- If fewer than 3 matching jobs available, notification is not sent that week
- Default setting: enabled for active job seekers

## Source Requirements
- [[3_6_2_In_App_Notifications|3.6.2]] — FR-151
- [[3_3_2_Job_Recommendation_Engine|3.3.2. Job Recommendation Engine]]

## Related Stories
- [[US-3.6.2-01-access-in-app-notification-center|US-3.6.2-01]]
