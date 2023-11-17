using System.Text;
using Wasmtime;
using Wazzy.Interop;

namespace Wazzy.WasiSnapshotPreview1.Environment;

/// <summary>
/// Setup an environment for WASI with environment variables and command line args
/// </summary>
public class BasicEnvironment
    : BaseWasiEnvironment
{
    private readonly Dictionary<string, Memory<byte>> _envVars = new();
    private uint _envBytesCount;

    private readonly List<Memory<byte>> _args = new();
    private uint _argsBytesCount;

    /// <summary>
    /// Create a new virtual environment, optionally setting all arguments and environment variables
    /// </summary>
    /// <param name="env">Environment variables</param>
    /// <param name="args">Arguments</param>
    public BasicEnvironment(IReadOnlyDictionary<string, string>? env = null, IReadOnlyList<string>? args = null)
    {
        if (env != null)
            foreach (var (k, v) in env)
                SetEnvironmentVariable(k, v);

        SetArgs(args ?? Array.Empty<string>());
    }

    /// <summary>
    /// Set an environment variable to a value. Set to null to clear the value.
    /// </summary>
    /// <param name="key">Name of the environment variable.</param>
    /// <param name="value">Value of the environment variable, or null to clear it.</param>
    /// <returns></returns>
    public BasicEnvironment SetEnvironmentVariable(string key, string? value = null)
    {
        // Remove old value
        if (_envVars.TryGetValue(key, out var oldValue))
            _envBytesCount -= (uint)oldValue.Length;

        // Add new value if it is not null
        if (value != null)
        {
            var utf8Length = Encoding.UTF8.GetByteCount(key) + 1 + Encoding.UTF8.GetByteCount(value) + 1;
            var bytes = new byte[utf8Length];

            // Write out `Key=Value\0`
            var kLength = Encoding.UTF8.GetBytes(key, bytes);
            bytes[kLength] = 61;
            Encoding.UTF8.GetBytes(value, bytes.AsSpan()[(kLength + 1)..]);

            // Add new value
            _envVars[key] = bytes;
            _envBytesCount += (uint)bytes.Length;
        }

        return this;
    }

    /// <summary>
    /// Set the command line arguments for this virtual environment
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public BasicEnvironment SetArgs(params string[] args)
    {
        return SetArgs((IReadOnlyList<string>)args);
    }

    /// <summary>
    /// Set the command line arguments for this virtual environment
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public BasicEnvironment SetArgs(IReadOnlyList<string> args)
    {
        _args.Clear();
        _argsBytesCount = 0;

        foreach (var arg in args)
        {
            var count = Encoding.UTF8.GetByteCount(arg);
            var bytes = new byte[count + 1];
            Encoding.UTF8.GetBytes(arg, bytes);

            _argsBytesCount += (uint)bytes.Length;
            _args.Add(bytes);
        }

        return this;
    }

    protected override WasiError EnvironGetSizes(Caller caller, out uint argNum, out uint dataLen)
    {
        argNum = (uint)_envVars.Count;
        dataLen = _envBytesCount;
        return WasiError.SUCCESS;
    }

    protected override WasiError EnvironGet(Caller caller, ReadonlyPointer<Pointer<uint>> environ, Pointer<byte> environBuffer)
    {
        var addr = environBuffer.Addr;
        var environBufferSpan = new Buffer<byte>(environBuffer.Addr, _envBytesCount).GetSpan(caller);

        var environSpan = new Buffer<uint>(environ.Addr, (uint)_envVars.Count).GetSpan(caller);

        // Write out the env vars one by one to environBuffer, write a pointer to each item into environ
        foreach (var (_, value) in _envVars)
        {
            value.Span.CopyTo(environBufferSpan);
            environBufferSpan = environBufferSpan[value.Length..];

            environSpan[0] = (uint)addr;
            environSpan = environSpan[1..];

            addr += value.Length;
        }

        return WasiError.SUCCESS;
    }

    protected override WasiError ArgsGetSizes(Caller caller, out uint argNum, out uint dataLen)
    {
        argNum = (uint)_args.Count;
        dataLen = _argsBytesCount;
        return WasiError.SUCCESS;
    }

    protected override WasiError ArgsGet(Caller caller, ReadonlyPointer<Pointer<uint>> args, Pointer<byte> argsBuffer)
    {
        var addr = argsBuffer.Addr;
        var argsBufferSpan = new Buffer<byte>(argsBuffer.Addr, _argsBytesCount).GetSpan(caller);

        var argsSpan = new Buffer<uint>(args.Addr, (uint)_args.Count).GetSpan(caller);

        // Write out the args one by one to argsBuffer, write a pointer to each item into args
        foreach (var value in _args)
        {
            value.Span.CopyTo(argsBufferSpan);
            argsBufferSpan = argsBufferSpan[value.Length..];

            argsSpan[0] = (uint)addr;
            argsSpan = argsSpan[1..];

            addr += value.Length;
        }

        return WasiError.SUCCESS;
    }
}