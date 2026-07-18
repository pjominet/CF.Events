using Microsoft.AspNetCore.StaticFiles;

namespace CF.Events.Web.Models.Requests;

public class InlineAttachment(string fileName, string contentType, byte[] content)
{
    public string FileName { get; init; } = fileName;
    public string ContentType { get; init; } = contentType;
    public byte[] Content { get; init; } = content;

    public static InlineAttachment BuildInlineImage(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Image file not found", filePath);

        var fileName = Path.GetFileName(filePath);
        new FileExtensionContentTypeProvider().TryGetContentType(fileName, out var contentType);
        return new InlineAttachment(fileName, contentType ?? "application/octet-stream", File.ReadAllBytes(filePath));
    }
}
