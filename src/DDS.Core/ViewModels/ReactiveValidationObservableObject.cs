// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root/.licenses/ReactiveUI/ReactiveUI.Validation/ for more license information.
// Original file from Jan 7 2022 is from:
// https://github.com/reactiveui/ReactiveUI.Validation/blob/9c131c7656333cd6ff4b5b91344f47a2f4415163/src/ReactiveUI.Validation/Helpers/ReactiveValidationObject.cs
// Changes of that file for ReactiveObservableObject and ReactiveObservableValidator compatibility: Copyright (c) 2022 Binkus. All rights reserved.
// These changes of that file are licensed to you by Binkus under the MIT license.
// See the LICENSE.txt file in the project root for more license information.
// (Full license information is the combination of root/.licenses/ReactiveUI/ReactiveUI.Validation/LICENSE and root/LICENSE.txt)

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Concurrency;
using DDS.Core.Helper;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Collections;
using ReactiveUI.Validation.Components.Abstractions;
using ReactiveUI.Validation.Formatters;
using ReactiveUI.Validation.Formatters.Abstractions;
using ReactiveUI.Validation.Helpers;
using ValidationContext = ReactiveUI.Validation.Contexts.ValidationContext;

// ReSharper disable MemberCanBePrivate.Global

namespace DDS.Core.ViewModels;

