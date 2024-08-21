namespace Wazzy.Tests.wasi_testsuite.tests.c
{
    [TestClass]
    public class CSpecTests
    {
        private static void Run(string path, bool logsOnlyFs = true)
        {
            using var runner = new WasiTestSuiteRunner(path, logsOnlyFs);
            runner.Run();
        }

        [TestMethod]
        public void ClockGetresMonotonic()
        {
            Run("c/testsuite/clock_getres-monotonic");
        }

        [TestMethod]
        public void ClockGetresRealtime()
        {
            Run("c/testsuite/clock_getres-realtime");
        }

        [TestMethod]
        public void ClockGettimeMonotonic()
        {
            Run("c/testsuite/clock_gettime-monotonic");
        }

        [TestMethod]
        public void ClockGettimeRealtime()
        {
            Run("c/testsuite/clock_gettime-realtime");
        }

        // Note: VFS does not support inodes (everything has inode == 0)
        //[TestMethod]
        //public void FdOpendirWithAccess()
        //{
        //    Run("c/testsuite/fdopendir-with-access", false);
        //}

        [TestMethod]
        public void FOpenWithAccess()
        {
            Run("c/testsuite/fopen-with-access", false);
        }

        // Note: VFS does not support file rights
        //[TestMethod]
        //public void FOpendirWithNoAccess()
        //{
        //    Run("c/testsuite/fopendir-with-no-access", false);
        //}

        [TestMethod]
        public void LSeek()
        {
            Run("c/testsuite/lseek", false);
        }

        [TestMethod]
        public void PReadWithAccess()
        {
            Run("c/testsuite/pread-with-access", false);
        }

        [TestMethod]
        public void PWriteWithAccess()
        {
            Run("c/testsuite/pwrite-with-access", false);
        }

        // Note: VFS does not support inodes (everything has inode == 0)
        //[TestMethod]
        //public void StatDevIno()
        //{
        //    Run("c/testsuite/stat-dev-ino", false);
        //}
    }
}
