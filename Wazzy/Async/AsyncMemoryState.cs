using Wasmtime;

namespace Wazzy.Async;

internal readonly struct AsyncMemoryState
{
    private const int BaseAddress = 16;
    private const int AsyncStackStructAddr = BaseAddress + 8;
    private const int StackStartAddr = AsyncStackStructAddr + 16;

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

    public void WriteRewindStruct(Memory memory)
    {
        // Set up rewind structure (start and end of asyncify stack)
        if (memory.Is64Bit)
        {
            ref var stackStruct = ref GetAsyncStackStruct64(memory);
            stackStruct.StackStart = StackStartAddr;
            stackStruct.StackEnd = Align8(_address + _size) - 8;
        }
        else
        {
            ref var stackStruct = ref GetAsyncStackStruct32(memory);
            stackStruct.StackStart = StackStartAddr;
            stackStruct.StackEnd = Align8(_address + _size) - 8;
        }
    }
}