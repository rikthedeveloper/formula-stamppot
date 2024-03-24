using SqlKata;
using WebUI.Domain.ObjectStore;
using WebUI.Types;

namespace WebUI.Endpoints.Internal.Specifications;

public class TrackIdSpecification(ChampionshipId championshipId, TrackId trackId) : ISpecification
{
    public ChampionshipId ChampionshipId { get; } = championshipId;
    public TrackId TrackId { get; } = trackId;
    public Query Apply(Query query) => query.Where(new { ChampionshipId = ChampionshipId.Value, TrackId = TrackId.Value });
}
