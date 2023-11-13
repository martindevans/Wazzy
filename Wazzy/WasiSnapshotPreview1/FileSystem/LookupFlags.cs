namespace Wazzy.WasiSnapshotPreview1.FileSystem;

[Flags]
public enum LookupFlags
{
    None = 0,

    FollowSymlinks = 1 << 0,
}