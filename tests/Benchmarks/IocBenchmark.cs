using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using Binkus.DependencyInjection;
using Binkus.DependencyInjection.Extensions;
using Binkus.Ioc.Tests;
using Xunit.Abstractions;
using Xunit.Sdk;
using ServiceProviderServiceExtensions = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions;

namespace Binkus;

public abstract class IocSetup
{
    // private sealed class TextOutputHelper : ITestOutputHelper { public void WriteLine(string message) => Console.WriteLine(message); public void WriteLine(string format, params object[] args) => Console.WriteLine(format, args); }
    private sealed class NullOutputHelper : ITestOutputHelper { public void WriteLine(string message) {} public void WriteLine(string format, params object[] args) {} }
    private protected IocContainerTests ContainerTests { get; } = new IocContainerTests(new NullOutputHelper());
    
    internal IocContainerScope ContainerScope => ContainerTests.ContainerScope;
    internal IServiceProvider MsServiceProvider => ContainerTests.MsServiceProvider;
    
    protected IocSetup()
    {
        var t = typeof(IInnerRequesterSingletonService);
        var d = ContainerScope.Root.CachedDescriptors[t];
        TypeIocDescriptors.TryAdd(t, d);

        var inst = ContainerScope.GetService<IInnerRequesterSingletonService>();
        IocDescriptorInstanceProviders[d] = new ServiceInstanceProvider(inst);
        
        TestDescriptor = d;
        
        ServicesParams = new IServiceProvider[] { ContainerScope, MsServiceProvider };
    }

    private protected IocDescriptor TestDescriptor { get; }

    internal ConcurrentDictionary<Type, IocDescriptor> TypeIocDescriptors { get; } = new();

    internal ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider> IocDescriptorInstanceProviders { get; } = new();

    //
    
    private IServiceProvider[] ServicesParams { get; }

    // ReSharper disable once MemberCanBeProtected.Global
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public virtual int ServicesParamIndex { get; set; }

    private protected IServiceProvider Services => ServicesParams[ServicesParamIndex];
}

public abstract class IocSetupParams : IocSetup
{
    [Params(0,1)]
    public override int ServicesParamIndex { get; set; }
}

[MemoryDiagnoser]
public class BenchmarkConcurrentDictionaryLookup : IocSetup
{
    [Benchmark]
    public void ConcurrentDictionaryTypeIocDescriptors()
    {
        var t = typeof(IInnerRequesterSingletonService);
        var d = TypeIocDescriptors.GetValueOrDefault(t);
    }
    
    [Benchmark]
    public void ConcurrentDictionaryIocDescriptorInstanceProviders()
    {
        var d = TestDescriptor;
        var instanceProvider = IocDescriptorInstanceProviders.GetValueOrDefault(d);
    }
}

[MemoryDiagnoser]
public class BenchmarkGetHashCode : IocSetup
{
    [Benchmark]
    public void Hash2Test()
    {
        var t = typeof(IInnerRequesterSingletonService);
        var h = t.GetHashCode();
        var h2 = 0.GetHashCode();
    }
    
    [Benchmark]
    public void HashTest()
    {
        var h2 = 0.GetHashCode();
    }
}
[MemoryDiagnoser]
public class BenchmarkCreateAndDisposeScopeAsync : IocSetup
{
    [Benchmark]
    public async Task MsScopeCreationWithDisposeAsync()
    {
        await using var _ = ServiceProviderServiceExtensions.CreateAsyncScope(MsServiceProvider);
    }
    
    [Benchmark]
    public async Task BinkusScopeCreationWithDisposeAsync()
    {
        await using var _ = ContainerScope.CreateScope();
    }
}

[MemoryDiagnoser]
public class BenchmarkCreateScope : IocSetup
{
    [Benchmark]
    public void MsScopeCreation()
    {
        var s = ServiceProviderServiceExtensions.CreateAsyncScope(MsServiceProvider);
    }
    
    [Benchmark]
    public void BinkusScopeCreation()
    {
        var s = ContainerScope.CreateScope();
    }
}

[MemoryDiagnoser]
public class BenchmarkConcurrentDictionaryCreation : IocSetup
{
    [GlobalSetup]
    public void Setup() { }

    [Params(1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16)]
    private readonly int _concurrencyLevel = 1;
    
    [Params(0,11,31,71)]
    private readonly int _capacity = 0;

    [Benchmark]
    public void CreationOfConcurrentDictionaryOfKIocDescriptorVServiceInstanceProvider()
    {
        var d = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>(_concurrencyLevel, _capacity);
    }
}

[MemoryDiagnoser]
public class BenchmarkGeneralIocContainerTests : IocSetup
{
    [Benchmark]
    public void MsBasic()
    {
        ContainerTests.TestBasicMsDiContainer();
    }
    
    [Benchmark]
    public void BinkusBasic()
    {
        ContainerTests.TestBasicContainer();
    }
    
    [Benchmark]
    public void MsWithInner()
    {
        ContainerTests.TestMsDiContainer();
    }
    
