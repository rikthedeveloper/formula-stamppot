using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;
using WebUI.Endpoints;
using WebUI.Endpoints.Resources;
using WebUI.Types;
using WebUI.UnitTests.Builder;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Endpoints;
public class TrackEndpointsTests
{
    readonly ChampionshipId championshipId = new (IdGeneratorHelper.GenerateId());
    readonly TrackId trackId = new (IdGeneratorHelper.GenerateId());

    static TrackChangeBody SomeTrackChange => new TrackChangeBody() { Name = "Updated Test Track", City = "Test City", Country = "Netherlands", Length = Distance.FromKilometers(5) };
    readonly string versionEtag = EndpointTestsHelpers.WrapEtag(FakeObjectStore.DefaultObjectVersion);

    [Fact]
    public async Task CreateTrack_Returns_CreatedAtRouteResult()
    {
        // Arrange
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()]);
        var trackChange = SomeTrackChange;

        // Act
        var result = await TrackEndpoints.CreateTrack(championshipId, trackChange, objectStore, IdGeneratorHelper.GenerateId);

        // Assert
        var createdAtRouteResult = result.Should().BeOfType<CreatedAtRoute<TrackResource>>().Subject;
        createdAtRouteResult.RouteName.Should().Be(nameof(TrackEndpoints.FindTrackById));
    }

    [Fact]
    public async Task ListTracks_Returns_OkResult_With_TrackResourceCollection()
    {
        // Arrange
        Track[] tracks = [
            Some.Track.ThatIsValid(),
            Some.Track.ThatIsValid()
        ];
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()], tracks);

        // Act
        var result = await TrackEndpoints.ListTracks(championshipId, objectStore);

        // Assert
        var resourceCollection = result.Should().BeOfType<Ok<TrackResourceCollection>>()
            .Which.Value.Should().BeOfType<TrackResourceCollection>().Subject;
        resourceCollection.Items.Count.Should().Be(tracks.Length);
    }

    [Fact]
    public async Task FindTrackById_Returns_OkResult_With_TrackResource()
    {
        // Arrange
        var track = Some.Track.ThatIsValid().WithId(trackId);
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()], [track]);

        // Act
        var result = await TrackEndpoints.FindTrackById(championshipId, trackId, objectStore);

        // Assert
        var trackResource = result.Should().BeOfType<Ok<TrackResource>>()
            .Which.Value.Should().BeOfType<TrackResource>().Subject;
        trackResource.TrackId.Should().Be(trackId);
    }

    [Fact]
    public async Task UpdateTrackById_Returns_OkResult_With_TrackResource()
    {
        // Arrange
        var track = Some.Track.ThatIsValid().WithId(trackId);
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()], [track]);
        var trackChange = SomeTrackChange;

        // Act
        var result = await TrackEndpoints.UpdateTrackById(championshipId, trackId, trackChange, versionEtag, objectStore);

        // Assert
        var trackResource = result.Should().BeOfType<Ok<TrackResource>>()
            .Which.Value.Should().BeOfType<TrackResource>().Subject;
        trackResource.TrackId.Should().Be(trackId);
    }

    [Fact]
    public async Task CreateTrack_Throws_InvalidChampionshipException_When_ChampionshipDoesNotExist()
    {
        // Arrange
        var objectStore = new FakeObjectStore([]);

        // Act & Assert
        var createFunc = () => TrackEndpoints.CreateTrack(championshipId, SomeTrackChange, objectStore, IdGeneratorHelper.GenerateId);
        await createFunc.Should().ThrowExactlyAsync<InvalidChampionshipException>();
    }

    [Fact]
    public async Task ListTracks_Throws_InvalidChampionshipException_When_ChampionshipDoesNotExist()
    {
        // Arrange
        var championshipId = new ChampionshipId(IdGeneratorHelper.GenerateId());
        var objectStore = new FakeObjectStore(tracks: [Some.Track.ThatIsValid()]);

        // Act & Assert
        var listFunc = () => TrackEndpoints.ListTracks(championshipId, objectStore);
        await listFunc.Should().ThrowExactlyAsync<InvalidChampionshipException>();
    }

    [Fact]
    public async Task FindTrackById_Throws_InvalidChampionshipException_When_ChampionshipDoesNotExist()
    {
        // Arrange
        var championshipId = new ChampionshipId(IdGeneratorHelper.GenerateId());
        var objectStore = new FakeObjectStore(tracks: [Some.Track.ThatIsValid()]);

        // Act & Assert
        var findFunc = () => TrackEndpoints.FindTrackById(championshipId, trackId, objectStore);
        await findFunc.Should().ThrowExactlyAsync<InvalidChampionshipException>();
    }

    [Fact]
    public async Task UpdateTrackById_Throws_InvalidChampionshipException_When_ChampionshipDoesNotExist()
    {
        // Arrange
        var objectStore = new FakeObjectStore(tracks: [Some.Track.ThatIsValid()]);

        // Act & Assert
        var updateFunc = () => TrackEndpoints.UpdateTrackById(championshipId, trackId, SomeTrackChange, versionEtag, objectStore);
        await updateFunc.Should().ThrowExactlyAsync<InvalidChampionshipException>();
    }

    [Fact]
    public async Task UpdateTrackById_Throws_InvalidTrackException_When_TrackDoesNotExist()
    {
        // Arrange
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()]);

        // Act & Assert
        var updateFunc = () => TrackEndpoints.UpdateTrackById(championshipId, trackId, SomeTrackChange, versionEtag, objectStore);
        await updateFunc.Should().ThrowExactlyAsync<InvalidTrackException>();
    }

    [Fact]
    public async Task UpdateTrackById_Throws_OptimisticConcurrencyException_When_UpdateAsync_Returns_Zero()
    {
        // Arrange
        var objectStore = new FakeObjectStore([Some.Championship.ThatIsValid()], [Some.Track.ThatIsValid()]);

        // Act & Assert
        var updateFunc = () => TrackEndpoints.UpdateTrackById(championshipId, trackId, SomeTrackChange, "invalid-version", objectStore);
        await updateFunc.Should().ThrowExactlyAsync<OptimisticConcurrencyException>();
    }
}
