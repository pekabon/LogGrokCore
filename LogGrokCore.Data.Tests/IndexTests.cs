using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokCore.Data.Tests
{
    [TestClass]
    public class IndexTests
    {
        [TestMethod]
        public void TestEmpty()
        {
            var index = new Index.Index();
            Assert.AreEqual(false, index.EnumerateFrom(0).Any());
        }

        [TestMethod]
        public void TestEnumerateAll()
        {
            var testSequence = Enumerable.Range(0, 1024);
            var index = new Index.Index(1);
            foreach (var value in testSequence)
            {
                index.Add(value);
            }
            
            Assert.IsTrue(testSequence.SequenceEqual(index.EnumerateFrom(0)));

        }
    }
}