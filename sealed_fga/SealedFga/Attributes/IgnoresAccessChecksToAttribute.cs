// We explicitly WANT to write into another namespace
// ReSharper disable once CheckNamespace

namespace System.Runtime.CompilerServices;

/// <summary>
///     This attribute is used to disable visibility checks for e.g., the "internal" modifier ON RUNTIME ONLY!
///     The attribute mimics an existing one in the <see cref="System.Runtime.CompilerServices" /> namespace.
///     Interestingly enough, although we are not able to use the original one, this imposter is honored the same.
///     See https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/.
///     Also, could be potentially used together with https://github.com/aelij/IgnoresAccessChecksToGenerator.
/// </summary>
/// <param name="assemblyName">The assembly's full name we want unrestricted access to.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class IgnoresAccessChecksToAttribute(string assemblyName) : Attribute {
    public string AssemblyName { get; } = assemblyName;
}
