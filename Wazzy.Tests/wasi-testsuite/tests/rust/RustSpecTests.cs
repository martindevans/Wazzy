namespace Wazzy.Tests.wasi_testsuite.tests.rust
{
    [TestClass]
    public class RustSpecTests
    {
        private static void Run(string path, bool logsOnlyFs = true)
        {
            using var runner = new WasiTestSuiteRunner(path, logsOnlyFs);

            // Remove this once fd_allocate is supported. Probably never, since
            // it seems to have been removed in future versions of WASI.
            runner.ExtraEnv.Add("NO_FD_ALLOCATE", "1");

            // Get better backtraces from Rust panics
            runner.ExtraEnv.Add("WASMTIME_BACKTRACE_DETAILS", "1");

            runner.Run();
        }

        [TestMethod]
        public void BigRandomBuf()
        {
            Run("rust/testsuite/big_random_buf");
        }

        [TestMethod]
        public void BigRandomBufAsync()
        {
            Run("rust/testsuite/big_random_buf_async");
        }

        [TestMethod]
        public void ClockTimeGet()
        {
            Run("rust/testsuite/clock_time_get");
        }

        [TestMethod]
        public void ClosePreopen()
        {
            Run("rust/testsuite/close_preopen", false);
        }

        // Todo: disabled. Currently this test attempts to delete a file while there is an open file handle.
        //                 However, dotnet prevents this from happening because you can't delete the file
        //                 while it is open! Need to disable this safety check.
        //[TestMethod]
        //public void DanglingFd()
        //{
        //    Run("rust/testsuite/dangling_fd", false);
        //}

        // Note: symlinks are not supported by the VFS at the moment
        //[TestMethod]
        //public void DanglingSymlink()
        //{
        //    Run("rust/testsuite/dangling_symlink", false);
        //}

        // Note: failing due to rights (which are not supported by the VFS at the moment
        //[TestMethod]
        //public void DirectorySeek()
        //{
        //    Run("rust/testsuite/directory_seek", false);
        //}

        [TestMethod]
        public void FdAdvise()
        {
            Run("rust/testsuite/fd_advise", false);
        }

        [TestMethod]
        public void FdFilestatGet()
        {
            Run("rust/testsuite/fd_filestat_get", false);
        }

        [TestMethod]
        public void FdFilestatSet()
        {
            Run("rust/testsuite/fd_filestat_set", false);
        }

        //todo: failing
        //[TestMethod]
        //public void FdFlagsSet()
        //{
        //    //todo: https://github.com/WebAssembly/wasi-testsuite/blob/main/tests/rust/src/bin/fd_flags_set.rs
        //    Run("rust/testsuite/fd_flags_set", false);
        //}

        //todo: failing
        //[TestMethod]
        //public void FdFlagsSetAsync()
        //{
        //    Run("rust/testsuite/fd_flags_set_async", false);
        //}

        // todo:failing due to missing two files in an empty directory: . and ..
        //[TestMethod]
        //public void FdReadDir()
        //{
        //    Run("rust/testsuite/fd_readdir", false);
        //}

        //Note: failing due to rights (which are not supported by the VFS at the moment
        //[TestMethod]
        //public void InterestingPaths()
        //{
        //    Run("rust/testsuite/interesting_paths", false);
        //}
    }
}
