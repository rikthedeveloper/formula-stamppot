namespace WebUI.UnitTests.Builder;

internal static class Some
{
    public static ChampionshipBuilder Championship => new();
    public static TrackBuilder Track => new();
    public static DriverBuilder Driver => new();
}
