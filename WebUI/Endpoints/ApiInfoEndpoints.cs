using System.Diagnostics.CodeAnalysis;
using WebUI.Model;

namespace WebUI.Endpoints;

public class ApiInfoResource
{
    public required string Name { get; init; }
    public required Version Version { get; init; }
}

public static class ApiInfoEndpoints
{
    public static RouteGroupBuilder MapApi(this IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string prefix)
    {
        var groupBuilder = endpoints.MapGroup(prefix).WithTags("API");
        groupBuilder.MapGet("/", GetApiInfo)
            .WithName(nameof(GetApiInfo));
        return groupBuilder;
    }

    public static IResult GetApiInfo()
    {
        return Results.Ok(new ApiInfoResource
        {
            Name = "Formula Stamppot",
            Version = new(0, 1)
        });
    }
}
