using System.Collections.Immutable;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;

namespace WebUI.Types;

public delegate Task<Session?> GetSessionDelegate(ISpecification specification);
public interface IStartingOrderStrategy
{
    Task<IImmutableList<SessionParticipant>> GetOrderedParticipants(Session targetSession, IEnumerable<Driver> participants, GetSessionDelegate getSessionFromEvent);
}
