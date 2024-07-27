using SqlKata;
using WebUI.Domain.ObjectStore;
using WebUI.Types;

namespace WebUI.Endpoints.Internal.Specifications;

public class DriverIdSpecification(ChampionshipId championshipId, DriverId driverId) : ISpecification
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public DriverId DriverId { get; } = driverId;
    public Query Apply(Query query) => query.Where(new { ChampionshipId = ChampionshipId.Value, DriverId = DriverId.Value });
}
