namespace DDS.Core.Services;

public interface IKnowMyLifetime
{
    Type Type { get; }
    ServiceLifetime Lifetime { get; }
}

public class LifetimeOf<T> : IKnowMyLifetime
{
    public LifetimeOf(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
        Type = typeof(T);
    }

    [UsedImplicitly] public Type Type { get; }
    [UsedImplicitly] public ServiceLifetime Lifetime { get; }
}