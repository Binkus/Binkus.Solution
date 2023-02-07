using Avalonia.Controls;
using Avalonia.Platform;

namespace DDS.Avalonia.Controls.Application;

/// <summary>
/// Initializes platform-specific services for an <see cref="Application"/>.
/// </summary>
public sealed class ReactiveAppBuilder : AppBuilderBase<ReactiveAppBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveAppBuilder"/> class.
    /// </summary>
    public ReactiveAppBuilder() : this(default) { }

    ///<inheritdoc cref="ReactiveAppBuilder()"/>
    /// <param name="services">Presets ServiceCollection, when null or default,
    /// instantiates new ServiceCollection()</param>
    public ReactiveAppBuilder(IServiceCollection? services)
        : base(new StandardRuntimePlatform(),
            builder => StandardRuntimePlatformServices.Register(builder.ApplicationType?.Assembly))
    {
        ServiceCollection = services ?? new ServiceCollection();
    }
    
    public new App? Instance => base.Instance as App;

    public IServiceCollection ServiceCollection { get; }
}