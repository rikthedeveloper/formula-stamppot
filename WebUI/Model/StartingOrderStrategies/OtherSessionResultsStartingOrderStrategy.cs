using System.Collections.Immutable;
using WebUI.Domain;
using WebUI.Endpoints.Internal.Specifications;
using WebUI.Types;

namespace WebUI.Model.StartingOrderStrategies;

public class OtherSessionResultsStartingOrderStrategy(SessionId targetSessionId) : IStartingOrderStrategy
{
    public SessionId TargetSessionId { get; } = targetSessionId;

    /// <summary>
    /// Orders participants based on the results of a specified session.
    /// </summary>
    /// <exception cref="InvalidOperationException">When the target session does not exist in the same event or has not finished.</exception>
    public async Task<IImmutableList<SessionParticipant>> GetOrderedParticipants(Session session, IEnumerable<Driver> participants, GetSessionDelegate getSession)
    {
        var targetSession = await getSession(new SessionIdSpecification(session.ChampionshipId, session.EventId, TargetSessionId))
            ?? throw new InvalidOperationException($"Session with ID {TargetSessionId} not found.");

        if (targetSession.State != State.Finished)
            throw new InvalidOperationException($"Session with ID {TargetSessionId} is not finished.");

        var targetResults = targetSession.Participants.ToDictionary(p => p.DriverId, p => p.Result!.Position);
        return participants
            .Select(p => new SessionParticipant(p.DriverId, targetResults.GetValueOrDefault(p.DriverId, ushort.MaxValue), p.Data))
            .OrderBy(p => p.StartingPosition)
            .ToImmutableArray();
    }
}
