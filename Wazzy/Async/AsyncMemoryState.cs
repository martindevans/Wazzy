using Wasmtime;

namespace Wazzy.Async;

internal readonly struct AsyncMemoryState
{
    private const int BaseAddress = 16;
    private const int ExecutionStateAddr = BaseAddress;
    private const int LocalsSizeAddr = ExecutionStateAddr + 8;
    private const int AsyncStackStructAddr = LocalsSizeAddr + 8;
    private const int LocalsDataAddr = AsyncStackStructAddr + 16;

    private readonly int _address;
    private readonly int _size;

    public AsyncMemoryState(int address, int size)
    {
        _size = size;
        _address = Align8(address);
    }

    private static int Align8(int ptr)
    {
        return ptr + 7 & -8;
    }

    public void WriteExecutionStateNumber(Memory memory, int executionState)
    {
        memory.WriteInt32(_address + ExecutionStateAddr, executionState);
    }

    public int ReadExecutionStateNumber(Memory memory)
    {
        return memory.ReadInt32(_address + ExecutionStateAddr);
    }

    public void IncrementStateNumber(Memory memory)
    {
        var state = memory.ReadInt32(_address + ExecutionStateAddr);
        memory.WriteInt32(_address + ExecutionStateAddr, state + 1);
    }

    public int GetRewindStructAddress()
    {
        return _address + AsyncStackStructAddr;
    }

    private ref AsyncStackStruct32 GetAsyncStackStruct32(Memory memory)
    {
        unsafe
        {
            var ptr = memory.GetPointer() + GetRewindStructAddress();
            return ref *((AsyncStackStruct32*)ptr.ToPointer());
        }
    }

    private ref AsyncStackStruct64 GetAsyncStackStruct64(Memory memory)
    {
        unsafe
        {
            var ptr = memory.GetPointer() + GetRewindStructAddress();
            return ref *((AsyncStackStruct64*)ptr.ToPointer());
        }
    }

    public void WriteRewindStruct(Memory memory, int localsSize)
    {
        var start = _address + LocalsDataAddr + localsSize;

        // Set up rewind structure (start and end of asyncify stack)
        if (memory.Is64Bit)
        {
            ref var stackStruct = ref GetAsyncStackStruct64(memory);
            stackStruct.StackStart = start;
            stackStruct.StackEnd = Align8(_address + _size) - 8;
        }
        else
        {
            ref var stackStruct = ref GetAsyncStackStruct32(memory);
            stackStruct.StackStart = start;
            stackStruct.StackEnd = Align8(_address + _size) - 8;
        }
    }

    public void WriteLocals<T>(Memory memory, int localSize, T locals)
        where T : unmanaged
    {
        memory.WriteInt32(_address + LocalsSizeAddr, localSize);
        memory.Write(_address + LocalsDataAddr, locals);
    }

    public T ReadLocals<T>(Memory memory)
        where T : unmanaged
    {
        // Sanity check the size of the locals data
        var savedSize = memory.ReadInt32(_address + LocalsSizeAddr);
        int localSize;
        unsafe { localSize = sizeof(T); }
        if (savedSize != localSize)
            throw new InvalidOperationException($"Attempted to read locals data {typeof(T).Name}, but size does not match! Wrong type?");

        return memory.Read<T>(_address + LocalsDataAddr);
    }
}