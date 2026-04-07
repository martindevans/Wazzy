(module
    (import "wasi_snapshot_preview1" "path_create_directory"
        (func $path_create_directory (param i32 i32 i32) (result i32)))

    (memory (export "memory") 1)

    ;; "new_dir" at offset 0, length 7
    (data (i32.const 0) "new_dir")

    ;; "nonexistent_parent/new_dir" at offset 64, length 26
    (data (i32.const 64) "nonexistent_parent/new_dir")

    ;; "already_exists" at offset 128, length 14
    (data (i32.const 128) "already_exists")

    ;; Create "new_dir" in the root directory (fd=3)
    (func $create_directory_success (result i32)
        (call $path_create_directory
            (i32.const 3)   ;; root fd
            (i32.const 0)   ;; path ptr = "new_dir"
            (i32.const 7))  ;; path len = 7
    )
    (export "create_directory_success" (func $create_directory_success))

    ;; Attempt to create using an invalid file descriptor
    (func $create_directory_invalid_fd (result i32)
        (call $path_create_directory
            (i32.const 999) ;; invalid fd
            (i32.const 0)   ;; path ptr = "new_dir"
            (i32.const 7))  ;; path len = 7
    )
    (export "create_directory_invalid_fd" (func $create_directory_invalid_fd))

    ;; Attempt to create using fd=0 (stdin - a file, not a directory)
    (func $create_directory_fd_is_file (result i32)
        (call $path_create_directory
            (i32.const 0)   ;; stdin fd (file handle, not directory)
            (i32.const 0)   ;; path ptr = "new_dir"
            (i32.const 7))  ;; path len = 7
    )
    (export "create_directory_fd_is_file" (func $create_directory_fd_is_file))

    ;; Attempt to create a path whose parent directory does not exist
    (func $create_directory_parent_not_found (result i32)
        (call $path_create_directory
            (i32.const 3)   ;; root fd
            (i32.const 64)  ;; path ptr = "nonexistent_parent/new_dir"
            (i32.const 26)) ;; path len = 26
    )
    (export "create_directory_parent_not_found" (func $create_directory_parent_not_found))

    ;; Attempt to create a directory that already exists in the VFS
    (func $create_directory_already_exists (result i32)
        (call $path_create_directory
            (i32.const 3)   ;; root fd
            (i32.const 128) ;; path ptr = "already_exists"
            (i32.const 14)) ;; path len = 14
    )
    (export "create_directory_already_exists" (func $create_directory_already_exists))
)
