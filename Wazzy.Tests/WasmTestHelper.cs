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
        Linker.Define(feature);
        return feature;
    }

    public Instance Instantiate()
    {
        var instance = Linker.Instantiate(Store, Module);
        return instance;
    }

    private class Wrapper
    {
        private readonly WasmTestHelper _helper;

        public Store Store => _helper.Store;
        public Instance Instance { get; }
        public Memory memory { get; }

        public Wrapper(WasmTestHelper helper, Instance instance)
        {
            _helper = helper;
            Instance = instance;
            memory = instance.GetMemory("memory")!;
        }
    }
}