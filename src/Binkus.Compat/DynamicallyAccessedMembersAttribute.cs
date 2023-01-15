#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0 || NETCOREAPP3_1 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472 || NET48

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Indicates that certain members on a specified <see cref="T:System.Type" /> are accessed dynamically, for example, through <see cref="N:System.Reflection" />.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, Inherited = false)]
public sealed class DynamicallyAccessedMembersAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" /> class with the specified member types.</summary>
    /// <param name="memberTypes">The types of the dynamically accessed members.</param>
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) => this.MemberTypes = memberTypes;

    /// <summary>Gets the <see cref="T:System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes" /> that specifies the type of dynamically accessed members.</summary>
    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

/// <summary>Specifies the types of members that are dynamically accessed.
/// This enumeration has a <see cref="T:System.FlagsAttribute" /> attribute that allows a bitwise combination of its member values.</summary>
[Flags]
public enum DynamicallyAccessedMemberTypes
{
    /// <summary>Specifies no members.</summary>
    None = 0,
    /// <summary>Specifies the default, parameterless public constructor.</summary>
    PublicParameterlessConstructor = 1,
    /// <summary>Specifies all public constructors.</summary>
    PublicConstructors = 3,
    /// <summary>Specifies all non-public constructors.</summary>
    NonPublicConstructors = 4,
    /// <summary>Specifies all public methods.</summary>
    PublicMethods = 8,
    /// <summary>Specifies all non-public methods.</summary>
    NonPublicMethods = 16, // 0x00000010
    /// <summary>Specifies all public fields.</summary>
    PublicFields = 32, // 0x00000020
    /// <summary>Specifies all non-public fields.</summary>
    NonPublicFields = 64, // 0x00000040
    /// <summary>Specifies all public nested types.</summary>
    PublicNestedTypes = 128, // 0x00000080
    /// <summary>Specifies all non-public nested types.</summary>
    NonPublicNestedTypes = 256, // 0x00000100
    /// <summary>Specifies all public properties.</summary>
    PublicProperties = 512, // 0x00000200
    /// <summary>Specifies all non-public properties.</summary>
    NonPublicProperties = 1024, // 0x00000400
    /// <summary>Specifies all public events.</summary>
    PublicEvents = 2048, // 0x00000800
    /// <summary>Specifies all non-public events.</summary>
    NonPublicEvents = 4096, // 0x00001000
    /// <summary>Specifies all interfaces implemented by the type.</summary>
    Interfaces = 8192, // 0x00002000
    /// <summary>Specifies all members.</summary>
    All = -1, // 0xFFFFFFFF
}

/// <summary>
/// Suppresses reporting of a specific rule violation, allowing multiple suppressions on a
/// single code artifact.
/// </summary>
/// <remarks>
/// <see cref="UnconditionalSuppressMessageAttribute"/> is different than
/// <see cref="SuppressMessageAttribute"/> in that it doesn't have a
/// <see cref="ConditionalAttribute"/>. So it is always preserved in the compiled assembly.
/// </remarks>
[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
#if SYSTEM_PRIVATE_CORELIB
public
#else
internal
#endif
sealed class UnconditionalSuppressMessageAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnconditionalSuppressMessageAttribute"/>
    /// class, specifying the category of the tool and the identifier for an analysis rule.
    /// </summary>
    /// <param name="category">The category for the attribute.</param>
    /// <param name="checkId">The identifier of the analysis rule the attribute applies to.</param>
    public UnconditionalSuppressMessageAttribute(string category, string checkId)
    {
        Category = category;
        CheckId = checkId;
    }

    /// <summary>
    /// Gets the category identifying the classification of the attribute.
    /// </summary>
    /// <remarks>
    /// The <see cref="Category"/> property describes the tool or tool analysis category
    /// for which a message suppression attribute applies.
    /// </remarks>
    public string Category { get; }

    /// <summary>
    /// Gets the identifier of the analysis tool rule to be suppressed.
    /// </summary>
    /// <remarks>
    /// Concatenated together, the <see cref="Category"/> and <see cref="CheckId"/>
    /// properties form a unique check identifier.
    /// </remarks>
    public string CheckId { get; }

    /// <summary>
    /// Gets or sets the scope of the code that is relevant for the attribute.
    /// </summary>
    /// <remarks>
    /// The Scope property is an optional argument that specifies the metadata scope for which
    /// the attribute is relevant.
    /// </remarks>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets a fully qualified path that represents the target of the attribute.
    /// </summary>
    /// <remarks>
    /// The <see cref="Target"/> property is an optional argument identifying the analysis target
    /// of the attribute. An example value is "System.IO.Stream.ctor():System.Void".
    /// Because it is fully qualified, it can be long, particularly for targets such as parameters.
    /// The analysis tool user interface should be capable of automatically formatting the parameter.
    /// </remarks>
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets an optional argument expanding on exclusion criteria.
    /// </summary>
    /// <remarks>
    /// The <see cref="MessageId "/> property is an optional argument that specifies additional
    /// exclusion where the literal metadata target is not sufficiently precise. For example,
    /// the <see cref="UnconditionalSuppressMessageAttribute"/> cannot be applied within a method,
    /// and it may be desirable to suppress a violation against a statement in the method that will
    /// give a rule violation, but not against all statements in the method.
    /// </remarks>
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the justification for suppressing the code analysis message.
    /// </summary>
    public string? Justification { get; set; }
}
#endif