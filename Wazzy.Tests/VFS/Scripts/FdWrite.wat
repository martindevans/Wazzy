(module
    (import "wasi_snapshot_preview1" "fd_write"
        (func $fd_write (param i32 i32 i32 i32) (result i32)))
    (import "wasi_snapshot_preview1" "fd_pwrite"
        (func $fd_pwrite (param i32 i32 i32 i64 i32) (result i32)))
    (import "wasi_snapshot_preview1" "path_open"
        (func $path_open (param i32 i32 i32 i32 i32 i64 i64 i32 i32) (result i32)))

    (memory 1)
    (export "memory" (memory 0))

    ;; Memory layout:
    ;;   0: "test.txt"       len=8  – path for writable file
    ;;  16: "readonly.txt"   len=12 – path for read-only file
    ;;  64: "hello"          len=5  – data written by every write/pwrite call
    ;; 128: ciovec[0] = {buf_ptr=64, buf_len=5} (set at runtime, 8 bytes)
    ;; 256: nwritten result (4 bytes, output of fd_write / fd_pwrite)
    ;; 260: new fd result   (4 bytes, output of path_open)

    (data (i32.const 0)  "test.txt")
    (data (i32.const 16) "readonly.txt")
    (data (i32.const 64) "hello")

    ;; ── internal helpers ─────────────────────────────────────────────────────

    ;; Populate ciovec[0] = {buf=64, len=5} and call fd_write.
    (func $do_write (param $fd i32) (result i32)
        (i32.store (i32.const 128) (i32.const 64))
        (i32.store (i32.const 132) (i32.const 5))
        (call $fd_write
            (local.get $fd)
            (i32.const 128)
            (i32.const 1)
            (i32.const 256)
        )
    )

    ;; Populate ciovec[0] = {buf=64, len=5} and call fd_pwrite at a given offset.
    (func $do_pwrite (param $fd i32) (param $offset i64) (result i32)
        (i32.store (i32.const 128) (i32.const 64))
        (i32.store (i32.const 132) (i32.const 5))
        (call $fd_pwrite
            (local.get $fd)
            (i32.const 128)
            (i32.const 1)
            (local.get $offset)
            (i32.const 256)
        )
    )

    ;; Open "test.txt" in the root pre-open (fd=3) and return the new fd.
    ;; Returns -1 if path_open itself fails.
    (func $open_test_file (result i32)
        (local $errno i32)
        (local.set $errno
            (call $path_open
                (i32.const 3)    ;; dirfd  – root pre-open
                (i32.const 0)    ;; dirflags = 0
                (i32.const 0)    ;; path = "test.txt"
                (i32.const 8)    ;; path_len = 8
                (i32.const 0)    ;; oflags = None (file already exists)
                (i64.const -1)   ;; fs_rights_base = all rights
                (i64.const -1)   ;; fs_rights_inheriting = all rights
                (i32.const 0)    ;; fdflags = None
                (i32.const 260)  ;; output fd ptr
            )
        )
        (if (i32.ne (local.get $errno) (i32.const 0))
            (then (return (i32.const -1)))
        )
        (i32.load (i32.const 260))
    )

    ;; Open "readonly.txt" in the root pre-open (fd=3) and return the new fd.
    ;; Returns -1 if path_open fails.
    (func $open_readonly_file (result i32)
        (local $errno i32)
        (local.set $errno
            (call $path_open
                (i32.const 3)    ;; dirfd  – root pre-open
                (i32.const 0)    ;; dirflags = 0
                (i32.const 16)   ;; path = "readonly.txt"
                (i32.const 12)   ;; path_len = 12
                (i32.const 0)    ;; oflags = None (file already exists)
                (i64.const -1)   ;; fs_rights_base = all rights
                (i64.const -1)   ;; fs_rights_inheriting = all rights
                (i32.const 0)    ;; fdflags = None
                (i32.const 260)  ;; output fd ptr
            )
        )
        (if (i32.ne (local.get $errno) (i32.const 0))
            (then (return (i32.const -1)))
        )
        (i32.load (i32.const 260))
    )

    ;; ── fd_write exported functions ───────────────────────────────────────────

    ;; fd_write with an unknown file descriptor → EBADF
    (func $write_bad_fd (result i32)
        (call $do_write (i32.const 99))
    )
    (export "write_bad_fd" (func $write_bad_fd))

    ;; fd_write with the root directory fd (fd=3) → EISDIR
    (func $write_directory_fd (result i32)
        (call $do_write (i32.const 3))
    )
    (export "write_directory_fd" (func $write_directory_fd))

    ;; fd_write to a writable file → SUCCESS, nwritten = 5
    (func $write_success (result i32)
        (local $fd i32)
        (local.set $fd (call $open_test_file))
        (if (i32.lt_s (local.get $fd) (i32.const 0))
            (then (return (i32.const -1)))
        )
        (call $do_write (local.get $fd))
    )
    (export "write_success" (func $write_success))

    ;; fd_write to a file that was created as read-only → EPERM
    (func $write_readonly (result i32)
        (local $fd i32)
        (local.set $fd (call $open_readonly_file))
        (if (i32.lt_s (local.get $fd) (i32.const 0))
            (then (return (i32.const -1)))
        )
        (call $do_write (local.get $fd))
    )
    (export "write_readonly" (func $write_readonly))

    ;; ── fd_pwrite exported functions ──────────────────────────────────────────

    ;; fd_pwrite with an unknown file descriptor → EBADF
    (func $pwrite_bad_fd (result i32)
        (call $do_pwrite (i32.const 99) (i64.const 0))
    )
    (export "pwrite_bad_fd" (func $pwrite_bad_fd))

    ;; fd_pwrite with the root directory fd (fd=3) → EISDIR
    (func $pwrite_directory_fd (result i32)
        (call $do_pwrite (i32.const 3) (i64.const 0))
    )
    (export "pwrite_directory_fd" (func $pwrite_directory_fd))

    ;; fd_pwrite to a writable file at offset 0 → SUCCESS, nwritten = 5
    (func $pwrite_success (result i32)
        (local $fd i32)
        (local.set $fd (call $open_test_file))
        (if (i32.lt_s (local.get $fd) (i32.const 0))
            (then (return (i32.const -1)))
        )
        (call $do_pwrite (local.get $fd) (i64.const 0))
    )
    (export "pwrite_success" (func $pwrite_success))

    ;; fd_pwrite to a file that was created as read-only → EPERM
    (func $pwrite_readonly (result i32)
        (local $fd i32)
        (local.set $fd (call $open_readonly_file))
        (if (i32.lt_s (local.get $fd) (i32.const 0))
            (then (return (i32.const -1)))
        )
        (call $do_pwrite (local.get $fd) (i64.const 0))
    )
    (export "pwrite_readonly" (func $pwrite_readonly))

    ;; fd_pwrite with a negative offset → EINVAL (seek to a negative position fails)
    (func $pwrite_negative_offset (result i32)
        (local $fd i32)
        (local.set $fd (call $open_test_file))
        (if (i32.lt_s (local.get $fd) (i32.const 0))
            (then (return (i32.const -1)))
        )
        (call $do_pwrite (local.get $fd) (i64.const -1))
    )
    (export "pwrite_negative_offset" (func $pwrite_negative_offset))

    ;; ── utility ───────────────────────────────────────────────────────────────

    ;; Return the nwritten value stored at offset 256 by the most recent write.
    (func $get_nwritten (result i32)
        (i32.load (i32.const 256))
    )
    (export "get_nwritten" (func $get_nwritten))
)
