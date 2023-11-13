namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum ReadDirectoryResult
{
    Success = WasiError.SUCCESS,
    BadFileDescriptor = WasiError.EBADF,
    InvalidParameter = WasiError.EINVAL,
    NotADirectory = WasiError.ENOTDIR,
    NoPermission = WasiError.EPERM,
}