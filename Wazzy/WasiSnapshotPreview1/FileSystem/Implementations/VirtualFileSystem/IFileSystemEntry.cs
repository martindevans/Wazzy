﻿namespace Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem;

public interface IFilesystemEntry
{
    bool CanMove { get; }

    FileType FileType { get; }

    ulong AccessTime { get; set; }

    ulong ModificationTime { get; set; }

    ulong ChangeTime { get; set; }

    IFilesystemEntry ToInMemory();

    public WasiError SetTimes(ulong timestamp, long atime, long mtime, FstFlags fstFlags)
    {
        if (fstFlags is { AdjustAccessTime: true, AdjustAccessTimeNow: true } or { AdjustModifyTime: true, AdjustModifyTimeNow: true })
            return WasiError.EINVAL;

        if (fstFlags.AdjustAccessTime)
            AccessTime = unchecked((ulong)atime);
        if (fstFlags.AdjustAccessTimeNow)
            AccessTime = timestamp;

        if (fstFlags.AdjustModifyTime)
            ModificationTime = unchecked((ulong)mtime);
        if (fstFlags.AdjustModifyTimeNow)
            ModificationTime = timestamp;

        return WasiError.SUCCESS;
    }
}