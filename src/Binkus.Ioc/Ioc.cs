namespace Binkus.DependencyInjection;

public sealed class IocProvider
{
    public IocProvider Default { get; set; } = new();
    
    public static IContainerScope? Container { get; set; }
}