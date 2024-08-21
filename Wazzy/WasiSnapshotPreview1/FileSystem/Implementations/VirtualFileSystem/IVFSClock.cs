namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem;

public interface IVFSClock
{
    ulong GetTime();

    ulong FromRealTime(DateTimeOffset time);

    DateTimeOffset ToRealTime(ulong time);
}