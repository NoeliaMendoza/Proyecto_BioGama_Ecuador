using BioGamaEcuador.Data;
using BioGamaEcuador.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class QueryQualityTests
{
    [Fact]
    public void Course_search_uses_parameterized_case_insensitive_SQL()
    {
        using var database = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused").Options);
        var query = CourseSearch.Filter(database.Courses, "  Amazonía  ");
        var sql = query.ToQueryString();
        Assert.Contains(" ILIKE ", sql);
        Assert.Contains(" ESCAPE ", sql);
        Assert.DoesNotContain("lower(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%Amazonía%", sql);
        Assert.Same(database.Courses, CourseSearch.Filter(database.Courses, null));
    }

    [Fact]
    public void Search_metacharacters_remain_literal_characters()
    {
        Assert.Equal("%100\\%\\_\\\\ test%", CourseSearch.ContainsPattern("100%_\\ test"));
    }

    [Fact]
    public async Task Anonymous_cart_count_does_not_query_the_database()
    {
        await using var database = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var summary = new CartSummary(database);
        Assert.Equal(0, await summary.CountAsync(null));
    }
}
