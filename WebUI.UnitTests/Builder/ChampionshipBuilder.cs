using Ease;
using WebUI.Domain;
using WebUI.Types;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Builder;

internal class ChampionshipBuilder : EntityBuilder<Championship>
{
    protected override Championship CreateInstance() => 
        new(Get(c => c.ChampionshipId, new(IdGeneratorHelper.GenerateId())))
        {
            Name = Get(c => c.Name, string.Empty),
            Features = Get(c => c.Features, new())
        };

    public ChampionshipBuilder WithName(string name)
    {
        With(c => c.Name, name);
        return this;
    }

    public ChampionshipBuilder WithId(ChampionshipId championshipId)
    {
        With(c => c.ChampionshipId, championshipId);
        return this;
    }

    public ChampionshipBuilder WithId(long championshipId)
    {
        With(c => c.ChampionshipId, new(championshipId));
        return this;
    }

    public ChampionshipBuilder WithFeature(IFeature feature)
    {
        With(c => c.Features, new(Get(c => c.Features, new()).Append(feature)));
        return this;
    }

    public override ChampionshipBuilder ThatIsValid() => WithName("Formula 1");
}
