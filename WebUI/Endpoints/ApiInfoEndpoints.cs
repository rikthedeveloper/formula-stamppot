using System.Diagnostics.CodeAnalysis;
using WebUI.Model;

namespace WebUI.Endpoints;

public static class ApiInfoEndpoints
{
    public static RouteGroupBuilder MapApi(this IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string prefix)
    {
        var groupBuilder = endpoints.MapGroup(prefix);
        groupBuilder.MapGet("/", GetApiInfo)
            .WithName(nameof(GetApiInfo));
        return groupBuilder;
    }

    public static ApiInfoDto GetApiInfo()
    {
        return new ApiInfoDto
        {
            Name = "Formula Stamppot",
            Version = new(0, 1)
        };
    }
}
