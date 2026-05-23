using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.Reporting.Core.Application.DTOs;
using Nexhire.Modules.Reporting.Core.Application.Reports.Commands;
using Nexhire.Modules.Reporting.Core.Application.Reports.Queries;
using Nexhire.Modules.Reporting.Core.Domain.Enums;

namespace Nexhire.Modules.Reporting.Infrastructure.Endpoints;

public static class ReportingEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reporting").WithTags("Reporting");

        // Activity
        group.MapGet("/activity/dashboard", async (ISender sender) =>
        {
            var result = await sender.Send(new GetUserActivityDashboardQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Message);
        }).WithName("GetUserActivityDashboard");

        group.MapGet("/activity/job-seekers", async (ISender sender, DateTime? from, DateTime? to, int page = 1, int pageSize = 50) =>
        {
            var result = await sender.Send(new GetJobSeekerActivityReportQuery(from, to, page, pageSize));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Message);
        }).WithName("GetJobSeekerActivity");

        group.MapGet("/activity/employers", async (ISender sender, DateTime? from, DateTime? to, int page = 1, int pageSize = 50) =>
        {
            var result = await sender.Send(new GetEmployerActivityReportQuery(from, to, page, pageSize));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Message);
        }).WithName("GetEmployerActivity");

        group.MapGet("/activity/{userId}/timeline", async (ISender sender, Guid userId, DateTime? from, DateTime? to) =>
        {
            var result = await sender.Send(new GetActivityTimelineQuery(userId, from, to));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        }).WithName("GetActivityTimeline");

        // Retention
        group.MapGet("/retention/policies", async (ISender sender) =>
        {
            var result = await sender.Send(new GetRetentionPoliciesQuery());
            return Results.Ok(result.Value);
        }).WithName("GetRetentionPolicies");

        group.MapPost("/retention/policies", async (ISender sender, CreateRetentionPolicyCommand cmd) =>
        {
            var result = await sender.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/reporting/retention/policies/{result.Value}", new { id = result.Value }) : Results.BadRequest(result.Error);
        }).WithName("CreateRetentionPolicy");

        group.MapDelete("/retention/policies/{id}", async (ISender sender, Guid id) =>
        {
            var result = await sender.Send(new ArchiveRetentionPolicyCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.NotFound();
        }).WithName("ArchiveRetentionPolicy");

        // LMIS
        group.MapGet("/lmis/employment-stats", async (ISender sender, DateTime from, DateTime to, RollupGrain grain = RollupGrain.Day) =>
        {
            var result = await sender.Send(new GetEmploymentStatsDashboardQuery(from, to, grain));
            return Results.Ok(result.Value);
        }).WithName("GetEmploymentStats");

        group.MapGet("/lmis/skill-demand", async (ISender sender, RollupGrain grain = RollupGrain.Month) =>
        {
            var result = await sender.Send(new GetSkillDemandTrendsQuery(grain));
            return Results.Ok(result.Value);
        }).WithName("GetSkillDemand");

        // Performance
        group.MapGet("/performance/system", async (ISender sender, DateTime from, DateTime to) =>
        {
            var result = await sender.Send(new GetSystemPerformanceDashboardQuery(from, to));
            return Results.Ok(result.Value);
        }).WithName("GetSystemPerformance");

        group.MapGet("/performance/matching", async (ISender sender, RollupGrain grain = RollupGrain.Month) =>
        {
            var result = await sender.Send(new GetMatchingPerformanceQuery(grain));
            return Results.Ok(result.Value);
        }).WithName("GetMatchingPerformance");

        // Alerts
        group.MapGet("/alerts/rules", async (ISender sender) =>
        {
            var result = await sender.Send(new GetAlertRulesQuery());
            return Results.Ok(result.Value);
        }).WithName("GetAlertRules");

        group.MapPost("/alerts/rules", async (ISender sender, CreateAlertRuleCommand cmd) =>
        {
            var result = await sender.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/reporting/alerts/rules/{result.Value}", new { id = result.Value }) : Results.BadRequest(result.Error);
        }).WithName("CreateAlertRule");

        group.MapPost("/alerts/rules/{id}/enable", async (ISender sender, Guid id) =>
        {
            var result = await sender.Send(new EnableAlertRuleCommand(id));
            return result.IsSuccess ? Results.Ok() : Results.NotFound();
        }).WithName("EnableAlertRule");

        group.MapPost("/alerts/rules/{id}/disable", async (ISender sender, Guid id) =>
        {
            var result = await sender.Send(new DisableAlertRuleCommand(id));
            return result.IsSuccess ? Results.Ok() : Results.NotFound();
        }).WithName("DisableAlertRule");

        group.MapGet("/alerts/incidents", async (ISender sender) =>
        {
            var result = await sender.Send(new GetActiveAlertIncidentsQuery());
            return Results.Ok(result.Value);
        }).WithName("GetActiveAlertIncidents");

        group.MapPost("/alerts/incidents/{ruleId}/{incidentId}/acknowledge", async (ISender sender, Guid ruleId, Guid incidentId, Guid byUserId) =>
        {
            var result = await sender.Send(new AcknowledgeAlertIncidentCommand(ruleId, incidentId, byUserId));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).WithName("AcknowledgeAlertIncident");

        group.MapPost("/alerts/incidents/{ruleId}/{incidentId}/suppress", async (ISender sender, Guid ruleId, Guid incidentId, DateTime untilUtc) =>
        {
            var result = await sender.Send(new SuppressAlertIncidentCommand(ruleId, incidentId, untilUtc));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).WithName("SuppressAlertIncident");

        group.MapPost("/alerts/incidents/{ruleId}/{incidentId}/escalate", async (ISender sender, Guid ruleId, Guid incidentId) =>
        {
            var result = await sender.Send(new EscalateAlertIncidentCommand(ruleId, incidentId));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).WithName("EscalateAlertIncident");

        // Reports
        group.MapGet("/reports/menu", async (ISender sender, string callerRole = "SystemAdministrator") =>
        {
            var result = await sender.Send(new GetAdminReportsMenuQuery(callerRole));
            return Results.Ok(result.Value);
        }).WithName("GetAdminReportsMenu");

        group.MapGet("/reports/templates", async (ISender sender, string callerRole = "SystemAdministrator") =>
        {
            var result = await sender.Send(new GetReportTemplateLibraryQuery(callerRole));
            return Results.Ok(result.Value);
        }).WithName("GetReportTemplates");

        group.MapPost("/reports/templates", async (ISender sender, CreateReportTemplateCommand cmd) =>
        {
            var result = await sender.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/reporting/reports/definitions/{result.Value}", new { id = result.Value }) : Results.BadRequest(result.Error);
        }).WithName("CreateReportTemplate");

        group.MapPost("/reports/custom", async (ISender sender, CreateCustomReportCommand cmd) =>
        {
            var result = await sender.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/reporting/reports/definitions/{result.Value}", new { id = result.Value }) : Results.BadRequest(result.Error);
        }).WithName("CreateCustomReport");

        group.MapPost("/reports/{definitionId}/generate", async (ISender sender, Guid definitionId, GenerateReportCommand cmd) =>
        {
            var result = await sender.Send(cmd with { DefinitionId = definitionId });
            return result.IsSuccess ? Results.Accepted($"/api/reporting/reports/runs/{result.Value}", new { runId = result.Value }) : Results.BadRequest(result.Error);
        }).WithName("GenerateReport");

        group.MapGet("/reports/runs/{runId}", async (ISender sender, Guid runId) =>
        {
            var result = await sender.Send(new GetReportRunQuery(runId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        }).WithName("GetReportRun");

        // Schedules
        group.MapGet("/schedules", async (ISender sender) =>
        {
            var result = await sender.Send(new GetReportSchedulesQuery());
            return Results.Ok(result.Value);
        }).WithName("GetReportSchedules");

        group.MapPost("/schedules", async (ISender sender, CreateReportScheduleCommand cmd) =>
        {
            var result = await sender.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/reporting/schedules/{result.Value}", new { id = result.Value }) : Results.BadRequest(result.Error);
        }).WithName("CreateReportSchedule");

        group.MapPost("/schedules/{id}/pause", async (ISender sender, Guid id) =>
        {
            var result = await sender.Send(new PauseReportScheduleCommand(id));
            return result.IsSuccess ? Results.Ok() : Results.NotFound();
        }).WithName("PauseReportSchedule");

        group.MapPost("/schedules/{id}/resume", async (ISender sender, Guid id) =>
        {
            var result = await sender.Send(new ResumeReportScheduleCommand(id));
            return result.IsSuccess ? Results.Ok() : Results.NotFound();
        }).WithName("ResumeReportSchedule");

        group.MapDelete("/schedules/{id}", async (ISender sender, Guid id) =>
        {
            var result = await sender.Send(new DeleteReportScheduleCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteReportSchedule");

        // Audit
        group.MapGet("/reports/access-audit", async (ISender sender, Guid? userId, int page = 1, int pageSize = 50) =>
        {
            var result = await sender.Send(new GetReportAccessAuditQuery(userId, page, pageSize));
            return Results.Ok(result.Value);
        }).WithName("GetReportAccessAudit");
    }
}
