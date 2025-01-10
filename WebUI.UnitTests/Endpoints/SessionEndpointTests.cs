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
        var result = await SessionEndpoints.CreateSession(new(1), new(1), sessionChange, objectStore, IdGeneratorHelper.GenerateId);

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
            events: [Some.Event.ThatIsValid()],
            sessions: sessions);

        // Act
        var result = await SessionEndpoints.ListSessions(new(1), new(1), objectStore);

        // Assert
        var resourceCollection = result.Should().BeOfType<Ok<SessionResourceCollection>>()
            .Which.Value.Should().BeOfType<SessionResourceCollection>().Subject;
        resourceCollection.Items.Should().HaveCount(sessions.Length);
    }

    [Fact]
    public async Task FindSessionById_Returns_OkResult_With_SessionResource()
    {
        // Arrange
        var session = Some.Session.ThatIsValid();
        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid()],
            sessions: [session]);

        // Act
        var result = await SessionEndpoints.FindSessionById(new(1), new(1), new(1), objectStore);

        // Assert
        var sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;
        sessionResource.SessionId.Should().Be(new SessionId(1));
    }
    [Fact]
    public async Task UpdateSessionById_Returns_OkResult_With_SessionResource()
    {
        // Arrange
        var session = Some.Session.ThatIsValid();
        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid()],
            sessions: [session]);

        // Act
        var result = await SessionEndpoints.UpdateSessionById(new(1), new(1), new(1), SomeSessionChange, versionEtag, objectStore);

        // Assert
        var sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;
        sessionResource.SessionId.Should().Be(new SessionId(1));
        sessionResource.Name.Should().Be(SomeSessionChange.Name);
    }

    [Fact]
    public async Task StartSessionById_Returns_OkResult_With_SessionResource()
    {
        // Arrange
        var session = Some.Session.ThatIsValid();
        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid().WithFeature(new FlatDriverSkillFeature(true))],
            drivers: [
                Some.Driver.ThatIsValid().WithData(new FlatDriverSkillDriverData(5)),
                Some.Driver.ThatIsValid().WithData(new FlatDriverSkillDriverData(7))
            ],
            events: [Some.Event.ThatIsValid()],
            sessions: [session]);

        // Act
        var result = await SessionEndpoints.UpdateSessionStateById(new(1), new(1), new(1), new SessionStateChangeBody
        {
            State = State.Running
        }, versionEtag, objectStore);

        // Assert
        var sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;
        sessionResource.SessionId.Should().Be(new SessionId(1));
        sessionResource.State.Should().Be(State.Running);
        sessionResource.Participants.Should().HaveCount(2);
    }

    [Fact]
    public async Task FinishSessionById_Returns_OkResult_With_SessionResource()
    {
        // Arrange
        var session = Some.Session.ThatIsValid().ThatHasStarted(
            features: [new FlatDriverSkillFeature(true)],
            participants: [
                new SessionParticipant(new(1), 1, new([new FlatDriverSkillDriverData(5)])),
                new SessionParticipant(new(2), 2, new([new FlatDriverSkillDriverData(3)]))
            ]).ThatHasProgressedToTheEnd();

        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid()],
            events: [Some.Event.ThatIsValid()],
            sessions: [session]);

        // Act
        var result = await SessionEndpoints.UpdateSessionStateById(new(1), new(1), new(1), new SessionStateChangeBody
        {
            State = State.Finished
        }, versionEtag, objectStore);

        // Assert
        var sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;
        sessionResource.SessionId.Should().Be(new SessionId(1));
        sessionResource.State.Should().Be(State.Finished);
    }

    [Fact]
    public async Task FinishSessionById_SetsSessionFinished_OnNextSession()
    {
        // Arrange
        var session1 = Some.Session.ThatIsValid().ThatHasStarted(
            features: [new FlatDriverSkillFeature(true)],
            participants: [
                new SessionParticipant(new(1), 1, new([new FlatDriverSkillDriverData(5)])),
                new SessionParticipant(new(2), 2, new([new FlatDriverSkillDriverData(3)]))
            ]).ThatHasProgressedToTheEnd().Build();

        var session2 = Some.Session.ThatIsValid().WithSessionId(2).Build();

        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid().WithChampionshipId(1)],
            events: [Some.Event.ThatIsValid().WithSchedule(session1.SessionId, session2.SessionId)],
            sessions: [session1, session2]);

        // Act
        await SessionEndpoints.UpdateSessionStateById(new(1), new(1), new(1), new SessionStateChangeBody
        {
            State = State.Finished
        }, versionEtag, objectStore);

        // Assert
        session2.PreviousSessionHasFinished.Should().BeTrue();
    }

    [Fact]
    public async Task ProgressSessionById_Returns_OkResult_With_SessionResource()
    {
        // Arrange
        var session = Some.Session.ThatIsValid().ThatHasStarted(
            features: [new FlatDriverSkillFeature(true)],
            participants: [
                new SessionParticipant(new(1), 1, new([new FlatDriverSkillDriverData(5)])), 
                new SessionParticipant(new(2), 2, new([new FlatDriverSkillDriverData(3)]))
            ]);

        var objectStore = new FakeObjectStore(
            championships: [Some.Championship.ThatIsValid()],
            sessions: [session]);

        // Act
        var result = await SessionEndpoints.UpdateSessionProgressById(new(1), new(1), new(1), new SessionProgressChangeBody
        {
            ElapsedLaps = 1
        }, versionEtag, objectStore);

        // Assert
        var sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;
        sessionResource.SessionId.Should().Be(new SessionId(1));
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
                Some.Driver.ThatIsValid().WithDriverId(1).WithData(new FlatDriverSkillDriverData(5)),
                Some.Driver.ThatIsValid().WithDriverId(2).WithData(new FlatDriverSkillDriverData(7))
            ],
            events: [Some.Event.ThatIsValid()]
        );

        var sessionChange = SomeSessionChange;

        // Act
        var result = await SessionEndpoints.CreateSession(new(1), new(1), sessionChange, objectStore, IdGeneratorHelper.GenerateId);
        var sessionResource = result.Should().BeOfType<CreatedAtRoute<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;

        result = await SessionEndpoints.UpdateSessionStateById(new(1),new(1), sessionResource.SessionId, new SessionStateChangeBody
        {
            State = State.Running
        }, sessionResource.Version, objectStore);

        sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;

        result = await SessionEndpoints.UpdateSessionProgressById(new(1), new(1), sessionResource.SessionId, new SessionProgressChangeBody
        {
            ElapsedLaps = sessionResource.LapCount
        }, sessionResource.Version, objectStore);

        sessionResource = result.Should().BeOfType<Ok<SessionResource>>()
            .Which.Value.Should().BeOfType<SessionResource>().Subject;

        result = await SessionEndpoints.UpdateSessionStateById(new(1), new(1), sessionResource.SessionId, new SessionStateChangeBody
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
