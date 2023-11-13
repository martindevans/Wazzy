namespace Wazzy.WasiSnapshotPreview1.Socket;

[Flags]
public enum SdFlags
    : ushort
{
    /// <summary>
    /// Disables further receive operations.
    /// </summary>
    ShutdownReceive = 1,

    /// <summary>
    /// Disables further send operations.
    /// </summary>
    ShutdownSend = 2,
}