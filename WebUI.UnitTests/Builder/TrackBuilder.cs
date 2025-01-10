using WebUI.Domain;
using WebUI.Types;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Builder;

internal class TrackBuilder : EntityBuilder<Track, TrackBuilder>
{
    public TrackBuilder WithChampionshipId(ChampionshipId championshipId) => With(t => t.ChampionshipId, championshipId);
    public TrackBuilder WithChampionshipId(long championshipId) => WithChampionshipId(new(championshipId));

    public TrackBuilder WithRandomTrackId() => WithTrackId(IdGeneratorHelper.GenerateId());
    public TrackBuilder WithTrackId(TrackId trackId) => With(c => c.TrackId, trackId);
    public TrackBuilder WithTrackId(long trackId) => WithTrackId(new(trackId));

    public TrackBuilder WithName(string name) => With(c => c.Name, name);

    public override TrackBuilder ThatIsValid() => WithTrackId(1).WithChampionshipId(1).WithName("Track 1");
}
