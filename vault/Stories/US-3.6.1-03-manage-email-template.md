---
story_id: "US-3.6.1-03"
title: "Manage customizable email templates with dynamic content"
section_id: "3.6.1"
related_requirements: ["FR-144"]
related_stories: ["US-3.6.1-01", "US-3.6.1-05"]
role: "MoL Administrator"
status: draft
priority: must
tags:
  - story
  - bc/notifications
---

# US-3.6.1-03 — Manage customizable email templates with dynamic content

## Story
As a **MoL Administrator**, I want to create and manage email templates with dynamic content placeholders, so that notifications are personalized and on-brand without requiring code changes for each event type.

## Acceptance Criteria

**AC-01 — Create email template**
- Given I am in the email template management interface
- When I create a new template with subject, body (HTML), and footer
- Then the template is stored and available for use

**AC-02 — Support dynamic content**
- Given I am creating a template
- When I insert placeholders like {{user.firstName}}, {{job.title}}, {{link.actionUrl}}
- Then the system recognizes and populates these at send time with actual user/job data

**AC-03 — Template preview**
- Given I have created a template with placeholders
- When I request a preview with sample data
- Then the system renders the template with example values so I can verify layout and content

**AC-04 — Version control and rollback**
- Given I edit an existing template
- When I save changes
- Then the previous version is preserved, and I can revert if needed

**AC-05 — Template for each event type**
- Given the system sends multiple notification types
- When I manage templates
- Then I maintain separate templates for: job recommendations, application updates, messages, status changes, announcements

## Assumptions
- Template engine: Liquid or Handlebars (common for email platforms)
- HTML email support with fallback plain text
- Placeholder syntax: {{variable.property}} or {{#if condition}}{{/if}} for logic
- Maximum template size: 100 KB
- Character limit: 50,000 characters per template
- Admin role required for template management
- Version history retention: 12 months

## Source Requirements
- [[3_6_1_Email_Notifications|3.6.1]] — FR-144

## Related Stories
- [[US-3.6.1-01-receive-important-event-email|US-3.6.1-01]]
- [[US-3.6.1-05-ensure-spam-compliance|US-3.6.1-05]]
