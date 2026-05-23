using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nexhire.Modules.AdministratorsConfiguration.Core.Application.Commands;
using Nexhire.Modules.AdministratorsConfiguration.Core.Application.Queries;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.AdministratorsConfiguration.Infrastructure.Endpoints;

public static class TaxonomyEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("api/admin/taxonomies").WithTags("Administrators Configuration (Taxonomies)");

        // 1. Seed Taxonomies
        admin.MapPost("seed", async (ClaimsPrincipal principal, ISender sender) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            var result = await sender.Send(new SeedTaxonomiesCommand());
            return ToHttp(result);
        });

        // 2. View Taxonomy (Hierarchical Tree)
        admin.MapGet("{kind}", async (string kind, ClaimsPrincipal principal, ISender sender) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            var result = await sender.Send(new GetTaxonomyQuery(kind));
            return ToHttp(result, Results.Ok);
        });

        // 3. Search / Filter Terms
        admin.MapGet("{kind}/terms", async (
            string kind,
            ClaimsPrincipal principal,
            ISender sender,
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] string? status) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            var result = await sender.Send(new SearchTaxonomyTermsQuery(kind, search, category, status));
            return ToHttp(result, Results.Ok);
        });

        // 4. View Specific Term Detail
        admin.MapGet("{kind}/terms/{code}", async (string kind, string code, ClaimsPrincipal principal, ISender sender) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            var result = await sender.Send(new GetTaxonomyTermQuery(kind, code));
            return ToHttp(result, Results.Ok);
        });

        // 5. Add Term
        admin.MapPost("{kind}/terms", async (string kind, AddTermRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            var result = await sender.Send(new AddTaxonomyTermCommand(kind, request.Code, request.Label, request.Category, request.ParentCode));
            if (result.IsSuccess)
            {
                return Results.Created($"api/admin/taxonomies/{kind}/terms/{request.Code}", request.Code);
            }
            return ToHttp(result);
        });

        // 6. Rename Term
        admin.MapPut("{kind}/terms/{code}/label", async (string kind, string code, RenameTermRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            var result = await sender.Send(new RenameTaxonomyTermCommand(kind, code, request.NewLabel));
            return ToHttp(result);
        });

        // 7. Recategorize Skill
        admin.MapPut("Skills/terms/{code}/category", async (string code, RecategorizeSkillRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            var result = await sender.Send(new RecategorizeSkillCommand(code, request.NewCategory));
            return ToHttp(result);
        });

        // 8. Reparent Term
        admin.MapPut("{kind}/terms/{code}/parent", async (string kind, string code, ReparentTermRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            var result = await sender.Send(new ReparentTaxonomyTermCommand(kind, code, request.NewParentCode));
            return ToHttp(result);
        });

        // 9. Deprecate Term
        admin.MapPost("{kind}/terms/{code}/deprecate", async (string kind, string code, DeprecateTermRequest request, ClaimsPrincipal principal, ISender sender) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            var result = await sender.Send(new DeprecateTaxonomyTermCommand(kind, code, request.ReplacedByCode));
            return ToHttp(result);
        });

        // 10. Reactivate Term
        admin.MapPost("{kind}/terms/{code}/reactivate", async (string kind, string code, ClaimsPrincipal principal, ISender sender) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            var result = await sender.Send(new ReactivateTaxonomyTermCommand(kind, code));
            return ToHttp(result);
        });

        // 11. Bulk Import CSV
        admin.MapPost("{kind}/import", async (string kind, IFormFile file, ClaimsPrincipal principal, ISender sender) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            if (file == null || file.Length == 0) return Results.BadRequest("CSV file is required.");
            if (file.Length > 5 * 1024 * 1024) return Results.BadRequest("File size exceeds the 5MB limit.");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest("Only CSV files (.csv) are supported.");
            }

            using var stream = file.OpenReadStream();
            var result = await sender.Send(new BulkImportTaxonomyCommand(kind, stream));
            
            // Returns 200 OK even with partial failures inside the report body
            return ToHttp(result, Results.Ok);
        });

        // 12. Usage stats
        admin.MapGet("{kind}/usage", async (string kind, ClaimsPrincipal principal, ISender sender) =>
        {
            if (!principal.IsInRole("MoLAdministrator")) return Results.Forbid();
            var result = await sender.Send(new GetTaxonomyUsageStatsQuery(kind));
            return ToHttp(result, Results.Ok);
        });
    }

    private static IResult ToHttp(Result result)
    {
        if (result.IsSuccess) return Results.Ok();

        return result.Error.Code switch
        {
            "E-TAXO-TERM-NOT-FOUND" or "E-TAXO-NOT-FOUND" or "E-TAXO-REPLACEMENT-NOT-FOUND" or "E-TAXO-PARENT-NOT-FOUND" => Results.NotFound(result.Error),
            "E-TAXO-DUPLICATE-CODE" => Results.Conflict(result.Error),
            "E-TAXO-CONCURRENCY-CONFLICT" => Results.Json(result.Error, statusCode: StatusCodes.Status409Conflict),
            "E-TAXO-CYCLE" or "E-TAXO-CYCLE-IN-BATCH" or "E-TAXO-PARENT-DEPRECATED" or "E-TAXO-SELF-REPLACE" => Results.Conflict(result.Error),
            "E-TAXO-CATEGORY-REQUIRED" or "E-TAXO-INVALID-CATEGORY" or "E-TAXO-CODE-PREFIX-MISMATCH" or "E-TAXO-INVALID-KIND" or "E-TAXO-INVALID-OPERATION" => Results.BadRequest(result.Error),
            _ => Results.BadRequest(result.Error)
        };
    }

    private static IResult ToHttp<T>(Result<T> result, Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess) return onSuccess(result.Value);
        return ToHttp(Result.Failure(result.Error));
    }
}

public sealed record AddTermRequest(string Code, string Label, string? Category, string? ParentCode);
public sealed record RenameTermRequest(string NewLabel);
public sealed record RecategorizeSkillRequest(string NewCategory);
public sealed record ReparentTermRequest(string? NewParentCode);
public sealed record DeprecateTermRequest(string? ReplacedByCode);
