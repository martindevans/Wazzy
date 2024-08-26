using System.Collections;
using Wazzy.Coroutines;

namespace Wazzy.Tests;

[TestClass]
public class CoroutineTests
{
    [TestMethod]
    public void SimpleCoroutine()
    {
        var coro = Simple();

        Assert.IsFalse(coro.HasResult);
        Assert.IsTrue(coro.Resume());
        Assert.IsFalse(coro.HasResult);
        Assert.IsFalse(coro.TryGetResult(out _));
        Assert.IsFalse(coro.Resume());
        Assert.IsFalse(coro.Resume());
        Assert.IsTrue(coro.HasResult);
        Assert.IsTrue(coro.TryGetResult(out var result));
        Assert.AreEqual(12, result);

        static Coroutine<int> Simple()
        {
            return new Coroutine<int>(SimpleInner());

            IEnumerator SimpleInner()
            {
                yield return null;
                yield return 12;
            }
        }
    }

    [TestMethod]
    public void NestedCoroutine()
    {
        var coro = Nested();

        Assert.IsFalse(coro.HasResult);
        Assert.IsTrue(coro.Resume());
        Assert.IsTrue(coro.Resume());
        Assert.IsTrue(coro.Resume());
        Assert.IsTrue(coro.Resume());
        Assert.IsFalse(coro.HasResult);
        Assert.IsFalse(coro.Resume());
        Assert.IsTrue(coro.HasResult);
        Assert.IsTrue(coro.TryGetResult(out var result));
        Assert.AreEqual(12, result);

        static Coroutine<int> Nested()
        {
            return new Coroutine<int>(RootInner());

            IEnumerator RootInner()
            {
                yield return NestedInner();
                yield return FinalInner();
            }

            IEnumerator NestedInner()
            {
                yield return null;
            }

            IEnumerator FinalInner()
            {
                yield return 12;
            }
        }
    }

    [TestMethod]
    public void ErrorNoResultCoroutine()
    {
        var coro = NoResult();

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            for (var i = 0; i < 128; i++)
            {
                if (!coro.Resume())
                    break;
            }
        });

        static Coroutine<int> NoResult()
        {
            return new Coroutine<int>(Inner());

            IEnumerator Inner()
            {
                yield return null;
            }
        }
    }

    [TestMethod]
    public void ExceptionCoroutine()
    {
        var coro = Throw();

        Assert.IsFalse(coro.HasResult);
        Assert.IsTrue(coro.Resume());
        Assert.IsFalse(coro.Resume());

        Assert.IsTrue(coro.HasResult);
        Assert.IsTrue(coro.HasExceptionResult);

        Assert.ThrowsException<Exception>(() =>
        {
            Assert.IsTrue(coro.TryGetResult(out var result));
        });

        static Coroutine<int> Throw()
        {
            return new Coroutine<int>(Inner());

            IEnumerator Inner()
            {
                yield return null;
                throw new Exception("ex");
            }
        }
    }
}