namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum ReadResult
{
    Success = WasiError.SUCCESS,
    BadFileDescriptor = WasiError.EBADF,
    InvalidParameter = WasiError.EINVAL,
    WouldBlock = WasiError.EAGAIN,
    IoError = WasiError.EIO,
    IsDirectory = WasiError.EISDIR,
    NoPermission = WasiError.EPERM,
}