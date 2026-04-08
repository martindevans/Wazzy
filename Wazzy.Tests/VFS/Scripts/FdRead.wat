(module
    (import "wasi_snapshot_preview1" "fd_read"
        (func $fd_read (param i32 i32 i32 i32) (result i32)))

    (import "wasi_snapshot_preview1" "fd_pread"
        (func $fd_pread (param i32 i32 i32 i64 i32) (result i32)))

    (import "wasi_snapshot_preview1" "path_open"
        (func $path_open (param i32 i32 i32 i32 i32 i64 i64 i32 i32) (result i32)))

    (memory 1)
    (export "memory" (memory 0))

    ;; Memory layout:
    ;;   0- 3:  iov_full[0].buf_ptr  = 256   (reads up to 64 bytes)
    ;;   4- 7:  iov_full[0].buf_len  = 64
    ;;   8-11:  iov_small[0].buf_ptr = 256   (reads up to 2 bytes)
    ;;  12-15:  iov_small[0].buf_len = 2
    ;;  16-19:  nread output
    ;;  20-23:  fd output for path_open
    ;; 128-135: "test.txt"
    ;; 256-319: data buffer (64 bytes)

    (data (i32.const 0)   "\00\01\00\00")  ;; iov_full.buf_ptr  = 256
    (data (i32.const 4)   "\40\00\00\00")  ;; iov_full.buf_len  = 64
    (data (i32.const 8)   "\00\01\00\00")  ;; iov_small.buf_ptr = 256
    (data (i32.const 12)  "\02\00\00\00")  ;; iov_small.buf_len = 2
    (data (i32.const 128) "test.txt")

    ;; Call fd_read on the given fd using iov_full (up to 64 bytes).
    ;; Returns the errno from fd_read.
    (func (export "read_fd") (param $fd i32) (result i32)
        (call $fd_read
            (local.get $fd)
            (i32.const 0)   ;; iovs ptr  (iov_full)
            (i32.const 1)   ;; iovs count
            (i32.const 16)  ;; nread ptr
        )
    )

    ;; Call fd_pread on the given fd at the given offset using iov_full.
    ;; Returns the errno from fd_pread.
    (func (export "pread_fd") (param $fd i32) (param $offset i64) (result i32)
        (call $fd_pread
            (local.get $fd)
            (i32.const 0)         ;; iovs ptr  (iov_full)
            (i32.const 1)         ;; iovs count
            (local.get $offset)
            (i32.const 16)        ;; nread ptr
        )
    )

    ;; Open "test.txt" from the root pre-open (fd=3), read its full content
    ;; with iov_full, and return the errno from fd_read.
    (func (export "open_and_read") (result i32)
        (local $open_errno i32)
        (local.set $open_errno
            (call $path_open
                (i32.const 3)    ;; root fd
                (i32.const 0)    ;; dirflags
                (i32.const 128)  ;; path ptr ("test.txt")
                (i32.const 8)    ;; path len
                (i32.const 0)    ;; oflags
                (i64.const -1)   ;; rights_base  (all)
                (i64.const -1)   ;; rights_inheriting (all)
                (i32.const 0)    ;; fdflags
                (i32.const 20)   ;; fd output ptr
            )
        )
        (if (i32.ne (local.get $open_errno) (i32.const 0))
            (then (return (local.get $open_errno)))
        )
        (call $fd_read
            (i32.load (i32.const 20))  ;; opened fd
            (i32.const 0)              ;; iovs ptr (iov_full)
            (i32.const 1)              ;; iovs count
            (i32.const 16)             ;; nread ptr
        )
    )

    ;; Open "test.txt" from the root pre-open (fd=3), pread at the given
    ;; offset with iov_full, and return the errno from fd_pread.
    (func (export "open_and_pread") (param $offset i64) (result i32)
        (local $open_errno i32)
        (local.set $open_errno
            (call $path_open
                (i32.const 3)
                (i32.const 0)
                (i32.const 128)  ;; "test.txt"
                (i32.const 8)
                (i32.const 0)
                (i64.const -1)
                (i64.const -1)
                (i32.const 0)
                (i32.const 20)   ;; fd output ptr
            )
        )
        (if (i32.ne (local.get $open_errno) (i32.const 0))
            (then (return (local.get $open_errno)))
        )
        (call $fd_pread
            (i32.load (i32.const 20))  ;; opened fd
            (i32.const 0)              ;; iovs ptr (iov_full)
            (i32.const 1)              ;; iovs count
            (local.get $offset)
            (i32.const 16)             ;; nread ptr
        )
    )

    ;; Open "test.txt", do a small 2-byte fd_read (advancing position to 2),
    ;; then do a fd_pread at offset=0 (which must NOT advance the position),
    ;; then do a final fd_read from the current position (should be 2, not 0).
    ;; Returns the errno of the final fd_read; call get_nread() afterwards
    ;; to read the byte count returned by the final fd_read.
    (func (export "open_small_read_pread_read") (result i32)
        (local $open_errno i32)
        (local $file_fd i32)
        (local.set $open_errno
            (call $path_open
                (i32.const 3)
                (i32.const 0)
                (i32.const 128)  ;; "test.txt"
                (i32.const 8)
                (i32.const 0)
                (i64.const -1)
                (i64.const -1)
                (i32.const 0)
                (i32.const 20)
            )
        )
        (if (i32.ne (local.get $open_errno) (i32.const 0))
            (then (return (local.get $open_errno)))
        )
        (local.set $file_fd (i32.load (i32.const 20)))

        ;; Step 1: small read – reads 2 bytes, advances file position to 2.
        (drop (call $fd_read
            (local.get $file_fd)
            (i32.const 8)   ;; iov_small ptr (buf_len=2)
            (i32.const 1)
            (i32.const 16)
        ))

        ;; Step 2: pread at offset 0 – reads from the beginning, but must
        ;; leave the file position unchanged (still at 2).
        (drop (call $fd_pread
            (local.get $file_fd)
            (i32.const 0)   ;; iov_full
            (i32.const 1)
            (i64.const 0)   ;; offset = 0
            (i32.const 16)
        ))

        ;; Step 3: regular read – must resume from position 2, not 0.
        ;; The caller verifies get_nread() == (file_length - 2).
        (call $fd_read
            (local.get $file_fd)
            (i32.const 0)   ;; iov_full
            (i32.const 1)
            (i32.const 16)
        )
    )

    ;; Return the nread value written by the most recent fd_read / fd_pread call.
    (func (export "get_nread") (result i32)
        (i32.load (i32.const 16))
    )

    ;; Return the byte at index $idx inside the data buffer (base address 256).
    (func (export "get_data_byte") (param $idx i32) (result i32)
        (i32.load8_u (i32.add (i32.const 256) (local.get $idx)))
    )
)
