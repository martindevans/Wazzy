using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Wasmtime;
using Wazzy.Async.Extensions;
using Wazzy.Async;
using Wazzy.WasiSnapshotPreview1.Clock;
using Wazzy.WasiSnapshotPreview1.Environment;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Builder;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;
using Wazzy.WasiSnapshotPreview1.Process;
using Wazzy.WasiSnapshotPreview1.Random;
using Exception = System.Exception;

namespace Wazzy.Tests.wasi_testsuite
{
    public sealed class WasiTestSuiteRunner
        : IDisposable
    {
        private readonly bool _logOnlyFs;
        public const string BasePath = "wasi-testsuite/tests";

        private readonly string _wasm;
        private readonly JsonSpec _spec;

        public Dictionary<string, string> ExtraEnv = new();

        public WasiTestSuiteRunner(string name, bool logOnlyFs)
        {
            _logOnlyFs = logOnlyFs;
            var fullPath = Path.Combine(BasePath, name);

            _wasm = fullPath + ".wasm";
            Assert.IsTrue(File.Exists(_wasm), $"WASM File `{_wasm}` does not exist!");

            // Remove the _async tag when looking for the JSON spec file
            var jsonPath = fullPath.Replace("_async", "") + ".json";
            if (!File.Exists(jsonPath))
            {
                _spec = new JsonSpec();
            }
            else
            {
                var d = new DataContractJsonSerializer(typeof(JsonSpec), new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true,
                });
                _spec = (JsonSpec)d.ReadObject(new MemoryStream(File.ReadAllBytes(jsonPath)))!;
            }
        }

        public void Run()
        {
            using var helper = new WasmTestHelper(_wasm);

            // Add basic wasi stuff
            helper.AddWasiFeature(new SeededRandomSource(9593));
            helper.AddWasiFeature(new RealtimeClock());

            // Set env and args
            var args = _spec.Args.ToList();
            args.Insert(0, Path.GetFileName(_wasm));
            var env = helper.AddWasiFeature(new BasicEnvironment(
                _spec.Env,
                args
            ));

            foreach (var (k, v) in ExtraEnv)
                env.SetEnvironmentVariable(k, v);

            // Create VFS
            StringBuilder stdout;
            StringBuilder stderr;
            if (_logOnlyFs)
            {
                stdout = new StringBuilder();
                stderr = new StringBuilder();
                var vfs = new WriteToTextWriterFilesystem(new StringWriter(stdout), new StringWriter(stderr));
                helper.AddWasiFeature(vfs);
            }
            else
            {
                (var vfs, stdout, stderr) = SetupVfs();
                helper.AddWasiFeature(vfs);
            }

            // Setup process to capture exit code
            helper.AddWasiFeature(new ThrowExitProcess());

            // Run the test (pumping until async execution is complete)
            var instance = helper.Instantiate();
            var start = instance.GetAction("_start")!;
            var exitCode = 0;
            try
            {
                start();

                if (instance.IsAsyncCapable())
                {
                    while (instance.GetAsyncState() == AsyncState.Suspending)
                    {
                        var stack = instance.StopUnwind();
                        instance.StartRewind(stack);

                        start();
                    }
                }
            }
            catch (WasmtimeException e)
            {
                if (e.GetBaseException() is ThrowExitProcessException ex)
                    exitCode = ex.ExitCode;
            }

            // Check exit code
            Assert.AreEqual(_spec.ExitCode, exitCode);

            // Checks outputs
            Assert.AreEqual(_spec.StdOut, stdout.ToString());
            Assert.AreEqual(_spec.StdErr, stderr.ToString());
        }

        private (VirtualFileSystem, StringBuilder, StringBuilder) SetupVfs()
        {
            var vfs = new VirtualFileSystemBuilder();

            vfs.WithVirtualRoot(root =>
            {
                foreach (var specDir in _spec.Dirs)
                {
                    var path = Path.Combine(Path.GetDirectoryName(_wasm)!, specDir);
                    root.MapDirectory(specDir, path);
                    vfs.WithPreopen(specDir);


                    var cleanup = Directory.EnumerateFileSystemEntries(path, "*.cleanup", SearchOption.AllDirectories);
                    foreach (var item in cleanup)
                        new FileInfo(item).Delete();
                }
            });

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            vfs.WithPipes(new ZeroFile(), new StringBuilderLog(stdout), new StringBuilderLog(stderr));

            return (vfs.Build(), stdout, stderr);
        }

        public void Dispose()
        {
            foreach (var specDir in _spec.Dirs)
            {
                var path = Path.Combine(Path.GetDirectoryName(_wasm)!, specDir);

                var cleanupDirs = Directory.EnumerateDirectories(path, "*.cleanup", SearchOption.AllDirectories);
                foreach (var item in cleanupDirs)
                {
                    try
                    {
                        new DirectoryInfo(item).Delete();
                    }
                    catch (Exception ex)
                    {
                        // We tried our best :(
                        Console.Error.WriteLine($"Failed to perform post-test cleanup: {ex.Message}");
                    }
                }

                var cleanupFiles = Directory.EnumerateFiles(path, "*.cleanup", SearchOption.AllDirectories);
                foreach (var item in cleanupFiles)
                {
                    try
                    {
                        new FileInfo(item).Delete();
                    }
                    catch (Exception ex)
                    {
                        // We tried our best :(
                        Console.Error.WriteLine($"Failed to perform post-test cleanup: {ex.Message}");
                    }
                }
            }
        }

        [DataContract]
        private class JsonSpec
        {
            [DataMember(Name = "args")] private string[]? _args;
            [DataMember(Name = "dirs")] private string[]? _dirs;
            [DataMember(Name = "env")] private Dictionary<string, string>? _env = [ ];
            [DataMember(Name = "exit_code")] public int _exitcode;
            [DataMember(Name = "stderr")] public string? _stderr;
            [DataMember(Name = "stdout")] public string? _stdout;

            public string[] Args => _args ?? [ ];
            public string[] Dirs => _dirs ?? [ ];
            public Dictionary<string, string> Env => _env ?? [ ];

            public int ExitCode => _exitcode;
            public string StdErr => _stderr ?? "";
            public string StdOut => _stdout ?? "";
        }
    }
}
