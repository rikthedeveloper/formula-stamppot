namespace WebUI.UnitTests.Fakes;

public static class IdGeneratorHelper
{
    private static long _currentId = 0;

    public static long GenerateId()
    {
        return ++_currentId;
    }
}