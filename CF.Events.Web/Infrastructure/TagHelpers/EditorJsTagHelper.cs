using System.Text.Json;
using AngleSharp.Html.Parser;
using EditorJsonToHtmlConverter;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CF.Events.Web.Infrastructure.TagHelpers;

[HtmlTargetElement("jsonrenderer", Attributes = "input")]
public class EditorJsTagHelper(HtmlRenderer htmlRenderer, IHtmlParser parser) : TagHelper
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
            html = await ApplyImageSrcAsync(html, Input);
            html = await ApplyImageTunesAsync(html, Input);

            output.Content.SetHtmlContent(html);
        }
        catch (Exception)
        {
            // If conversion fails, return original string
            output.Content.SetHtmlContent(Input);
        }
    }

    private async Task<string> ApplyImageSrcAsync(string html, string json)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(json);
            if (!jsonDoc.RootElement.TryGetProperty("blocks", out var blocks) || blocks.ValueKind is not JsonValueKind.Array)
                return html;

            var imageBlocks = blocks.EnumerateArray()
                .Where(b => b.GetProperty("type").GetString() == "image")
                .ToList();

            if (imageBlocks.Count == 0) return html;

            using var document = await parser.ParseDocumentAsync(html);
            var images = document.QuerySelectorAll("img").ToList();

            if (images.Count == 0) return html;

            for (var i = 0; i < Math.Min(imageBlocks.Count, images.Count); i++)
            {
                var block = imageBlocks[i];
                var img = images[i];

                // If src is already present, we might not want to override it,
                // but the issue is that it's MISSING or empty.
                if (!string.IsNullOrEmpty(img.GetAttribute("src"))) continue;

                var data = block.GetProperty("data");
                if (data.TryGetProperty("file", out var file) && file.TryGetProperty("url", out var url))
                {
                    img.SetAttribute("src", url.GetString());
                }
                else if (data.TryGetProperty("url", out var directUrl))
                {
                    img.SetAttribute("src", directUrl.GetString());
                }
            }

            return document.Body?.InnerHtml ?? document.Source.Text;
        }
        catch
        {
            return html;
        }
    }

    private async Task<string> ApplyImageTunesAsync(string html, string json)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(json);
            if (!jsonDoc.RootElement.TryGetProperty("blocks", out var blocks) || blocks.ValueKind is not JsonValueKind.Array)
                return html;

            // Find image blocks with imageTunePlus
            var imageBlocks = blocks.EnumerateArray()
                .Where(b => b.GetProperty("type").GetString() == "image" &&
                            b.TryGetProperty("tunes", out var tunes) &&
                            tunes.TryGetProperty("imageTunePlus", out _))
                .ToList();

            if (imageBlocks.Count == 0) return html;

            // Use AngleSharp to parse HTML
            using var document = await parser.ParseDocumentAsync(html);

            var images = document.QuerySelectorAll("img").ToList();

            if (images.Count == 0) return html;

            for (var i = 0; i < Math.Min(imageBlocks.Count, images.Count); i++)
            {
                var block = imageBlocks[i];
                var img = images[i];
                var tunes = block.GetProperty("tunes").GetProperty("imageTunePlus");

                var styles = new List<string>();

                if (tunes.TryGetProperty("width", out var width) && width.ValueKind is not JsonValueKind.Null)
                {
                    styles.Add($"width: {width.GetString()};");
                }

                if (tunes.TryGetProperty("ratio", out var ratio) && ratio.ValueKind is not JsonValueKind.Null)
                {
                    styles.Add($"aspect-ratio: {ratio.GetString()?.Replace(":", " / ")};");
                    styles.Add("object-fit: cover;");
                }

                if (tunes.TryGetProperty("borderRadius", out var borderRadius) && borderRadius.ValueKind is not JsonValueKind.Null)
                {
                    styles.Add($"border-radius: {borderRadius.GetString()};");
                }

                if (tunes.TryGetProperty("alignment", out var alignment) && alignment.ValueKind is not JsonValueKind.Null)
                {
                    var align = alignment.GetString();
                    switch (align)
                    {
                        case "center":
                            styles.Add("margin-left: auto; margin-right: auto; display: block;");
                            break;
                        case "left":
                            styles.Add("margin-right: auto; margin-left: 0; display: block;");
                            break;
                        case "right":
                            styles.Add("margin-left: auto; margin-right: 0; display: block;");
                            break;
                    }
                }

                if (styles.Count <= 0) continue;

                var existingStyle = img.GetAttribute("style");
                var newStyles = string.Join(" ", styles);
                img.SetAttribute("style", string.IsNullOrEmpty(existingStyle) ? newStyles : $"{existingStyle.TrimEnd(';', ' ' )}; {newStyles}");
            }

            // Return the body content or the whole HTML if no body
            return document.Body?.InnerHtml ?? document.Source.Text;
        }
        catch
        {
            return html;
        }
    }
}
