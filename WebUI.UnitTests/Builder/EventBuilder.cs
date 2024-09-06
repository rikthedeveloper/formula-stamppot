using WebUI.Domain;
using WebUI.Types;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Builder;
internal class EventBuilder : EntityBuilder<Event>
{
    protected override Event CreateInstance()
        => new(Get(c => c.ChampionshipId, new(IdGeneratorHelper.GenerateId())), Get(e => e.EventId, new(IdGeneratorHelper.GenerateId())));

    public EventBuilder AtTrack(TrackId trackId)
    {
        With(c => c.TrackId, trackId);
        return this;
    }

    public EventBuilder WithChampionshipId(ChampionshipId championshipId)
    {
        With(t => t.ChampionshipId, championshipId);
        return this;
    }

    public EventBuilder WithChampionshipId() => WithChampionshipId(IdGeneratorHelper.GenerateId());

    public EventBuilder WithChampionshipId(long championshipId) => WithChampionshipId(new(championshipId));

    public EventBuilder WithEventId(EventId eventId)
    {
        With(c => c.EventId, eventId);
        return this;
    }

    public EventBuilder WithId() => WithId(IdGeneratorHelper.GenerateId());

    public EventBuilder WithId(long trackId) => WithEventId(new(trackId));

    public override EventBuilder ThatIsValid() => WithId().WithChampionshipId().AtTrack(new(IdGeneratorHelper.GenerateId()));
}
