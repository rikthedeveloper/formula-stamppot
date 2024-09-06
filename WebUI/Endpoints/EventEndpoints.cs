using Microsoft.AspNetCore.Mvc;
using WebUI.Domain.ObjectStore;
using WebUI.Domain;
using WebUI.Endpoints.Internal.Specifications;
using WebUI.Endpoints.Resources.Interfaces;
using WebUI.Endpoints.Resources;
using WebUI.Types;
using WebUI.Filters;
using FluentValidation.Results;
using FluentValidation;
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

public class EventChangeBody : IValidator2
{
    public TrackId TrackId { get; init; }

    public void Apply(Event @event)
    {
        @event.TrackId = TrackId;
    }

    static readonly Validator _validator = new();
    public async Task<ValidationResult> ValidateAsync() => await _validator.ValidateAsync(this);

    class Validator : AbstractValidator<EventChangeBody>
    {
        public Validator()
        {
            RuleFor(d => d.TrackId).NotEmpty();
        }
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
        ChampionshipId championshipId,
        EventChangeBody change,
        [FromServices] IObjectStore objectStore,
        [FromServices] GenerateId generateId,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([new ChampionshipIdSpecification(championshipId)], cancellationToken))
        {
            throw new InvalidChampionshipException(championshipId);
        }

        EventId @eventId = new(generateId());
        var @event = new Event(championshipId, @eventId);
        change.Apply(@event);

        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        await transaction.Events.InsertAsync(@event, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var createdEvent = await objectStore.Events.FindAsync([new EventIdSpecification(championshipId, @eventId)], cancellationToken) ?? throw new InvalidEventException(championshipId, @eventId);
        return Results.CreatedAtRoute(nameof(FindEventById), new
        {
            championshipId = championshipId.ToString("BASE36"),
            @eventId = @eventId.ToString("BASE36")
        }, new EventResource(createdEvent, createdEvent.Version));
    }

    public static async Task<IResult> ListEvents(
        ChampionshipId championshipId,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([new ChampionshipIdSpecification(championshipId)], cancellationToken))
        {
            throw new InvalidChampionshipException(championshipId);
        }

        var @events = await objectStore.Events.ListAsync([new ChampionshipIdSpecification(championshipId)], cancellationToken);
        return Results.Ok(new EventResourceCollection(championshipId, @events.Select(d => new EventResource(d, d.Version))));
    }

    public static async Task<IResult> FindEventById(
        ChampionshipId championshipId,
        EventId @eventId,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([new ChampionshipIdSpecification(championshipId)], cancellationToken))
        {
            throw new InvalidChampionshipException(championshipId);
        }

        var @event = await objectStore.Events.FindAsync([new EventIdSpecification(championshipId, @eventId)], cancellationToken)
            ?? throw new InvalidEventException(championshipId, @eventId);

        return Results.Ok(new EventResource(@event, @event.Version));
    }

    public static async Task<IResult> UpdateEventById(
        ChampionshipId championshipId,
        EventId @eventId,
        [FromBody] EventChangeBody change,
        [FromHeader(Name = "If-Match")] string version,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        version = version.Trim('"'); // HTTP header MUST have quotation marks, but we don't want them here

        if (!await objectStore.Championships.ExistsAsync([new ChampionshipIdSpecification(championshipId)], cancellationToken))
        {
            throw new InvalidChampionshipException(championshipId);
        }

        var @eventIdSpecification = new EventIdSpecification(championshipId, @eventId);
        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        var @event = await transaction.Events.FindAsync([@eventIdSpecification], cancellationToken)
            ?? throw new InvalidEventException(championshipId, @eventId);

        change.Apply(@event.Object);
        if (await transaction.Events.UpdateAsync([@eventIdSpecification, new VersionMatchSpecification(version)], @event.Object, cancellationToken) == 0)
        {
            throw new OptimisticConcurrencyException();
        }

        await transaction.CommitAsync(cancellationToken);
        var updatedObject = await objectStore.Events.FindAsync([@eventIdSpecification], cancellationToken)
            ?? throw new InvalidEventException(championshipId, @eventId);
        return Results.Ok(new EventResource(updatedObject, updatedObject.Version));
    }
}

