using WebUI.Domain;
using WebUI.Types;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Builder;

internal class DriverBuilder : EntityBuilder<Driver>
{
    public DriverBuilder WithName(NameToken[] name)
    {
        With(c => c.Name, [.. name]);
        return this;
    }

    public DriverBuilder WithChampionshipId() => WithChampionshipId(IdGeneratorHelper.GenerateId());
    public DriverBuilder WithChampionshipId(long championshipId) => WithChampionshipId(new(championshipId));
    public DriverBuilder WithChampionshipId(ChampionshipId championshipId)
    {
        With(t => t.ChampionshipId, championshipId);
        return this;
    }


    public DriverBuilder WithDriverId() => WithDriverId(IdGeneratorHelper.GenerateId());
    public DriverBuilder WithDriverId(long trackId) => WithDriverId(new(trackId));
    public DriverBuilder WithDriverId(DriverId driverId)
    {
        With(c => c.DriverId, driverId);
        return this;
    }

    public DriverBuilder WithData(IFeatureDriverData data)
    {
        With(c => c.Data, new(Get(c => c.Data, new()).Append(data)));
        return this;
    }

    public override DriverBuilder ThatIsValid() => WithDriverId().WithChampionshipId().WithName([new("A"), new("Driver", true)]);
}
