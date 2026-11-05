using Microsoft.AspNetCore.Mvc;

namespace BioGamaEcuador.Services;

public static class ImageSource
{
    public static string Resolve(string imageUrl, IUrlHelper url)
    {
        var value = imageUrl.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out _))
            return value.Replace(" ", "%20", StringComparison.Ordinal);
        if (!value.StartsWith("~/", StringComparison.Ordinal) && !value.StartsWith('/'))
            value = "~/images/" + value.TrimStart('/');
        return url.Content(value)!;
    }
}
