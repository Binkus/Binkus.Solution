using CommunityToolkit.Mvvm.Messaging;

namespace DDS.Core.ViewModels;

/// <summary>
/// <see cref="ReactiveObservableObject"/>
/// <see cref="ObservableRecipient"/>
/// </summary>
[DataContract]
[ObservableRecipient]
public abstract partial class ReactiveObservableRecipient : ReactiveObservableObject,
    IReactiveNotifyPropertyChanged<IReactiveObject>, IReactiveObject
{
    /// <inheritdoc cref="ReactiveObservableObject(bool)"/>
    protected ReactiveObservableRecipient(bool reactiveObjectCompatibility = true) : base(reactiveObjectCompatibility)
    {
        Messenger = WeakReferenceMessenger.Default;
    }
    
    /// <summary> 
    /// <inheritdoc cref="ReactiveObservableObject(bool)"/>
    /// </summary>
    /// <param name="messenger">The <see cref="IMessenger"/> instance to use to send messages.</param>
    /// <param name="reactiveObjectCompatibility"><inheritdoc cref="ReactiveObservableObject(bool)"/></param>
    protected ReactiveObservableRecipient(IMessenger? messenger, bool reactiveObjectCompatibility = true) : base(reactiveObjectCompatibility)
    {
        Messenger = messenger ?? WeakReferenceMessenger.Default;
    }
}

/// <summary>
/// <see cref="ReactiveObservableObject"/>
/// <see cref="ObservableRecipient"/>
/// <see cref="ObservableValidator"/>
/// </summary>
[DataContract]
[ObservableRecipient]
public abstract partial class ReactiveObservableRecipientValidator : ReactiveObservableValidator,
    IReactiveNotifyPropertyChanged<IReactiveObject>, IReactiveObject
{
    /// <inheritdoc cref="ReactiveObservableObject(bool)"/>
    protected ReactiveObservableRecipientValidator(bool reactiveObjectCompatibility = true) : base(reactiveObjectCompatibility)
    {
        Messenger = WeakReferenceMessenger.Default;
    }

    /// <summary> 
    /// <inheritdoc cref="ReactiveObservableObject(bool)"/>
    /// </summary>
    /// <param name="messenger">The <see cref="IMessenger"/> instance to use to send messages.</param>
    /// <param name="items"></param>
    /// <param name="reactiveObjectCompatibility"><inheritdoc cref="ReactiveObservableObject(bool)"/></param>
    /// <param name="serviceProvider"></param>
    protected ReactiveObservableRecipientValidator(IMessenger? messenger, IServiceProvider? serviceProvider, IDictionary<object, object?>? items = null, bool reactiveObjectCompatibility = true) : base(serviceProvider, items, reactiveObjectCompatibility)
    {
        Messenger = messenger ?? WeakReferenceMessenger.Default;
    }
}