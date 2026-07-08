namespace CF.Events.Web.Models.Requests;

public record EmailInlineAttachment(string FileName, string ContentType, byte[] Content);
