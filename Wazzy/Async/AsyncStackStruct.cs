using System.Runtime.InteropServices;

namespace Wazzy.Async;

[StructLayout(LayoutKind.Explicit)]
internal struct AsyncStackStruct32
{
    [FieldOffset(0)] public int StackStart;
    [FieldOffset(4)] public int StackEnd;
}

[StructLayout(LayoutKind.Explicit)]
internal struct AsyncStackStruct64
{
    [FieldOffset(0)] public long StackStart;
    [FieldOffset(8)] public long StackEnd;
}