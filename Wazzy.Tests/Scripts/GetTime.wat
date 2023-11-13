(module

    (type $t0 (func (param i32) (param i64) (param i32) (result i32)))
    (import "wasi_snapshot_preview1" "clock_time_get" (func $wasi_snapshot_preview1.clock_time_get (type $t0)))

    (memory $memory 1)
    (export "memory" (memory 0))

    (func $get_clock (param i32) (result i32) (result i64)

        ;; Get clock into a pointer addr:0
        (local.get 0)
        (i64.const 0)
        (i32.const 0)
        (call $wasi_snapshot_preview1.clock_time_get)

        ;; Load an i64 from addr:0
        (i32.const 0)
        (i64.load)
    )
    (export "get_clock" (func $get_clock))
)