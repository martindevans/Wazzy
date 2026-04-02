# Wazzy

Wazzy is an implementation of WASI, written in pure C#.

## WASI Features

Wazzy provides parts of WASI as "features" that can be added to the linker. You can implement these interfaces yourself for custom behaviour, or use the prebuilt ones.

```csharp
var environment = new BasicEnvironment();
environment.SetArgs("--foo --bar");
environment.SetEnvironmentVariable("foo", "bar");

var clock = new RealtimeClock();

var engine = new Engine();
var linker = new Linker(engine);
linker.DefineFeature(clock);
linker.DefineFeature(new SeededRandomSource(123));
linker.DefineFeature(environment);
linker.DefineFeature(new ThrowExitProcess());
linker.DefineFeature(new AsyncifyPoll(clock));
linker.DefineFeature(new AsyncifyYieldProcess());
linker.DefineFeature(new NullFilesystem());
linker.DefineFeature(new NonFunctionalSocket());
```

## Async Support

Wazzy supports suspending and resuming execution, using the [Binaryen](https://github.com/WebAssembly/binaryen?tab=readme-ov-file) `Asyncify` process. This is used by several WASI features - for example if `sched_yield` is called, this will async suspend the WASM process.

There are several ways to add async support to a compiled WASM module.

### Just Asyncify

Simply asyncify the module using:

```
wasm-opt module.wasm -o module-async.wasm --asyncify
```

### Cooperative Allocation

Asyncify the module the same way as before.

Export a two methods for cooperative allocation: `asyncify_malloc_buffer(int32) -> int32` & `asyncify_free_buffer(int32, int32)`.

 - `asyncify_malloc_buffer` should accept a size and should return a pointer to a buffer of that size (or a negative number, to indicate allocation failure).
 - `asyncify_free_buffer` should accept a pointer to a buffer and the size of that buffer (both matching a previous `asyncify_malloc_buffer` call) and free it.

Wazzy will use these methods to allocate memory for itself, which makes async unwind/rewind more efficient.

### Multi Memory

Export a second memory named `asyncify_unwind_stack_memory_heap` and asyncify using:

```
wasm-opt module.wasm -o module-async.wasm --asyncify --enable-multimemory --pass-arg=asyncify-memory@asyncify_unwind_stack_memory_heap
```

Wazzy/Asyncify will use this second memory for all unwind/rewind operations, which is more efficient.