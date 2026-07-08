public class PagedResultTests
{
    [Fact]
    public void Constructor_WhenTotalItemsIsZero_ShouldSetTotalPagesToZero()
    {
        PagedResult<RecipeSummary> result = new PagedResult<RecipeSummary>(
            new List<RecipeSummary>(),
            pageNumber: 1,
            pageSize: 10,
            totalItems: 0);

        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void Constructor_WhenTotalItemsFitsOnOnePage_ShouldSetTotalPagesToOne()
    {
        PagedResult<RecipeSummary> result = new PagedResult<RecipeSummary>(
            new List<RecipeSummary> { new RecipeSummary() },
            pageNumber: 1,
            pageSize: 10,
            totalItems: 1);

        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public void Constructor_WhenTotalItemsNeedsMultiplePages_ShouldRoundTotalPagesUp()
    {
        PagedResult<RecipeSummary> result = new PagedResult<RecipeSummary>(
            new List<RecipeSummary>(),
            pageNumber: 1,
            pageSize: 10,
            totalItems: 11);

        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public void Constructor_WhenPageNumberIsInvalid_ShouldNormalizePageNumberToOne()
    {
        PagedResult<RecipeSummary> result = new PagedResult<RecipeSummary>(
            new List<RecipeSummary>(),
            pageNumber: 0,
            pageSize: 10,
            totalItems: 11);

        Assert.Equal(1, result.PageNumber);
    }

    [Fact]
    public void Constructor_WhenPageHasNeighbors_ShouldExposePreviousAndNextFlags()
    {
        PagedResult<RecipeSummary> result = new PagedResult<RecipeSummary>(
            new List<RecipeSummary>(),
            pageNumber: 2,
            pageSize: 10,
            totalItems: 25);

        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }
}
