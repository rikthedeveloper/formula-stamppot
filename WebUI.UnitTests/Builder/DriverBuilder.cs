using WebUI.Domain;
using WebUI.Types;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Builder;

internal class DriverBuilder : EntityBuilder<Driver, DriverBuilder>
{

    public DriverBuilder WithChampionshipId(long championshipId) => WithChampionshipId(new(championshipId));
    public DriverBuilder WithChampionshipId(ChampionshipId championshipId) => With(t => t.ChampionshipId, championshipId);

    public DriverBuilder WithRandomDriverId() => WithDriverId(IdGeneratorHelper.GenerateId());
    public DriverBuilder WithDriverId(long trackId) => WithDriverId(new(trackId));
    public DriverBuilder WithDriverId(DriverId driverId) => With(c => c.DriverId, driverId);

    public DriverBuilder WithName(NameToken[] name) => With(c => c.Name, [.. name]);
    public DriverBuilder WithData(IFeatureDriverData data) => With(c => c.Data, new(Get(c => c.Data, new()).Append(data)));

    public override DriverBuilder ThatIsValid() => WithChampionshipId(1).WithDriverId(1).WithName([new("A"), new("Driver", true)]);
}
