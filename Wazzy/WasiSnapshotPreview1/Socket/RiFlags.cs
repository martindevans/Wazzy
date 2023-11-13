namespace Wazzy.WasiSnapshotPreview1.Socket;

[Flags]
public enum RiFlags
    : ushort
{
    /// <summary>
    /// Returns the message without removing it from the socket's receive queue.
    /// </summary>
    Peek = 1,

    /// <summary>
    /// On byte-stream sockets, block until the full amount of data can be returned.
    /// </summary>
    WaitAll = 2,
}