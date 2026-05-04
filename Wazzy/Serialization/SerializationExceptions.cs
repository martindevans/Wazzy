using Wasmtime;

namespace Wazzy.Serialization;

public class SerializationException
    : Exception
{
    public SerializationException(string message)
        : base(message)
    {
    }
}

public class CannotSerializeTableKind
    : SerializationException
{
    public TableKind Kind { get; }

    public CannotSerializeTableKind(TableKind kind)
        : base($"Cannot serialise {kind}")
    {
        Kind = kind;
    }
}

public class CannotSerializeValueKind
    : SerializationException
{
    public ValueKind Kind { get; }

    public CannotSerializeValueKind(ValueKind kind)
        : base($"Cannot serialise {kind}")
    {
        Kind = kind;
    }
}