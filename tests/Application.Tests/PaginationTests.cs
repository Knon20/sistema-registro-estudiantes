using Application.Common;
using FluentAssertions;

namespace Application.Tests;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(1, 20, 1, 20)]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-5, 20, 1, 20)]
    [InlineData(3, 10, 3, 10)]
    [InlineData(1, 0, 1, 1)]
    [InlineData(1, -3, 1, 1)]
    [InlineData(2, 500, 2, 100)]
    public void Normalize_clamps_page_and_page_size(int page, int pageSize, int expPage, int expSize)
    {
        var req = PageRequest.Normalize(page, pageSize);
        req.Page.Should().Be(expPage);
        req.PageSize.Should().Be(expSize);
    }

    [Fact]
    public void Skip_is_zero_based_from_page()
    {
        PageRequest.Normalize(3, 10).Skip.Should().Be(20);
        PageRequest.Normalize(1, 10).Skip.Should().Be(0);
    }
}
