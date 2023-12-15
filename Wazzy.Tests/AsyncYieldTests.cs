using Wasmtime;
using Wazzy.Async;
using Wazzy.Async.Extensions;

namespace Wazzy.Tests;

[TestClass]
public sealed class AsyncYieldTests
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
    public void SimpleAsyncCall()
    {
        var instance = _helper.Instantiate();

        var call = instance.GetFunction<int, int>("run")!;
        var result = call(10);

        while (instance.GetAsyncState() == AsyncState.Suspending)
        {
            var stack = instance.StopUnwind();
            Console.WriteLine($"Unwind:{stack.UnwindTime.TotalMilliseconds}ms");
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
    public void NestedAsyncCall()
    {
        var instance = _helper.Instantiate();

        var call = instance.GetFunction<int, int>("run_double")!;
        var result = call(11);

        while (instance.GetAsyncState() == AsyncState.Suspending)
        {
            var stack = instance.StopUnwind();
            Console.WriteLine($"Unwind:{stack.UnwindTime.TotalMilliseconds}ms");
            instance.StartRewind(stack);

            result = call(default);
        }

        // The final result should be the initial input
        Assert.AreEqual(11, result);
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

        Console.WriteLine($"Unwind:{stack.UnwindTime.TotalMilliseconds}ms");

        // Resume once
        instance.StartRewind(stack);
        call(default);
        instance.StopUnwind();

        // Resume again, using the wrong stack
        Assert.ThrowsException<ObjectDisposedException>(() =>
        {
            instance.StartRewind(stack);
        });
    }

    [TestMethod]
    public void IllegalIncorrectLocalsType()
    {
        //Redefine "print" to get a `long` the first time it is suspended, then save a `long`, then next time it tries to load an `int`
        var counter = 0;
        _helper.Linker.DefineFunction("spectest", "print", (Caller call, int _) =>
        {
            if (counter == 0)
            {
                call.GetSuspendedLocals<long>();
                counter++;
            }
            else if (counter == 1)
            {
                call.GetSuspendedLocals<int>();
                counter++;
            }

            // Do the actual work, step by step
            switch (call.Resume(out var eState))
            {
                case 0:
                    call.Suspend(1L, eState);
                    break;

                default:
                    throw new BadExecutionStateException(eState, "spectest.print");
            }
        });

        var instance = _helper.Instantiate();

        // Call
        var call = instance.GetFunction<int, int>("run")!;
        call(10);

        // Catch unwind
        var stack = instance.StopUnwind();

        Console.WriteLine($"Unwind:{stack.UnwindTime.TotalMilliseconds}ms");

        // Resume
        instance.StartRewind(stack);

        // Now this should throw
        try
        {
            call(default);
        }
        catch (WasmtimeException ex)
        {
            Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidOperationException));
            return;
        }

        Assert.Fail();
    }

    [TestMethod]
    public void IllegalExecutionState()
    {
        //Redefine "print" to trigger a bad execution state
        _helper.Linker.DefineFunction("spectest", "print", (Caller call, int _) =>
        {
            switch (call.Resume(out var eState))
            {
                case 0:
                    call.Suspend(eState + 10); // feed in an invalid state
                    break;

                default:
                    throw new BadExecutionStateException(eState, "spectest.print");
            }
        });

        var instance = _helper.Instantiate();

        // Call
        var call = instance.GetFunction<int, int>("run")!;
        call(10);

        // Catch unwind
        var stack = instance.StopUnwind();

        Console.WriteLine($"Unwind:{stack.UnwindTime.TotalMilliseconds}ms");

        // Resume
        instance.StartRewind(stack);

        // Now this should throw
        try
        {
            call(default);
        }
        catch (WasmtimeException ex)
        {
            Assert.IsInstanceOfType(ex.InnerException, typeof(BadExecutionStateException));
            var bad = (BadExecutionStateException)ex.InnerException;
            Assert.AreEqual(11, bad.ExecutionState);
            Assert.AreEqual("spectest.print", bad.MethodName);
            return;
        }

        Assert.Fail();
    }
}