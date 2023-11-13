namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum PrestatDirNameResult
{
    Success = WasiError.SUCCESS,
    BadFileDescriptor = WasiError.EBADF,
    WrongBufferSize = WasiError.EINVAL,
}