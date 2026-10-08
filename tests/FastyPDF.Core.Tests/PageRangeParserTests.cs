using FastyPDF.Core.Exceptions;
using FastyPDF.Core.Models;
using FastyPDF.Core.Services;
using Xunit;

namespace FastyPDF.Core.Tests;

public class PageRangeParserTests
{
    [Fact]
    public void Parse_SinglePage_ReturnsCorrectRange()
    {
        var ranges = PageRangeParser.Parse("3", 5);
        Assert.Single(ranges);
        Assert.Equal(3, ranges[0].StartPage);
        Assert.Equal(3, ranges[0].EndPage);
    }

    [Fact]
    public void Parse_ValidRanges_ReturnsRanges()
    {
        var ranges = PageRangeParser.Parse("1-3, 5, 7-8", 10);
        Assert.Equal(3, ranges.Count);
        Assert.Equal(new PageRange(1, 3), ranges[0]);
        Assert.Equal(new PageRange(5, 5), ranges[1]);
        Assert.Equal(new PageRange(7, 8), ranges[2]);
    }

    [Fact]
    public void Parse_ReversedRange_AutoReverses()
    {
        var ranges = PageRangeParser.Parse("5-2", 10);
        Assert.Single(ranges);
        Assert.Equal(2, ranges[0].StartPage);
        Assert.Equal(5, ranges[0].EndPage);
    }

    [Fact]
    public void Parse_EmptyOrWhitespace_ThrowsInvalidPageRange()
    {
        var ex1 = Assert.Throws<PdfOperationException>(() => PageRangeParser.Parse("", 5));
        Assert.Equal(PdfErrorKind.InvalidPageRange, ex1.Kind);

        var ex2 = Assert.Throws<PdfOperationException>(() => PageRangeParser.Parse("   ", 5));
        Assert.Equal(PdfErrorKind.InvalidPageRange, ex2.Kind);
    }

    [Fact]
    public void Parse_ZeroPageCount_ThrowsEmptyInput()
    {
        var ex = Assert.Throws<PdfOperationException>(() => PageRangeParser.Parse("1-2", 0));
        Assert.Equal(PdfErrorKind.EmptyInput, ex.Kind);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("6")]
    [InlineData("1-6")]
    [InlineData("0-3")]
    public void Parse_OutOfBounds_ThrowsInvalidPageRange(string input)
    {
        var ex = Assert.Throws<PdfOperationException>(() => PageRangeParser.Parse(input, 5));
        Assert.Equal(PdfErrorKind.InvalidPageRange, ex.Kind);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1-2-3")]
    [InlineData("1-")]
    [InlineData("-2")]
    public void Parse_MalformedInput_ThrowsInvalidPageRange(string input)
    {
        var ex = Assert.Throws<PdfOperationException>(() => PageRangeParser.Parse(input, 5));
        Assert.Equal(PdfErrorKind.InvalidPageRange, ex.Kind);
    }

    [Fact]
    public void ToPageIndices_ConvertsAndDeduplicatesAndOrders()
    {
        var ranges = new List<PageRange>
        {
            new(3, 5),
            new(1, 2),
            new(4, 6)
        };

        var indices = PageRangeParser.ToPageIndices(ranges);
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, indices);
    }
}
