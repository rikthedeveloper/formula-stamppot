using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;
using WebUI.Endpoints.Internal;
using WebUI.Endpoints.Internal.Specifications;
using WebUI.Endpoints.Resources;
using WebUI.Endpoints.Resources.Interfaces;
using WebUI.Filters;
using WebUI.Types;

namespace WebUI.Endpoints;

public class TrackResourceCollection(ChampionshipId championshipId, IEnumerable<TrackResource> items) : ResourceCollection<TrackResource>(items)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
}

public class TrackResource(Track track, string version) : IVersioned
{
    public ChampionshipId ChampionshipId { get; } = track.ChampionshipId;
    public TrackId TrackId { get; } = track.TrackId;
    public string Name { get; } = track.Name;
    public Distance Length { get; init; } = track.Length;
    public string City { get; init; } = track.City;
    public string Country { get; init; } = track.Country;
    public string Version { get; } = version;
}

public class TrackChangeBody : IValidator2
{
    public string Name { get; init; } = string.Empty;
    public Distance Length { get; init; } = Distance.Zero;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;

    public void Apply(Track track)
    {
        track.Name = Name;
        track.Length = Length;
        track.City = City;
        track.Country = Country;
    }

    public async Task<ValidationResult> ValidateAsync() => await new TrackChangeBodyValidator().ValidateAsync(this);
    class TrackChangeBodyValidator : AbstractValidator<TrackChangeBody>
    {
        public TrackChangeBodyValidator()
        {
            RuleFor(t => t.Name).NotEmpty().Length(3, 100);
            RuleFor(t => t.City).NotEmpty().Length(3, 100);
            RuleFor(t => t.Country).NotEmpty().Length(3, 100);
        }
    }
}

public static class TrackEndpoints
{
    public static RouteGroupBuilder MapTracks(this IEndpointRouteBuilder endpoints)
    {
        var groupBuilder = endpoints.MapGroup("championships/{championshipId}/tracks").WithTags("Tracks");
        groupBuilder.MapGet("/", ListTracks).WithName(nameof(ListTracks));
        groupBuilder.MapPost("/", CreateTrack).WithName(nameof(CreateTrack));
        groupBuilder.MapGet("/{trackId}", FindTrackById).WithName(nameof(FindTrackById));
        groupBuilder.MapPut("/{trackId}", UpdateTrackById).WithName(nameof(UpdateTrackById));
        return groupBuilder;
    }

    public static async Task<IResult> CreateTrack(
        [AsParameters] ChampionshipRouteParameters routeParameters,
        TrackChangeBody change,
        [FromServices] IObjectStore objectStore,
        [FromServices] GenerateId generateId,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        TrackId trackId = new(generateId());
        var track = new Track(routeParameters.ChampionshipId, trackId);
        change.Apply(track);

        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        await transaction.Tracks.InsertAsync(track, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var createdTrack = await objectStore.Tracks.FindAsync([routeParameters.TrackIdSpecification(trackId)], cancellationToken) ?? throw new InvalidTrackException(routeParameters, trackId);
        return Results.CreatedAtRoute(nameof(FindTrackById), routeParameters.ToRouteValues(trackId), new TrackResource(createdTrack, createdTrack.Version));
    }

    public static async Task<IResult> ListTracks(
        [AsParameters] ChampionshipRouteParameters routeParameters,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        var tracks = await objectStore.Tracks.ListAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken);
        return Results.Ok(new TrackResourceCollection(routeParameters.ChampionshipId, tracks.Select(c => new TrackResource(c, c.Version))));
    }

    public static async Task<IResult> FindTrackById(
        [AsParameters] TrackRouteParameters routeParameters,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        var track = await objectStore.Tracks.FindAsync([routeParameters.TrackIdSpecification()], cancellationToken)
            ?? throw new InvalidTrackException(routeParameters);

        return Results.Ok(new TrackResource(track, track.Version));
    }

    public static async Task<IResult> UpdateTrackById(
        [AsParameters] TrackRouteParameters routeParameters,
        [FromBody] TrackChangeBody change,
        [FromHeader(Name = "If-Match")] VersionMatchSpecification versionMatch,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        var track = await transaction.Tracks.FindAsync([routeParameters.TrackIdSpecification()], cancellationToken)
            ?? throw new InvalidTrackException(routeParameters);

        change.Apply(track.Object);
        if (await transaction.Tracks.UpdateAsync([routeParameters.TrackIdSpecification(), versionMatch], track.Object, cancellationToken) == 0)
        {
            throw new OptimisticConcurrencyException();
        }

        await transaction.CommitAsync(cancellationToken);
        var updatedObject = await objectStore.Tracks.FindAsync([routeParameters.TrackIdSpecification()], cancellationToken)
            ?? throw new InvalidTrackException(routeParameters);
        return Results.Ok(new TrackResource(updatedObject, updatedObject.Version));
    }
}

