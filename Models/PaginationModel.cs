using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace BioGamaEcuador.Models;

public sealed record PaginationModel(int Page, int TotalPages, string Path, string PageParameter,
    IReadOnlyDictionary<string, StringValues> Query)
{
    public int FirstPage => Math.Max(1, Page - 4);
    public int LastPage => Math.Min(TotalPages, Page + 4);

    public string Link(int page)
    {
        var values = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in Query) values[value.Key] = value.Value;
        values[PageParameter] = Math.Clamp(page, 1, Math.Max(1, TotalPages)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var localPath = Path.StartsWith('/') && !Path.StartsWith("//", StringComparison.Ordinal) ? Path : "/";
        return QueryHelpers.AddQueryString(localPath, values);
    }

    public static PaginationModel From(ViewContext context, string pageParameter = "pagina")
    {
        var page = (int?)context.ViewData["PaginaActual"] ?? (int?)context.ViewData["Page"] ?? 1;
        var total = Math.Max(0, (int?)context.ViewData["TotalPaginas"] ?? (int?)context.ViewData["TotalPages"] ?? 0);
        var request = context.HttpContext.Request;
        return new(Math.Clamp(page, 1, Math.Max(1, total)), total, (request.PathBase + request.Path).ToString(),
            pageParameter, request.Query.ToDictionary(value => value.Key, value => value.Value, StringComparer.OrdinalIgnoreCase));
    }
}
