using System.Reflection;
using System.Runtime.CompilerServices;

namespace Binkus.ReactiveMvvm;

[DataContract]
public abstract class ReactiveObservableObject : ObservableObject,
    IReactiveNotifyPropertyChanged<IReactiveObject>, IReactiveObject
{
    /// <summary>
    /// Initializes a new instance a new instance.
    /// </summary>
    /// <param name="reactiveObjectCompatibility">When true enable compatibility to ReactiveUI</param>
    protected ReactiveObservableObject(bool reactiveObjectCompatibility = true)
    {
        if(reactiveObjectCompatibility) ReactiveObjectCompatibility();
    }
    
    //

    #region Fix ObservableProperties partially not notifying ReactiveUI observables

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        this.RaisePropertyChanged(e.PropertyName);
    }

    protected override void OnPropertyChanging(PropertyChangingEventArgs e)
    {
        this.RaisePropertyChanging(e.PropertyName);
    }

    #endregion

    //

    #region Compatibility for ReactiveUI, IReactiveObject x ObservableObject, CommunityToolkit.Mvvm

    /// <summary>
    /// Call this once from ctor of this base class for quick setup for ReactiveUI compatibility,
    /// without SetupReactiveObject() which is called by this method, ReactiveUI would not be able to notify our
    /// INotifyProperty*-implementations, so e.g. ReactiveUI.Fody would not work without it 
    /// </summary>
    private void ReactiveObjectCompatibility()
    {
        SetupReactiveNotifyPropertyChanged();
        SetupReactiveObject();
    }

    /// <summary>
    /// Important setup for this ViewModel that ReactiveUI is able to notify our INotifyProperty*-implementations
    /// </summary>
    private void SetupReactiveObject()
    {
        this.SubscribePropertyChangingEvents();
        this.SubscribePropertyChangedEvents();
    }
    
    // IReactiveObject (inherited from IRoutableViewModel)

    public virtual void RaisePropertyChanging(PropertyChangingEventArgs args) => base.OnPropertyChanging(args);

    public virtual void RaisePropertyChanged(PropertyChangedEventArgs args) => base.OnPropertyChanged(args);
    
    // IReactiveNotifyPropertyChanged<IReactiveObject>
    
    [IgnoreDataMember] private Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>> _changing = null!;
    [IgnoreDataMember] private Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>> _changed = null!;

    /// <summary>
    /// Sets up Observables for IReactiveNotifyPropertyChanged
    /// </summary>
    private void SetupReactiveNotifyPropertyChanged()
    {
        _changing = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(
            () => Observable.FromEventPattern<PropertyChangingEventHandler, PropertyChangingEventArgs>
            (
                changingHandler => PropertyChanging += changingHandler,
                changingHandler => PropertyChanging -= changingHandler
            ).Select(eventPattern => // new ReactivePropertyChangedEventArgs works too, interface uses IReactivePropertyChangedEventArgs
                new ReactivePropertyChangingEventArgs<ReactiveObservableObject>(
                    (eventPattern.Sender as ReactiveObservableObject)!, eventPattern.EventArgs.PropertyName!)));

        _changed = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(
            () => Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>
            (
                changedHandler => PropertyChanged += changedHandler,
                changedHandler => PropertyChanged -= changedHandler
            ).Select(eventPattern => 
                new ReactivePropertyChangedEventArgs<ReactiveObservableObject>(
                    (eventPattern.Sender as ReactiveObservableObject)!, eventPattern.EventArgs.PropertyName!)));
    }

    /// <summary>
    /// Implementation of ReactiveObject calls ReactiveUI-internal functions, this one needs testing and may not work.
    /// <inheritdoc />
    /// </summary>
    /// <returns><inheritdoc /></returns>
    public virtual IDisposable SuppressChangeNotifications() => Disposable.Empty;
    
    [IgnoreDataMember]
    public virtual IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changing
    {
        get => _changing.Value;
        set => _changing = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(value);
    }

    [IgnoreDataMember]
    public virtual IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changed
    {
        get => _changed.Value;
        set => _changed = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(value);
    }

    #endregion
}

