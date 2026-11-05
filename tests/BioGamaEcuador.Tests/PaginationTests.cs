using BioGamaEcuador.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class PaginationTests
{
    [Theory]
    [InlineData("PaginaActual", "TotalPaginas", "pagina")]
    [InlineData("Page", "TotalPages", "page")]
    public void View_context_preserves_the_application_path_and_parameter_names(string pageKey, string totalKey, string parameter)
    {
        var context = new ViewContext
        {
            HttpContext = new DefaultHttpContext(),
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };
        context.HttpContext.Request.PathBase = "/bio";
        context.HttpContext.Request.Path = "/Species";
        context.HttpContext.Request.QueryString = new QueryString("?busqueda=Oso%20Andino");
        context.ViewData[pageKey] = 3;
        context.ViewData[totalKey] = 8;
        var pagination = PaginationModel.From(context, parameter);
        Assert.Equal(3, pagination.Page);
        Assert.Equal(8, pagination.TotalPages);
        Assert.StartsWith("/bio/Species?", pagination.Link(4));
        Assert.Contains(parameter + "=4", pagination.Link(4));
        Assert.Contains("busqueda=Oso%20Andino", pagination.Link(4));
    }

    [Fact]
    public void Changing_page_preserves_filters_and_encodes_query_values()
    {
        var pagination = new PaginationModel(1, 5, "/Public/Catalogo", "pagina",
            new Dictionary<string, StringValues> { ["busqueda"]="Oso & flora", ["familia"]="Ursidae", ["pagina"]="1" });
        var link = pagination.Link(2);
        var query = QueryHelpers.ParseQuery(new Uri("https://localhost" + link).Query);
        Assert.Equal("2", query["pagina"]);
        Assert.Equal("Oso & flora", query["busqueda"]);
        Assert.Equal("Ursidae", query["familia"]);
        Assert.Equal(3, query.Count);
    }

    [Fact]
    public void Page_links_stay_local_and_inside_the_available_range()
    {
        var pagination = new PaginationModel(5, 10, "//untrusted.example/path", "pagina", new Dictionary<string,StringValues>());
        Assert.StartsWith("/?", pagination.Link(0));
        Assert.Contains("pagina=1", pagination.Link(0));
        Assert.Contains("pagina=10", pagination.Link(999));
        Assert.Equal(1, pagination.FirstPage);
        Assert.Equal(9, pagination.LastPage);
    }
}
