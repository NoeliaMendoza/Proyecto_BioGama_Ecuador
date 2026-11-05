using BioGamaEcuador.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class SharedFormTests
{
    [Theory]
    [InlineData("Families", "Name", "Kingdom")]
    [InlineData("Locations", "PlaceName", "NaturalReserveId")]
    [InlineData("NaturalReserves", "Name", "SurfaceHectares")]
    public async Task Shared_fields_preserve_unprefixed_binding_names(string folder, string first, string second)
    {
        object model = folder switch
        {
            "Families" => new Family { Name = "Felidae" },
            "Locations" => new Location { PlaceName = "Bosque" },
            _ => new NaturalReserve { Name = "Reserva" }
        };
        var html = await Render(folder, model);
        Assert.Contains($"name=\"{first}\"", html);
        Assert.Contains($"name=\"{second}\"", html);
        Assert.Contains($"data-valmsg-for=\"{first}\"", html);
        Assert.DoesNotContain("name=\"Model.", html);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Species_preserves_creation_and_editing_selection_defaults(bool create)
    {
        var html = await Render("Species", new Species { CommonName = "Jaguar", FamilyId = 5 },
            new() { ["IsCreateForm"] = create });
        Assert.Contains("name=\"CommonName\"", html);
        Assert.Contains("value=\"Jaguar\"", html);
        Assert.Contains("name=\"FamilyId\"", html);
        Assert.Contains($"<option value=\"{(create ? "0" : "")}\">", html);
        Assert.Equal(create, html.Contains("placeholder=\"https://...\"", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Record_form_preserves_investigator_selection_and_encodes_readonly_name(bool investigator)
    {
        var html = await Render("Records", new BioGamaEcuador.Models.Record { ResearcherId = 7 },
            new() { ["IsCurrentInvestigator"] = investigator, ["CurrentResearcherName"] = "<script>alert(1)</script>" });
        Assert.Contains("name=\"SpeciesId\"", html);
        Assert.Contains("name=\"ObservedQuantity\"", html);
        Assert.Contains("name=\"ResearcherId\"", html);
        Assert.Equal(investigator, System.Text.RegularExpressions.Regex.IsMatch(html,
            "<input\\b(?=[^>]*type=\"hidden\")(?=[^>]*name=\"ResearcherId\")[^>]*>",
            System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1)));
        Assert.Equal(!investigator, System.Text.RegularExpressions.Regex.IsMatch(html,
            "<select\\b[^>]*name=\"ResearcherId\"", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1)));
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        if (investigator)
        {
            Assert.Contains("&lt;script&gt;", html);
            Assert.Contains("id=\"CurrentResearcherName\"", html);
            Assert.Contains("for=\"CurrentResearcherName\"", html);
        }
    }

    [Fact]
    public async Task Shared_stat_cards_encode_labels_and_preserve_values()
    {
        var cards = new[] { new StatCardModel("<script>unsafe()</script>", 25, "bi-bug", "primary", "col-md-4") };
        var html = await Render("Shared", cards, viewName: "_StatCards");
        Assert.Contains("25", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>unsafe()", html);
        Assert.Contains("col-md-4", html);
    }

    [Theory]
    [InlineData("Administrador", false, true, true)]
    [InlineData("Investigador", false, true, false)]
    [InlineData("Investigador", true, false, false)]
    [InlineData("UsuarioPublico", false, false, false)]
    public async Task Shared_row_actions_preserve_role_permissions(string role, bool adminOnly, bool edit, bool delete)
    {
        var html = await Render("Shared", new RowActionsModel(7, AdminOnlyEdit: adminOnly),
            viewName: "_RowActions", role: role);
        Assert.Contains("btn-action view", html);
        Assert.Contains("/Families/Details/7", html);
        Assert.Equal(edit, html.Contains("btn-action edit", StringComparison.Ordinal));
        Assert.Equal(delete, html.Contains("btn-action delete", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Shared_activity_encodes_content_and_keeps_empty_state()
    {
        var empty = await Render("Shared", new List<BioGamaEcuador.Models.Record>(), viewName: "_RecentActivity");
        Assert.Contains("No hay registros recientes", empty);
        var html = await Render("Shared", new List<BioGamaEcuador.Models.Record>
        {
            new() { Species = new Species { CommonName = "<script>unsafe()</script>" } }
        }, viewName: "_RecentActivity");
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>unsafe()", html);
    }

    private static async Task<string> Render(string folder, object model, Dictionary<string, object?>? values = null,
        string viewName = "_FormFields", string? role = null)
    {
        await using var application = new TestApplication();
        using var client = application.NewClient(null);
        await using var scope = application.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var http = new DefaultHttpContext { RequestServices = services };
        http.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Razor test"));
        if (role is not null)
            http.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)], "Test"));
        var route = new RouteData();
        route.Values["controller"] = "Families";
        route.Values["action"] = "Index";
        var action = new ActionContext(http, route, new ActionDescriptor());
        var view = services.GetRequiredService<IRazorViewEngine>().GetView(null, $"/Views/{folder}/{viewName}.cshtml", false);
        Assert.True(view.Success);
        var data = new ViewDataDictionary(services.GetRequiredService<IModelMetadataProvider>(), new ModelStateDictionary()) { Model = model };
        if (values is not null) foreach (var value in values) data[value.Key] = value.Value;
        using var writer = new StringWriter();
        var context = new ViewContext(action, view.View, data,
            new TempDataDictionary(http, services.GetRequiredService<ITempDataProvider>()), writer, new HtmlHelperOptions());
        await view.View.RenderAsync(context);
        return writer.ToString();
    }
}
