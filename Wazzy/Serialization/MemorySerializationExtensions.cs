using Wasmtime;

namespace Wazzy.Serialization;

internal static class MemorySerializationExtensions
{
    internal static void WriteMemory(this BinaryWriter writer, Memory memory, string name)
    {
        writer.Write(name);
        writer.Write(memory.GetLength());
        writer.Write(memory.GetSize());
        writer.Write(memory.Minimum);
        writer.Write(memory.Maximum.HasValue);
        writer.Write(memory.Maximum ?? 0);
        writer.Write(memory.Is64Bit);

        // Write raw bytes
        var checksum = 0u;
        unsafe
        {
            var length = memory.GetLength();
            var ptr = (byte*)memory.GetPointer();
            for (long i = 0; i < length; i++)
            {
                var @byte = ptr[i];
                writer.Write(@byte);
                FNV_1A(@byte, ref checksum);
            }
        }

        // Write checksum
        writer.Write(checksum);
    }

    internal static (string, Memory) ReadMemory(this BinaryReader reader, Store store)
    {
        var name = reader.ReadString();
        var length = reader.ReadInt64();
        var size = reader.ReadInt64();
        var minimum = reader.ReadInt64();
        var hasMax = reader.ReadBoolean();
        var maximum = reader.ReadInt64();
        var is64Bit = reader.ReadBoolean();

        var memory = new Memory(store, minimum, hasMax ? maximum : null, is64Bit);
        memory.Grow(size);

        var checksum = 0u;
        unsafe
        {
            var ptr = (byte*)memory.GetPointer();
            for (long i = 0; i < length; i++)
            {
                var @byte = reader.ReadByte();
                ptr[i] = @byte;
                FNV_1A(@byte, ref checksum);
            }
        }

        var expectedChecksum = reader.ReadUInt32();
        if (expectedChecksum != checksum)
            throw new InvalidOperationException($"Checksum {expectedChecksum} != {checksum}");

        return (name, memory);
    }

    private static void FNV_1A(byte value, ref uint hash)
    {
        unchecked
        {
            if (hash == default)
                hash = 2166136261;

            hash ^= value;
            hash *= 16777619;
        }
    }
}