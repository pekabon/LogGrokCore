using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace LogGrokCore.Tests
{
    [TestClass]
    public class ToggleMarksHandlerTests
    {
        [TestMethod]
        public void EmptyItems()
        {
            List<LineMarkMock> items = new List<LineMarkMock>();

            RoutedCommands.ToggleMarksHandler(items);
        }
        [TestMethod]
        public void OneUnmarked()
        {
            List<LineMarkMock> items = new List<LineMarkMock>();
            items.Add(new LineMarkMock() { IsMarked = false });

            RoutedCommands.ToggleMarksHandler(items);
            Assert.AreEqual(true, items[0].IsMarked);
        }
        [TestMethod]
        public void TwoUnmarked()
        {
            List<LineMarkMock> items = new List<LineMarkMock>();
            items.Add(new LineMarkMock() { IsMarked = false });
            items.Add(new LineMarkMock() { IsMarked = false });

            RoutedCommands.ToggleMarksHandler(items);
            Assert.AreEqual(true, items[0].IsMarked);
            Assert.AreEqual(true, items[1].IsMarked);
        }
        [TestMethod]
        public void OneMarked()
        {
            List<LineMarkMock> items = new List<LineMarkMock>();
            items.Add(new LineMarkMock() { IsMarked = true });

            RoutedCommands.ToggleMarksHandler(items);
            Assert.AreEqual(false, items[0].IsMarked);
        }
        [TestMethod]
        public void TwoMarked()
        {
            List<LineMarkMock> items = new List<LineMarkMock>();
            items.Add(new LineMarkMock() { IsMarked = true });
            items.Add(new LineMarkMock() { IsMarked = true });

            RoutedCommands.ToggleMarksHandler(items);
            Assert.AreEqual(false, items[0].IsMarked);
            Assert.AreEqual(false, items[1].IsMarked);
        }
        [TestMethod]
        public void MarkedUnmarked1()
        {
            List<LineMarkMock> items = new List<LineMarkMock>();
            items.Add(new LineMarkMock() { IsMarked = false });
            items.Add(new LineMarkMock() { IsMarked = true });

            RoutedCommands.ToggleMarksHandler(items);
            Assert.AreEqual(true, items[0].IsMarked);
            Assert.AreEqual(true, items[1].IsMarked);
        }
        [TestMethod]
        public void MarkedUnmarked2()
        {
            List<LineMarkMock> items = new List<LineMarkMock>();
            items.Add(new LineMarkMock() { IsMarked = true });
            items.Add(new LineMarkMock() { IsMarked = false });

            RoutedCommands.ToggleMarksHandler(items);
            Assert.AreEqual(true, items[0].IsMarked);
            Assert.AreEqual(true, items[1].IsMarked);
        }
    }
}