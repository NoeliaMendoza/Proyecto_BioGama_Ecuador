using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using BioGamaEcuador.Controllers;
using BioGamaEcuador.Data;
using BioGamaEcuador.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class RequiredFormBindingTests
{
    [Theory]
    [InlineData(typeof(LocationsController), "Altitude")]
    [InlineData(typeof(LocationsController), "NaturalReserveId")]
    [InlineData(typeof(NaturalReservesController), "YearCreated")]
    [InlineData(typeof(SpeciesController), "IsEndemic")]
    [InlineData(typeof(RecordsController), "SpeciesId")]
    [InlineData(typeof(RecordsController), "LocationId")]
    [InlineData(typeof(RecordsController), "ResearcherId")]
    [InlineData(typeof(RecordsController), "DiscoveryDate")]
    [InlineData(typeof(RecordsController), "ObservedQuantity")]
    [InlineData(typeof(AdminCoursesController), "RequiresPriorKnowledge")]
    public async Task Omitted_required_value_creates_a_binding_error(Type controller, string field)
    {
        var binding = await Bind(controller, "Create", new());
        Assert.True(binding.ContainsKey(field));
        Assert.NotEmpty(binding[field]!.Errors);
    }

    [Fact]
    public async Task Explicit_zero_altitude_is_allowed_but_omission_is_not()
    {
        var binding = await Bind(typeof(LocationsController), "Create", new() { ["Altitude"] = "0" });
        Assert.Empty(binding["Altitude"]!.Errors);
    }

    [Theory]
    [InlineData(typeof(FamiliesController))]
    [InlineData(typeof(SpeciesController))]
    [InlineData(typeof(LocationsController))]
    [InlineData(typeof(ResearchersController))]
    [InlineData(typeof(NaturalReservesController))]
    [InlineData(typeof(RecordsController))]
    [InlineData(typeof(AdminProductsController))]
    [InlineData(typeof(AdminCoursesController))]
    public async Task Create_does_not_require_id_but_edit_does(Type controller)
    {
        var creation = await Bind(controller, "Create", new());
        Assert.False(creation.ContainsKey("Id"));
        var edit = await Bind(controller, "Edit", new());
        Assert.NotEmpty(edit["Id"]!.Errors);
    }

    [Theory]
    [InlineData("Families")]
    [InlineData("Species")]
    [InlineData("Locations")]
    [InlineData("Researchers")]
    [InlineData("NaturalReserves")]
    [InlineData("Records")]
    [InlineData("AdminProducts")]
    [InlineData("AdminCourses")]
    public async Task Valid_creation_without_id_still_succeeds(string controller)
    {
        await using var application = new TestApplication();
        using var client = application.NewClient();
        using (var scope = application.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Families.Add(new Family { Id = 10, Name = "Familia", Kingdom = "Animal" });
            db.NaturalReserves.Add(new NaturalReserve { Id = 10, Name = "Reserva", Region = "Costa", SurfaceHectares = 10, YearCreated = 2000 });
            db.Locations.Add(new Location { Id = 10, PlaceName = "Lugar", NaturalReserveId = 10 });
            db.Researchers.Add(new Researcher { Id = 10, Name = "Investigador", Institution = "Universidad", Email = "test@example.test", Specialty = "Biologia" });
            db.Species.Add(new Species { Id = 10, CommonName = "Especie", ScientificName = "Specimen test", FamilyId = 10 });
            await db.SaveChangesAsync();
        }
        var token = (await client.GetFromJsonAsync<Dictionary<string, string>>("/_test/antiforgery"))!["token"];
        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Nueva", ["Kingdom"] = "Animal", ["CommonName"] = "Nueva especie", ["ScientificName"] = "New specimen",
            ["FamilyId"] = "10", ["IsEndemic"] = "false", ["PlaceName"] = "Lugar nuevo", ["Altitude"] = "0",
            ["Latitude"] = "0", ["Longitude"] = "0", ["NaturalReserveId"] = "10", ["Region"] = "Costa",
            ["SurfaceHectares"] = "10", ["YearCreated"] = "2000", ["Institution"] = "Universidad",
            ["Email"] = "new@example.test", ["Specialty"] = "Biologia", ["SpeciesId"] = "10", ["LocationId"] = "10",
            ["ResearcherId"] = "10", ["DiscoveryDate"] = "2026-11-01", ["ObservedQuantity"] = "1",
            ["Price"] = "10", ["Stock"] = "5", ["MinStock"] = "1", ["SKU"] = "NEW-KIT",
            ["Title"] = "Curso nuevo", ["Description"] = "Descripcion", ["TotalSeats"] = "10",
            ["StartDate"] = "2027-01-01", ["EndDate"] = "2027-01-02", ["StartTime"] = "09:00", ["EndTime"] = "10:00",
            ["Modality"] = "Presencial", ["Venue"] = "Aula", ["Instructor"] = "Docente", ["RequiresPriorKnowledge"] = "false"
        };
        var path = controller.StartsWith("Admin", StringComparison.Ordinal) ? $"/Admin/{controller[5..]}/Create" : $"/{controller}/Create";
        var response = await client.PostAsync(path, new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<ModelStateDictionary> Bind(Type controller, string action, Dictionary<string, string> values)
    {
        await using var application = new TestApplication();
        using var client = application.NewClient(null);
        await using var scope = application.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var parameter = controller.GetMethods().Single(m => m.Name == action && m.GetCustomAttribute<HttpPostAttribute>() is not null)
            .GetParameters().Last();
        var metadata = services.GetRequiredService<IModelMetadataProvider>().GetMetadataForType(parameter.ParameterType);
        var information = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes()) ?? new BindingInfo();
        var binder = services.GetRequiredService<IModelBinderFactory>().CreateBinder(new ModelBinderFactoryContext
        {
            Metadata = metadata, BindingInfo = information, CacheToken = parameter
        });
        var http = new DefaultHttpContext { RequestServices = services };
        var form = new FormCollection(values.ToDictionary(pair => pair.Key, pair => new StringValues(pair.Value)));
        http.Request.Form = form;
        var context = DefaultModelBindingContext.CreateBindingContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new FormValueProvider(BindingSource.Form, form, CultureInfo.InvariantCulture), metadata, information, "");
        await binder.BindModelAsync(context);
        return context.ModelState;
    }
}
