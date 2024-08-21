namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

public interface IFile
    : IFilesystemEntry
{
    bool IsReadable { get; }

    bool IsWritable { get; }

    IFileHandle Open(FdFlags flags);

    void MoveTo(Stream stream, ulong timestamp);
}