using Wasmtime;

namespace Wazzy.Serialization;

internal static class TableSerializationExtensions
{
    internal static void WriteTable(this BinaryWriter writer, Table table, string name)
    {
        writer.Write(name);
        writer.Write(table.GetSize());
        writer.Write(table.Minimum);
        writer.Write(table.Maximum);
        writer.Write((int)table.Kind);

        throw new CannotSerializeTableKind(table.Kind);
    }

    internal static (string, Table) ReadTable(this BinaryReader reader)
    {
        var name = reader.ReadString();
        var size = reader.ReadUInt64();
        var min = reader.ReadUInt32();
        var max = reader.ReadUInt32();
        var kind = (TableKind)reader.ReadInt32();

        throw new CannotSerializeTableKind(kind);
    }
}