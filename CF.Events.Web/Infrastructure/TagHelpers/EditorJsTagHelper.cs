using System.Text.Json;
using EditorJsonToHtmlConverter;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CF.Events.Web.Infrastructure.TagHelpers;

[HtmlTargetElement("jsonrenderer", Attributes = "input")]
public class EditorJsTagHelper(HtmlRenderer htmlRenderer) : TagHelper
{
    public string? Input { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null; // Remove the <editorjs> tag itself

        if (string.IsNullOrWhiteSpace(Input))
        {
            output.Content.SetContent(string.Empty);
            return;
        }

        try
        {
            // Simple check to see if it's likely JSON
            if (!Input.TrimStart().StartsWith('{'))
            {
                output.Content.SetHtmlContent(Input);
                return;
            }

            var renderer = new EjsHtmlRenderer(htmlRenderer);
            var html = await renderer.ParseAsync(Input);

            output.Content.SetHtmlContent(html);
        }
        catch (Exception)
        {
            // If conversion fails, return original string
            output.Content.SetHtmlContent(Input);
        }
    }

    private static string? BuildStylingMap(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("blocks", out var blocks) || blocks.ValueKind != JsonValueKind.Array)
                return null;

            var mappings = new List<object>();
            foreach (var block in blocks.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) && type.GetString() == "image" &&
                    block.TryGetProperty("id", out var id) &&
                    block.TryGetProperty("tunes", out var tunes) &&
                    tunes.TryGetProperty("imageSize", out var imageSize) &&
                    imageSize.TryGetProperty("size", out var size))
                {
                    mappings.Add(new
                    {
                        type = "image",
                        id = id.GetString(),
                        @class = $"img-sz-{size.GetString()}"
                    });
                }
            }

            return mappings.Count > 0 ? JsonSerializer.Serialize(mappings) : null;
        }
        catch
        {
            return null;
        }
    }
}
