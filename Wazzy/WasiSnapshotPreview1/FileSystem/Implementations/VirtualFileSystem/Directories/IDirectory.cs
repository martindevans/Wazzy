using System.Text;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Directories;

public interface IDirectory
    : IFilesystemEntry
{
    bool Add(ReadOnlySpan<byte> name, IFilesystemEntry child);

    DirectoryItem? GetChild(ReadOnlySpan<byte> relativePath);

    (DirectoryItem?, PathOpenResult) Create(ReadOnlySpan<byte> name, IFilesystemEntry content);

    (DirectoryItem?, PathOpenResult) CreateFile(ReadOnlySpan<byte> name, IFile? content = null);

    (DirectoryItem?, WasiError) CreateDirectory(ReadOnlySpan<byte> name, IDirectory? content = null);

    IFilesystemHandle Open();

    bool Delete(ReadOnlySpan<byte> name);

    WasiError Move(
        ReadOnlySpan<byte> currentName,
        IDirectory destinationDirectory,
        ReadOnlySpan<byte> destinationName);
}

public readonly struct DirectoryItem
{
    public ReadOnlyMemory<byte> NameUtf8 { get; }
    public IFilesystemEntry Content { get; }

    public DirectoryItem(ReadOnlyMemory<byte> nameUtf8, IFilesystemEntry content)
    {
        NameUtf8 = nameUtf8;
        Content = content;
    }

    public DirectoryItem(string name, IFilesystemEntry content)
    {
        NameUtf8 = Encoding.UTF8.GetBytes(name);
        Content = content;
    }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(NameUtf8.Span);
    }
}