namespace Binkus.Ioc.Tests;

public static class TestServices
{
    
}

public interface IGuidProvider { public Guid Id { get; } }
public abstract class GuidProvider : IGuidProvider { public Guid Id { get; } = Guid.NewGuid(); }
public record GuidProviderRecord : IGuidProvider { public Guid Id { get; } = Guid.NewGuid(); }

public interface ISingletonService : IGuidProvider { }
public interface IScopedService : IGuidProvider { }
public interface ITransientService : IGuidProvider { }

public interface IInnerRequesterSingletonService : IInnerProvider { }
public interface IInnerRequesterScopedService : IInnerProvider { }
public interface IInnerRequesterTransientService : IInnerProvider { }

//

public interface IInnerProvider : IGuidProvider
{
    ISingletonService Transient { get; }
    IScopedService Scoped { get; }
    ITransientService Singleton { get; }
}

//

public sealed class TestService : GuidProvider, ISingletonService, IScopedService, ITransientService { }

//

public record TestRecordService : GuidProviderRecord, ISingletonService, IScopedService, ITransientService { }

//

public sealed class InnerRequesterTestService : GuidProvider, ISingletonService, IScopedService, ITransientService,
    IInnerRequesterSingletonService, IInnerRequesterScopedService, IInnerRequesterTransientService, IInnerProvider
{
    public ISingletonService Transient { get; }
    public IScopedService Scoped { get; }
    public ITransientService Singleton { get; }

    public InnerRequesterTestService(
        ISingletonService transient, 
        IScopedService scoped, 
        ITransientService single)
    {
        Transient = transient;
        Scoped = scoped;
        Singleton = single;
    }
}

//

public record InnerRequesterTestRecordService : GuidProviderRecord, ISingletonService, IScopedService, ITransientService,
    IInnerRequesterSingletonService, IInnerRequesterScopedService, IInnerRequesterTransientService, IInnerProvider
{
    public ISingletonService Transient { get; }
    public IScopedService Scoped { get; }
    public ITransientService Singleton { get; }

    // ReSharper disable once ConvertToPrimaryConstructor
    public InnerRequesterTestRecordService(
        ISingletonService transient, 
        IScopedService scoped, 
        ITransientService single)
    {
        Transient = transient;
        Scoped = scoped;
        Singleton = single;
    }
}