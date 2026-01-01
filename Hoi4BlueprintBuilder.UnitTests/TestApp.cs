namespace Hoi4BlueprintBuilder.UnitTests;

public static class TestApp
{
    public static readonly string TestDataDirectory = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "TestData"
    );

    public static readonly string TempDirectory = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "Temp"
    );
}
