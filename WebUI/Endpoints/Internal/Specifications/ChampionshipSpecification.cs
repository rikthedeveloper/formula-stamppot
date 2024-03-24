using SqlKata;
using WebUI.Domain.ObjectStore;
using WebUI.Types;

namespace WebUI.Endpoints.Internal.Specifications;

public class ChampionshipIdSpecification(ChampionshipId championshipId) : ISpecification
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public Query Apply(Query query) => query.Where(new { ChampionshipId = ChampionshipId.Value });
}
