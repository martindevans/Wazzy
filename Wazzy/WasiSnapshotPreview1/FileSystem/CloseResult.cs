namespace Wazzy.WasiSnapshotPreview1.FileSystem;

public enum CloseResult
{
    Success = WasiError.SUCCESS,
    BadFileDescriptor = WasiError.EBADF,
}