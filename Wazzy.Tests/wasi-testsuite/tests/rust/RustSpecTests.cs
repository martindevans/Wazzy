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

            runner.Run();
        }

        [TestMethod]
        public void BigRandomBuf()
        {
            Run("rust/testsuite/big_random_buf");
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

        [TestMethod]
        public void DanglingFd()
        {
            Run("rust/testsuite/dangling_fd", false);
        }

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

        [TestMethod]
        public void FdFlagsSet()
        {
            Run("rust/testsuite/fd_flags_set", false);
        }

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