    [Benchmark]
    public void BinkusWithInner()
    {
        ContainerTests.TestContainer();
    }
    
    [Benchmark]
    public void MsLazy()
    {
        ContainerTests.TestMsDiContainerSpecialsLazyT();
    }
    
    [Benchmark]
    public void BinkusLazy()
    {
        ContainerTests.TestContainerSpecialsLazyT();
    }
}

[MemoryDiagnoser]
public class BenchmarkServiceResolutionBasic : IocSetup
{
    [Benchmark]
    public void MsResolveMultiple()
    {
        var single = MsServiceProvider.GetService<ISingletonService>();
        var scoped = MsServiceProvider.GetService<IScopedService>();
        var transient = MsServiceProvider.GetService<ITransientService>();
    }
    
    [Benchmark]
    public void BinkusResolveMultiple()
    {
        var single = ContainerScope.GetService<ISingletonService>();
        var scoped = ContainerScope.GetService<IScopedService>();
        var transient = ContainerScope.GetService<ITransientService>();
    }
    
    [Benchmark]
    public void MsResolve_Transient()
    {
        var transient = MsServiceProvider.GetService<ITransientService>();
    }
    
    [Benchmark]
    public void BinkusResolve_Transient()
    {
        var transient = ContainerScope.GetService<ITransientService>();
    }
    
    [Benchmark]
    public void MsResolve_Scoped()
    {
        var scoped = MsServiceProvider.GetService<IScopedService>();
    }
    
    [Benchmark]
    public void BinkusResolve_Scoped()
    {
        var scoped = ContainerScope.GetService<IScopedService>();
    }
    
    [Benchmark]
    public void MsResolve_Singleton()
    {
        var single = MsServiceProvider.GetService<ISingletonService>();
    }
    
    [Benchmark]
    public void BinkusResolve_Singleton()
    {
        var single = ContainerScope.GetService<ISingletonService>();
    }
}

[MemoryDiagnoser]
public class BenchmarkServiceResolutionComplex : IocSetup
{
    [Benchmark]
    public void MsResolveMultiple()
    {
        var single = MsServiceProvider.GetService<IInnerRequesterSingletonService>();
        var scoped = MsServiceProvider.GetService<IInnerRequesterScopedService>();
        var transient = MsServiceProvider.GetService<IInnerRequesterTransientService>();
    }
    
    [Benchmark]
    public void BinkusResolveMultiple()
    {
        var single = ContainerScope.GetService<IInnerRequesterSingletonService>();
        var scoped = ContainerScope.GetService<IInnerRequesterScopedService>();
        var transient = ContainerScope.GetService<IInnerRequesterTransientService>();
    }
    
    [Benchmark]
    public void MsResolve_Transient()
    {
        var s = MsServiceProvider.GetService<IInnerRequesterTransientService>();
    }
    
    [Benchmark]
    public void BinkusResolve_Transient()
    {
        var s = ContainerScope.GetService<IInnerRequesterTransientService>();
    }
    
    [Benchmark]
    public void MsResolve_Scoped()
    {
        var s = MsServiceProvider.GetService<IInnerRequesterScopedService>();
    }
    
    [Benchmark]
    public void BinkusResolve_Scoped()
    {
        var s = ContainerScope.GetService<IInnerRequesterScopedService>();
    }
    
    [Benchmark]
    public void MsResolve_Singleton()
    {
        var s = MsServiceProvider.GetService<IInnerRequesterSingletonService>();
    }
    
    [Benchmark]
    public void BinkusResolve_Singleton()
    {
        var s = ContainerScope.GetService<IInnerRequesterSingletonService>();
    }
}

//

[MemoryDiagnoser]
public class BenchmarkServiceResolutionBasicToComplex : IocSetupParams
{
    [Benchmark]
    public void ResolveMultipleBasic()
    {
        var single = Services.GetService<ISingletonService>();
        var scoped = Services.GetService<IScopedService>();
        var transient = Services.GetService<ITransientService>();
    }
    
    [Benchmark]
    public void ResolveMultipleComplex()
    {
        var single = Services.GetService<IInnerRequesterSingletonService>();
        var scoped = Services.GetService<IInnerRequesterScopedService>();
        var transient = Services.GetService<IInnerRequesterTransientService>();
    }
    
    [Benchmark]
    public void Resolve_Transient()
    {
        var transient = Services.GetService<ITransientService>();
    }
    
    [Benchmark]
    public void Resolve_Scoped()
    {
        var scoped = Services.GetService<IScopedService>();
    }
    
    [Benchmark]
    public void Resolve_Singleton()
    {
        var single = Services.GetService<ISingletonService>();
    }
    
    [Benchmark]
    public void Resolve_Transient_Complex()
    {
        var s = Services.GetService<IInnerRequesterTransientService>();
    }
    
    [Benchmark]
    public void Resolve_Scoped_Complex()
    {
        var s = Services.GetService<IInnerRequesterScopedService>();
    }
    
    [Benchmark]
    public void Resolve_Singleton_Complex()
    {
        var s = Services.GetService<IInnerRequesterSingletonService>();
    }
}