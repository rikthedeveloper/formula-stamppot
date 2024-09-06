using SqlKata;
using WebUI.Domain.ObjectStore;
using WebUI.Types;
using EventId = WebUI.Types.EventId;

namespace WebUI.Endpoints.Internal.Specifications;

public class EventIdSpecification(ChampionshipId championshipId, EventId eventId) : ISpecification
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public EventId EventId { get; } = eventId;
    public Query Apply(Query query) => query.Where(new { ChampionshipId = ChampionshipId.Value, EventId = EventId.Value });
}
