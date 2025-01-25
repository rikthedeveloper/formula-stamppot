using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;
using WebUI.Endpoints;
using WebUI.Endpoints.Internal.Specifications;
using WebUI.Endpoints.Resources;
using WebUI.Types;
using WebUI.UnitTests.Builder;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Endpoints;
public class DriverEndpointsTests
{
    static DriverChangeBody SomeDriverChange => new() { Name = [new("Max"), new("Verstappen", true)] };
    readonly VersionMatchSpecification versionSpec = new(FakeObjectStore.DefaultObjectVersion);

    [Fact]
    public async Task CreateDriver_Returns_CreatedAtRouteResult()
    {
        // Arrange
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()]);
        var driverChange = SomeDriverChange;

        // Act
        var result = await DriverEndpoints.CreateDriver(new(new(1)), driverChange, objectStore, IdGeneratorHelper.GenerateId);

        // Assert
        var createdAtRouteResult = result.Should().BeOfType<CreatedAtRoute<DriverResource>>().Subject;
        createdAtRouteResult.RouteName.Should().Be(nameof(DriverEndpoints.FindDriverById));
    }

    [Fact]
    public async Task ListDrivers_Returns_OkResult_With_DriverResourceCollection()
    {
        // Arrange
        Driver[] drivers = [
            Some.Driver.ThatIsValid(),
            Some.Driver.ThatIsValid()
        ];
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()], [], drivers);

        // Act
        var result = await DriverEndpoints.ListDrivers(new(new(1)), objectStore);

        // Assert
        var resourceCollection = result.Should().BeOfType<Ok<DriverResourceCollection>>()
            .Which.Value.Should().BeOfType<DriverResourceCollection>().Subject;
        resourceCollection.Items.Should().HaveCount(drivers.Length);
    }

    [Fact]
    public async Task FindDriverById_Returns_OkResult_With_DriverResource()
    {
        // Arrange
        var driver = Some.Driver.ThatIsValid();
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()], [], [driver]);

        // Act
        var result = await DriverEndpoints.FindDriverById(new(new(1), new(1)), objectStore);

        // Assert
        var driverResource = result.Should().BeOfType<Ok<DriverResource>>()
            .Which.Value.Should().BeOfType<DriverResource>().Subject;
        driverResource.DriverId.Should().BeEquivalentTo(new DriverId(1));
    }

    [Fact]
    public async Task UpdateDriverById_Returns_OkResult_With_DriverResource()
    {
        // Arrange
        var driver = Some.Driver.ThatIsValid().WithDriverId(1);
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()], [], [driver]);
        var driverChange = SomeDriverChange;

        // Act
        var result = await DriverEndpoints.UpdateDriverById(new(new(1), new(1)), driverChange, versionSpec, objectStore);

        // Assert
        var driverResource = result.Should().BeOfType<Ok<DriverResource>>()
            .Which.Value.Should().BeOfType<DriverResource>().Subject;
        driverResource.DriverId.Should().BeEquivalentTo((new DriverId(1)));
    }

    [Fact]
    public async Task CreateDriver_Throws_InvalidChampionshipException_When_ChampionshipDoesNotExist()
    {
        // Arrange
        var objectStore = new FakeObjectStore([]);

        // Act & Assert
        var createFunc = () => DriverEndpoints.CreateDriver(new(new(1)), SomeDriverChange, objectStore, IdGeneratorHelper.GenerateId);
        await createFunc.Should().ThrowExactlyAsync<InvalidChampionshipException>();
    }

    [Fact]
    public async Task ListDrivers_Throws_InvalidChampionshipException_When_ChampionshipDoesNotExist()
    {
        // Arrange
        var championshipId = new ChampionshipId(IdGeneratorHelper.GenerateId());
        var objectStore = new FakeObjectStore(drivers: [Some.Driver.ThatIsValid()]);

        // Act & Assert
        var listFunc = () => DriverEndpoints.ListDrivers(new(championshipId), objectStore);
        await listFunc.Should().ThrowExactlyAsync<InvalidChampionshipException>();
    }

    [Fact]
    public async Task FindDriverById_Throws_InvalidChampionshipException_When_ChampionshipDoesNotExist()
    {
        // Arrange
        var objectStore = new FakeObjectStore(drivers: [Some.Driver.ThatIsValid()]);

        // Act & Assert
        var findFunc = () => DriverEndpoints.FindDriverById(new(new(1), new(1)), objectStore);
        await findFunc.Should().ThrowExactlyAsync<InvalidChampionshipException>();
    }

    [Fact]
    public async Task UpdateDriverById_Throws_InvalidChampionshipException_When_ChampionshipDoesNotExist()
    {
        // Arrange
        var objectStore = new FakeObjectStore(drivers: [Some.Driver.ThatIsValid()]);

        // Act & Assert
        var updateFunc = () => DriverEndpoints.UpdateDriverById(new(new(1), new(1)), SomeDriverChange, versionSpec, objectStore);
        await updateFunc.Should().ThrowExactlyAsync<InvalidChampionshipException>();
    }

    [Fact]
    public async Task UpdateDriverById_Throws_InvalidDriverException_When_DriverDoesNotExist()
    {
        // Arrange
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()]);

        // Act & Assert
        var updateFunc = () => DriverEndpoints.UpdateDriverById(new(new(1), new(1)), SomeDriverChange, versionSpec, objectStore);
        await updateFunc.Should().ThrowExactlyAsync<InvalidDriverException>();
    }

    [Fact]
    public async Task UpdateDriverById_Throws_OptimisticConcurrencyException_When_UpdateAsync_Returns_Zero()
    {
        // Arrange
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()], [], [Some.Driver.ThatIsValid()]);

        // Act & Assert
        var updateFunc = () => DriverEndpoints.UpdateDriverById(new(new(1), new(1)), SomeDriverChange, new("invalid-version"), objectStore);
        await updateFunc.Should().ThrowExactlyAsync<OptimisticConcurrencyException>();
    }
}
