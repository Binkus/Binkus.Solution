// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license:
//
// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
//
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.


#if NETFRAMEWORK || NETSTANDARD2_0
using System.Runtime.Serialization;
#else
using System.Runtime.CompilerServices;
#endif
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Binkus.DependencyInjection.Extensions;

// ReSharper disable CognitiveComplexity
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable SuggestVarOrType_Elsewhere

namespace Binkus.DependencyInjection;

/// <summary>
/// The result of <see cref="IocUtilities.CreateFactory(Type, Type[])"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get service arguments from.</param>
/// <param name="arguments">Additional constructor arguments.</param>
/// <returns>The instantiated type.</returns>
public delegate object ObjectFactory(IServiceProvider serviceProvider, object?[]? arguments);

/// <summary>
/// The result of <see cref="IocUtilities.CreateFactory{T}"/>. A delegate to specify a factory method to call to instantiate an instance of type `T`
/// </summary>
/// <typeparam name="T">The type of the instance being returned</typeparam>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get service arguments from.</param>
/// <param name="arguments">Additional constructor arguments.</param>
/// <returns>An instance of T</returns>
public delegate T ObjectFactory<T>(IServiceProvider serviceProvider, object?[]? arguments);


/// <summary>
/// Helper code for the various activator services.
/// </summary>
internal static class IocUtilities
{
    private static readonly MethodInfo GetServiceInfo =
        GetMethodInfo<Func<IServiceProvider, Type, Type, bool, object?>>((sp, t, r, c) => GetService(sp, t, r, c));

    /// <summary>
    /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="provider">The service provider used to resolve dependencies</param>
    /// <param name="instanceType">The type to activate</param>
    /// <param name="parameters">Constructor arguments not provided by the <paramref name="provider"/>.</param>
    /// <returns>An activated object of type instanceType</returns>
    public static object CreateInstance(
        IServiceProvider provider,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
        params object[] parameters)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (instanceType.IsAbstract)
        {
            // todo throw new InvalidOperationException(SR.CannotCreateAbstractClasses);
            throw new InvalidOperationException();
        }

        // todo
        IIocProviderIsService? serviceProviderIsService = provider.GetService<IIocProviderIsService>();
        // if container supports using IServiceProviderIsService, we try to find the longest ctor that
        // (a) matches all parameters given to CreateInstance
        // (b) matches the rest of ctor arguments as either a parameter with a default value or as a service registered
        // if no such match is found we fallback to the same logic used by CreateFactory which would only allow creating an
        // instance if all parameters given to CreateInstance only match with a single ctor
        if (serviceProviderIsService != null)
        {
            int bestLength = -1;
            bool seenPreferred = false;

            ConstructorMatcher bestMatcher = default;
            bool multipleBestLengthFound = false;

            foreach (ConstructorInfo? constructor in instanceType.GetConstructors())
            {
                var matcher = new ConstructorMatcher(constructor);
                bool isPreferred = constructor.IsDefined(typeof(IocUtilitiesConstructorAttribute), false);
                int length = matcher.Match(parameters, serviceProviderIsService);

                if (isPreferred)
                {
                    if (seenPreferred)
                    {
                        ThrowMultipleCtorsMarkedWithAttributeException();
                    }

                    if (length == -1)
                    {
                        ThrowMarkedCtorDoesNotTakeAllProvidedArguments();
                    }
                }

                if (isPreferred || bestLength < length)
                {
                    bestLength = length;
                    bestMatcher = matcher;
                    multipleBestLengthFound = false;
                }
                else if (bestLength == length)
                {
                    multipleBestLengthFound = true;
                }

                seenPreferred |= isPreferred;
            }

            if (bestLength != -1)
            {
                if (multipleBestLengthFound)
                {
                    // todo throw new InvalidOperationException(SR.Format(SR.MultipleCtorsFoundWithBestLength, instanceType, bestLength));
                    throw new InvalidOperationException();
                }

                return bestMatcher.CreateInstance(provider);
            }
        }

        Type?[] argumentTypes = new Type[parameters.Length];
        for (int i = 0; i < argumentTypes.Length; i++)
        {
            argumentTypes[i] = parameters[i]?.GetType();
        }

