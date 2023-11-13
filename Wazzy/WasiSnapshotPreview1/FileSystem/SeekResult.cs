namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum SeekResult
{
    Success = WasiError.SUCCESS,
    BadFileDescriptor = WasiError.EBADF,
    InvalidParameter = WasiError.EINVAL,
    CannotSeekPipe = WasiError.ESPIPE,
    NoPermission = WasiError.EPERM,
    IsDirectory = WasiError.EISDIR,
}