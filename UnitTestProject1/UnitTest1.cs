using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            short rssi = -50;
            int i = rssi;
            Console.WriteLine($"i={i}");
        }
    }
}
