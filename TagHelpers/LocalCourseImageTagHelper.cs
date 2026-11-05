using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BioGamaEcuador.TagHelpers;

[HtmlTargetElement("img", Attributes = "asp-course-image")]
public sealed class LocalCourseImageTagHelper(IWebHostEnvironment environment) : TagHelper
{
    private static readonly string[] Extensions = ["jpg", "png", "webp", "gif"];

    [HtmlAttributeName("asp-course-image")]
    public string Source { get; set; } = string.Empty;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Source)));
        var localPath = Extensions.Select(extension => $"images/courses/{hash}.{extension}")
            .FirstOrDefault(path => environment.WebRootFileProvider.GetFileInfo(path).Exists);
        output.Attributes.RemoveAll("asp-course-image");
        output.Attributes.SetAttribute("src", localPath is null ? Source : "/" + localPath);
    }
}
