namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum Advice
    : byte
{
    /// <summary>The application has no advice to give on its behavior with respect to the specified data.</summary>
    Normal,

    /// <summary>The application expects to access the specified data sequentially from lower offsets to higher offsets.</summary>
    Sequential,

    /// <summary>The application expects to access the specified data in a random order.</summary>
    Random,

    /// <summary>The application expects to access the specified data in the near future.</summary>
    WillNeed,

    /// <summary>The application expects that it will not access the specified data in the near future.</summary>
    DontNeed,

    /// <summary>The application expects to access the specified data once and then not reuse it thereafter.</summary>
    NoReuse
}