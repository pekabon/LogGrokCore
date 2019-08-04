using System.Linq;
using LogGrokCore.Data.IndexTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokCore.Data.Tests
{
    [TestClass]
    public class IndexTreeTests
    {

        [TestMethod]
        public void Smoke()
        {
            const int maxCount = 4000;
            var indexTree = new IndexTree<int, TestIndexTreeLeaf>(16, 
                i => new TestIndexTreeLeaf(i, 0));
            foreach (var value in Enumerable.Range(0, maxCount))
            {
                indexTree.Add(value);
            }

            for (var i = 0; i <= maxCount; i++)
            {
                var enumerable = indexTree.GetEnumerableFromIndex(i).ToList();
                Assert.IsTrue(
                    enumerable.SequenceEqual(Enumerable.Range(i, maxCount - i)),
                    $"Sequence not equal for {i}.");
            }
        }
    }
}