using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokCore.Tests
{
    [TestClass]
    public class TextOperationsTests
    {
        private const string ThreeLines = "Hello\r\nworld\r\n!";
        private const string OneLine = "Hello";
        
        [TestMethod]
        public void TestCountLinesEmpty()
        {
            Assert.AreEqual(0, TextOperations.CountLines(""));
        }

        [TestMethod]
        public void TestCountLinesOne()
        {
            Assert.AreEqual(1, TextOperations.CountLines(OneLine));
        }

        [TestMethod]
        public void TestCountLinesThree()
        {
            Assert.AreEqual(3, TextOperations.CountLines(ThreeLines));
        }

        [TestMethod]
        public void TestTrimLinesEmpty()
        {
            var (resultString, linesTrimmed) = TextOperations.TrimLines("", 3);
            Assert.AreEqual("", resultString);
            Assert.AreEqual(0, linesTrimmed);
        }
        
        [TestMethod]
        public void TestDontTrimLines()
        {
            var (resultString, linesTrimmed) = TextOperations.TrimLines(ThreeLines, 3);
            Assert.AreEqual(ThreeLines, resultString);
            Assert.AreEqual(0, linesTrimmed);
        }
        
        [TestMethod]
        public void TestTrimLinesZero()
        {
            var (resultString, linesTrimmed) = TextOperations.TrimLines(ThreeLines, 0);
            Assert.AreEqual("", resultString);
            Assert.AreEqual(3, linesTrimmed);
        }

        [TestMethod]
        public void TestTrimSomeLines()
        {
            var (resultString, linesTrimmed) = TextOperations.TrimLines(ThreeLines, 1);
            Assert.AreEqual("Hello\r\n", resultString);
            Assert.AreEqual(2, linesTrimmed);
        }
        
        [TestMethod]
        public void TestTrimSomeLines2()
        {
            var (resultString, linesTrimmed) = TextOperations.TrimLines(ThreeLines, 2);
            Assert.AreEqual("Hello\r\nworld\r\n", resultString);
            Assert.AreEqual(1, linesTrimmed);
        }
        }
}