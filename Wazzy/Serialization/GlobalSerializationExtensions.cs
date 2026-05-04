using Wasmtime;

namespace Wazzy.Serialization;

internal static class GlobalSerializationExtensions
{
    internal static void WriteGlobal(this BinaryWriter writer, Global global, string name)
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
                writer.WriteV128(global.Wrap<V128>()!.GetValue());
                break;

            case ValueKind.FuncRef:
            case ValueKind.ExternRef:
            case ValueKind.AnyRef:
            default:
                throw new CannotSerializeValueKind(global.Kind);
        }
    }

    internal static (string, Global) ReadGlobal(this BinaryReader reader, Store store)
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
            ValueKind.FuncRef => throw new CannotSerializeValueKind(kind),
            ValueKind.ExternRef => throw new CannotSerializeValueKind(kind),
            ValueKind.AnyRef => throw new CannotSerializeValueKind(kind),
            _ => throw new CannotSerializeValueKind(kind)
        };

        var global = new Global(store, kind, value, Mutability.Mutable);

        return (name, global);
    }
}