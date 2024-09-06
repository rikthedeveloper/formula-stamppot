using SqlKata;
using WebUI.Domain.ObjectStore;
using WebUI.Types;
using EventId = WebUI.Types.EventId;

namespace WebUI.Endpoints.Internal.Specifications;

public class SessionIdSpecification(ChampionshipId championshipId, EventId eventId, SessionId sessionId) : ISpecification
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public EventId EventId { get; } = eventId;
    public SessionId SessionId { get; } = sessionId;
    public Query Apply(Query query) => query.Where(new { ChampionshipId = ChampionshipId.Value, EventId = EventId.Value, SessionId = SessionId.Value });
}
