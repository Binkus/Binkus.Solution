namespace DDS.Services;

public class TestService
{
    public Func<string> Fn { get; }

    public IServiceProvider Services { get; } = Globals.ServiceProvider;
    
    public TestService(IServiceProvider services, TestInnerService service)
    {
        Services = services;
        Fn = () => service.Id;
    }

    public string ExecFn() => Fn();
}

public class TestInnerService
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
}


public static class TestServicesInstaller
{
    static IServiceCollection RegisterTestServices(this IServiceCollection services)
        => services
            .AddTransient<TestInnerService>()
            .AddTransient<TestService>()
            ;
}