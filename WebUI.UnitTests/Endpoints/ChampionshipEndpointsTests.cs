using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using WebUI.Domain;
using WebUI.Endpoints;
using WebUI.Endpoints.Resources;
using WebUI.Features;
using WebUI.Types;
using WebUI.UnitTests.Builder;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Endpoints;
public class ChampionshipEndpointsTests
{
    [Fact]
    public async Task CreateChampionship_Returns_CreatedAtRouteResult()
    {
        // Arrange
        var championshipChange = new ChampionshipChangeRequestBody { Name = "Test Championship" };
        var objectStore = new FakeObjectStore();

        // Act
        var result = await ChampionshipEndpoints.CreateChampionship(championshipChange, objectStore, IdGeneratorHelper.GenerateId);

        // Assert
        var createdAtRouteResult = result.Should().BeOfType<CreatedAtRoute<ChampionshipResource>>().Subject;
        createdAtRouteResult.RouteName.Should().Be(nameof(ChampionshipEndpoints.FindChampionshipById));
        createdAtRouteResult.RouteValues.Should().ContainSingle().And.ContainValue(createdAtRouteResult.Value?.ChampionshipId.ToBase36String());
    }

    [Fact]
    public async Task ListChampionships_Returns_OkResult_With_ResourceCollection()
    {
        // Arrange
        Championship[] championships = [
            Some.Championship.ThatIsValid(),
            Some.Championship.ThatIsValid()
        ];

        var objectStore = new FakeObjectStore(championships);

        // Act
        var result = await ChampionshipEndpoints.ListChampionships(objectStore);

        // Assert
        var okResult = result.Should().BeOfType<Ok<ResourceCollection<ChampionshipResource>>>().Subject;
        var resourceCollection = okResult.Value.Should().BeOfType<ResourceCollection<ChampionshipResource>>().Subject;
        resourceCollection.Amount.Should().Be(championships.Length);
        resourceCollection.Items.Count.Should().Be((int)resourceCollection.Amount);
    }

    [Fact]
    public async Task FindChampionshipById_Returns_OkResult_With_ChampionshipResource()
    {
        // Arrange
        var championship = Some.Championship.ThatIsValid()
            .WithFeature(new FlatDriverSkillFeature(true))
            .Build();

        var objectStore = new FakeObjectStore([championship]);

        // Act
        var result = await ChampionshipEndpoints.FindChampionshipById(championship.ChampionshipId, objectStore);

        // Assert
        var okResult = result.Should().BeOfType<Ok<ChampionshipResource>>().Subject;
        var championshipResource = okResult.Value.Should().BeOfType<ChampionshipResource>().Subject;
        championshipResource.ChampionshipId.Should().Be(championship.ChampionshipId);
        championshipResource.Name.Should().Be(championship.Name);
    }

    [Fact]
    public async Task UpdateChampionshipById_Returns_OkResult_With_ChampionshipResource()
    {
        // Arrange
        var championshipId = new ChampionshipId(IdGeneratorHelper.GenerateId());
        var championshipChange = new ChampionshipChangeRequestBody { Name = "Updated Test Championship" };
        var championship = Some.Championship.ThatIsValid().WithId(championshipId);
        var objectStore = new FakeObjectStore([championship]);

        // Act
        var result = await ChampionshipEndpoints.UpdateChampionshipById(championshipId, championshipChange, EndpointTestsHelpers.WrapEtag(FakeObjectStore.DefaultObjectVersion), objectStore);

        // Assert
        var okResult = result.Should().BeOfType<Ok<ChampionshipResource>>().Subject;
        var championshipResource = okResult.Value.Should().BeOfType<ChampionshipResource>().Subject;
        championshipResource.ChampionshipId.Should().Be(championshipId);
        championshipResource.Name.Should().Be(championshipChange.Name);
    }

    [Fact(Skip = "Not a fully finalized scenario, should probably throw a different exception")]
    public async Task CreateChampionship_Throws_InvalidChampionshipException_When_ObjectStore_Returns_Null()
    {
        // Arrange
        var championshipChange = new ChampionshipChangeRequestBody { Name = "Test Championship" };
        var objectStore = new FakeObjectStore();
        // Act & Assert
        var createFunc = () => ChampionshipEndpoints.CreateChampionship(championshipChange, objectStore, IdGeneratorHelper.GenerateId);
        await createFunc.Should().ThrowExactlyAsync<InvalidChampionshipException>();
    }

    [Fact]
    public async Task FindChampionshipById_Throws_InvalidChampionshipException_When_ObjectStore_Returns_Null()
    {
        // Arrange
        var championshipId = new ChampionshipId(IdGeneratorHelper.GenerateId());
        var objectStore = new FakeObjectStore();

        // Act & Assert
        var findFunc = () => ChampionshipEndpoints.FindChampionshipById(championshipId, objectStore);
        await findFunc.Should().ThrowExactlyAsync<InvalidChampionshipException>();
    }

    [Fact]
    public async Task UpdateChampionshipById_Throws_InvalidChampionshipException_When_ObjectStore_Returns_Null()
    {
        // Arrange
        var championshipId = new ChampionshipId(IdGeneratorHelper.GenerateId());
        var championshipChange = new ChampionshipChangeRequestBody { Name = "Updated Test Championship" };
        var objectStore = new FakeObjectStore();

        // Act & Assert
        var updateFunc = () => ChampionshipEndpoints.UpdateChampionshipById(championshipId, championshipChange, EndpointTestsHelpers.WrapEtag(FakeObjectStore.DefaultObjectVersion), objectStore);
        await updateFunc.Should().ThrowExactlyAsync<InvalidChampionshipException>();
    }

    [Fact]
    public async Task UpdateChampionshipById_Throws_OptimisticConcurrencyException_When_UpdateAsync_Returns_Zero()
    {
        // Arrange
        var championshipId = new ChampionshipId(IdGeneratorHelper.GenerateId());
        var championshipChange = new ChampionshipChangeRequestBody { Name = "Updated Test Championship" };
        var championship = Some.Championship.ThatIsValid().WithId(championshipId);
        var objectStore = new FakeObjectStore([championship]);

        // Act & Assert
        var updateFunc = () => ChampionshipEndpoints.UpdateChampionshipById(championshipId, championshipChange, EndpointTestsHelpers.WrapEtag("invalid-version"), objectStore);
        await updateFunc.Should().ThrowExactlyAsync<OptimisticConcurrencyException>();
    }
}