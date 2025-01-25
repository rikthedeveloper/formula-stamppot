using Microsoft.AspNetCore.Mvc;
using WebUI.Endpoints.Internal.Specifications;
using WebUI.Types;
using EventId = WebUI.Types.EventId;

namespace WebUI.Endpoints.Internal;

public record class ChampionshipRouteParameters([FromRoute] ChampionshipId ChampionshipId)
{
    public ChampionshipIdSpecification ChampionshipIdSpecification() => new(ChampionshipId);
    public EventIdSpecification EventIdSpecification(EventId eventId) => new(ChampionshipId, eventId);
    public DriverIdSpecification DriverIdSpecification(DriverId driverId) => new(ChampionshipId, driverId);
    public TrackIdSpecification TrackIdSpecification(TrackId trackId) => new(ChampionshipId, trackId);

    public object ToRouteValues() => new { ChampionshipId = ChampionshipId.ToBase36String() };
    public object ToRouteValues(EventId eventId) => new { ChampionshipId = ChampionshipId.ToBase36String(), EventId = eventId.ToBase36String() };
    public object ToRouteValues(DriverId driverId) => new { ChampionshipId = ChampionshipId.ToBase36String(), DriverId = driverId.ToBase36String() };
    public object ToRouteValues(TrackId trackId) => new { ChampionshipId = ChampionshipId.ToBase36String(), TrackId = trackId.ToBase36String() };
}

public record class EventRouteParameters([FromRoute] ChampionshipId ChampionshipId, [FromRoute] EventId EventId) : ChampionshipRouteParameters(ChampionshipId)
{
    public EventIdSpecification EventIdSpecification() => new(ChampionshipId, EventId);
    public SessionIdSpecification SessionIdSpecification(SessionId sessionId) => new(ChampionshipId, EventId, sessionId);

    public new object ToRouteValues() => new { ChampionshipId = ChampionshipId.ToBase36String(), EventId = EventId.ToBase36String() };
    public object ToRouteValues(SessionId sessionId) => new { ChampionshipId = ChampionshipId.ToBase36String(), EventId = EventId.ToBase36String(), SessionId = sessionId.ToBase36String() };
}

public record class SessionRouteParameters([FromRoute] ChampionshipId ChampionshipId, [FromRoute] EventId EventId, [FromRoute] SessionId SessionId) : EventRouteParameters(ChampionshipId, EventId)
{
    public SessionIdSpecification SessionIdSpecification() => new(ChampionshipId, EventId, SessionId);

    public new object ToRouteValues() => new { ChampionshipId = ChampionshipId.ToBase36String(), EventId = EventId.ToBase36String(), SessionId = SessionId.ToBase36String() };
}

public record class TrackRouteParameters([FromRoute] ChampionshipId ChampionshipId, [FromRoute] TrackId TrackId) : ChampionshipRouteParameters(ChampionshipId)
{
    public TrackIdSpecification TrackIdSpecification() => new(ChampionshipId, TrackId);

    public new object ToRouteValues() => new { ChampionshipId = ChampionshipId.ToBase36String(), TrackId = TrackId.ToBase36String() };
}

public record class DriverRouteParameters([FromRoute] ChampionshipId ChampionshipId, [FromRoute] DriverId DriverId) : ChampionshipRouteParameters(ChampionshipId)
{
    public DriverIdSpecification DriverIdSpecification() => new(ChampionshipId, DriverId);

    public new object ToRouteValues() => new { ChampionshipId = ChampionshipId.ToBase36String(), DriverId = DriverId.ToBase36String() };
}

public record class TeamRouteParameters([FromRoute] ChampionshipId ChampionshipId, [FromRoute] TeamId TeamId) : ChampionshipRouteParameters(ChampionshipId)
{
    // public TeamIdSpecification TeamIdSpecification => new(ChampionshipId, TeamId);

    public new object ToRouteValues() => new { ChampionshipId = ChampionshipId.ToBase36String(), TeamId = TeamId.ToBase36String() };
}
