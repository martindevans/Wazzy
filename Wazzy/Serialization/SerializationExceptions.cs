using Wasmtime;

namespace Wazzy.Serialization;

public class SerializationException
    : Exception
{
    protected SerializationException(string message)
        : base(message)
    {
    }
}

public sealed class IncorrectFileHeader
    : SerializationException
{
    /// <summary>
    /// The actual value read from the file header
    /// </summary>
    public uint Actual { get; }

    public IncorrectFileHeader(uint actual)
        : base("Incorrect file header")
    {
        Actual = actual;
    }
}

public sealed class UnknownVersionNumber
    : SerializationException
{
    public uint Version { get; }

    public UnknownVersionNumber(uint version)
        : base($"File version number {version} unknown")
    {
        Version = version;
    }
}

public sealed class UnknownFileSectionType
    : SerializationException
{
    internal UnknownFileSectionType(InstanceExtensions.SerializationSections header)
        : base($"Unknown file section: {header}")
    {
    }
}

public sealed class UnexpectedEndOfSection
    : SerializationException
{
    internal UnexpectedEndOfSection(InstanceExtensions.SerializationSections header)
        : base($"Encountered unexpected end-of-section '{header}' while not in a section (corrupt or invalid file)")
    {
        
    }
}

public sealed class CannotSerializeTableKind
    : SerializationException
{
    public TableKind Kind { get; }

    public CannotSerializeTableKind(TableKind kind)
        : base($"Cannot serialise {kind}")
    {
        Kind = kind;
    }
}

public sealed class CannotSerializeValueKind
    : SerializationException
{
    public ValueKind Kind { get; }

    public CannotSerializeValueKind(ValueKind kind)
        : base($"Cannot serialise {kind}")
    {
        Kind = kind;
    }
}