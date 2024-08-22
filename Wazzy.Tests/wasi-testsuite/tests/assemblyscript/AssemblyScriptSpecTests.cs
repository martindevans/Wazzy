namespace Wazzy.Tests.wasi_testsuite.tests.assemblyscript
{
    [TestClass]
    public class AssemblyScriptSpecTests
    {
        private static void Run(string path, bool logsOnlyFs = true)
        {
            using var runner = new WasiTestSuiteRunner(path, logsOnlyFs);
            runner.Run();
        }

        [TestMethod]
        public void ArgsGetMultipleArguments()
        {
            Run("assemblyscript/testsuite/args_get-multiple-arguments");
        }

        [TestMethod]
        public void ArgsSizesGetMultipleArguments()
        {
            Run("assemblyscript/testsuite/args_sizes_get-multiple-arguments");
        }

        [TestMethod]
        public void ArgsSizesGetNoArguments()
        {
            Run("assemblyscript/testsuite/args_sizes_get-no-arguments");
        }

        [TestMethod]
        public void EnvironGetMultipleVariables()
        {
            Run("assemblyscript/testsuite/environ_get-multiple-variables");
        }

        [TestMethod]
        public void EnvironSizesGetMultipleVariables()
        {
            Run("assemblyscript/testsuite/environ_sizes_get-multiple-variables");
        }

        [TestMethod]
        public void EnvironSizesGetNoVariables()
        {
            Run("assemblyscript/testsuite/environ_sizes_get-no-variables");
        }

        [TestMethod]
        public void FdWriteInvalidFd()
        {
            Run("assemblyscript/testsuite/fd_write-to-invalid-fd");
        }

        [TestMethod]
        public void FdWriteStdOut()
        {
            Run("assemblyscript/testsuite/fd_write-to-stdout");
        }

        [TestMethod]
        public void ProcExitFailure()
        {
            Run("assemblyscript/testsuite/proc_exit-failure");
        }

        [TestMethod]
        public void ProcExitSuccess()
        {
            Run("assemblyscript/testsuite/proc_exit-success");
        }

        [TestMethod]
        public void RandomGetNonZeroLength()
        {
            Run("assemblyscript/testsuite/random_get-non-zero-length");
        }

        [TestMethod]
        public void RandomGetZeroLength()
        {
            Run("assemblyscript/testsuite/random_get-zero-length");
        }
    }
}
