namespace Binkus.Ioc.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        #if USE_MEDI
        var r = DependencyInjection.Ioc.TestMedi();
        Assert.Equal(1, r);
        #endif
    }
}