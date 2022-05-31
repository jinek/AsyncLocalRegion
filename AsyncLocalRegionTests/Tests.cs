using System.Threading.Tasks;
using AsyncLocalRegion;
using NUnit.Framework;

namespace AsyncLocalRegionTests
{
    public class Tests
    {
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

        private static class TestClass
        {
            public static readonly AsyncLocalParameter<int> Value = new AsyncLocalParameter<int>();
            public static readonly AsyncLocalParameter<int> SecondValue = new AsyncLocalParameter<int>();
        }
    }
}