using WebUI.Domain;
using WebUI.Types;

namespace WebUI.Features;

public record FlatDriverSkillDriverData(int Skill) : IFeatureDriverData;
public record FlatDriverSkillFeature(bool Enabled) : FeatureBase(Enabled), IFeatureWithDriverData
{
    public static Type? DriverDataType { get; } = typeof(FlatDriverSkillDriverData);

    public override FeatureResult Apply(Session session, SessionParticipant participant, LapResult? previousLap, Random random) 
        => new(TimeSpan.FromSeconds(participant.Data.Get<FlatDriverSkillDriverData>()?.Skill ?? 0));
}
