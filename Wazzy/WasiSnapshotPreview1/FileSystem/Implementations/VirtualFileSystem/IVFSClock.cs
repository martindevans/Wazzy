namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem;

public interface IVFSClock
{
    /// <summary>
    /// Get the time according to this clock
    /// </summary>
    /// <returns></returns>
    ulong GetTime();

    /// <summary>
    /// Map from a real time into the time according to this clock. Used for converting the time of mapped files.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    ulong FromRealTime(DateTimeOffset time);

    /// <summary>
    /// Map from the time according to this clock into real time.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    DateTimeOffset ToRealTime(ulong time);
}