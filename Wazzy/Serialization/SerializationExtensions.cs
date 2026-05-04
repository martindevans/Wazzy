using Wasmtime;

namespace Wazzy.Serialization;

internal static class SerializationExtensions
{
    public static void WriteV128(this BinaryWriter writer, V128 value)
    {
        writer.Write(value.AsSpan());
    }

    public static V128 ReadV128(this BinaryReader reader)
    {
        return new V128(
            reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(),
            reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(),
            reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(),
            reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()
        );
    }
}