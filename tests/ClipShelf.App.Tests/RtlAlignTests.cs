using ClipShelf.App.Common;
using Microsoft.UI.Xaml;

namespace ClipShelf.App.Tests;

public sealed class RtlAlignTests
{
    [Theory]
    [InlineData(HorizontalAlignment.Left, true, HorizontalAlignment.Right)]
    [InlineData(HorizontalAlignment.Right, true, HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Left, false, HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Right, false, HorizontalAlignment.Right)]
    [InlineData(HorizontalAlignment.Center, true, HorizontalAlignment.Center)]
    public void Alignment_flips_only_in_rtl(HorizontalAlignment desired, bool rtl, HorizontalAlignment expected)
    {
        Assert.Equal(expected, RtlAlign.For(desired, rtl));
    }
}
