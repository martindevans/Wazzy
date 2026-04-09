(module
    (import "wasi_snapshot_preview1" "fd_write"
        (func $fd_write (param i32 i32 i32 i32) (result i32)))
    (import "wasi_snapshot_preview1" "fd_pwrite"
        (func $fd_pwrite (param i32 i32 i32 i64 i32) (result i32)))
    (import "wasi_snapshot_preview1" "fd_read"
        (func $fd_read (param i32 i32 i32 i32) (result i32)))
    (import "wasi_snapshot_preview1" "fd_pread"
        (func $fd_pread (param i32 i32 i32 i64 i32) (result i32)))
    (import "wasi_snapshot_preview1" "fd_seek"
        (func $fd_seek (param i32 i64 i32 i32) (result i32)))
    (import "wasi_snapshot_preview1" "fd_filestat_set_size"
        (func $fd_filestat_set_size (param i32 i64) (result i32)))
    (import "wasi_snapshot_preview1" "path_open"
        (func $path_open (param i32 i32 i32 i32 i32 i64 i64 i32 i32) (result i32)))

    (memory 1)
    (export "memory" (memory 0))

    ;; Memory layout:
    ;;   0- 7: "fuzz.txt"          – path used by open_file
    ;;  16-19: iov.buf_ptr (i32)   – set before each I/O call
    ;;  20-23: iov.buf_len (i32)
    ;;  32-35: nwritten/nread output (u32)
    ;;  40-47: new_offset output   (u64, written by fd_seek)
    ;;  48-51: fd output           (u32, written by path_open)
    ;; 256+:   data buffer         (used by the C# test to place read/write data)

    (data (i32.const 0) "fuzz.txt")

    ;; Open "fuzz.txt" from the root pre-open (fd=3).
    ;; Returns the new file descriptor, or -1 on failure.
    (func (export "open_file") (result i32)
        (local $errno i32)
        (local.set $errno
            (call $path_open
                (i32.const 3)    ;; root pre-open fd
                (i32.const 0)    ;; dirflags = 0
                (i32.const 0)    ;; path ptr  ("fuzz.txt")
                (i32.const 8)    ;; path len  = 8
                (i32.const 0)    ;; oflags    = None (file already exists)
                (i64.const -1)   ;; rights_base = all
                (i64.const -1)   ;; rights_inheriting = all
                (i32.const 0)    ;; fdflags = 0
                (i32.const 48)   ;; fd output ptr
            )
        )
        (if (i32.ne (local.get $errno) (i32.const 0))
            (then (return (i32.const -1)))
        )
        (i32.load (i32.const 48))
    )

    ;; fd_write: write len bytes from memory[buf_offset..] at the current file position.
    ;; Returns errno.
    (func (export "write_buf") (param $fd i32) (param $buf_offset i32) (param $len i32) (result i32)
        (i32.store (i32.const 16) (local.get $buf_offset))
        (i32.store (i32.const 20) (local.get $len))
        (call $fd_write
            (local.get $fd)
            (i32.const 16)  ;; iovs ptr
            (i32.const 1)   ;; iovs count
            (i32.const 32)  ;; nwritten ptr
        )
    )

    ;; fd_pwrite: write len bytes from memory[buf_offset..] at file_offset (no cursor advance).
    ;; Returns errno.
    (func (export "pwrite_buf") (param $fd i32) (param $buf_offset i32) (param $len i32) (param $file_offset i64) (result i32)
        (i32.store (i32.const 16) (local.get $buf_offset))
        (i32.store (i32.const 20) (local.get $len))
        (call $fd_pwrite
            (local.get $fd)
            (i32.const 16)  ;; iovs ptr
            (i32.const 1)   ;; iovs count
            (local.get $file_offset)
            (i32.const 32)  ;; nwritten ptr
        )
    )

    ;; fd_read: read len bytes from the current file position into memory[buf_offset..].
    ;; Returns errno.
    (func (export "read_buf") (param $fd i32) (param $buf_offset i32) (param $len i32) (result i32)
        (i32.store (i32.const 16) (local.get $buf_offset))
        (i32.store (i32.const 20) (local.get $len))
        (call $fd_read
            (local.get $fd)
            (i32.const 16)  ;; iovs ptr
            (i32.const 1)   ;; iovs count
            (i32.const 32)  ;; nread ptr
        )
    )

    ;; fd_pread: read len bytes at file_offset (no cursor advance) into memory[buf_offset..].
    ;; Returns errno.
    (func (export "pread_buf") (param $fd i32) (param $buf_offset i32) (param $len i32) (param $file_offset i64) (result i32)
        (i32.store (i32.const 16) (local.get $buf_offset))
        (i32.store (i32.const 20) (local.get $len))
        (call $fd_pread
            (local.get $fd)
            (i32.const 16)  ;; iovs ptr
            (i32.const 1)   ;; iovs count
            (local.get $file_offset)
            (i32.const 32)  ;; nread ptr
        )
    )

    ;; Return the last nwritten / nread value (u32 at offset 32).
    (func (export "get_io_result") (result i32)
        (i32.load (i32.const 32))
    )

    ;; fd_seek: move the file position.
    ;; whence: 0=SEEK_SET, 1=SEEK_CUR, 2=SEEK_END.
    ;; Returns errno; call get_seek_result() to read the new offset.
    (func (export "seek_file") (param $fd i32) (param $offset i64) (param $whence i32) (result i32)
        (call $fd_seek
            (local.get $fd)
            (local.get $offset)
            (local.get $whence)
            (i32.const 40)  ;; new_offset ptr
        )
    )

    ;; Return the new file offset written by the last fd_seek call (u64 at offset 40).
    (func (export "get_seek_result") (result i64)
        (i64.load (i32.const 40))
    )

    ;; fd_filestat_set_size: truncate / extend the file to exactly `size` bytes.
    ;; Returns errno.
    (func (export "truncate_file") (param $fd i32) (param $size i64) (result i32)
        (call $fd_filestat_set_size
            (local.get $fd)
            (local.get $size)
        )
    )
)
