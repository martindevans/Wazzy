(module
  (import "spectest" "print" (func $print (param i32)))
  (import "whatever" "double" (func $double (param i32) (result i32)))
  (memory (export "memory") 1 1024)

  (func $run (param $p1 i32) (result i32)

	(local $var i32)
	(local.set $var (local.get $p1))
	
    (call $print (local.get $var))

	(local.set $var (i32.const 22))
	(call $print (local.get $var))

	(local.set $var (i32.const 33))
	(call $print (local.get $var))

	(return (local.get 0))
  )
  (export "run" (func $run))

  (func $run_double (param i32) (result i32)

	(local $var i32)
	(local.set $var (call $double (local.get 0)))
	
    (call $print (local.get $var))

	(local.set $var (i32.const 22))
	(call $print (local.get $var))

	(local.set $var (i32.const 33))
	(call $print (local.get $var))

	(return (local.get 0))
  )
  (export "run_double" (func $run_double))

  (func $asyncify_malloc_buffer (param i32) (result i32)
	(return (i32.const 42))
  )
  (export "asyncify_malloc_buffer" (func $asyncify_malloc_buffer))

  (func $asyncify_free_buffer (param i32)
  )
  (export "asyncify_free_buffer" (func $asyncify_free_buffer))
)
