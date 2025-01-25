using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;
using WebUI.Endpoints.Internal;
using WebUI.Endpoints.Internal.Specifications;
using WebUI.Endpoints.Resources;
using WebUI.Endpoints.Resources.Interfaces;
using WebUI.Types;
using WebUI.Validation;
using EventId = WebUI.Types.EventId;

namespace WebUI.Endpoints;

public class EventResourceCollection(ChampionshipId championshipId, IEnumerable<EventResource> items) : ResourceCollection<EventResource>(items)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
}

public class EventResource(Event @event, string version) : IVersioned
{
    public ChampionshipId ChampionshipId { get; } = @event.ChampionshipId;
    public EventId EventId { get; } = @event.EventId;
    public TrackId TrackId { get; } = @event.TrackId;
    public string Version { get; } = version;
}

[UseValidator]
public partial class EventChangeBody
{
    public TrackId TrackId { get; init; }

    public void Apply(Event @event)
    {
        @event.TrackId = TrackId;
    }

    static partial void ConfigureValidator(AbstractValidator<EventChangeBody> validator)
    {
        validator.RuleFor(d => d.TrackId).NotEmpty();
    }
}

public static class EventEndpoints
{
    public static RouteGroupBuilder MapEvents(this IEndpointRouteBuilder endpoints)
    {
        var groupBuilder = endpoints.MapGroup("championships/{championshipId}/events").WithTags("Events");
        groupBuilder.MapGet("/", ListEvents).WithName(nameof(ListEvents));
        groupBuilder.MapPost("/", CreateEvent).WithName(nameof(CreateEvent));
        groupBuilder.MapGet("/{eventId}", FindEventById).WithName(nameof(FindEventById));
        groupBuilder.MapPut("/{eventId}", UpdateEventById).WithName(nameof(UpdateEventById));
        return groupBuilder;
    }

    public static async Task<IResult> CreateEvent(
        [AsParameters] ChampionshipRouteParameters routeParameters,
        EventChangeBody change,
        [FromServices] IObjectStore objectStore,
        [FromServices] GenerateId generateId,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        EventId @eventId = new(generateId());
        var @event = new Event(routeParameters.ChampionshipId, @eventId);
        change.Apply(@event);

        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        await transaction.Events.InsertAsync(@event, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var createdEvent = await objectStore.Events.FindAsync([routeParameters.EventIdSpecification(@eventId)], cancellationToken) ?? throw new InvalidEventException(routeParameters, @eventId);
        return Results.CreatedAtRoute(nameof(FindEventById), routeParameters.ToRouteValues(eventId), new EventResource(createdEvent, createdEvent.Version));
    }

    public static async Task<IResult> ListEvents(
        [AsParameters] ChampionshipRouteParameters routeParameters,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        var @events = await objectStore.Events.ListAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken);
        return Results.Ok(new EventResourceCollection(routeParameters.ChampionshipId, @events.Select(d => new EventResource(d, d.Version))));
    }

    public static async Task<IResult> FindEventById(
        [AsParameters] EventRouteParameters routeParameters,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        var @event = await objectStore.Events.FindAsync([routeParameters.EventIdSpecification()], cancellationToken)
            ?? throw new InvalidEventException(routeParameters);

        return Results.Ok(new EventResource(@event, @event.Version));
    }

    public static async Task<IResult> UpdateEventById(
        [AsParameters] EventRouteParameters routeParameters,
        [FromBody] EventChangeBody change,
        [FromHeader(Name = "If-Match")] VersionMatchSpecification versionMatch,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        var eventIdSpecification = routeParameters.EventIdSpecification();
        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        var @event = await transaction.Events.FindAsync([eventIdSpecification], cancellationToken)
            ?? throw new InvalidEventException(routeParameters);

        change.Apply(@event.Object);
        if (await transaction.Events.UpdateAsync([eventIdSpecification, versionMatch], @event.Object, cancellationToken) == 0)
        {
            throw new OptimisticConcurrencyException();
        }

        await transaction.CommitAsync(cancellationToken);
        var updatedObject = await objectStore.Events.FindAsync([eventIdSpecification], cancellationToken)
            ?? throw new InvalidEventException(routeParameters);
        return Results.Ok(new EventResource(updatedObject, updatedObject.Version));
    }
}

