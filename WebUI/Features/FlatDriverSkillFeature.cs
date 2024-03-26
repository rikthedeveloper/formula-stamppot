using WebUI.Types;

namespace WebUI.Features;

public record FlatDriverSkillDriverData(int Skill);
public record FlatDriverSkillFeature(bool Enabled) : FeatureBase(Enabled), IFeatureWithDriverData
{
    public static Type? DriverDataType { get; } = typeof(FlatDriverSkillDriverData);
}
