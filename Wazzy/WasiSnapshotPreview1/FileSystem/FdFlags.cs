namespace Wazzy.WasiSnapshotPreview1.FileSystem;

[Flags]
public enum FdFlags
    : ushort
{
    None = 0,

    /// <summary>Append mode: Data written to the file is always appended to the file's end</summary>
    Append = 1,

    /// <summary>Write according to synchronized I/O data integrity completion. Only the data stored in the file is synchronized</summary>
    DSync = 2,

    /// <summary>Non-blocking mode</summary>
    NonBlock = 4,

    /// <summary>Synchronized read I/O operations</summary>
    RSync = 8,

    /// <summary>Write according to synchronized I/O file integrity completion. In
    /// addition to synchronizing the data stored in the file, the implementation
    /// may also synchronously update the file's metadata.
    /// </summary>
    Sync = 16
}
