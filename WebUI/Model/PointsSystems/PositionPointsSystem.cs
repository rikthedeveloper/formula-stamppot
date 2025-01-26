using System.Collections.Immutable;
using WebUI.Domain;
using WebUI.Types;

namespace WebUI.Model.PointsSystems;

public record class PositionPointsSystem(IImmutableList<ushort> PointsPerPosition) : IPointsSystem
{
    public ushort GetPoints(ParticipantLapResult lastLapResult, IReadOnlyDictionary<ushort, LapResult> allLapResults)
    {
        return PointsPerPosition.Count >= lastLapResult.Position ? PointsPerPosition[lastLapResult.Position-1] : (ushort)0;
    }
}
