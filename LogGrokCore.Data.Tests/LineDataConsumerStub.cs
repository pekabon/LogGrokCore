using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LogGrokCore.Data.Tests
{

    public class LineDataConsumerStub : ILineDataConsumer
    {
        public void AddLineData(uint lineNumber, Span<byte> lineData)
        {
        }
    }
}
