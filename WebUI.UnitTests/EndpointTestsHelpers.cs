namespace WebUI.UnitTests;

internal static class EndpointTestsHelpers
{

    public static string WrapEtag(string s)
        => $"\"{s}\"";
}