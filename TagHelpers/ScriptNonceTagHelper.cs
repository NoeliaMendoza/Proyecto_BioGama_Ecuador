using BioGamaEcuador.Middleware;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BioGamaEcuador.TagHelpers;

[HtmlTargetElement("script")]
public sealed class ScriptNonceTagHelper : TagHelper
{
    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext.HttpContext.Items[SecurityHeadersMiddleware.NonceKey] is not string nonce) return;
        output.Attributes.SetAttribute("nonce", nonce);
        if (output.Attributes["src"]?.Value?.ToString()?.StartsWith("https://www.paypal.com/sdk/js", StringComparison.Ordinal) == true)
            output.Attributes.SetAttribute("data-csp-nonce", nonce);
    }
}
