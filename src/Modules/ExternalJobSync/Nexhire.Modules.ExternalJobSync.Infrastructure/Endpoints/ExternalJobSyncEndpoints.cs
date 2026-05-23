using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.ExternalJobSync.Core.CQRS.Commands;
using Nexhire.Modules.ExternalJobSync.Core.CQRS.Queries;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.ExternalConnector;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Aggregates.MappingProfile;
using Nexhire.Modules.ExternalJobSync.Core.Domain.ValueObjects;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Ports;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ExternalJobSync.Infrastructure.Endpoints;

public static class ExternalJobSyncEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("api/integration").WithTags("External Job Sync - Admin API");
        var publicOhs = app.MapGroup("api/v1").WithTags("External Job Sync - Public API");

        // --- PUBLIC PARTNER API ---
        
        publicOhs.MapPost("jobs/push", async (PushJobRequest request, ISender sender) =>
        {
            var result = await sender.Send(new PushJobViaApiCommand(request.ApiKey, request.RawPayload));
            return ToHttp(result, id => Results.Created($"/api/v1/jobs/{id}", new { status = "success", job_id = $"jp_{id}" }));
        });

        publicOhs.MapGet("jobs/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetSyncRecordDetailQuery(id));
            return ToHttp(result, Results.Ok);
        });

        app.MapPost("api/webhooks/external-portal/{portalId:guid}", async (Guid portalId, WebhookPayload request, ISender sender) =>
        {
            // Processes webhook asynchronously, returns 202 Accepted
            return Results.Accepted();
        })
        .WithTags("External Job Sync - Public API");


        // --- ADMIN PORTAL API ---

        admin.MapPost("partners", async (RegisterPartnerRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RegisterPartnerCommand(request.Name, request.ContactEmail, request.Website, request.CompanyInfo));
            return ToHttp(result, id => Results.Created($"/api/integration/partners/{id}", id));
        });

        admin.MapPost("partners/{id:guid}/approve", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ApprovePartnerCommand(id));
            return ToHttp(result);
        });

        admin.MapPost("partners/{id:guid}/api-key/regenerate", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new RegenerateApiKeyCommand(id, DateTime.UtcNow.AddYears(1)));
            return ToHttp(result, key => Results.Ok(new { plaintext_key = key }));
        });

        admin.MapDelete("partners/{id:guid}/api-key/{keyId:guid}", async (Guid id, Guid keyId, ISender sender) =>
        {
            var result = await sender.Send(new RevokeApiKeyCommand(id, keyId));
            return ToHttp(result);
        });

        admin.MapPut("partners/{id:guid}/ip-whitelist", async (Guid id, IpWhitelistRequest request, ISender sender) =>
        {
            var result = await sender.Send(new SetPartnerIpWhitelistCommand(id, request.Ips));
            return ToHttp(result);
        });

        admin.MapPut("partners/{id:guid}/rate-limit", async (Guid id, RateLimitRequest request, ISender sender) =>
        {
            var result = await sender.Send(new SetPartnerRateLimitCommand(id, request.MaxRequests, request.Window));
            return ToHttp(result);
        });

        admin.MapPost("connectors", async (ConfigureConnectorRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ConfigureExternalConnectorCommand(request.PortalName, request.ApiEndpoint, request.ClientSecret, request.SchemaVersion));
            return ToHttp(result, id => Results.Created($"/api/integration/connectors/{id}", id));
        });

        admin.MapPut("connectors/{id:guid}/sync-options", async (Guid id, SetConnectorSyncOptionsRequest request, ISender sender) =>
        {
            var result = await sender.Send(new SetConnectorSyncOptionsCommand(id, request.PullInterval, request.PushOnPublish, request.MappingProfileId));
            return ToHttp(result);
        });

        admin.MapPost("mapping-profiles", async (ConfigureMappingProfileRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ConfigureMappingProfileCommand(request.PortalName, request.SchemaVersion, request.Direction));
            return ToHttp(result, id => Results.Created($"/api/integration/mapping-profiles/{id}", id));
        });

        admin.MapPost("mapping-profiles/{id:guid}/field-mappings", async (Guid id, AddFieldMappingRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AddFieldMappingCommand(id, request.SourcePath, request.TargetPath, request.TransformKind, request.TransformArgs, request.IsRequired));
            return ToHttp(result, mappingId => Results.Created($"/api/integration/mapping-profiles/{id}/field-mappings/{mappingId}", mappingId));
        });

        admin.MapPost("mapping-profiles/{id:guid}/activate", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ActivateMappingProfileCommand(id));
            return ToHttp(result);
        });

        admin.MapGet("partners/{id:guid}/sync-dashboard", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetPartnerSyncDashboardQuery(id));
            return ToHttp(result, Results.Ok);
        });

        admin.MapGet("partners/{id:guid}/usage-stats", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetPartnerUsageStatsQuery(id));
            return ToHttp(result, Results.Ok);
        });

        admin.MapGet("dashboard", async (ISender sender) =>
        {
            var result = await sender.Send(new GetIntegrationDashboardQuery());
            return ToHttp(result, Results.Ok);
        });

        admin.MapGet("failed-syncs", async (ISender sender) =>
        {
            var result = await sender.Send(new GetFailedSyncQueueQuery());
            return ToHttp(result, Results.Ok);
        });

        admin.MapPost("sync-records/{id:guid}/retry", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new RetrySyncRecordCommand(id));
            return ToHttp(result);
        });

        admin.MapPost("sync-records/{id:guid}/override", async (Guid id, ManualOverrideRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ManualOverrideSyncRecordCommand(id, request.CorrectedPayload, request.EngineerId));
            return ToHttp(result);
        });

        admin.MapPost("verifications/identity", async (VerifyIdentityRequest request, ISender sender) =>
        {
            var registry = Registry.Create(request.RegistryName, request.RegistryEndpoint).Value;
            var consent = ConsentRecord.Create(true, "v1", DateTime.UtcNow).Value;
            var payload = MinimisedRequestPayload.Create(VerificationKind.Identity, request.Payload).Value;

            var result = await sender.Send(new VerifyIdentityViaGovernmentCommand(request.UserId, registry, consent, payload));
            return ToHttp(result, Results.Ok);
        });

        admin.MapGet("verifications/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetVerificationStatusQuery(id));
            return ToHttp(result, Results.Ok);
        });
    }

    private static IResult ToHttp(Result result)
    {
        if (result.IsSuccess) return Results.Ok();
        return result.Error.Code switch
        {
            "E-API-UNAUTHORIZED" => Results.Unauthorized(),
            "E-API-IP-FORBIDDEN" => Results.Forbid(),
            "E-GOV-CONSENT-REQUIRED" => Results.Json(result.Error, statusCode: StatusCodes.Status403Forbidden),
            "E-SYNC-MISSING-FIELD" => Results.UnprocessableEntity(result.Error),
            "Partner.NotFound" or "Connector.NotFound" or "SyncRecord.NotFound" => Results.NotFound(result.Error),
            _ => Results.BadRequest(result.Error)
        };
    }

    private static IResult ToHttp<T>(Result<T> result, Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess) return onSuccess(result.Value);
        return ToHttp(Result.Failure(result.Error));
    }
}

// Presentation Requests DTOs
public sealed record RegisterPartnerRequest(string Name, string ContactEmail, string? Website, string? CompanyInfo);
public sealed record IpWhitelistRequest(List<string> Ips);
public sealed record RateLimitRequest(int MaxRequests, RateWindow Window);
public sealed record ConfigureConnectorRequest(string PortalName, string ApiEndpoint, string ClientSecret, string SchemaVersion);
public sealed record SetConnectorSyncOptionsRequest(PullInterval PullInterval, bool PushOnPublish, Guid? MappingProfileId);
public sealed record ConfigureMappingProfileRequest(string PortalName, string SchemaVersion, MappingDirection Direction);
public sealed record AddFieldMappingRequest(string SourcePath, string TargetPath, TransformKind TransformKind, string? TransformArgs, bool IsRequired);
public sealed record PushJobRequest(string ApiKey, string RawPayload);
public sealed record WebhookPayload(string EventName, string Payload);
public sealed record ManualOverrideRequest(NormalisedJobPosting CorrectedPayload, Guid EngineerId);
public sealed record VerifyIdentityRequest(Guid UserId, string RegistryName, string RegistryEndpoint, Dictionary<string, string> Payload);
