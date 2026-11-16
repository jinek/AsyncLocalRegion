using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncLocalRegion;
using NUnit.Framework;

namespace AsyncLocalRegionTests;

public class Tests
{
    [Test]
    public void CapturedContextDoesNotExposeDisposedRegion()
    {
        var parameter = new AsyncLocalParameter<int>();
        ExecutionContext context;
        using (parameter.StartRegion(30))
        {
            context = ExecutionContext.Capture()
                      ?? throw new InvalidOperationException("Execution context flow is suppressed");
        }

        ExecutionContext.Run(context, _ =>
        {
            Assert.IsFalse(parameter.HasCurrentValue);
            Assert.That(() => parameter.CurrentValue, Throws.TypeOf<AsyncLocalRegionException>());
        }, null);
    }

    [Test]
    public void CapturedContextResolvesNearestLiveRegion()
    {
        var parameter = new AsyncLocalParameter<int>();
        ExecutionContext context;
        using (parameter.StartRegion(10))
        {
            using (parameter.StartRegion(30))
            {
                context = ExecutionContext.Capture()
                          ?? throw new InvalidOperationException("Execution context flow is suppressed");
            }

            ExecutionContext.Run(context, _ =>
            {
                Assert.IsTrue(parameter.HasCurrentValue);
                Assert.AreEqual(10, parameter.CurrentValue);
            }, null);
        }

        // The same captured context must also skip both regions once both have ended.
        ExecutionContext.Run(context, _ =>
        {
            Assert.IsFalse(parameter.HasCurrentValue);
            Assert.That(() => parameter.CurrentValue, Throws.TypeOf<AsyncLocalRegionException>());
        }, null);
    }

    [Test]
    public async Task OverlappingAsyncRegionsKeepTheirOwnValues()
    {
        var parameter = new AsyncLocalParameter<int>();
        var releaseFirst = new TaskCompletionSource<bool>();
        var releaseSecond = new TaskCompletionSource<bool>();

        async Task<int> ReadInRegion(int value, Task release)
        {
            using (parameter.StartRegion(value))
            {
                await release;
                return parameter.CurrentValue;
            }
        }

        using (parameter.StartRegion(30))
        {
            var first = ReadInRegion(40, releaseFirst.Task);
            var second = ReadInRegion(50, releaseSecond.Task);
            try
            {
                Assert.AreEqual(30, parameter.CurrentValue);
                // Finish the first branch while the second scope is still open.
                releaseFirst.SetResult(true);
                Assert.AreEqual(40, await first);
                Assert.AreEqual(30, parameter.CurrentValue);

                releaseSecond.SetResult(true);
                Assert.AreEqual(50, await second);
                Assert.AreEqual(30, parameter.CurrentValue);
            }
            finally
            {
                releaseFirst.TrySetResult(true);
                releaseSecond.TrySetResult(true);
                await Task.WhenAll(first, second);
            }
        }

        Assert.IsFalse(parameter.HasCurrentValue);
    }

    [Test]
    public void DisposingOuterRegionFirstLeavesInnerRegionIntact()
    {
        var parameter = new AsyncLocalParameter<int>();
        using (IDisposable outer = parameter.StartRegion(30))
        {
            using (parameter.StartRegion(40))
            {
                Assert.Throws<InvalidOperationException>(() => outer.Dispose());
                Assert.AreEqual(40, parameter.CurrentValue);
            }

            Assert.AreEqual(30, parameter.CurrentValue);
        }

        Assert.IsFalse(parameter.HasCurrentValue);
    }

    [Test]
    public void TestCast()
    {
        const int valueToCheck = 2;

        using (TestClass.Value.StartRegion(valueToCheck))
        {
            Assert.AreEqual(valueToCheck, (int)TestClass.Value);
        }
    }

    [Test]
    public void TestOneThreadHasParameter()
    {
        const int valueToCheck = 2;

        using (TestClass.Value.StartRegion(valueToCheck))
        {
            Assert.AreEqual(valueToCheck, TestClass.Value.CurrentValue);
        }

        Assert.Throws<AsyncLocalRegionException>(() =>
        {
            int _ = TestClass.Value.CurrentValue;
        });
    }

