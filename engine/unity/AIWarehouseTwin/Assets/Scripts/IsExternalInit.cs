// Polyfill: Unity Mono does not ship IsExternalInit; required for C# 9 init accessors.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
