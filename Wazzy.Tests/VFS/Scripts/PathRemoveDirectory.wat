(module
  (import "wasi_snapshot_preview1" "path_remove_directory" (func $path_remove_directory (param i32 i32 i32) (result i32)))

  (memory 1)
  (export "memory" (memory 0))

  ;; Path data stored at fixed offsets in linear memory
  (data (i32.const 0)  "testdir")            ;; offset=0,  len=7
  (data (i32.const 16) "subdir/child")       ;; offset=16, len=12
  (data (i32.const 32) "no_parent/testdir")  ;; offset=32, len=17
  (data (i32.const 64) "test.txt")           ;; offset=64, len=8
  (data (i32.const 80) "nonempty")           ;; offset=80, len=8

  ;; Call path_remove_directory with caller-supplied fd and path slice, returning the errno.
  (func (export "remove_directory") (param $fd i32) (param $path_ptr i32) (param $path_len i32) (result i32)
    (call $path_remove_directory
      (local.get $fd)
      (local.get $path_ptr)
      (local.get $path_len)
    )
  )
)
