namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum PathOpenResult
{
    Success = WasiError.SUCCESS,
    BadFileDescriptor = WasiError.EBADF,
    InvalidParameter = WasiError.EINVAL,
    NoEntity = WasiError.ENOENT,
    NotADirectory = WasiError.ENOTDIR,
    NoPermission = WasiError.EPERM,
    NoFileDescriptorsAvailable = WasiError.ENFILE,
    ReadOnly = WasiError.EROFS,
    AlreadyExists = WasiError.EEXIST,
    IsADirectory = WasiError.EISDIR
}