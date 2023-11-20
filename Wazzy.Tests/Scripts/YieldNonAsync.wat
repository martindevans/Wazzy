(module

    (type $t0 (func (result i32)))
    (import "wasi_snapshot_preview1" "sched_yield" (func $wasi_snapshot_preview1.sched_yield (type $t0)))

    (memory $memory 1)
    (export "memory" (memory 0))

    (func $call_yield (result i32)

        (call $wasi_snapshot_preview1.sched_yield)
    )
    (export "call_yield" (func $call_yield))
)