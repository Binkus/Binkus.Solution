namespace Binkus.DependencyInjection;

public sealed class IocProvider
{
    public IocProvider Default { get; set; } = new();
    
    public static IocContainer? Container { get; set; }
}