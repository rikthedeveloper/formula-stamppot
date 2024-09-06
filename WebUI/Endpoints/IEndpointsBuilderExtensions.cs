using System.Net;
using WebUI.Endpoints.Resources;
using WebUI.Endpoints.Resources.Hypermedia;
using WebUI.Filters;

namespace WebUI.Endpoints;

public static class IEndpointsBuilderExtensions
{
    static ProblemInfo ProblemInfoFactoryForValidationException(ValidationException ex)
        => new(HttpStatusCode.UnprocessableEntity, ex.Message, new
        {
            validationMessages = ex.Errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        });

    public static void MapFormulaApi(this IEndpointRouteBuilder endpoints, IWebHostEnvironment environment)
    {
        var exceptionFilter = new ExceptionHandlerFilter(environment.IsDevelopment())
            .AddStatusCodeTitle(HttpStatusCode.NotFound, "The specified resource was not found.")
            .AddStatusCodeTitle(HttpStatusCode.Conflict, "The request could not be completed due to a conflict with the current state of the target resource.")
            .AddStatusCodeTitle(HttpStatusCode.UnprocessableEntity, "The request data was invalid.")
            .AddStatusCodeTitle(HttpStatusCode.PreconditionFailed, "One or more conditions given in the request header fields evaluated to false when tested on the server.")
            .AddProblemInfoFactory<ValidationException>(ProblemInfoFactoryForValidationException)
            .AddProblemInfoFactory<MissingPreconditionException>(ex => new(HttpStatusCode.PreconditionRequired, ex.Message, null))
            .AddProblemInfoFactory<OptimisticConcurrencyException>(ex => new(HttpStatusCode.PreconditionFailed, ex.Message, null))
            .AddProblemInfoFactory<InvalidChampionshipException>(ex => new(HttpStatusCode.NotFound, ex.Message, new { ex.ChampionshipId }));

        var hypermediaOptions = new HypermediaOptions()
            .AddResource<ApiInfoHypermediaFactory, ApiInfoResource>()
            .AddResource<ChampionshipHypermediaFactory, ChampionshipResource>()
            .AddResource<ChampionshipsCollectionHypermediaFactory, ResourceCollection<ChampionshipResource>>()
            .AddResource<TrackHypermediaFactory, TrackResource>()
            .AddResource<TracksCollectionHypermediaFactory, TrackResourceCollection>()
            .AddResource<DriverHypermediaFactory, DriverResource>()
            .AddResource<DriversCollectionHypermediaFactory, DriverResourceCollection>()
            .AddResource<EventHypermediaFactory, EventResource>()
            .AddResource<EventsCollectionHypermediaFactory, EventResourceCollection>();

        var api = endpoints.MapGroup("/")
            .AddEndpointFilter(exceptionFilter)
            .AddEndpointFilter(new RequirePreconditionFilter())
            .AddEndpointFilterFactory(ValidationFilter.FilterFactory)
            .AddHypermediaFilters(hypermediaOptions);

        api.MapApi("/");
        api.MapChampionships();
        api.MapTracks();
        api.MapDrivers();
        api.MapEvents();
        api.MapSessions();
    }
}