        FindApplicableConstructor(instanceType, argumentTypes, out ConstructorInfo constructorInfo, out int?[] parameterMap);
        var constructorMatcher = new ConstructorMatcher(constructorInfo);
        constructorMatcher.MapParameters(parameterMap, parameters);
        return constructorMatcher.CreateInstance(provider);
    }

    /// <summary>
    /// Create a delegate that will instantiate a type with constructor arguments provided directly
    /// and/or from an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="instanceType">The type to activate</param>
    /// <param name="argumentTypes">
    /// The types of objects, in order, that will be passed to the returned function as its second parameter
    /// </param>
    /// <returns>
    /// A factory that will instantiate instanceType using an <see cref="IServiceProvider"/>
    /// and an argument array containing objects matching the types defined in argumentTypes
    /// </returns>
    public static ObjectFactory CreateFactory(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
        Type[] argumentTypes)
    {
        CreateFactoryInternal(instanceType, argumentTypes, out ParameterExpression provider, out ParameterExpression argumentArray, out Expression factoryExpressionBody);

        var factoryLambda = Expression.Lambda<Func<IServiceProvider, object?[]?, object>>(
            factoryExpressionBody, provider, argumentArray);

        Func<IServiceProvider, object?[]?, object>? result = factoryLambda.Compile();
        return result.Invoke;
    }

    /// <summary>
    /// Create a delegate that will instantiate a type with constructor arguments provided directly
    /// and/or from an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type to activate</typeparam>
    /// <param name="argumentTypes">
    /// The types of objects, in order, that will be passed to the returned function as its second parameter
    /// </param>
    /// <returns>
    /// A factory that will instantiate type T using an <see cref="IServiceProvider"/>
    /// and an argument array containing objects matching the types defined in argumentTypes
    /// </returns>
    public static ObjectFactory<T>
        CreateFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
            Type[] argumentTypes)
    {
        CreateFactoryInternal(typeof(T), argumentTypes, out ParameterExpression provider, out ParameterExpression argumentArray, out Expression factoryExpressionBody);

        var factoryLambda = Expression.Lambda<Func<IServiceProvider, object?[]?, T>>(
            factoryExpressionBody, provider, argumentArray);

        Func<IServiceProvider, object?[]?, T>? result = factoryLambda.Compile();
        return result.Invoke;
    }

    private static void CreateFactoryInternal([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType, Type[] argumentTypes, out ParameterExpression provider, out ParameterExpression argumentArray, out Expression factoryExpressionBody)
    {
        FindApplicableConstructor(instanceType, argumentTypes, out ConstructorInfo constructor, out int?[] parameterMap);

        provider = Expression.Parameter(typeof(IServiceProvider), "provider");
        argumentArray = Expression.Parameter(typeof(object[]), "argumentArray");
        factoryExpressionBody = BuildFactoryExpression(constructor, parameterMap, provider, argumentArray);
    }

    /// <summary>
    /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type to activate</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies</param>
    /// <param name="parameters">Constructor arguments not provided by the <paramref name="provider"/>.</param>
    /// <returns>An activated object of type T</returns>
    public static T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(IServiceProvider provider, params object[] parameters)
    {
        return (T)CreateInstance(provider, typeof(T), parameters);
    }

    /// <summary>
    /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
    /// </summary>
    /// <typeparam name="T">The type of the service</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies</param>
    /// <returns>The resolved service or created instance</returns>
    public static T GetServiceOrCreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(IServiceProvider provider)
    {
        return (T)GetServiceOrCreateInstance(provider, typeof(T));
    }

    /// <summary>
    /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
    /// </summary>
    /// <param name="provider">The service provider</param>
    /// <param name="type">The type of the service</param>
    /// <returns>The resolved service or created instance</returns>
    public static object GetServiceOrCreateInstance(
        IServiceProvider provider,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        return provider.GetService(type) ?? CreateInstance(provider, type);
    }

    private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
    {
        var mc = (MethodCallExpression)expr.Body;
        return mc.Method;
    }

    private static object? GetService(IServiceProvider sp, Type type, Type requiredBy, bool isDefaultParameterRequired)
    {
        object? service = sp.GetService(type);
        if (service == null && !isDefaultParameterRequired)
        {
            // todo throw new InvalidOperationException(SR.Format(SR.UnableToResolveService, type, requiredBy));
            throw new InvalidOperationException();
        }
        return service;
    }

    private static Expression BuildFactoryExpression(
        ConstructorInfo constructor,
        int?[] parameterMap,
        Expression serviceProvider,
        Expression factoryArgumentArray)
    {
        ParameterInfo[]? constructorParameters = constructor.GetParameters();
        var constructorArguments = new Expression[constructorParameters.Length];

        for (int i = 0; i < constructorParameters.Length; i++)
        {
            ParameterInfo? constructorParameter = constructorParameters[i];
            Type? parameterType = constructorParameter.ParameterType;
            bool hasDefaultValue = ParameterDefaultValue.TryGetDefaultValue(constructorParameter, out object? defaultValue);

            if (parameterMap[i] != null)
            {
                constructorArguments[i] = Expression.ArrayAccess(factoryArgumentArray, Expression.Constant(parameterMap[i]));
            }
            else
            {
                var parameterTypeExpression = new[] { serviceProvider,
                    Expression.Constant(parameterType, typeof(Type)),
                    Expression.Constant(constructor.DeclaringType, typeof(Type)),
                    Expression.Constant(hasDefaultValue) };
                constructorArguments[i] = Expression.Call(GetServiceInfo, parameterTypeExpression);
            }

            // Support optional constructor arguments by passing in the default value
            // when the argument would otherwise be null.
            if (hasDefaultValue)
            {
                ConstantExpression? defaultValueExpression = Expression.Constant(defaultValue);
                constructorArguments[i] = Expression.Coalesce(constructorArguments[i], defaultValueExpression);
            }

            constructorArguments[i] = Expression.Convert(constructorArguments[i], parameterType);
        }

        return Expression.New(constructor, constructorArguments);
    }

    private static void FindApplicableConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
        Type?[] argumentTypes,
        out ConstructorInfo matchingConstructor,
        out int?[] matchingParameterMap)
    {
        ConstructorInfo? constructorInfo = null;
        int?[]? parameterMap = null;

        if (!TryFindPreferredConstructor(instanceType, argumentTypes, ref constructorInfo, ref parameterMap) &&
            !TryFindMatchingConstructor(instanceType, argumentTypes, ref constructorInfo, ref parameterMap))
        {
            // todo throw new InvalidOperationException(SR.Format(SR.CtorNotLocated, instanceType));
            throw new InvalidOperationException();
        }

        matchingConstructor = constructorInfo;
        matchingParameterMap = parameterMap;
    }

    // Tries to find constructor based on provided argument types
    private static bool TryFindMatchingConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
        Type?[] argumentTypes,
        [NotNullWhen(true)] ref ConstructorInfo? matchingConstructor,
        [NotNullWhen(true)] ref int?[]? parameterMap)
    {
        foreach (ConstructorInfo? constructor in instanceType.GetConstructors())
        {
            if (TryCreateParameterMap(constructor.GetParameters(), argumentTypes, out int?[] tempParameterMap))
            {
                if (matchingConstructor != null)
                {
                    // todo throw new InvalidOperationException(SR.Format(SR.MultipleCtorsFound, instanceType));
                    throw new InvalidOperationException();
                }

                matchingConstructor = constructor;
                parameterMap = tempParameterMap;
            }
        }

        if (matchingConstructor != null)
        {
            Debug.Assert(parameterMap != null);
            return true;
        }

        return false;
    }

    // Tries to find constructor marked with ActivatorUtilitiesConstructorAttribute
    private static bool TryFindPreferredConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
        Type?[] argumentTypes,
        [NotNullWhen(true)] ref ConstructorInfo? matchingConstructor,
        [NotNullWhen(true)] ref int?[]? parameterMap)
    {
        bool seenPreferred = false;
        foreach (ConstructorInfo? constructor in instanceType.GetConstructors())
        {
            if (constructor.IsDefined(typeof(IocUtilitiesConstructorAttribute), false))
            {
                if (seenPreferred)
                {
                    ThrowMultipleCtorsMarkedWithAttributeException();
                }

                if (!TryCreateParameterMap(constructor.GetParameters(), argumentTypes, out int?[] tempParameterMap))
                {
                    ThrowMarkedCtorDoesNotTakeAllProvidedArguments();
                }

                matchingConstructor = constructor;
                parameterMap = tempParameterMap;
                seenPreferred = true;
            }
        }

        if (matchingConstructor != null)
        {
            Debug.Assert(parameterMap != null);
            return true;
        }

        return false;
    }

    // Creates an injective parameterMap from givenParameterTypes to assignable constructorParameters.
    // Returns true if each given parameter type is assignable to a unique; otherwise, false.
    private static bool TryCreateParameterMap(ParameterInfo[] constructorParameters, Type?[] argumentTypes, out int?[] parameterMap)
    {
        parameterMap = new int?[constructorParameters.Length];

        for (int i = 0; i < argumentTypes.Length; i++)
        {
            bool foundMatch = false;
            Type? givenParameter = argumentTypes[i];

            for (int j = 0; j < constructorParameters.Length; j++)
            {
                if (parameterMap[j] != null)
                {
                    // This ctor parameter has already been matched
                    continue;
                }

                if (constructorParameters[j].ParameterType.IsAssignableFrom(givenParameter))
                {
                    foundMatch = true;
                    parameterMap[j] = i;
                    break;
                }
            }

            if (!foundMatch)
            {
                return false;
            }
        }

        return true;
    }

    private struct ConstructorMatcher
    {
        private readonly ConstructorInfo _constructor;
        private readonly ParameterInfo[] _parameters;
        private readonly object?[] _parameterValues;

        public ConstructorMatcher(ConstructorInfo constructor)
        {
            _constructor = constructor;
            _parameters = _constructor.GetParameters();
            _parameterValues = new object?[_parameters.Length];
        }

        public int Match(object[] givenParameters, IIocProviderIsService iocProviderIsService)
        {
            for (int givenIndex = 0; givenIndex < givenParameters.Length; givenIndex++)
            {
                Type? givenType = givenParameters[givenIndex]?.GetType();
                bool givenMatched = false;

                for (int applyIndex = 0; applyIndex < _parameters.Length; applyIndex++)
                {
                    if (_parameterValues[applyIndex] == null &&
                        _parameters[applyIndex].ParameterType.IsAssignableFrom(givenType))
                    {
                        givenMatched = true;
                        _parameterValues[applyIndex] = givenParameters[givenIndex];
                        break;
                    }
                }

                if (!givenMatched)
                {
                    return -1;
                }
            }

            // confirms the rest of ctor arguments match either as a parameter with a default value or as a service registered
            for (int i = 0; i < _parameters.Length; i++)
            {
                if (_parameterValues[i] == null &&
                    !iocProviderIsService.IsService(_parameters[i].ParameterType))
                {
                    if (ParameterDefaultValue.TryGetDefaultValue(_parameters[i], out object? defaultValue))
                    {
                        _parameterValues[i] = defaultValue;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }

            return _parameters.Length;
        }

        public object CreateInstance(IServiceProvider provider)
        {
            for (int index = 0; index != _parameters.Length; index++)
            {
                if (_parameterValues[index] != null) continue;
                object? value = provider.GetService(_parameters[index].ParameterType);
                if (value == null)
                {
                    if (!ParameterDefaultValue.TryGetDefaultValue(_parameters[index], out object? defaultValue))
                    {
                        // todo throw new InvalidOperationException(SR.Format(SR.UnableToResolveService, _parameters[index].ParameterType, _constructor.DeclaringType));
                        throw new InvalidOperationException();
                    }

                    _parameterValues[index] = defaultValue;
                }
                else
                {
                    _parameterValues[index] = value;
                }
            }

#if NETFRAMEWORK || NETSTANDARD2_0
            try
            {
                return _constructor.Invoke(_parameterValues);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                // The above line will always throw, but the compiler requires we throw explicitly.
                throw;
            }
#else
            return _constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: _parameterValues, culture: null);
#endif
        }

        public void MapParameters(int?[] parameterMap, object[] givenParameters)
        {
            for (int i = 0; i < _parameters.Length; i++)
            {
                if (parameterMap[i] != null)
                {
                    _parameterValues[i] = givenParameters[(int)parameterMap[i]!];
                }
            }
        }
    }

    private static void ThrowMultipleCtorsMarkedWithAttributeException()
    {
        // todo throw new InvalidOperationException(SR.Format(SR.MultipleCtorsMarkedWithAttribute, nameof(ActivatorUtilitiesConstructorAttribute)));
        throw new InvalidOperationException();
    }

    private static void ThrowMarkedCtorDoesNotTakeAllProvidedArguments()
    {
        // todo throw new InvalidOperationException(SR.Format(SR.MarkedCtorMissingArgumentTypes, nameof(ActivatorUtilitiesConstructorAttribute)));
        throw new InvalidOperationException();
    }
}


