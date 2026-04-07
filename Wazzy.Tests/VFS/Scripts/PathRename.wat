(module
    (import "wasi_snapshot_preview1" "path_rename"
        (func $path_rename (param i32 i32 i32 i32 i32 i32) (result i32)))

    (memory $memory 1)
    (export "memory" (memory 0))

    ;; Memory layout (each string separated by at least 64 bytes for readability):
    ;;   0: "source.txt"          len=10
    ;;  64: "dest.txt"            len=8
    ;; 128: "sourcedir"           len=9
    ;; 192: "destdir"             len=7
    ;; 256: "subdir/source.txt"   len=17
    ;; 384: "subdir/dest.txt"     len=15
    ;; 512: "missing/source.txt"  len=18  (parent dir "missing" does not exist)
    ;; 640: "missing/dest.txt"    len=16  (parent dir "missing" does not exist)
    ;; 768: "notfound.txt"        len=12  (file itself does not exist)
    (data (i32.const 0)   "source.txt")
    (data (i32.const 64)  "dest.txt")
    (data (i32.const 128) "sourcedir")
    (data (i32.const 192) "destdir")
    (data (i32.const 256) "subdir/source.txt")
    (data (i32.const 384) "subdir/dest.txt")
    (data (i32.const 512) "missing/source.txt")
    (data (i32.const 640) "missing/dest.txt")
    (data (i32.const 768) "notfound.txt")

    ;; Rename "source.txt" -> "dest.txt" in the root pre-open (fd=3).
    ;; Expected: SUCCESS (0) when the file exists and the VFS is writable.
    ;; Also used as-is for the read-only (EROFS) test by supplying a
    ;; read-only VFS from the C# side.
    (func $rename_file (result i32)
        (call $path_rename
            (i32.const 3)  ;; old fd  – root pre-open
            (i32.const 0)  ;; "source.txt"
            (i32.const 10)
            (i32.const 3)  ;; new fd  – root pre-open
            (i32.const 64) ;; "dest.txt"
            (i32.const 8)
        )
    )
    (export "rename_file" (func $rename_file))

    ;; Rename directory "sourcedir" -> "destdir" in root.
    ;; Expected: SUCCESS (0).
    (func $rename_dir (result i32)
        (call $path_rename
            (i32.const 3)
            (i32.const 128) ;; "sourcedir"
            (i32.const 9)
            (i32.const 3)
            (i32.const 192) ;; "destdir"
            (i32.const 7)
        )
    )
    (export "rename_dir" (func $rename_dir))

    ;; Move "source.txt" from root into "subdir/dest.txt".
    ;; Expected: SUCCESS (0).
    (func $move_to_subdir (result i32)
        (call $path_rename
            (i32.const 3)
            (i32.const 0)   ;; "source.txt"
            (i32.const 10)
            (i32.const 3)
            (i32.const 384) ;; "subdir/dest.txt"
            (i32.const 15)
        )
    )
    (export "move_to_subdir" (func $move_to_subdir))

    ;; Move "subdir/source.txt" from a subdirectory to "dest.txt" in root.
    ;; Expected: SUCCESS (0).
    (func $move_from_subdir (result i32)
        (call $path_rename
            (i32.const 3)
            (i32.const 256) ;; "subdir/source.txt"
            (i32.const 17)
            (i32.const 3)
            (i32.const 64)  ;; "dest.txt"
            (i32.const 8)
        )
    )
    (export "move_from_subdir" (func $move_from_subdir))

    ;; Pass an invalid old fd (99 is not a registered handle).
    ;; Expected: ENOENT (44) from GetDirectory when handle is not found.
    (func $rename_bad_old_fd (result i32)
        (call $path_rename
            (i32.const 99)
            (i32.const 0)  ;; "source.txt"
            (i32.const 10)
            (i32.const 3)
            (i32.const 64) ;; "dest.txt"
            (i32.const 8)
        )
    )
    (export "rename_bad_old_fd" (func $rename_bad_old_fd))

    ;; Pass an invalid new fd (99 is not a registered handle).
    ;; Expected: ENOENT (44) from GetDirectory when handle is not found.
    (func $rename_bad_new_fd (result i32)
        (call $path_rename
            (i32.const 3)
            (i32.const 0)  ;; "source.txt"
            (i32.const 10)
            (i32.const 99)
            (i32.const 64) ;; "dest.txt"
            (i32.const 8)
        )
    )
    (export "rename_bad_new_fd" (func $rename_bad_new_fd))

    ;; Pass fd=1 (stdout – a file handle, not a directory) as the old fd.
    ;; Expected: ENOTDIR (54) from GetDirectory when the handle is not a directory.
    (func $rename_file_fd_as_old (result i32)
        (call $path_rename
            (i32.const 1)  ;; stdout file handle
            (i32.const 0)  ;; "source.txt"
            (i32.const 10)
            (i32.const 3)
            (i32.const 64) ;; "dest.txt"
            (i32.const 8)
        )
    )
    (export "rename_file_fd_as_old" (func $rename_file_fd_as_old))

    ;; Rename a file that does not exist in the VFS.
    ;; Expected: ENOENT (44) from Move returning NoEntity.
    (func $rename_nonexistent_source (result i32)
        (call $path_rename
            (i32.const 3)
            (i32.const 768) ;; "notfound.txt"
            (i32.const 12)
            (i32.const 3)
            (i32.const 64)  ;; "dest.txt"
            (i32.const 8)
        )
    )
    (export "rename_nonexistent_source" (func $rename_nonexistent_source))

    ;; Source path whose parent directory ("missing") does not exist.
    ;; Expected: ENOENT (44) from ResolveParent returning null.
    (func $rename_source_parent_missing (result i32)
        (call $path_rename
            (i32.const 3)
            (i32.const 512) ;; "missing/source.txt"
            (i32.const 18)
            (i32.const 3)
            (i32.const 64)  ;; "dest.txt"
            (i32.const 8)
        )
    )
    (export "rename_source_parent_missing" (func $rename_source_parent_missing))

    ;; Destination path whose parent directory ("missing") does not exist.
    ;; Expected: ENOENT (44) from ResolveParent returning null.
    (func $rename_dest_parent_missing (result i32)
        (call $path_rename
            (i32.const 3)
            (i32.const 0)   ;; "source.txt"
            (i32.const 10)
            (i32.const 3)
            (i32.const 640) ;; "missing/dest.txt"
            (i32.const 16)
        )
    )
    (export "rename_dest_parent_missing" (func $rename_dest_parent_missing))

    ;; Rename "source.txt" to "dest.txt" when "dest.txt" already exists.
    ;; Expected: EEXIST (20) because VirtualDirectoryContent.Create returns
    ;; PathOpenResult.AlreadyExists when the destination name is occupied.
    (func $rename_dest_exists (result i32)
        (call $path_rename
            (i32.const 3)
            (i32.const 0)  ;; "source.txt"
            (i32.const 10)
            (i32.const 3)
            (i32.const 64) ;; "dest.txt"  (already present in VFS)
            (i32.const 8)
        )
    )
    (export "rename_dest_exists" (func $rename_dest_exists))
)
