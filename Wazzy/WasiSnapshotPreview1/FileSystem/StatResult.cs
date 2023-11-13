namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum StatResult
{
    Success = WasiError.SUCCESS,
    BadFileDescriptor = WasiError.EBADF,
    InvalidParameter = WasiError.EINVAL,
    NoEntity = WasiError.ENOENT,
    NotADirectory = WasiError.ENOTDIR,
    NoPermission = WasiError.EPERM,
}