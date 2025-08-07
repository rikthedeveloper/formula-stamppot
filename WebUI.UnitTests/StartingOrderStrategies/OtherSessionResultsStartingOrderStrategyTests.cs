using FluentAssertions;
using WebUI.Domain;
using WebUI.Model.StartingOrderStrategies;
using WebUI.UnitTests.Builder;

namespace WebUI.UnitTests.StartingOrderStrategies;
public class OtherSessionResultsStartingOrderStrategyTests
{
    [Fact]
    public async Task DriversAreOrderedCorrectly()
    {
        // Arrange
        var strategy = new OtherSessionResultsStartingOrderStrategy(new(1));
        var participants = new List<SessionParticipant>
        {
            new(new(1), 1, new()),
            new(new(2), 1, new()),
            new(new(3), 1, new())
        };

        var drivers = new List<Driver>
        {
            new(new(1), new(1)),
            new(new(1), new(2)),
            new(new(1), new(3)),
        };

        var previousSession = Some.Session.ThatIsValid().ThatHasStarted([], participants).ThatHasProgressedToTheEnd().Build();
        previousSession.Finish();

        var session = Some.Session.ThatIsValid();
        // Act
        var result = await strategy.GetOrderedParticipants(session, drivers, _ => Task.FromResult<Session?>(previousSession));

        // Assert
        result.Should().HaveCount(3);
    }
}
