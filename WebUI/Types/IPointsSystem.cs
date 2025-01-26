using WebUI.Domain;

namespace WebUI.Types;

public interface IPointsSystem
{
    public ushort GetPoints(ParticipantLapResult lastLapResult, IReadOnlyDictionary<ushort, LapResult> allLapResults);
}
