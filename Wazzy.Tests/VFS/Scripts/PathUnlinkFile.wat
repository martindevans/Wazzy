(module
  (import "wasi_snapshot_preview1" "path_unlink_file" (func $path_unlink_file (param i32 i32 i32) (result i32)))

  (memory 1)
  (export "memory" (memory 0))

  ;; Path data stored at fixed offsets in linear memory
  (data (i32.const 0)  "test.txt")           ;; offset=0,  len=8
  (data (i32.const 16) "subdir/test.txt")    ;; offset=16, len=15
  (data (i32.const 32) "no_parent/test.txt") ;; offset=32, len=18
  (data (i32.const 64) "subdir")             ;; offset=64, len=6

  ;; Call path_unlink_file with caller-supplied fd and path slice, returning the errno.
  (func (export "unlink_file") (param $fd i32) (param $path_ptr i32) (param $path_len i32) (result i32)
    (call $path_unlink_file
      (local.get $fd)
      (local.get $path_ptr)
      (local.get $path_len)
    )
  )
)
