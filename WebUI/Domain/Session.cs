using WebUI.Types;

namespace WebUI.Domain;

public class Session(ChampionshipId championshipId, Types.EventId eventId, SessionId sessionId)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public Types.EventId EventId { get; } = eventId;
    public SessionId SessionId { get; } = sessionId;
}
