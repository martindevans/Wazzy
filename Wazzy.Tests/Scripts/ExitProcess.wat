(module

    (type $t0 (func (param i32)))
    (import "wasi_snapshot_preview1" "proc_exit" (func $wasi_snapshot_preview1.proc_exit (type $t0)))

    (memory $memory 1)
    (export "memory" (memory 0))

    (func $call_exit (param i32)
        (local.get 0)
        (call $wasi_snapshot_preview1.proc_exit)
    )
    (export "call_exit" (func $call_exit))
)