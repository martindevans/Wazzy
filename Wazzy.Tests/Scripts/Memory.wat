(module
  (memory (export "memory") 0x10000)
  (func $start

	i32.const 0
    i32.const 0xFFFFFFFF
	i32.store
  )
  (start $start)
)