/// <summary>
/// Marks the constructor to be used when activating type using <see cref="IocUtilities"/>.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class IocUtilitiesConstructorAttribute : Attribute
{
}

// later todo for MS DI compatibility proj register IServiceProviderIsService -> IIocProviderIsService
/// <summary>
/// Optional service used to determine if the specified type is available from the <see cref="IServiceProvider"/>.
/// </summary>
public interface IIocProviderIsService
{
    /// <summary>
    /// Determines if the specified service type is available from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="serviceType">An object that specifies the type of service object to test.</param>
    /// <returns>true if the specified service is a available, false if it is not.</returns>
    bool IsService(Type serviceType);
}

//

internal static partial class ParameterDefaultValue
{
    public static bool TryGetDefaultValue(ParameterInfo parameter, out object? defaultValue)
    {
        bool hasDefaultValue = CheckHasDefaultValue(parameter, out bool tryToGetDefaultValue);
        defaultValue = null;

        if (hasDefaultValue)
        {
            if (tryToGetDefaultValue)
            {
                defaultValue = parameter.DefaultValue;
            }

            bool isNullableParameterType = parameter.ParameterType.IsGenericType &&
                parameter.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>);

            // Workaround for https://github.com/dotnet/runtime/issues/18599
            if (defaultValue == null && parameter.ParameterType.IsValueType
                && !isNullableParameterType) // Nullable types should be left null
            {
                defaultValue = CreateValueType(parameter.ParameterType);
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
                Justification = "CreateValueType is only called on a ValueType. You can always create an instance of a ValueType.")]
            static object? CreateValueType(Type t) =>
#if NETFRAMEWORK || NETSTANDARD2_0
                FormatterServices.GetUninitializedObject(t);
#else
                RuntimeHelpers.GetUninitializedObject(t);
#endif

