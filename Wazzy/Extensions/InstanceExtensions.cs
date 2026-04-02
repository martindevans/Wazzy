using System.IO.Compression;
using System.Text;
using Wasmtime;

namespace Wazzy.Extensions;

public static class InstanceExtensions
{
    /// <summary>
    /// Any function prefixed with this magic GUID will be invoked as part of serialization.
    /// </summary>
    internal const string SerializationGuid = "ef3acb7f_45ef_4647_8e3b_44a66d273cac";

    /// <summary>
    /// Write the complete state of this instance to the given stream
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="output"></param>
    public static void Freeze(this Instance instance, Stream output)
    {
        using var compression = new GZipStream(output, CompressionLevel.Optimal, true);
        using var writer = new BinaryWriter(compression, Encoding.UTF8, true);

        var writers = (from func in instance.GetFunctions()
                       where func.Name.StartsWith(SerializationGuid)
                       select (func.Name, Func: func.Function.WrapAction<BinaryWriter>())).ToArray();

        // Write file header
        writer.Write("SerializedInstance");

        // Write out memories
        foreach (var (name, memory) in instance.GetMemories())
        {
            writer.Write((int)SerializationSections.Memory);
            SerializeMemory(memory, name, writer);
            writer.Write((int)SerializationSections.EndMemory);
        }

        // Write out tables
        foreach (var (name, table) in instance.GetTables())
        {
            writer.Write((int)SerializationSections.Table);
            //SerializeTable(table, name, writer);
            throw new NotImplementedException("serialize table");
            writer.Write((int)SerializationSections.EndTable);
        }
        
        // Write out globals
        foreach (var (name, global) in instance.GetGlobals())
        {
            // Can't write immutable globals, so no need to save them
            if (global.Mutability == Mutability.Immutable)
                continue;

            writer.Write((int)SerializationSections.Global);
            SerializeGlobal(global, name, writer);
            writer.Write((int)SerializationSections.EndGlobal);
        }

        // Write out func serializers
        foreach (var action in writers)
        {
            writer.Write((int)SerializationSections.SerializerFunc);
            writer.Write(action.Name);
            action.Func.Invoke(writer);
            writer.Write((int)SerializationSections.EndSerializer);
        }

        // Write end-of-file marker
        writer.Write((int)SerializationSections.EndOfFile);
    }

    /// <summary>
    /// Restore an instance that was saved with <see cref="Freeze"/> from the given stream
    /// </summary>
    /// <param name="module"></param>
    /// <param name="store"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Instance Thaw(this Module module, Store store, Stream input)
    {
        using var compression = new GZipStream(input, CompressionMode.Decompress, true);
        using var reader = new BinaryReader(compression, Encoding.UTF8, true);

        // Check file header. This could be extended to support versioning
        // by storing other string with version info embedded.
        if (reader.ReadString() != "SerializedInstance")
            throw new ArgumentException("File header is incorrect", nameof(input));

        var memories = new List<(string, Memory)>();
        var globals = new List<(string, Global)>();
        while (true)
        {
            var header = (SerializationSections)reader.ReadInt32();
            if (header == SerializationSections.EndOfFile)
                break;

            switch (header)
            {
                case SerializationSections.Memory:
                    memories.Add(DeserializeMemory(store, reader));
                    CheckSection(reader, SerializationSections.EndMemory);
                    break;
                case SerializationSections.Table:
                    throw new NotImplementedException("Table");
                    break;
                case SerializationSections.Global:
                    globals.Add(DeserializeGlobal(store, reader));
                    CheckSection(reader, SerializationSections.EndGlobal);
                    break;
                case SerializationSections.SerializerFunc:
                    throw new NotImplementedException("SerializerFunc");
                    break;

                case SerializationSections.EndMemory:
                case SerializationSections.EndTable:
                case SerializationSections.EndGlobal:
                case SerializationSections.EndSerializer:
                    throw new ArgumentException("Encountered unexpected end-of-section while not in a section", nameof(input));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // todo: the order needs to be right!
        return new Instance(
            store,
            module,
            [
                ..memories,
                ..globals
            ]
        );

        throw new NotImplementedException("create an instance from what we read");

        static void CheckSection(BinaryReader reader, SerializationSections expected)
        {
            var header = (SerializationSections)reader.ReadInt32();
            if (header != expected)
                throw new ArgumentException("Encountered unexpected end-of-section while not in a section", nameof(input));
        }
    }

    internal static void SerializeMemory(this Memory memory, string name, BinaryWriter writer)
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

    internal static (string, Memory) DeserializeMemory(Store store, BinaryReader reader)
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
        {
            throw new InvalidOperationException($"Checksum {expectedChecksum} != {checksum}");
        }

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

    internal static void SerializeGlobal(Global global, string name, BinaryWriter writer)
    {
        writer.Write(name);
        writer.Write((byte)global.Kind);

        switch (global.Kind)
        {
            case ValueKind.Int32:
                writer.Write(global.Wrap<int>()!.GetValue());
                break;

            case ValueKind.Int64:
                writer.Write(global.Wrap<long>()!.GetValue());
                break;

            case ValueKind.Float32:
                writer.Write(global.Wrap<float>()!.GetValue());
                break;

            case ValueKind.Float64:
                writer.Write(global.Wrap<double>()!.GetValue());
                break;

            case ValueKind.V128:
                var v128 = global.Wrap<V128>()!.GetValue().AsSpan();
                for (var i = 0; i < v128.Length; i++)
                    writer.Write(v128[i]);
                break;

            case ValueKind.FuncRef:
                throw new NotSupportedException("Cannot serialise FuncRef");

            case ValueKind.ExternRef:
                throw new NotSupportedException("Cannot serialise ExternRef");

            case ValueKind.AnyRef:
                throw new NotSupportedException("Cannot serialise AnyRef");

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    internal static (string, Global) DeserializeGlobal(Store store, BinaryReader reader)
    {
        var name = reader.ReadString();
        var kind = (ValueKind)reader.ReadByte();

        var value = kind switch
        {
            ValueKind.Int32 => (object)reader.ReadInt32(),
            ValueKind.Int64 => reader.ReadInt64(),
            ValueKind.Float32 => reader.ReadSingle(),
            ValueKind.Float64 => reader.ReadDouble(),
            ValueKind.V128 => reader.ReadV128(),
            ValueKind.FuncRef => throw new NotSupportedException(),
            ValueKind.ExternRef => throw new NotSupportedException(),
            ValueKind.AnyRef => throw new NotSupportedException(),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var global = new Global(store, kind, value, Mutability.Mutable);

        return (name, global);
    }

    private static V128 ReadV128(this BinaryReader reader)
    {
        var v128 = default(V128);
        var span = v128.AsSpan();
        for (var i = 0; i < 16; i++)
            span[i] = reader.ReadByte();

        return v128;
    }

    private enum SerializationSections
    {
        Memory = 1,
        EndMemory = -Memory,

        Table = 2,
        EndTable = -Table,

        Global = 3,
        EndGlobal = -Global,

        SerializerFunc = 4,
        EndSerializer = -SerializerFunc,

        EndOfFile = int.MaxValue,
    }
}