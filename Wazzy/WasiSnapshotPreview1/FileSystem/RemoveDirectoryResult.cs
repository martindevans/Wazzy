namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum RemoveDirectoryResult
{
    Success = WasiError.SUCCESS,
    ReadonlyFilesystem = WasiError.EROFS,
    BadFileDescriptor = WasiError.EBADF,
    NotADirectory = WasiError.ENOTDIR,
    NotEmpty = WasiError.ENOTEMPTY,
    NoEntity = WasiError.ENOENT,
}