            // Handle nullable enums
            if (defaultValue != null && isNullableParameterType)
            {
                Type? underlyingType = Nullable.GetUnderlyingType(parameter.ParameterType);
                if (underlyingType != null && underlyingType.IsEnum)
                {
                    defaultValue = Enum.ToObject(underlyingType, defaultValue);
                }
            }
        }

        return hasDefaultValue;
    }
}

#if NETFRAMEWORK || NETSTANDARD2_0
internal static partial class ParameterDefaultValue
{
    public static bool CheckHasDefaultValue(ParameterInfo parameter, out bool tryToGetDefaultValue)
    {
        tryToGetDefaultValue = true;
        try
        {
            return parameter.HasDefaultValue;
        }
        catch (FormatException) when (parameter.ParameterType == typeof(DateTime))
        {
            // Workaround for https://github.com/dotnet/runtime/issues/18844
            // If HasDefaultValue throws FormatException for DateTime
            // we expect it to have default value
            tryToGetDefaultValue = false;
            return true;
        }
    }
}
#else
internal static partial class ParameterDefaultValue
{
    public static bool CheckHasDefaultValue(ParameterInfo parameter, out bool tryToGetDefaultValue)
    {
        tryToGetDefaultValue = true;
        return parameter.HasDefaultValue;
    }
}
#endif