namespace Hoi4BlueprintBuilder.UnitTests;

public static class TestHelper
{
    public static string CreateUniqueTempDirectory()
    {
        var tempDir = Path.Combine(TestApp.TempDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}
