using System.Diagnostics.CodeAnalysis;
using System.Net;
using WebUI.Endpoints.Resources;
using WebUI.Filters;

namespace WebUI.Endpoints;

public static class IEndpointsBuilderExtensions
{
    public static void MapFormulaApi(this IEndpointRouteBuilder endpoints, IWebHostEnvironment environment)
    {
        var exceptionFilter = new ExceptionHandlerFilter(environment.IsDevelopment())
            .AddStatusCodeTitle(HttpStatusCode.NotFound, "The specified resource was not found.")
            .AddStatusCodeTitle(HttpStatusCode.Conflict, "The request could not be completed due to a conflict with the current state of the target resource.")
            .AddStatusCodeTitle(HttpStatusCode.UnprocessableEntity, "The request data was invalid.")
            .AddStatusCodeTitle(HttpStatusCode.PreconditionFailed, "One or more conditions given in the request header fields evaluated to false when tested on the server.")
            .AddProblemInfoFactory<MissingPreconditionException>(ex => new(HttpStatusCode.PreconditionRequired, ex.Message, null))
            .AddProblemInfoFactory<OptimisticConcurrencyException>(ex => new(HttpStatusCode.PreconditionFailed, ex.Message, null))
            .AddProblemInfoFactory<InvalidChampionshipException>(ex => new(HttpStatusCode.NotFound, ex.Message, new { ex.ChampionshipId }));

        var api = endpoints.MapGroup("/")
            .AddEndpointFilter(exceptionFilter)
            .AddEndpointFilter(new RequirePreconditionFilter())
            .AddHypermediaFilters();

        api.MapChampionships();
        api.MapTracks();
    }
}
