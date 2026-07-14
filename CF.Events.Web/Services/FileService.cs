namespace CF.Events.Web.Services;

public interface IFileService
{
    void DeleteInvitationImage(int eventId, string? fileName = null);
}

public class FileService(IWebHostEnvironment env) : IFileService
{
    public void DeleteInvitationImage(int eventId, string? fileName = null)
    {
        try
        {
            var invitationsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources", "Invitations"));
            var dir = Path.GetFullPath(Path.Combine(invitationsRoot, eventId.ToString()));
            if (!dir.StartsWith(invitationsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                return;

            if (string.IsNullOrEmpty(fileName))
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            else
            {
                var filePath = Path.Combine(dir, fileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }
        catch
        {
            // Best-effort cleanup; ignore filesystem errors during deletion.
        }
    }
}
