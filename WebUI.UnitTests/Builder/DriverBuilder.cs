using WebUI.Domain;
using WebUI.Types;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Builder;

internal class DriverBuilder : EntityBuilder<Driver>
{
    protected override Driver CreateInstance()
        => new(Get(c => c.ChampionshipId, new(IdGeneratorHelper.GenerateId())), Get(c => c.DriverId, new(IdGeneratorHelper.GenerateId())));

    public DriverBuilder WithName(NameToken[] name)
    {
        With(c => c.Name, [.. name]);
        return this;
    }

    public DriverBuilder WithChampionshipId(ChampionshipId championshipId)
    {
        With(t => t.ChampionshipId, championshipId);
        return this;
    }

    public DriverBuilder WithChampionshipId() => WithChampionshipId(IdGeneratorHelper.GenerateId());

    public DriverBuilder WithChampionshipId(long championshipId) => WithChampionshipId(new(championshipId));

    public DriverBuilder WithId(DriverId driverId)
    {
        With(c => c.DriverId, driverId);
        return this;
    }

    public DriverBuilder WithId() => WithId(IdGeneratorHelper.GenerateId());

    public DriverBuilder WithId(long trackId) => WithId(new(trackId));

    public override DriverBuilder ThatIsValid() => WithId().WithChampionshipId().WithName([new("Max"), new("Verstappen", true)]);
}
