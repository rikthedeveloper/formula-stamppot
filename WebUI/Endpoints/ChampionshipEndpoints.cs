﻿using FluentValidation;
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

public class ChampionshipResource(Championship championship, string version) : IVersioned
{
    public ChampionshipId ChampionshipId { get; } = championship.ChampionshipId;
    public string Name { get; } = championship.Name;
    public string Version { get; } = version;
    public FeatureCollection Features { get; } = championship.Features;
}

public class ChampionshipChangeRequestBody : IValidator2
{
    public string Name { get; init; } = string.Empty;
    public FeatureCollection Features { get; init; } = new();

    public void Apply(Championship championship)
    {
        championship.Name = Name;
        championship.Features = Features;
    }

    static readonly Validator _validator = new();
    public async Task<ValidationResult> ValidateAsync() => await _validator.ValidateAsync(this);

    class Validator : AbstractValidator<ChampionshipChangeRequestBody>
    {
        public Validator()
        {
             RuleFor(x => x.Name).NotEmpty().Length(3, 100);
        }
    }
}

public static class ChampionshipEndpoints
{
    public static RouteGroupBuilder MapChampionships(this IEndpointRouteBuilder endpoints)
    {
        var groupBuilder = endpoints.MapGroup("championships").WithTags("Championships");
        groupBuilder.MapGet("/", ListChampionships).WithName(nameof(ListChampionships));
        groupBuilder.MapPost("/", CreateChampionship).WithName(nameof(CreateChampionship));
        groupBuilder.MapGet("/{championshipId}", FindChampionshipById).WithName(nameof(FindChampionshipById));
        groupBuilder.MapPut("/{championshipId}", UpdateChampionshipById).WithName(nameof(UpdateChampionshipById));
        return groupBuilder;
    }

    public static async Task<IResult> CreateChampionship(
        ChampionshipChangeRequestBody change,
        [FromServices] IObjectStore objectStore,
        [FromServices] GenerateId generateId,
        CancellationToken cancellationToken = default)
    {
        ChampionshipId newId = new(generateId());
        var championship = new Championship(newId);
        change.Apply(championship);

        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        await transaction.Championships.InsertAsync(championship, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var createdChampionship = await objectStore.Championships.FindAsync([new ChampionshipIdSpecification(newId)], cancellationToken) ?? throw new InvalidChampionshipException(newId);
        return Results.CreatedAtRoute(nameof(FindChampionshipById), new
        {
            championshipId = newId.ToString("BASE36")
        }, new ChampionshipResource(createdChampionship, createdChampionship.Version));
    }

    public static async Task<IResult> ListChampionships([FromServices] IObjectStore objectStore, CancellationToken cancellationToken = default)
    {
        var championships = await objectStore.Championships.ListAsync(cancellationToken);
        return Results.Ok(new ResourceCollection<ChampionshipResource>(championships.Select(c => new ChampionshipResource(c, c.Version))));
    }

    public static async Task<IResult> FindChampionshipById(
        [AsParameters] ChampionshipRouteParameters routeParameters,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        var championship = await objectStore.Championships.FindAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken)
            ?? throw new InvalidChampionshipException(routeParameters);

        return Results.Ok(new ChampionshipResource(championship, championship.Version));
    }

    public static async Task<IResult> UpdateChampionshipById(
        [AsParameters] ChampionshipRouteParameters routeParameters,
        [FromBody] ChampionshipChangeRequestBody change,
        [FromHeader(Name = "If-Match")] ObjectVersion version,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        var championship = await transaction.Championships.FindAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken)
            ?? throw new InvalidChampionshipException(routeParameters);

        change.Apply(championship);
        if (await transaction.Championships.UpdateAsync([routeParameters.ChampionshipIdSpecification(), new VersionMatchSpecification(version)], championship.Object, cancellationToken) == 0)
        {
            throw new OptimisticConcurrencyException();
        }

        await transaction.CommitAsync(cancellationToken);
        var updatedObject = await objectStore.Championships.FindAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken)
            ?? throw new InvalidChampionshipException(routeParameters);
        return Results.Ok(new ChampionshipResource(updatedObject, updatedObject.Version));
    }
}

