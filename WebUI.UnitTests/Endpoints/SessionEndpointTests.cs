using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using WebUI.Domain;
using WebUI.Endpoints;
using WebUI.Features;
using WebUI.Types;
using WebUI.UnitTests.Builder;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Endpoints;
public class SessionEndpointTests
{
    readonly ChampionshipId _championshipId = new(IdGeneratorHelper.GenerateId());
    readonly EventId _eventId = new(IdGeneratorHelper.GenerateId());
    readonly SessionId _sessionId = new(IdGeneratorHelper.GenerateId());

    static SessionChangeBody SomeSessionChange => new() { Name = new("SessionEndpointTests"), LapCount = 3 };
    readonly string versionEtag = EndpointTestsHelpers.WrapEtag(FakeObjectStore.DefaultObjectVersion);

    [Fact]
    public async Task CreateSession_Returns_CreatedAtRouteResult()
    {
        // Arrange
        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid()],
            events: [Some.Event.ThatIsValid()]
        );
        var sessionChange = SomeSessionChange;

        // Act
        var result = await SessionEndpoints.CreateSession(_championshipId, _eventId, sessionChange, objectStore, IdGeneratorHelper.GenerateId);

        // Assert
        var createdAtRouteResult = result.Should().BeOfType<CreatedAtRoute<SessionResource>>().Subject;
        createdAtRouteResult.RouteName.Should().Be(nameof(SessionEndpoints.FindSessionById));
    }

    [Fact]
    public async Task ListSessions_Returns_OkResult_With_SessionResourceCollection()
    {
        // Arrange
        Session[] sessions = {
            Some.Session.ThatIsValid(),
            Some.Session.ThatIsValid()
        };

        var objectStore = new FakeObjectStore(
            events: [Some.Event.ThatIsValid().WithChampionshipId(_championshipId).WithEventId(_eventId)],
            sessions: sessions);

        // Act
        var result = await SessionEndpoints.ListSessions(_championshipId, _eventId, objectStore);

        // Assert
        var resourceCollection = result.Should().BeOfType<Ok<SessionResourceCollection>>()
            .Which.Value.Should().BeOfType<SessionResourceCollection>().Subject;
        resourceCollection.Items.Should().HaveCount(sessions.Length);
    }

    [Fact]
    public async Task FindSessionById_Returns_OkResult_With_SessionResource()
    {
        // Arrange
        var session = Some.Session.ThatIsValid().WithSessionId(_sessionId);
        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid()],
            sessions: [session]);

        // Act
        var result = await SessionEndpoints.FindSessionById(_championshipId, _eventId, _sessionId, objectStore);

        // Assert
        var sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;
        sessionResource.SessionId.Should().Be(_sessionId);
    }
    [Fact]
    public async Task UpdateSessionById_Returns_OkResult_With_SessionResource()
    {
        // Arrange
        var session = Some.Session.ThatIsValid().WithSessionId(_sessionId);
        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid()],
            sessions: [session]);

        // Act
        var result = await SessionEndpoints.UpdateSessionById(_championshipId, _eventId, _sessionId, SomeSessionChange, versionEtag, objectStore);

        // Assert
        var sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;
        sessionResource.SessionId.Should().Be(_sessionId);
        sessionResource.Name.Should().Be(SomeSessionChange.Name);
    }

    [Fact]
    public async Task StartSessionById_Returns_OkResult_With_SessionResource()
    {
        // Arrange
        var session = Some.Session.ThatIsValid().WithSessionId(_sessionId);
        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid().WithFeature(new FlatDriverSkillFeature(true))],
            drivers: [
                Some.Driver.ThatIsValid().WithData(new FlatDriverSkillDriverData(5)),
                Some.Driver.ThatIsValid().WithData(new FlatDriverSkillDriverData(7))
            ],
            sessions: [session]);

        // Act
        var result = await SessionEndpoints.UpdateSessionStateById(_championshipId, _eventId, _sessionId, new SessionStateChangeBody
        {
            State = State.Running
        }, versionEtag, objectStore);

        // Assert
        var sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;
        sessionResource.SessionId.Should().Be(_sessionId);
        sessionResource.State.Should().Be(State.Running);
        sessionResource.Participants.Should().HaveCount(2);
    }

    [Fact]
    public async Task FinishSessionById_Returns_OkResult_With_SessionResource()
    {
        // Arrange
        var session = Some.Session.ThatIsValid().WithSessionId(_sessionId).ThatHasStarted(
            features: [new FlatDriverSkillFeature(true)],
            participants: [
                new SessionParticipant(new(1), 1, new([new FlatDriverSkillDriverData(5)])),
                new SessionParticipant(new(2), 2, new([new FlatDriverSkillDriverData(3)]))
            ]).ThatHasProgressedToTheEnd();

        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid()],
            sessions: [session]);

        // Act
        var result = await SessionEndpoints.UpdateSessionStateById(_championshipId, _eventId, _sessionId, new SessionStateChangeBody
        {
            State = State.Finished
        }, versionEtag, objectStore);

        // Assert
        var sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;
        sessionResource.SessionId.Should().Be(_sessionId);
        sessionResource.State.Should().Be(State.Finished);
    }

    [Fact]
    public async Task ProgressSessionById_Returns_OkResult_With_SessionResource()
    {
        // Arrange
        var session = Some.Session.ThatIsValid().WithSessionId(_sessionId).ThatHasStarted(
            features: [new FlatDriverSkillFeature(true)],
            participants: [
                new SessionParticipant(new(1), 1, new([new FlatDriverSkillDriverData(5)])), 
                new SessionParticipant(new(2), 2, new([new FlatDriverSkillDriverData(3)]))
            ]);

        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid()],
            sessions: [session]);

        // Act
        var result = await SessionEndpoints.UpdateSessionProgressById(_championshipId, _eventId, _sessionId, new SessionProgressChangeBody
        {
            ElapsedLaps = 1
        }, versionEtag, objectStore);

        // Assert
        var sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;
        sessionResource.SessionId.Should().Be(_sessionId);
        sessionResource.State.Should().Be(State.Running);
        sessionResource.Participants.Should().HaveCount(2);
    }

    [Fact]
    public async Task CanRunAFullSession()
    {
        // Arrange
        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid().WithFeature(new FlatDriverSkillFeature(true))],
            drivers: [
                Some.Driver.ThatIsValid().WithData(new FlatDriverSkillDriverData(5)),
                Some.Driver.ThatIsValid().WithData(new FlatDriverSkillDriverData(7))
            ],
            events: [Some.Event.ThatIsValid()]
        );

        var sessionChange = SomeSessionChange;

        // Act
        var result = await SessionEndpoints.CreateSession(_championshipId, _eventId, sessionChange, objectStore, IdGeneratorHelper.GenerateId);
        var sessionResource = result.Should().BeOfType<CreatedAtRoute<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;

        // Act
        result = await SessionEndpoints.UpdateSessionStateById(_championshipId, _eventId, sessionResource.SessionId, new SessionStateChangeBody
        {
            State = State.Running
        }, sessionResource.Version, objectStore);

        sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;

        // Act
        result = await SessionEndpoints.UpdateSessionProgressById(_championshipId, _eventId, sessionResource.SessionId, new SessionProgressChangeBody
        {
            ElapsedLaps = sessionResource.LapCount
        }, sessionResource.Version, objectStore);

        sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;

        // Act
        result = await SessionEndpoints.UpdateSessionStateById(_championshipId, _eventId, sessionResource.SessionId, new SessionStateChangeBody
        {
            State = State.Finished
        }, sessionResource.Version, objectStore);

        // Assert
        sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;

        sessionResource.State.Should().Be(State.Finished);
        sessionResource.Participants.Should()
            .AllSatisfy(p => p.Result!.TotalTime.Should().Be(TimeSpan.FromMilliseconds(sessionResource.LapResults.Values.Select(lr => lr.Results[p.DriverId].LapTime).Sum(ts => ts.TotalMilliseconds))));
        sessionResource.Participants.OrderBy(p => p.Result!.Position).Should().BeInAscendingOrder(p => p.Result!.TotalTime);
    }
}
