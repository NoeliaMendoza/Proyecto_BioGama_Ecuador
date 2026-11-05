using BioGamaEcuador.TagHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class LocalCourseImageTests
{
    private const string Source = "https://example.com/course.jpg";
    private const string Hash = "dccab81ac459093cbd03c9108ef66ddee37d827501573b70e147ce16596c56c5";

    [Theory]
    [InlineData("jpg")]
    [InlineData("png")]
    [InlineData("webp")]
    [InlineData("gif")]
    public void Existing_local_photo_is_served_from_the_application(string extension)
    {
        var root = Path.Combine(Path.GetTempPath(), "biogama-image-test-" + Guid.NewGuid().ToString("N"));
        var relative = $"images/courses/{Hash}.{extension}";
        Directory.CreateDirectory(Path.Combine(root, "images/courses"));
        try
        {
            File.WriteAllText(Path.Combine(root, relative), "test fixture");
            using var files = new PhysicalFileProvider(root);
            var output = Render(files);
            Assert.Equal("/" + relative, output.Attributes["src"].Value);
            Assert.False(output.Attributes.ContainsName("asp-course-image"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Missing_local_copy_keeps_the_original_source()
    {
        var output = Render(new NullFileProvider());
        Assert.Equal(Source, output.Attributes["src"].Value);
    }

    private static TagHelperOutput Render(IFileProvider files)
    {
        var helper = new LocalCourseImageTagHelper(new ImageEnvironment { WebRootFileProvider = files }) { Source = Source };
        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "image-test");
        var output = new TagHelperOutput("img", new TagHelperAttributeList { { "asp-course-image", Source } },
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        helper.Process(context, output);
        return output;
    }

    private sealed class ImageEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ImageTests";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
