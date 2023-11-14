(module

    (type $t0 (func (param i32) (param i32) (result i32)))
    (import "wasi_snapshot_preview1" "environ_get" (func $wasi_snapshot_preview1.environ_get (type $t0)))
    (import "wasi_snapshot_preview1" "environ_sizes_get" (func $wasi_snapshot_preview1.environ_sizes_get (type $t0)))
    (import "wasi_snapshot_preview1" "args_get" (func $wasi_snapshot_preview1.args_get (type $t0)))
    (import "wasi_snapshot_preview1" "args_sizes_get" (func $wasi_snapshot_preview1.args_sizes_get (type $t0)))

    (memory $memory 1)
    (export "memory" (memory 0))

    (func $test_environ_get_sizes (result i32) (result i32) (result i32)

        ;; Get env sizes into two pointers at addr:0 and addr:4
        (i32.const 0)
        (i32.const 4)
        (call $wasi_snapshot_preview1.environ_sizes_get)

        ;; Load  those values as results
        (i32.const 0)
        (i32.load)
        (i32.const 4)
        (i32.load)
    )
    (export "test_environ_get_sizes" (func $test_environ_get_sizes))

    (func $test_environ_get (result i32)

        ;; Get env into two pointers at addr:0 and addr:128
        (i32.const 0)
        (i32.const 128)
        (call $wasi_snapshot_preview1.environ_get)
    )
    (export "test_environ_get" (func $test_environ_get))

    (func $test_args_get_sizes (result i32) (result i32) (result i32)

        ;; Get arg sizes into two pointers at addr:0 and addr:4
        (i32.const 0)
        (i32.const 4)
        (call $wasi_snapshot_preview1.args_sizes_get)

        ;; Load  those values as results
        (i32.const 0)
        (i32.load)
        (i32.const 4)
        (i32.load)
    )
    (export "test_args_get_sizes" (func $test_args_get_sizes))

    (func $test_args_get (result i32)

        ;; Get args into two pointers at addr:0 and addr:128
        (i32.const 0)
        (i32.const 128)
        (call $wasi_snapshot_preview1.args_get)
    )
    (export "test_args_get" (func $test_args_get))
)