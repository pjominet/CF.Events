namespace CF.Events.Web.Services;

public interface IFileService
{
    void DeleteInvitationImage(int eventId, string? fileName = null);
    Task<string> SaveImageAsync(string folderName, IFormFile file);
    Task MoveEventImagesAsync(string fromFolder, int toEventId);
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

    public async Task<string> SaveImageAsync(string folderName, IFormFile file)
    {
        var eventsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources", "Events"));
        var dir = Path.Combine(eventsRoot, folderName);

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(dir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/events/{folderName}/image/{fileName}";
    }

    public async Task MoveEventImagesAsync(string fromFolder, int toEventId)
    {
        var eventsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources", "Events"));
        var sourceDir = Path.Combine(eventsRoot, fromFolder);
        var targetDir = Path.Combine(eventsRoot, toEventId.ToString());

        if (!Directory.Exists(sourceDir)) return;

        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(targetDir, Path.GetFileName(file));
            if (File.Exists(destFile)) File.Delete(destFile);
            File.Move(file, destFile);
        }

        Directory.Delete(sourceDir, true);
        await Task.CompletedTask;
    }
}
