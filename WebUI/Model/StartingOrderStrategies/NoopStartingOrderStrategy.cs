using System.Collections.Immutable;
using WebUI.Domain;
using WebUI.Types;

namespace WebUI.Model.StartingOrderStrategies;

public class NoopStartingOrderStrategy : IStartingOrderStrategy
{
    /// <summary>
    /// Orders participants by their original order in the provided list.
    /// </summary>
    public Task<IImmutableList<SessionParticipant>> GetOrderedParticipants(Session session, IEnumerable<Driver> participants, GetSessionDelegate getSession) 
        => Task.FromResult<IImmutableList<SessionParticipant>>(participants.Select((d, i) => new SessionParticipant(d.DriverId, (ushort)(i + 1), d.Data)).ToImmutableList());
}
