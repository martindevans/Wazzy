(module
  (import "spectest" "print" (func $print (param i32)))
  (import "whatever" "double" (func $double (param i32) (result i32)))
  (memory (export "memory") 1 1024)

  (func $run (param i32) (result i32)

	(local $var i32)
	(local.set $var (param.get 0))
	
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
)
