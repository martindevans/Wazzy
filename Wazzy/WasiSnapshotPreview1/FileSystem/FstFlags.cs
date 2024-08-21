namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public readonly struct FstFlags(int bits)
{
    /// <summary>
    /// Adjust the last data access timestamp
    /// </summary>
    public bool AdjustAccessTime => (bits & 0b0001) != 0;

    /// <summary>
    /// Adjust the last data access timestamp to the time of clock
    /// </summary>
    public bool AdjustAccessTimeNow => (bits & 0b0010) != 0;

    /// <summary>
    /// Adjust the last data modify timestamp
    /// </summary>
    public bool AdjustModifyTime => (bits & 0b0100) != 0;

    /// <summary>
    /// Adjust the last data modify timestamp to the time of clock
    /// </summary>
    public bool AdjustModifyTimeNow => (bits & 0b1000) != 0;
}