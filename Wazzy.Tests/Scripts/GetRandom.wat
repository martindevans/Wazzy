(module

    (type $t0 (func (param i32) (param i32) (result i32)))
    (import "wasi_snapshot_preview1" "random_get" (func $wasi_snapshot_preview1.random_get (type $t0)))

    (memory $memory 1)
    (export "memory" (memory 0))

    (func $get_random_i64 (result i32) (result i64)

        ;; Get a random number into a buffer addr:0 length:8
        (i32.const 0)
        (i32.const 8)
        (call $wasi_snapshot_preview1.random_get)

        ;; Load an i64 from addr:0
        (i32.const 0)
        (i64.load)
    )
    (export "get_random_i64" (func $get_random_i64))
)