/// <summary>
/// Base class for <see cref="ReactiveObservableObject"/> that supports <see cref="INotifyDataErrorInfo"/> validation
/// and implements <see cref="IValidatableViewModel"/>.
/// </summary>
public abstract partial class ReactiveValidationObservableObject : ReactiveObservableObject, IValidatableViewModel, INotifyDataErrorInfo
{
    private readonly HashSet<string> _mentionedPropertyNames = new();
    protected IValidationTextFormatter<string> Formatter { get; init; }
    private bool _hasErrors;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveValidationObservableObject"/> class which is basically a
    /// copy of <see cref="ReactiveValidationObject"/>, which uses <see cref="ReactiveObservableObject"/> instead of
    /// <see cref="ReactiveObject"/> as its base, for compatibility with CommunityToolkit.Mvvm.
    /// </summary>
    /// <param name="validationContext"></param>
    /// <param name="subscribeToValidationStatusChange"></param>
    /// <param name="reactiveObjectCompatibility"></param>
    /// <param name="formatter">
    /// Validation formatter. Defaults to <see cref="SingleLineFormatter"/>. In order to override the global
    /// default value, implement <see cref="IValidationTextFormatter{TOut}"/> and register an instance of
    /// IValidationTextFormatter&lt;string&gt; into Global ServiceProvider on Startup.
    /// </param>
    /// <param name="services">IServiceProvider instance, defaults to Global IServiceProvider, used to retrieve
    /// IValidationTextFormatter&lt;string&gt;</param>
    protected ReactiveValidationObservableObject(
        ValidationContext validationContext,
        bool subscribeToValidationStatusChange = true,
        bool reactiveObjectCompatibility = true,
        IValidationTextFormatter<string>? formatter = null,
        IServiceProvider? services = null) : base(reactiveObjectCompatibility)
    {
        Formatter = formatter 
                    ?? (services ?? Globals.Services).GetService<IValidationTextFormatter<string>>()
                    ?? SingleLineFormatter.Default;
    
        ValidationContext = validationContext;
        if (subscribeToValidationStatusChange) SubscribeToValidationStatusChange();
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveValidationObservableObject"/> class which is basically a
    /// copy of <see cref="ReactiveValidationObject"/>, which uses <see cref="ReactiveObservableObject"/> instead of
    /// <see cref="ReactiveObject"/> as its base, for compatibility with CommunityToolkit.Mvvm.
    /// </summary>
    /// <param name="services">IServiceProvider instance, defaults to Global IServiceProvider, used to retrieve
    /// IValidationTextFormatter&lt;string&gt;</param>
    /// <param name="scheduler">
    /// Scheduler for the <see cref="ValidationContext"/>. Uses <see cref="CurrentThreadScheduler"/> by default.
    /// </param>
    /// <param name="formatter">
    /// Validation formatter. Defaults to <see cref="SingleLineFormatter"/>. In order to override the global
    /// default value, implement <see cref="IValidationTextFormatter{TOut}"/> and register an instance of
    /// IValidationTextFormatter&lt;string&gt; into Global ServiceProvider on Startup.
    /// </param>
    protected ReactiveValidationObservableObject(
        IServiceProvider? services = null, 
        IScheduler? scheduler = null, 
        IValidationTextFormatter<string>? formatter = null)
    {
        Formatter = formatter
                    ?? (services ?? Globals.Services).GetService<IValidationTextFormatter<string>>()
                    ?? SingleLineFormatter.Default;

        ValidationContext = new ValidationContext(scheduler);
        
        SubscribeToValidationStatusChange();
    }

    protected IDisposable SubscribeToValidationStatusChange() =>
        ValidationContext.Validations
            .ToObservableChangeSet()
            .ToCollection()
            .Select(components => components
                .Select(component => component
                    .ValidationStatusChange
                    .Select(_ => component))
                .Merge()
                .StartWith(ValidationContext))
            .Switch()
            .Subscribe(OnValidationStatusChange);

    /// <inheritdoc />
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <inheritdoc />
    public bool HasErrors
    {
        get => _hasErrors;
        private set => this.RaiseAndSetIfChanged(ref _hasErrors, value);
    }

    /// <inheritdoc />
    public ValidationContext ValidationContext { get; }

    /// <summary>
    /// Returns a collection of error messages, required by the INotifyDataErrorInfo interface.
    /// </summary>
    /// <param name="propertyName">Property to search error notifications for.</param>
    /// <returns>A list of error messages, usually strings.</returns>
    /// <inheritdoc />
    public virtual IEnumerable GetErrors(string? propertyName) =>
        propertyName is null || string.IsNullOrEmpty(propertyName)
            ? SelectInvalidPropertyValidations()
                .Select(state => Formatter.Format(state.Text ?? ValidationText.None))
                .ToArray()
            : SelectInvalidPropertyValidations()
                .Where(validation => validation.ContainsPropertyName(propertyName))
                .Select(state => Formatter.Format(state.Text ?? ValidationText.None))
                .ToArray();

    /// <summary>
    /// Raises the <see cref="ErrorsChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the validated property.</param>
    protected void RaiseErrorsChanged(string propertyName = "") =>
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

    /// <summary>
    /// Selects validation components that are invalid.
    /// </summary>
    /// <returns>Returns the invalid property validations.</returns>
    private IEnumerable<IPropertyValidationComponent> SelectInvalidPropertyValidations() =>
        ValidationContext.Validations
            .OfType<IPropertyValidationComponent>()
            .Where(validation => !validation.IsValid);

    /// <summary>
    /// Updates the <see cref="HasErrors" /> property before raising the <see cref="ErrorsChanged" />
    /// event, and then raises the <see cref="ErrorsChanged" /> event. This behaviour is required by WPF, see:
    /// https://stackoverflow.com/questions/24518520/ui-not-calling-inotifydataerrorinfo-geterrors/24837028.
    /// </summary>
    /// <remarks>
    /// WPF doesn't understand string.Empty as an argument for the <see cref="ErrorsChanged"/>
    /// event, so we are sending <see cref="ErrorsChanged"/> notifications for every saved property.
    /// This is required for e.g. cases when a <see cref="IValidationComponent"/> is disposed and
    /// detached from the <see cref="ValidationContext"/>, and we'd like to mark all invalid
    /// properties as valid (because the thing that validates them no longer exists).
    /// </remarks>
    private void OnValidationStatusChange(IValidationComponent component)
    {
        HasErrors = !ValidationContext.GetIsValid();
        if (component is IPropertyValidationComponent propertyValidationComponent)
        {
            foreach (var propertyName in propertyValidationComponent.Properties)
            {
                RaiseErrorsChanged(propertyName);
                _mentionedPropertyNames.Add(propertyName);
            }
        }
        else
        {
            foreach (var propertyName in _mentionedPropertyNames)
            {
                RaiseErrorsChanged(propertyName);
            }
        }
    }
}

//

/// <summary>
/// Base class for <see cref="ReactiveObservableValidator"/> that supports <see cref="INotifyDataErrorInfo"/> validation
/// and implements <see cref="IValidatableViewModel"/>.
/// </summary>
public abstract partial class ReactiveValidationObservableValidator : ReactiveObservableValidator, IValidatableViewModel, INotifyDataErrorInfo
{
    private readonly HashSet<string> _mentionedPropertyNames = new();
    protected IValidationTextFormatter<string> Formatter { get; init; }
    private bool _hasErrors;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveValidationObservableObject"/> class which is basically a
    /// copy of <see cref="ReactiveValidationObject"/>, which uses <see cref="ReactiveObservableObject"/> instead of
    /// <see cref="ReactiveObject"/> as its base, for compatibility with CommunityToolkit.Mvvm.
    /// </summary>
    /// <param name="validationContext"></param>
    /// <param name="subscribeToValidationStatusChange"></param>
    /// <param name="reactiveObjectCompatibility"></param>
    /// <param name="formatter">
    /// Validation formatter. Defaults to <see cref="SingleLineFormatter"/>. In order to override the global
    /// default value, implement <see cref="IValidationTextFormatter{TOut}"/> and register an instance of
    /// IValidationTextFormatter&lt;string&gt; into Global ServiceProvider on Startup.
    /// </param>
    /// <param name="services">IServiceProvider instance, defaults to Global IServiceProvider, used to retrieve
    /// IValidationTextFormatter&lt;string&gt;</param>
    protected ReactiveValidationObservableValidator(
        ValidationContext validationContext,
        bool subscribeToValidationStatusChange = true,
        bool reactiveObjectCompatibility = true,
        IValidationTextFormatter<string>? formatter = null,
        IServiceProvider? services = null) : base(services, null, reactiveObjectCompatibility)
    {
        Formatter = formatter 
                    ?? (services ?? Globals.Services).GetService<IValidationTextFormatter<string>>()
                    ?? SingleLineFormatter.Default;
    
        ValidationContext = validationContext;
        if (subscribeToValidationStatusChange) SubscribeToValidationStatusChange();
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveValidationObservableObject"/> class which is basically a
    /// copy of <see cref="ReactiveValidationObject"/>, which uses <see cref="ReactiveObservableObject"/> instead of
    /// <see cref="ReactiveObject"/> as its base, for compatibility with CommunityToolkit.Mvvm.
    /// </summary>
    /// <param name="services">IServiceProvider instance, defaults to Global IServiceProvider, used to retrieve
    /// IValidationTextFormatter&lt;string&gt;</param>
    /// <param name="scheduler">
    /// Scheduler for the <see cref="ValidationContext"/>. Uses <see cref="CurrentThreadScheduler"/> by default.
    /// </param>
    /// <param name="formatter">
    /// Validation formatter. Defaults to <see cref="SingleLineFormatter"/>. In order to override the global
    /// default value, implement <see cref="IValidationTextFormatter{TOut}"/> and register an instance of
    /// IValidationTextFormatter&lt;string&gt; into Global ServiceProvider on Startup.
    /// </param>
    protected ReactiveValidationObservableValidator(
        IServiceProvider? services = null, 
        IScheduler? scheduler = null, 
        IValidationTextFormatter<string>? formatter = null) : base(services, null)
    {
        Formatter = formatter
                    ?? (services ?? Globals.Services).GetService<IValidationTextFormatter<string>>()
                    ?? SingleLineFormatter.Default;

        ValidationContext = new ValidationContext(scheduler);
        
        SubscribeToValidationStatusChange();
    }

    protected IDisposable SubscribeToValidationStatusChange() =>
        ValidationContext.Validations
            .ToObservableChangeSet()
            .ToCollection()
            .Select(components => components
                .Select(component => component
                    .ValidationStatusChange
                    .Select(_ => component))
                .Merge()
                .StartWith(ValidationContext))
            .Switch()
            .Subscribe(OnValidationStatusChange);

    bool INotifyDataErrorInfo.HasErrors => _hasErrors || base.HasErrors;

    /// <inheritdoc cref="INotifyDataErrorInfo.HasErrors" />
    public new bool HasErrors
    {
        get => ((INotifyDataErrorInfo)this).HasErrors;
        private set => this.RaiseAndSetIfChanged(ref _hasErrors, value);
    }

    /// <inheritdoc />
    public ValidationContext ValidationContext { get; }

    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName) => GetErrors(propertyName);

