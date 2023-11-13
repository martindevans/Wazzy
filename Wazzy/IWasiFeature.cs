using Wasmtime;

namespace Wazzy;

public interface IWasiFeature
{
    /// <summary>
    /// Add the functions of this feature to the given linker
    /// </summary>
    /// <param name="linker"></param>
    void DefineOn(Linker linker);
}