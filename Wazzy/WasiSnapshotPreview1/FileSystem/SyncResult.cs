namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum SyncResult
{
    Success = WasiError.SUCCESS,
    BadFileDescriptor = WasiError.EBADF,
    IoError = WasiError.EIO,
    ReadOnly = WasiError.EROFS,
    InvalidParameter = WasiError.EINVAL,
    NoPermission = WasiError.EPERM,
}