    /// <summary>
    /// Returns a collection of error messages, required by the INotifyDataErrorInfo interface.
    /// </summary>
    /// <inheritdoc cref="INotifyDataErrorInfo.GetErrors(string)" />
    public new virtual IEnumerable<ValidationResult> GetErrors(string? propertyName)
    {
        var errors = propertyName is null || string.IsNullOrEmpty(propertyName)
            ? SelectInvalidPropertyValidations()
                .Select(state =>
                    (errorText: Formatter.Format(state.Text ?? ValidationText.None), properties: state.Properties))
                .Select(errorTuple => new ValidationResult(errorTuple.errorText, errorTuple.properties))
            : SelectInvalidPropertyValidations()
                .Where(validation => validation.ContainsPropertyName(propertyName))
                .Select(state =>
                    (errorText: Formatter.Format(state.Text ?? ValidationText.None), properties: state.Properties))
                .Select(errorTuple => new ValidationResult(errorTuple.errorText, errorTuple.properties));
        return errors.Concat(base.GetErrors(propertyName)).ToArray();
    }

    /// <summary>
    /// Selects validation components that are invalid.
    /// </summary>
    /// <returns>Returns the invalid property validations.</returns>
    private IEnumerable<IPropertyValidationComponent> SelectInvalidPropertyValidations() =>
        ValidationContext.Validations
            .OfType<IPropertyValidationComponent>()
            .Where(validation => !validation.IsValid);

