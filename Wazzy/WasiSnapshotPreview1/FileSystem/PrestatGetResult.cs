namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum PrestatGetResult
{
    Success = WasiError.SUCCESS,
    BadFileDescriptor = WasiError.EBADF,
}