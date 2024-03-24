using WebUI.Domain;
using WebUI.Types;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Builder;

internal class TrackBuilder : EntityBuilder<Track>
{
    protected override Track CreateInstance()
        => new(Get(c => c.ChampionshipId, new(IdGeneratorHelper.GenerateId())), Get(c => c.TrackId, new(IdGeneratorHelper.GenerateId())));

    public TrackBuilder WithName(string name)
    {
        With(c => c.Name, name);
        return this;
    }

    public TrackBuilder WithChampionshipId(ChampionshipId championshipId)
    {
        With(t => t.ChampionshipId, championshipId);
        return this;
    }

    public TrackBuilder WithChampionshipId() => WithChampionshipId(IdGeneratorHelper.GenerateId());

    public TrackBuilder WithChampionshipId(long championshipId) => WithChampionshipId(new(championshipId));

    public TrackBuilder WithId(TrackId trackId)
    {
        With(c => c.TrackId, trackId);
        return this;
    }

    public TrackBuilder WithId() => WithId(IdGeneratorHelper.GenerateId());

    public TrackBuilder WithId(long trackId) => WithId(new(trackId));

    public override TrackBuilder ThatIsValid() => WithId().WithChampionshipId().WithName("Formula 1");
}
