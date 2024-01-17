using Wasmtime;
using Wazzy.Extensions;

namespace Wazzy.Tests;

public class WasmTestHelper
    : IDisposable
{
    private readonly List<IWasiFeature> _features = new();

    public Engine Engine { get; }
    public Module Module { get; }
    public Store Store { get; }
    public Linker Linker { get; }

    public WasmTestHelper(string path)
    {
        var wasm = path.EndsWith(".wasm");

        Engine = new Engine(new Config());
        Module = wasm
            ? Module.FromFile(Engine, path)
            : Module.FromTextFile(Engine, path);
        Store = new Store(Engine);
        Linker = new Linker(Engine);
        Linker.AllowShadowing = true;
    }

    public void Dispose()
    {
        Engine.Dispose();
        Module.Dispose();
        Store.Dispose();
        Linker.Dispose();

        foreach (var wasiFeature in _features)
            if (wasiFeature is IDisposable disposable)
                disposable.Dispose();
    }

    public T AddWasiFeature<T>(T feature)
        where T : IWasiFeature
    {
        _features.Add(feature);
        Linker.DefineFeature(feature);
        return feature;
    }

    public Instance Instantiate(int grow = 0)
    {
        var instance = Linker.Instantiate(Store, Module);

        if (grow > 0)
            instance.GetMemory("memory")!.Grow(grow);

        return instance;
    }
}