namespace Wazzy.WasiSnapshotPreview1.Socket;

[Flags]
public enum RoFlags
    : ushort
{
    /// <summary>
    /// Message data has been truncated.
    /// </summary>
    DataTruncated = 1,
}