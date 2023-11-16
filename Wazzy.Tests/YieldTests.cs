using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Wasmtime;
using Wazzy.Async;
using Wazzy.Async.Extensions;

namespace Wazzy.Tests;

[TestClass]
public sealed class YieldTests
    : IDisposable
{
    private readonly WasmTestHelper _helper = new("Scripts/Simple_Async.wasm");

    private readonly List<(int, string)> _printCalls = new();

    [TestInitialize]
    public void Init()
    {
        _helper.Linker.DefineFunction("spectest", "print", (Caller call, int arg) =>
        {
            // Get or restore locals
            (byte, long now) locals = call.GetSuspendedLocals<(byte, long)>(out var eState)
                                   ?? (0, DateTime.UtcNow.Ticks);

            // Do some setup stuff. This happens on every pass!
            var hex = (locals.now & 0xFFFF).ToString("X");

            // Do the actual work, step by step
            switch (call.Resume())
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
                    throw new BadExecutionStateException(eState);
            }
        });

        _helper.Linker.DefineFunction("whatever", "double", (Caller call, int arg) =>
        {
            // Get or restore locals
            var locals = call.GetSuspendedLocals<(int, int, int)>(out var eState)
                      ?? (0, 0, 0);

            // Do the actual work, step by step
            switch (call.Resume())
            {
                case 0:
                    call.Suspend(locals, eState);
                    return 0;

                case 1:
                    return arg * 2;

                default:
                    throw new BadExecutionStateException(eState);
            }
        });
    }

    public void Dispose()
    {
        _helper.Dispose();
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

    [TestMethod]
    public void IllegalStopUnwind()
    {
        var instance = _helper.Instantiate();

        // illegal in current state
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            instance.StopUnwind();
        });
    }

    [TestMethod]
    public void IllegalNullStack()
    {
        var instance = _helper.Instantiate();

        Assert.ThrowsException<ArgumentException>(() =>
        {
            instance.StartRewind(default);
        });
    }

    [TestMethod]
    public void IllegalUseStackTwice()
    {
        var instance = _helper.Instantiate();

        // Start call and grab the stack
        var call = instance.GetFunction<int, int>("run")!;
        call(10);
        var stack = instance.StopUnwind();

        // Resume once
        instance.StartRewind(stack);
        call(default);
        var stack2 = instance.StopUnwind();

        // Resume again, using the wrong stack
        Assert.ThrowsException<ObjectDisposedException>(() =>
        {
            instance.StartRewind(stack);
        });
    }

    [TestMethod]
    public void IllegalExecutionState()
    {
        var instance = _helper.Instantiate();

        // Start call and grab the stack
        var call = instance.GetFunction<int, int>("run")!;
        call(10);
        var stack = instance.StopUnwind();

        // Do evil things to corrupt the execution state
        unsafe
        {
            fixed (byte* buffer = &stack.Value.GetPinnableReference())
            {
                buffer[16] = 9;
            }
        }

        // Resume, but now the execution state is corrupted so we should get an exception
        instance.StartRewind(stack);
        stack.Dispose();

        try
        {
            call(default);
        }
        catch (WasmtimeException ex)
        {
            Assert.IsTrue(ex.InnerException is BadExecutionStateException);
            return;
        }

        Assert.Fail();
    }
}