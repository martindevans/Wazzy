using Wasmtime;
using Wazzy.Interop;

namespace Wazzy.WasiSnapshotPreview1.Environment;

/// <summary>
/// Low level interface for WASI environment related functions. This is a direct translation of the low level WASI API.
/// </summary>
public abstract class BaseWasiEnvironment
    : IWasiFeature
{
    /// <summary>
    /// The module which the exports of this feature are defined in
    /// </summary>
    public static readonly string Module = "wasi_snapshot_preview1";

    /// <summary>
    /// Get the size of the environment variable data
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="argNum">Number of environment data items</param>
    /// <param name="dataLen">Total length of all environment data items. Each individual item must be encoded into UTF8 and null terminated. e.g. `Key=Value\0`</param>
    /// <returns></returns>
    protected abstract WasiError EnvironGetSizes(Caller caller, out uint argNum, out uint dataLen);

    /// <summary>
    /// Get the environment data
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="environ">Write out pointers to the start of each item into this buffer</param>
    /// <param name="environBuffer">Write out all of the environment data into this buffer (UTF8 encoded, null terminated. e.g. `Key=Value\0`)</param>
    /// <returns></returns>
    protected abstract WasiError EnvironGet(Caller caller, ReadonlyPointer<Pointer<uint>> environ, Pointer<byte> environBuffer);

    /// <summary>
    /// Get the size of the argument variable data
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="argNum">Number of args</param>
    /// <param name="dataLen">Total length of all args items. Each individual item must be encoded into UTF8 and null terminated. e.g. `--foo\0`</param>
    /// <returns></returns>
    protected abstract WasiError ArgsGetSizes(Caller caller, out uint argNum, out uint dataLen);

    /// <summary>
    /// Get the args data
    /// </summary>
    /// <param name="caller">Context for this call</param>
    /// <param name="environ">Write out pointers to the start of each item into this buffer</param>
    /// <param name="environBuffer">Write out all of the arg data into this buffer (UTF8 encoded, null terminated. e.g. `--foo\0`)</param>
    /// <returns></returns>
    protected abstract WasiError ArgsGet(Caller caller, ReadonlyPointer<Pointer<uint>> environ, Pointer<byte> environBuffer);

    /// <inheritdoc />
    void IWasiFeature.DefineOn(Linker linker)
    {
        linker.DefineFunction(Module, "environ_get", (Caller c, int a, int b) => (int)EnvironGet(
            c,
            new ReadonlyPointer<Pointer<uint>>(a),
            new Pointer<byte>(b)
        ));

        linker.DefineFunction(Module, "environ_sizes_get", (Caller c, int a, int b) => (int)EnvironGetSizes(
            c,
            out new Pointer<uint>(a).Deref(c),
            out new Pointer<uint>(b).Deref(c)
        ));

        linker.DefineFunction(Module, "args_get", (Caller c, int a, int b) => (int)ArgsGet(
            c,
            new ReadonlyPointer<Pointer<uint>>(a),
            new Pointer<byte>(b)
        ));

        linker.DefineFunction(Module, "args_sizes_get", (Caller c, int a, int b) => (int)ArgsGetSizes(
            c,
            out new Pointer<uint>(a).Deref(c),
            out new Pointer<uint>(b).Deref(c)
        ));
    }
}