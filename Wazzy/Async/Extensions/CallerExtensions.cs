using Wasmtime;

namespace Wazzy.Async.Extensions;

public static class CallerExtensions
{
    internal static AsyncState GetAsyncState(this Caller caller, ref Func<int>? getter)
    {
        if (getter == null)
        {
            getter = caller.GetFunction("asyncify_get_state")?.WrapFunc<int>()
                ?? throw new InvalidOperationException("Cannot `GetAsyncState()` - instance is not async capable");
        }

        return (AsyncState)getter();
    }

    /// <summary>
    /// Get the current async state of the WASM instance behind this caller.
    /// </summary>
    /// <param name="caller"></param>
    /// <returns></returns>
    public static AsyncState GetAsyncState(this Caller caller)
    {
        Func<int>? getter = null;
        return GetAsyncState(caller, ref getter);
    }

    /// <summary>
    /// Check if the given caller is capable of async suspension/resumption
    /// </summary>
    /// <param name="caller"></param>
    /// <returns></returns>
    public static bool IsAsyncCapable(this Caller caller)
    {
        return caller.GetFunction("asyncify_start_unwind") != null
            && caller.GetFunction("asyncify_stop_unwind") != null
            && caller.GetFunction("asyncify_start_rewind") != null
            && caller.GetFunction("asyncify_stop_rewind") != null
            && caller.GetFunction("asyncify_get_state") != null;
    }

    /// <summary>
    /// Get memory to use for asyncify data.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="dedicated">Indicates if the special `asyncify_unwind_stack_memory_heap` dedicated memory was found (multi memory). If so
    /// then this memory is only used for asyncify work and can be freely used without worrying about other data stored in it.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal static Memory GetAsyncifyMemory(this Caller caller, out bool dedicated)
    {
        var mem = caller.GetMemory("asyncify_unwind_stack_memory_heap");
        if (mem != null)
        {
            dedicated = true;
            return mem;
        }

        mem = caller.GetMemory("memory");
        if (mem != null)
        {
            dedicated = false;
            return mem;
        }

        throw new InvalidOperationException("Cannot get exported memory");
    }

    internal static void AsyncifyStartUnwind(this Caller caller, int addr, ref Func<int>? getter)
    {
        caller.GetAsyncState(ref getter).AssertState(AsyncState.None);
        caller.GetFunction("asyncify_start_unwind")!.WrapAction<int>()!.Invoke(addr);


#if DEBUG
        caller.GetAsyncState(ref getter).AssertState(AsyncState.Suspending);
#endif
    }

    internal static void AsyncifyStopRewind(this Caller caller, ref Func<int>? getter)
    {
        caller.GetAsyncState(ref getter).AssertState(AsyncState.Resuming);
        caller.GetFunction("asyncify_stop_rewind")!.WrapAction()!.Invoke();

#if DEBUG
        caller.GetAsyncState(ref getter).AssertState(AsyncState.None);
#endif
    }

    /// <summary>
    /// Allocate a buffer of the given size.
    /// - If `asyncify_malloc_buffer(int32) -> int32` is defined, uses that
    /// - If `malloc(int32) -> int32` AND `free(int32)` are defined, uses malloc
    /// </summary>
    /// <remarks>It is allowed for `asyncify_free_buffer` to not exist, in which case freeing is a no-op</remarks>
    /// <param name="caller"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    internal static int? AsyncifyMallocBuffer(this Caller caller, int size)
    {
        // Check if the special purpose malloc exists and use it
        var async_malloc = caller.GetAsyncifyMalloc();
        if (async_malloc is not null)
        {
            var ptr = async_malloc(size);
            if (ptr < 0)
                return null;
            return ptr;
        }

        // Try to use general purpose malloc
        var mallocs = caller.GetMalloc();
        if (mallocs is (var malloc, not null))
        {
            var ptr = malloc(size);
            if (ptr <= 0)
                return null;
            return ptr;
        }

        // No malloc available
        return null;
    }

    /// <summary>
    /// Free a previously allocated buffer
    /// - If `asyncify_malloc_buffer(int32) -> int32` exists, use `asyncify_free_buffer(int32, int32)`
    /// - Otherwise, if `malloc(int32) -> int32` AND free(int32)` exist use `free`
    /// </summary>
    /// <remarks>It is allowed for `asyncify_free_buffer` to not exist, in which case freeing is a no-op</remarks>
    /// <param name="caller"></param>
    /// <param name="addr"></param>
    /// <param name="size"></param>
    internal static void AsyncifyFreeBuffer(this Caller caller, int addr, int size)
    {
        // Check if the special purpose malloc exists and use it
        var async_malloc = caller.GetAsyncifyMalloc();
        if (async_malloc is not null)
        {
            caller.GetAsyncifyFree()?.Invoke(addr, size);
            return;
        }

        // Try to use general purpose malloc/free
        var mallocs = caller.GetMalloc();
        if (mallocs is (not null, var free))
        {
            free(addr);
            return;
        }

        // No malloc available
    }

    /// <summary>
    /// Get the special asyncify malloc function
    /// </summary>
    /// <param name="caller"></param>
    /// <returns></returns>
    private static Func<int, int>? GetAsyncifyMalloc(this Caller caller)
    {
        return caller.GetFunction("asyncify_malloc_buffer")?.WrapFunc<int, int>();
    }

    /// <summary>
    /// Get the special asyncify free function
    /// </summary>
    /// <param name="caller"></param>
    /// <returns></returns>
    private static Action<int, int>? GetAsyncifyFree(this Caller caller)
    {
        return caller.GetFunction("asyncify_free_buffer")?.WrapAction<int, int>();
    }

    /// <summary>
    /// Get the general malloc functions
    /// </summary>
    /// <param name="caller"></param>
    /// <returns></returns>
    private static (Func<int, int> malloc, Action<int> free)? GetMalloc(this Caller caller)
    {
        var malloc = caller.GetFunction("malloc")?.WrapFunc<int, int>();
        var free = caller.GetFunction("free")?.WrapAction<int>();
        if (malloc != null && free != null)
            return (malloc, free);
        return null;
    }
}