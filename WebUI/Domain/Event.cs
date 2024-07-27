using System.Collections.Immutable;
using WebUI.Types;

namespace WebUI.Domain;

public class Event(ChampionshipId championshipId, Types.EventId eventId)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public Types.EventId EventId { get; } = eventId;

    public TrackId TrackId { get; set; }

    public ImmutableArray<SessionId> Schedule { get; set; }
}
