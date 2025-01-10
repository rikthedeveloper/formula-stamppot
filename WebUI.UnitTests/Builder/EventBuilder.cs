using System.Collections.Immutable;
using WebUI.Domain;
using WebUI.Types;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Builder;
internal class EventBuilder : EntityBuilder<Event, EventBuilder>
{
    public EventBuilder WithChampionshipId(ChampionshipId championshipId) => With(t => t.ChampionshipId, championshipId);
    public EventBuilder WithChampionshipId(long championshipId) => WithChampionshipId(new(championshipId));

    public EventBuilder WithRandomEventId() => WithEventId(IdGeneratorHelper.GenerateId());
    public EventBuilder WithEventId(EventId eventId) => With(c => c.EventId, eventId);
    public EventBuilder WithEventId(long trackId) => WithEventId(new(trackId));

    public EventBuilder AtTrack(TrackId trackId) => With(c => c.TrackId, trackId);
    public EventBuilder AtTrack(long trackId) => AtTrack(new(trackId));

    public EventBuilder WithSchedule(params SessionId[] sessionIds) => With(c => c.Schedule, sessionIds.ToImmutableArray());

    public override EventBuilder ThatIsValid() => WithChampionshipId(1).WithEventId(1).AtTrack(1);
}
