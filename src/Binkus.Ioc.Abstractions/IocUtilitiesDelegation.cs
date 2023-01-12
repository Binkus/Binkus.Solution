using System.Diagnostics.CodeAnalysis;
using Binkus.DependencyInjection.Extensions;

// ReSharper disable MemberCanBePrivate.Global

namespace Binkus.DependencyInjection;

public delegate object GetServiceOrCreateInstance(IServiceProvider provider,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] 
    Type type);

public delegate object CreateInstance(
    IServiceProvider provider,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    Type instanceType,
    params object[] parameters);

public sealed class IocUtilitiesDelegation
{
    // public static IocUtilitiesDelegation GetFrom(IServiceProvider services) => services.GetIocUtilities();

    public IocUtilitiesDelegation(CreateInstance createInstance) : this(createInstance,
        (provider, type) => provider.GetService(type) ?? createInstance(provider, type)) { }
    public IocUtilitiesDelegation(CreateInstance createInstance, GetServiceOrCreateInstance getServiceOrCreateInstance)
    {
        _funcGetServiceOrCreateInstance = getServiceOrCreateInstance;
        _funcCreateInstance = createInstance;
    }
    public static IocUtilitiesDelegation NewUninitializedIocUtilitiesDelegation() => new(null!, null!);
    
    
    private GetServiceOrCreateInstance? _funcGetServiceOrCreateInstance;
    public GetServiceOrCreateInstance FuncGetServiceOrCreateInstance
    {
        get => _funcGetServiceOrCreateInstance ??=
            (provider, type) => provider.GetService(type) ?? CreateInstance(provider, type);
        set => _funcGetServiceOrCreateInstance = value;
    }

    public object GetServiceOrCreateInstance(IServiceProvider provider,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type type) => FuncGetServiceOrCreateInstance(provider, type);

    public T GetServiceOrCreateInstance<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        T>(IServiceProvider provider) => (T)FuncGetServiceOrCreateInstance(provider, typeof(T));
    
    
    private CreateInstance? _funcCreateInstance;
    public CreateInstance FuncCreateInstance
    {
        get => _funcCreateInstance ??
               throw new InvalidOperationException($"{nameof(FuncCreateInstance)} has not been set yet.");
        set => _funcCreateInstance = value;
    }

    public object CreateInstance(
        IServiceProvider provider,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type instanceType,
        params object[] parameters) => FuncCreateInstance(provider, instanceType, parameters);

    public T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        IServiceProvider provider, params object[] parameters) => (T)FuncCreateInstance(provider, typeof(T), parameters);
}

public static class ServiceProviderExtensionsForIocUtilitiesDelegation
{
    private static IocUtilitiesDelegation DefaultIocUtilitiesDelegation { get; } =
        IocUtilitiesDelegation.NewUninitializedIocUtilitiesDelegation();
    
    public static IocUtilitiesDelegation GetIocUtilities(this IServiceProvider services) =>
        services.GetService<IocUtilitiesDelegation>() ?? DefaultIocUtilitiesDelegation;
}