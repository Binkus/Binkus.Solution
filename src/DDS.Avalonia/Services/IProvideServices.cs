using System.Linq.Expressions;
using System.Reflection;

namespace DDS.Avalonia.Services;

public interface IProvideServices
{
    IServiceProvider Services { get; }

    TService GetService<TService>() where TService : notnull;
    
    object GetService(Type serviceType);
}
