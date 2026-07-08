namespace CF.Events.Web.Models.Requests;

public record InlineAttachment(string FileName, string ContentType, byte[] Content);