[DataContract]
public abstract class ReactiveObservableValidator : ObservableValidator,
    IReactiveNotifyPropertyChanged<IReactiveObject>, IReactiveObject
{
    private static readonly FieldInfo ErrorsChangedFieldInfo =
        typeof(ObservableValidator).GetEvent(nameof(INotifyDataErrorInfo.ErrorsChanged))?.DeclaringType?.GetField(
            nameof(INotifyDataErrorInfo.ErrorsChanged), BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new UnreachableException("CommunityToolkit.Mvvm's ObservableValidator class changed its " +
                                          "implementation of INotifyDataErrorInfo and made " +
                                          "INotifyDataErrorInfo.ErrorsChanged inaccessible through reflection.");

#if DEBUG
    /// When debug, checks that ErrorsChanged event can be accessed through reflection
    /// (without static ctor it would be lazy until first accessed, so this is for catching potential breaking API
    /// changes by CommunityToolkit.Mvvm in regards to the ErrorsChanged event; but realistically as long as
    /// ObservableValidator continues implementing INotifyDataErrorInfo, the small reflection magic should always work).
    static ReactiveObservableValidator()
    {
        // assignment just to visualize, the magic is the existence of static ctor, if any static ctor is present,
        // even without assignment static properties would get initialized before static ctor runs,
        // static properties are lazy when no static ctor exists
        _ = ErrorsChangedFieldInfo;
    }
#endif
     

    // virtual ErrorsChanged event would be nice and or a RaiseErrorsChanged method inside ObservableValidator or
    // another way to invoke the ErrorsChanged event, as well as a way to validate only one property by its name without
    // having to provide a value
    private EventHandler<DataErrorsChangedEventArgs>? ErrorsChangedEventHandler
        => ErrorsChangedFieldInfo.GetValue(this) as EventHandler<DataErrorsChangedEventArgs>;
    
    /// <summary>
    /// Raises the <see cref="INotifyDataErrorInfo.ErrorsChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the validated property, or null or empty string for object level.</param>
    /// <see cref="DataErrorsChangedEventArgs"/>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ReactiveObservableValidator))]
    protected void RaiseErrorsChanged(/*no [CallerMemberName] by design*/string propertyName = "")
        => ErrorsChangedEventHandler?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

    /// <summary>
    /// Initializes a new instance a new instance.
    /// </summary>
    /// <param name="reactiveObjectCompatibility">When true enable compatibility to ReactiveUI</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ReactiveObservableValidator))]
    protected ReactiveObservableValidator(bool reactiveObjectCompatibility = true)
    {
        if(reactiveObjectCompatibility) ReactiveObjectCompatibility();
    }

    /// <summary>
    /// Initializes a new instance a new instance.
    /// </summary>
    /// <param name="items"></param>
    /// <param name="reactiveObjectCompatibility">When true enable compatibility to ReactiveUI</param>
    /// <param name="serviceProvider"></param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ReactiveObservableValidator))]
    protected ReactiveObservableValidator(IServiceProvider? serviceProvider, IDictionary<object, object?>? items = null, bool reactiveObjectCompatibility = true) 
        : base(serviceProvider, items)
    {
        if(reactiveObjectCompatibility) ReactiveObjectCompatibility();
    }
    
    //

    #region Fix ObservableProperties partially not notifying ReactiveUI observables

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        this.RaisePropertyChanged(e.PropertyName);
    }

    protected override void OnPropertyChanging(PropertyChangingEventArgs e)
    {
        this.RaisePropertyChanging(e.PropertyName);
    }

    #endregion

    //

    #region Compatibility for ReactiveUI, IReactiveObject x ObservableObject, CommunityToolkit.Mvvm

    /// <summary>
    /// Call this once from ctor of this base class for quick setup for ReactiveUI compatibility,
    /// without SetupReactiveObject() which is called by this method, ReactiveUI would not be able to notify our
    /// INotifyProperty*-implementations, so e.g. ReactiveUI.Fody would not work without it 
    /// </summary>
    private void ReactiveObjectCompatibility()
    {
        SetupReactiveNotifyPropertyChanged();
        SetupReactiveObject();
    }

    /// <summary>
    /// Important setup for this ViewModel that ReactiveUI is able to notify our INotifyProperty*-implementations
    /// </summary>
    private void SetupReactiveObject()
    {
        this.SubscribePropertyChangingEvents();
        this.SubscribePropertyChangedEvents();
    }
    
    // IReactiveObject (inherited from IRoutableViewModel)

    public virtual void RaisePropertyChanging(PropertyChangingEventArgs args) => base.OnPropertyChanging(args);

    public virtual void RaisePropertyChanged(PropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        ValidateProperty(args.PropertyName ?? "", false);
    }

    // IReactiveNotifyPropertyChanged<IReactiveObject>
    
    [IgnoreDataMember] private Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>> _changing = null!;
    [IgnoreDataMember] private Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>> _changed = null!;

    /// <summary>
    /// Sets up Observables for IReactiveNotifyPropertyChanged
    /// </summary>
    private void SetupReactiveNotifyPropertyChanged()
    {
        _changing = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(
            () => Observable.FromEventPattern<PropertyChangingEventHandler, PropertyChangingEventArgs>
            (
                changingHandler => PropertyChanging += changingHandler,
                changingHandler => PropertyChanging -= changingHandler
            ).Select(eventPattern => // new ReactivePropertyChangedEventArgs works too, interface uses IReactivePropertyChangedEventArgs
                new ReactivePropertyChangingEventArgs<ReactiveObservableValidator>(
                    (eventPattern.Sender as ReactiveObservableValidator)!, eventPattern.EventArgs.PropertyName!)));

        _changed = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(
            () => Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>
            (
                changedHandler => PropertyChanged += changedHandler,
                changedHandler => PropertyChanged -= changedHandler
            ).Select(eventPattern => 
                new ReactivePropertyChangedEventArgs<ReactiveObservableValidator>(
                    (eventPattern.Sender as ReactiveObservableValidator)!, eventPattern.EventArgs.PropertyName!)));
    }

    /// <summary>
    /// Implementation of ReactiveObject calls ReactiveUI-internal functions, this one needs testing and may not work.
    /// <inheritdoc />
    /// </summary>
    /// <returns><inheritdoc /></returns>
    public virtual IDisposable SuppressChangeNotifications() => Disposable.Empty;
    
    [IgnoreDataMember]
    public virtual IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changing
    {
        get => _changing.Value;
        set => _changing = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(value);
    }

    [IgnoreDataMember]
    public virtual IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changed
    {
        get => _changed.Value;
        set => _changed = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(value);
    }

    #endregion
    
    // Validation

    [UsedImplicitly] protected bool IsValidatingPropertiesOnPropertyChanged { get; init; } = true;
    // [UsedImplicitly] protected bool IsValidatingPropertiesOnPropertyChangedReactiveAttribute { get; init; }
    
    [UsedImplicitly]
    protected void ValidateProperty([CallerMemberName]string propertyName = "", bool forceValidation = true)
    {
        if (!IsValidatingPropertiesOnPropertyChanged && !forceValidation) return;

        // this.ValidateAllProperties();

        try
        {
            var propertyInfo = GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo is null || ( //!Attribute.IsDefined(propertyInfo, typeof(ReactiveAttribute)) &&
                    !Attribute.IsDefined(propertyInfo, typeof(NotifyDataErrorInfoProperty)) &&
                    !Attribute.IsDefined(GetType(), typeof(NotifyDataErrorInfoProperty)))) return;
            var value = propertyInfo.GetValue(this);
            ValidateProperty(value, propertyName);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    protected sealed class NotifyDataErrorInfoProperty : Attribute
    {
    }
}