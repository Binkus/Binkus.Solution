namespace DDS.Core.Services;

public interface IProvideServices
{
    IServiceProvider Services { get; }

    TService GetService<TService>() where TService : notnull;
    
    object GetService(Type serviceType);
}
