using BioGamaEcuador.Middleware;
using BioGamaEcuador.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class ScriptNonceTests
{
    [Theory]
    [InlineData("https://www.paypal.com/sdk/js?client-id=test", true, "script")]
    [InlineData("/js/product-stock.js", false, "script")]
    [InlineData(null, false, "script")]
    [InlineData(null, false, "style")]
    public void Trusted_blocks_receive_nonce_and_paypal_receives_sdk_parameter(string? source, bool paypal, string tag)
    {
        var http = new DefaultHttpContext();
        http.Items[SecurityHeadersMiddleware.NonceKey] = "unit-test-nonce";
        var helper = new ScriptNonceTagHelper { ViewContext = new ViewContext { HttpContext = http } };
        var attributes = new TagHelperAttributeList();
        if (source is not null) attributes.Add("src", source);
        var output = new TagHelperOutput(tag, attributes,
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        helper.Process(new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), tag), output);
        Assert.Equal("unit-test-nonce", output.Attributes["nonce"].Value);
        Assert.Equal(paypal, output.Attributes.ContainsName("data-csp-nonce"));
        if (paypal) Assert.Equal("unit-test-nonce", output.Attributes["data-csp-nonce"].Value);
    }
}
