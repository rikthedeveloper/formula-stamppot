using System.Collections.Immutable;
using WebUI.Types;

namespace WebUI.Domain;

public class Driver(ChampionshipId championshipId, DriverId driverId)
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public DriverId DriverId { get; } = driverId;

    public ImmutableArray<NameToken> Name { get; set; } = [];
    public string Abbreviation { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;

    public FeatureDataCollection<IFeatureDriverData> Data { get; set; } = new();
}
