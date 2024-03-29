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

public interface IGenericService<T> : IGuidProvider { }
public interface IDoubleGenericService<T0,T1> : IGuidProvider { }
public interface ITripleGenericService<T0,T1,T2> : IGuidProvider { }

public record TestGenericRecordService<T> : GuidProviderRecord, IGenericService<T>, ISingletonService, IScopedService, ITransientService;
public record TestDoubleGenericRecordService<T0,T1> : GuidProviderRecord, IDoubleGenericService<T0,T1>, ISingletonService, IScopedService, ITransientService;
public record TestTripleGenericRecordService<T,T1,T2> : GuidProviderRecord, ITripleGenericService<T,T1,T2>, ISingletonService, IScopedService, ITransientService;