    /// <summary>
    /// Updates the <see cref="HasErrors" /> property before raising the <see cref="INotifyDataErrorInfo.ErrorsChanged" />
    /// event, and then raises the <see cref="INotifyDataErrorInfo.ErrorsChanged" /> event. This behaviour is required by WPF, see:
    /// https://stackoverflow.com/questions/24518520/ui-not-calling-inotifydataerrorinfo-geterrors/24837028.
    /// </summary>
    /// <remarks>
    /// WPF doesn't understand string.Empty as an argument for the <see cref="INotifyDataErrorInfo.ErrorsChanged"/>
    /// event, so we are sending <see cref="INotifyDataErrorInfo.ErrorsChanged"/> notifications for every saved property.
    /// This is required for e.g. cases when a <see cref="IValidationComponent"/> is disposed and
    /// detached from the <see cref="ValidationContext"/>, and we'd like to mark all invalid
    /// properties as valid (because the thing that validates them no longer exists).
    /// </remarks>
    private void OnValidationStatusChange(IValidationComponent component)
    {
        HasErrors = !ValidationContext.GetIsValid();
        if (component is IPropertyValidationComponent propertyValidationComponent)
        {
            foreach (var propertyName in propertyValidationComponent.Properties)
            {
                RaiseErrorsChanged(propertyName);
                _mentionedPropertyNames.Add(propertyName);
            }
        }
        else
        {
            foreach (var propertyName in _mentionedPropertyNames)
            {
                RaiseErrorsChanged(propertyName);
            }
        }
    }
}