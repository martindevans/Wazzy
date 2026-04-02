using Wasmtime;
using Wazzy.Async;
using Wazzy.Async.Extensions;

namespace Wazzy.Tests;

[TestClass]
public sealed class MultiMemoryAsyncYieldTests
    : IDisposable
{
    private readonly WasmTestHelper _helper = new("Scripts/simple_merged_async.wasm");

    private readonly List<(int, string)> _printCalls = [ ];

    [TestInitialize]
    public void Init()
    {
        _helper.Linker.DefineFunction("spectest", "print", (Caller call, int arg) =>
        {
            // Get or restore locals
            (byte, long now) locals = call.GetSuspendedLocals<(byte, long)>()
                                   ?? (0, DateTime.UtcNow.Ticks);

            // Do some setup stuff. This happens on every pass!
            var hex = (locals.now & 0xFFFF).ToString("X");

            // Do the actual work, step by step
            switch (call.Resume(out var eState))
            {
                case 0:
                    _printCalls.Add((arg, hex));
                    Console.WriteLine($"Print Part 1: {arg} {hex}");
                    call.Suspend(locals, eState);
                    break;

                case 1:
                    _printCalls.Add((arg, hex));
                    Console.WriteLine($"Print Part 2: {arg} {hex}");
                    call.Suspend(locals, eState);
                    break;

                case 2:
                    _printCalls.Add((arg, hex));
                    Console.WriteLine($"Print Part 3: {arg} {hex}");
                    break;

                default:
                    throw new BadExecutionStateException(eState, "spectest.print");
            }
        });

        _helper.Linker.DefineFunction("whatever", "double", (Caller call, int arg) =>
        {
            // Do the actual work, step by step
            switch (call.Resume(out var eState))
            {
                case 0:
                    call.Suspend(eState);
                    return 0;

                case 1:
                    return arg * 2;

                default:
                    throw new BadExecutionStateException(eState, "whatever.double");
            }
        });
    }

    public void Dispose()
    {
        _helper.Dispose();
    }

    [TestMethod]
    public void IsAsyncCapable()
    {
        var instance = _helper.Instantiate();

        Assert.IsTrue(instance.IsAsyncCapable());
    }

    [TestMethod]
    public void BasicState()
    {
        var instance = _helper.Instantiate();

        Assert.AreEqual(AsyncState.None, instance.GetAsyncState());
    }

    [TestMethod]
    public void SimpleAsyncCall()
    {
        var instance = _helper.Instantiate();

        var call = instance.GetFunction<int, int>("run")!;
        var result = call(10);

        while (instance.GetAsyncState() == AsyncState.Suspending)
        {
            var stack = instance.StopUnwind();
            instance.StartRewind(stack);

            result = call(default);
        }

        // The final result should be the initial input
        Assert.AreEqual(10, result);

        // The wasm code calls "print" 3 times (with input, 22 and 33) and the args each time.
        // Each call should print the value and the time 3 times
        Assert.AreEqual(9, _printCalls.Count);

        Assert.AreEqual(10, _printCalls[0].Item1);
        Assert.AreEqual(10, _printCalls[1].Item1);
        Assert.AreEqual(10, _printCalls[2].Item1);

        Assert.AreEqual(22, _printCalls[3].Item1);
        Assert.AreEqual(22, _printCalls[4].Item1);
        Assert.AreEqual(22, _printCalls[5].Item1);

        Assert.AreEqual(33, _printCalls[6].Item1);
        Assert.AreEqual(33, _printCalls[7].Item1);
        Assert.AreEqual(33, _printCalls[8].Item1);
    }
}