    [Test]
    public void SeveralThreadsOneRegion()
    {
        const int value1 = 30;
        const int value2 = 40;

        var secondThread = new Task(() =>
        {
            using (TestClass.Value.StartRegion(value2))
            {
                Assert.AreEqual(value2, TestClass.Value.CurrentValue);
            }
        });

        Task firstThread = Task.Run(() =>
        {
            using (TestClass.Value.StartRegion(value1))
            {
                secondThread.Start();
                secondThread.Wait();
                Assert.AreEqual(value1, TestClass.Value.CurrentValue);
            }
        });
        firstThread.Wait();
        Assert.Throws<AsyncLocalRegionException>(() =>
        {
            int _ = TestClass.Value.CurrentValue;
        });
    }

    [Test]
    public void OneTaskFlowInnerRegions()
    {
        const int value1 = 30;
        const int value2 = 40;
        const int value3 = 50;
        using (TestClass.Value.StartRegion(value1))
        {
            Task.Run(() =>
            {
                using (TestClass.Value.StartRegion(value2))
                {
                    Task.Run(async () =>
                    {
                        Assert.AreEqual(value2, TestClass.Value.CurrentValue);
                        using (TestClass.Value.StartRegion(value3))
                        {
                            await AssertValue3();

                            async Task AssertValue3()
                            {
                                await Task.Yield();
                                Assert.AreEqual(value3, TestClass.Value.CurrentValue);
                            }
                        }

                        Assert.AreEqual(value2, TestClass.Value.CurrentValue);
                    }).Wait();
                    Assert.AreEqual(value2, TestClass.Value.CurrentValue);
                }

                Assert.AreEqual(value1, TestClass.Value.CurrentValue);
            }).Wait();
        }
    }

    [Test]
    public void OneThreadInnerRegions()
    {
        const int value1 = 30;
        const int value2 = 40;
        const int value3 = 50;
        using (TestClass.Value.StartRegion(value1))
        {
            using (TestClass.Value.StartRegion(value2))
            {
                Assert.AreEqual(value2, TestClass.Value.CurrentValue);
                using (TestClass.Value.StartRegion(value3))
                {
                    Assert.AreEqual(value3, TestClass.Value.CurrentValue);
                }

                Assert.AreEqual(value2, TestClass.Value.CurrentValue);
            }

            Assert.AreEqual(value1, TestClass.Value.CurrentValue);
        }
    }

    [Test]
    public void OneThreadTwoParameters()
    {
        const int value1 = 30;
        const int value2 = 40;

        using (TestClass.Value.StartRegion(value1))
        {
            Assert.AreEqual(value1, TestClass.Value.CurrentValue);
            Assert.Throws<AsyncLocalRegionException>(() =>
            {
                int _ = TestClass.SecondValue.CurrentValue;
            });

            using (TestClass.SecondValue.StartRegion(value2))
            {
                Assert.AreEqual(value1, TestClass.Value.CurrentValue);
                Assert.AreEqual(value2, TestClass.SecondValue.CurrentValue);
            }

            Assert.AreEqual(value1, TestClass.Value.CurrentValue);
            Assert.Throws<AsyncLocalRegionException>(() =>
            {
                int _ = TestClass.SecondValue.CurrentValue;
            });
        }

        Assert.Throws<AsyncLocalRegionException>(() =>
        {
            int _ = TestClass.Value.CurrentValue;
        });
        Assert.Throws<AsyncLocalRegionException>(() =>
        {
            int _ = TestClass.SecondValue.CurrentValue;
        });
    }

    [Test]
    public void TestHasNoValues()
    {
        const int value1 = 30;

        Assert.IsFalse(TestClass.VeryUntouchedValue.HasCurrentValue);
        Assert.IsFalse(TestClass.SecondValue.HasCurrentValue);
        Assert.IsFalse(TestClass.Value.HasCurrentValue);
        using (TestClass.VeryUntouchedValue.StartRegion(value1))
        {
            Assert.IsTrue(TestClass.VeryUntouchedValue.HasCurrentValue);
            Assert.IsFalse(TestClass.SecondValue.HasCurrentValue);
            Assert.IsFalse(TestClass.Value.HasCurrentValue);
        }

        Assert.IsFalse(TestClass.VeryUntouchedValue.HasCurrentValue);
        Assert.IsFalse(TestClass.SecondValue.HasCurrentValue);
        Assert.IsFalse(TestClass.Value.HasCurrentValue);
    }

    private static class TestClass
    {
        public static readonly AsyncLocalParameter<int> Value = new();
        public static readonly AsyncLocalParameter<int> SecondValue = new();
        public static readonly AsyncLocalParameter<int> VeryUntouchedValue = new();
    }
}