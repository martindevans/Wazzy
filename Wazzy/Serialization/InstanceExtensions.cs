using System.IO.Compression;
using System.Text;
using Wasmtime;

namespace Wazzy.Serialization;

public static class InstanceExtensions
{
    /// <summary>
    /// Write the complete state of this instance to the given stream
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="output"></param>
    public static void Freeze(this Instance instance, Stream output)
    {
        using var compression = new GZipStream(output, CompressionLevel.Optimal, true);
        using var writer = new BinaryWriter(compression, Encoding.UTF8, true);

        // Write file header
        writer.Write("SerializedInstance");
        writer.Write((uint)1);

        // Write out memories
        foreach (var (name, memory) in instance.GetMemories())
        {
            writer.Write((int)SerializationSections.Memory);
            writer.WriteMemory(memory, name);
            writer.Write((int)SerializationSections.EndMemory);
        }

        // Write out tables
        foreach (var (name, table) in instance.GetTables())
        {
            writer.Write((int)SerializationSections.Table);
            writer.WriteTable(table, name);
            writer.Write((int)SerializationSections.EndTable);
        }

        // Write out globals
        foreach (var (name, global) in instance.GetGlobals())
        {
            // Can't write immutable globals, so no need to save them
            if (global.Mutability == Mutability.Immutable)
                continue;

            writer.Write((int)SerializationSections.Global);
            writer.WriteGlobal(global, name);
            writer.Write((int)SerializationSections.EndGlobal);
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
    public static Instance Thaw(this Module module, Store store, Linker linker, Stream input)
    {
        using var compression = new GZipStream(input, CompressionMode.Decompress, true);
        using var reader = new BinaryReader(compression, Encoding.UTF8, true);

        // Check file header
        if (reader.ReadString() != "SerializedInstance")
            throw new ArgumentException("File header is incorrect", nameof(input));
        
        // Check that we know how to deserialise the version
        var version = reader.ReadUInt32();
        if (version != 1)
            throw new NotSupportedException($"File version number {version} unknown");

        var memories = new List<(string, Memory)>();
        var globals = new List<(string, Global)>();
        var tables = new List<(string, Table)>();
        while (true)
        {
            var header = (SerializationSections)reader.ReadInt32();
            if (header == SerializationSections.EndOfFile)
                break;

            switch (header)
            {
                case SerializationSections.Memory:
                    memories.Add(reader.ReadMemory(store));
                    CheckSection(reader, SerializationSections.EndMemory);
                    break;
                case SerializationSections.Table:
                    tables.Add(reader.ReadTable());
                    CheckSection(reader, SerializationSections.EndTable);
                    break;
                case SerializationSections.Global:
                    globals.Add(reader.ReadGlobal(store));
                    CheckSection(reader, SerializationSections.EndGlobal);
                    break;

                case SerializationSections.EndMemory:
                case SerializationSections.EndTable:
                case SerializationSections.EndGlobal:
                    throw new ArgumentException("Encountered unexpected end-of-section while not in a section", nameof(input));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        // Configure linker
        foreach (var (name, global) in globals)
            linker.Define(module.Name, name, global);
        foreach (var (name, memory) in memories)
            linker.Define(module.Name, name, memory);
        foreach (var (name, table) in tables)
            linker.Define(module.Name, name, table);

        // Instantiate module
        return linker.Instantiate(store, module);

        static void CheckSection(BinaryReader reader, SerializationSections expected)
        {
            var header = (SerializationSections)reader.ReadInt32();
            if (header != expected)
                throw new ArgumentException("Encountered unexpected end-of-section while not in a section", nameof(input));
        }
    }

    private enum SerializationSections
    {
        Memory = 1,
        EndMemory = -Memory,

        Table = 2,
        EndTable = -Table,

        Global = 3,
        EndGlobal = -Global,

        EndOfFile = int.MaxValue,
    }
}