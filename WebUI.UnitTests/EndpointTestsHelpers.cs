using WebUI.Domain.ObjectStore;

namespace WebUI.UnitTests;

internal static class EndpointTestsHelpers
{
    public static string WrapEtag(string s)
        => $"\"{s}\"";

    public static string WrapEtag(ObjectVersion s)
        => WrapEtag(s.ToString());
}