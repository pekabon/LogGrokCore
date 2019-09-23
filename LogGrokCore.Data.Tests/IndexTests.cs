using System.Collections.Generic;
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
            Assert.AreEqual(false, index.GetEnumerableFromValue(0).Any());
        }

        [TestMethod]
        public void TestEnumerateAllSmall()
        {
            var testSequence = Enumerable.Range(0, 3).ToList();
            var index = FillIndex(testSequence);
            Assert.IsTrue(testSequence.SequenceEqual(index.GetEnumerableFromValue(0)));
        }

        [TestMethod]
        public void TestEnumerateAll()
        {
            var testSequence = Enumerable.Range(0, 1024).ToList();
            var index = FillIndex(testSequence);
            Assert.IsTrue(testSequence.SequenceEqual(index.GetEnumerableFromValue(0)));
        }

        [TestMethod]
        public void TestFindLast()
        {
            var testSequence = Enumerable.Range(0, 1024).ToList();
            var index = FillIndex(testSequence);

            var foundSequence = index.GetEnumerableFromValue(testSequence.Last()).ToList();
            Assert.AreEqual(1, foundSequence.Count);
            var lastValue = foundSequence.SingleOrDefault();
            Assert.AreEqual(testSequence.Last(), lastValue);
        }

        [TestMethod]
        public void TestFindValueSmoke()
        {
            var sequenceLength = 8193;
            var testSequence = Enumerable.Range(0, sequenceLength).ToList();
            var index = FillIndex(testSequence);
            for (var i = 0; i < sequenceLength; i++)
            {
                var enumerable = index.GetEnumerableFromValue(i);
                Assert.IsTrue(testSequence.Skip(i).SequenceEqual(enumerable), $"Failed for i={i}.");
            }
        }

        private Index.Index FillIndex(IEnumerable<int> sequence)
        {
            var index = new Index.Index(16);
            foreach (var value in sequence)
            {
                index.Add(value);
            }

            return index;
        }
    }
}