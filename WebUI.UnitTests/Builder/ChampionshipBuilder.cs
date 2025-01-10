using Ease;
using WebUI.Domain;
using WebUI.Types;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Builder;

internal class ChampionshipBuilder : EntityBuilder<Championship, ChampionshipBuilder>
{
    public ChampionshipBuilder WithChampionshipId(ChampionshipId championshipId) =>  With(c => c.ChampionshipId, championshipId);
    public ChampionshipBuilder WithChampionshipId(long championshipId) => With(c => c.ChampionshipId, new(championshipId));

    public ChampionshipBuilder WithName(string name) => With(c => c.Name, name);

    public ChampionshipBuilder WithFeature(IFeature feature) => With(c => c.Features, new(Get(c => c.Features, new()).Append(feature)));

    public override ChampionshipBuilder ThatIsValid() => WithChampionshipId(1).WithName("Formula 1").With(c => c.Features, new());
}
