using BioGamaEcuador.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class ImageSourceTests
{
    [Theory]
    [InlineData("bird.jpg", "/images/bird.jpg")]
    [InlineData("~/images/bird.jpg", "/images/bird.jpg")]
    [InlineData("/images/bird.jpg", "/images/bird.jpg")]
    [InlineData(" https://example.invalid/bird photo.jpg ", "https://example.invalid/bird%20photo.jpg")]
    public void Shared_image_resolution_preserves_relative_and_external_paths(string input, string expected)
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(helper => helper.Content(It.IsAny<string>())).Returns((string path) =>
            path.StartsWith("~/", StringComparison.Ordinal) ? path[1..] : path);
        Assert.Equal(expected, ImageSource.Resolve(input, url.Object));
    }
}
