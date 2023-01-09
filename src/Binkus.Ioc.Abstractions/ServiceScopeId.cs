namespace Binkus.DependencyInjection;

public sealed record ServiceScopeId(Guid Id)
{
    public ServiceScopeId() : this(Guid.NewGuid()) { }
    
    public static readonly ServiceScopeId Empty = new(Guid.Empty); 
    public static implicit operator ServiceScopeId(Guid id) => new(id);
    public static implicit operator Guid(ServiceScopeId id) => id.Id;
}