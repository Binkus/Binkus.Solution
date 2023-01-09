using System.Diagnostics.CodeAnalysis;

namespace Binkus.DependencyInjection;


public delegate object GetServiceOrCreateInstance(IServiceProvider provider,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] 
    Type type);

// public delegate T GetServiceOrCreateInstance<
//     [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(IServiceProvider provider);

public delegate object CreateInstance(
    IServiceProvider provider,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    Type instanceType,
    params object[] parameters);

// public delegate T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
//     IServiceProvider provider, params object[] parameters);

public sealed class IocUtilitiesDelegation
{
    public static IocUtilitiesDelegation Default { get; set; } = new();

    public GetServiceOrCreateInstance FuncGetServiceOrCreateInstance { get; set; } = null!;

    public object GetServiceOrCreateInstance(IServiceProvider provider,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type type) => FuncGetServiceOrCreateInstance(provider, type);
    

    public T GetServiceOrCreateInstance<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        T>(IServiceProvider provider) => (T)FuncGetServiceOrCreateInstance(provider, typeof(T));

    public CreateInstance FuncCreateInstance { get; set; } = null!;

    public object CreateInstance(
        IServiceProvider provider,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type instanceType,
        params object[] parameters) => FuncCreateInstance(provider, instanceType, parameters);
    

    public T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        IServiceProvider provider, params object[] parameters) => (T)FuncCreateInstance(provider, typeof(T), parameters);
}