using Wasmtime;

namespace Wazzy.Extensions;

/// <summary>
/// Extension methods for Wasmtime Linker
/// </summary>
public static class LinkerExtensions
{
    /// <summary>
    /// Define all the methods of an IWasiFeature on this linker
    /// </summary>
    /// <typeparam name="T">Type of WASI feature</typeparam>
    /// <param name="linker">Linker to define feature on</param>
    /// <param name="feature">Feature to define</param>
    /// <returns></returns>
    public static T DefineFeature<T>(this Linker linker, T feature)
        where T : IWasiFeature
    {
        feature.DefineOn(linker);
        return feature;
    }
}