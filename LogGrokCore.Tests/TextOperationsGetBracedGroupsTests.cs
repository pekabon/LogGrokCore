using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokCore.Tests;

[TestClass]
public class TextOperationsGetBracedGroupsTests
{
    [DataTestMethod]
    [DataRow("{}", 0, 2)]
    [DataRow("{{}}", 0, 4)]
    [DataRow("{}{", 0, 2)]
    [DataRow("}{}", 1, 2)]
    [DataRow("AAA{{}}", 3, 4)]
    [DataRow("AAA{{}}AAA", 3, 4)]
    [DataRow("AAA{AA{AA}AA}AA", 3, 10)]
    public void TestGetBracedGroups(string source, int start, int length)
    {
        var result = TextOperations.GetBracedGroups(source.AsSpan()).ToList();
        
        Assert.AreEqual(1, result.Count);
        var (actualStart, actualLength) = result[0];
        Assert.AreEqual(start, actualStart);
        Assert.AreEqual(length, actualLength);
    }
    
    [DataTestMethod]
    [DataRow("{}{}", 0, 2, 2, 2)]
    [DataRow("{}AAA{}", 0, 2, 5, 2)]
    public void TestGetBracedGroups_TwoGroups(string source, int start1, int length1, int start2, int length2)
    {
        var result = TextOperations.GetBracedGroups(source.AsSpan()).ToList();
        
        Assert.AreEqual(2, result.Count);
        var (actualStart1, actualLength1) = result[0];
        var (actualStart2, actualLength2) = result[1];

        Assert.AreEqual(start1, actualStart1);
        Assert.AreEqual(length1, actualLength1);
        Assert.AreEqual(start2, actualStart2);
        Assert.AreEqual(length2, actualLength2);
    }

    [DataTestMethod]
    [DataRow("{{}{}")]
    [DataRow("{AAA{}")]
    [DataRow("")]
    [DataRow("{")]
    [DataRow("}")]
    public void TestGetBracedGroups_EmptyResult(string source)
    {
        var result = TextOperations.GetBracedGroups(source.AsSpan()).ToList();
        
        Assert.AreEqual(0, result.Count);
    }

}