using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CF.Events.Web.Infrastructure.TagHelpers;

[HtmlTargetElement("script", Attributes = "src")]
[HtmlTargetElement("link", Attributes = "rel")]
public class IntegrityTagHelper(IAssetIntegrityService assetIntegrityService) : TagHelper
{
    public override int Order => 1000;

    [HtmlAttributeName("asp-sri")]
    public bool? AspSri { get; set; }

    [HtmlAttributeName("asp-integrity")]
    public bool? AspIntegrity { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var sriEnabled = AspSri ?? AspIntegrity;

        // If explicitly disabled or not enabled, do nothing
        if (sriEnabled != true) return;

        // If integrity attribute is already explicitly set on the HTML tag, do not overwrite it
        if (context.AllAttributes.ContainsName("integrity") || output.Attributes.ContainsName("integrity")) return;

        var targetPath = output.Attributes["src"]?.Value?.ToString()
                         ?? output.Attributes["href"]?.Value?.ToString()
                         ?? context.AllAttributes["src"]?.Value?.ToString()
                         ?? context.AllAttributes["href"]?.Value?.ToString();

        if (string.IsNullOrWhiteSpace(targetPath)) return;

        var hash = assetIntegrityService.GetIntegrityHash(targetPath);
        if (hash.HasValue())
            output.Attributes.SetAttribute("integrity", hash);
    }
}
