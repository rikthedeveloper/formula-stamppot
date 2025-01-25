using Microsoft.AspNetCore.Mvc;
using WebUI.Domain.ObjectStore;
using WebUI.Domain;
using WebUI.Endpoints.Internal.Specifications;
using WebUI.Endpoints.Resources.Interfaces;
using WebUI.Endpoints.Resources;
using WebUI.Types;
using System.Collections.Immutable;
using WebUI.Filters;
using FluentValidation.Results;
using FluentValidation;
using WebUI.Endpoints.Internal;

namespace WebUI.Endpoints;

public class DriverResourceCollection(ChampionshipId championshipId, IEnumerable<DriverResource> items) : ResourceCollection<DriverResource>(items)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
}

public class DriverResource(Driver driver, string version) : IVersioned
{
    public ChampionshipId ChampionshipId { get; } = driver.ChampionshipId;
    public DriverId DriverId { get; } = driver.DriverId;
    public ImmutableArray<NameToken> Name { get; } = driver.Name;
    public FeatureDataCollection<IFeatureDriverData> Data { get; } = driver.Data;

    public string Version { get; } = version;
}

public class DriverChangeBody : IValidator2
{
    public ImmutableArray<NameToken> Name { get; init; } = [];
    public FeatureDataCollection<IFeatureDriverData> Data { get; init; } = new();

    public void Apply(Driver driver)
    {
        driver.Name = Name;
        driver.Data = Data;
    }

    static readonly Validator _validator = new();
    public async Task<ValidationResult> ValidateAsync() => await _validator.ValidateAsync(this);

    class Validator : AbstractValidator<DriverChangeBody>
    {
        public Validator()
        {
            RuleFor(d => d.Name).NotEmpty();
        }
    }
}

public static class DriverEndpoints
{
    public static RouteGroupBuilder MapDrivers(this IEndpointRouteBuilder endpoints)
    {
        var groupBuilder = endpoints.MapGroup("championships/{championshipId}/drivers").WithTags("Drivers");
        groupBuilder.MapGet("/", ListDrivers).WithName(nameof(ListDrivers));
        groupBuilder.MapPost("/", CreateDriver).WithName(nameof(CreateDriver));
        groupBuilder.MapGet("/{driverId}", FindDriverById).WithName(nameof(FindDriverById));
        groupBuilder.MapPut("/{driverId}", UpdateDriverById).WithName(nameof(UpdateDriverById));
        return groupBuilder;
    }

    public static async Task<IResult> CreateDriver(
        [AsParameters] ChampionshipRouteParameters routeParameters,
        DriverChangeBody change,
        [FromServices] IObjectStore objectStore,
        [FromServices] GenerateId generateId,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        DriverId driverId = new(generateId());
        var driver = new Driver(routeParameters.ChampionshipId, driverId);
        change.Apply(driver);

        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        await transaction.Drivers.InsertAsync(driver, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var createdDriver = await objectStore.Drivers.FindAsync([routeParameters.DriverIdSpecification(driverId)], cancellationToken) ?? throw new InvalidDriverException(routeParameters, driverId);
        return Results.CreatedAtRoute(nameof(FindDriverById), routeParameters.ToRouteValues(driverId), new DriverResource(createdDriver, createdDriver.Version));
    }

    public static async Task<IResult> ListDrivers(
        [AsParameters] ChampionshipRouteParameters routeParameters,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        var drivers = await objectStore.Drivers.ListAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken);
        return Results.Ok(new DriverResourceCollection(routeParameters.ChampionshipId, drivers.Select(d => new DriverResource(d, d.Version))));
    }

    public static async Task<IResult> FindDriverById(
        [AsParameters] DriverRouteParameters routeParameters,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        var driver = await objectStore.Drivers.FindAsync([routeParameters.DriverIdSpecification()], cancellationToken)
            ?? throw new InvalidDriverException(routeParameters);

        return Results.Ok(new DriverResource(driver, driver.Version));
    }

    public static async Task<IResult> UpdateDriverById(
        [AsParameters] DriverRouteParameters routeParameters,
        [FromBody] DriverChangeBody change,
        [FromHeader(Name = "If-Match")] VersionMatchSpecification versionMatch,
        [FromServices] IObjectStore objectStore,
        CancellationToken cancellationToken = default)
    {
        if (!await objectStore.Championships.ExistsAsync([routeParameters.ChampionshipIdSpecification()], cancellationToken))
        {
            throw new InvalidChampionshipException(routeParameters);
        }

        using var transaction = await objectStore.BeginTransactionAsync(cancellationToken);
        var driver = await transaction.Drivers.FindAsync([routeParameters.DriverIdSpecification()], cancellationToken)
            ?? throw new InvalidDriverException(routeParameters);

        change.Apply(driver.Object);
        if (await transaction.Drivers.UpdateAsync([routeParameters.DriverIdSpecification(), versionMatch], driver.Object, cancellationToken) == 0)
        {
            throw new OptimisticConcurrencyException();
        }

        await transaction.CommitAsync(cancellationToken);
        var updatedObject = await objectStore.Drivers.FindAsync([routeParameters.DriverIdSpecification()], cancellationToken)
            ?? throw new InvalidDriverException(routeParameters);
        return Results.Ok(new DriverResource(updatedObject, updatedObject.Version));
    }
}

