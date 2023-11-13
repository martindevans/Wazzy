namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum FileType
    : byte
{
    Unknown = 0,
    BlockDevice = 1,
    CharacterDevice = 2,
    Directory = 3,
    RegularFile = 4,
    DatagramSocket = 5,
    StreamSocket = 6,
    SymbolicLink = 7,
}