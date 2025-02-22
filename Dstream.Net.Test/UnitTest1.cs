using DStream.Net.Config;
namespace Dstream.Net.Test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var configLoader = new ConfigLoader("dstream.yaml");
        var appConfig = configLoader.LoadConfig();

        Console.WriteLine($"DbType: {appConfig.DbType}");
    }
}
