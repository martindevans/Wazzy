namespace Wazzy.WasiSnapshotPreview1.FileSystem;

[Flags]
public enum OpenFlags
{
    None = 0,

    /// <summary>Create file if it does not exist</summary>
    Create = 1,

    /// <summary>Fail if not a directory</summary>
    Directory = 2,

    /// <summary>Fail if file already exists</summary>
    Exclusive = 4,

    /// <summary>Truncate file to size 0</summary>
    Truncate = 8
}