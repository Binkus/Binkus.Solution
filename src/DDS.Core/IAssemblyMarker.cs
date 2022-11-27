using System.Reflection;

namespace DDS.Core;

/// <summary>
/// This assembly marker is for assembly scanning
/// </summary>
public interface IAssemblyMarker
{
    public static Assembly Assembly => typeof(IAssemblyMarker).Assembly;
}