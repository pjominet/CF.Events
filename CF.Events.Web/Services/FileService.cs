using CF.Events.Web.Data;
using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Services;

public interface IFileService
{
    Task<string> SaveImageAsync(string folderName, IFormFile file);
    Task MoveEventImagesAsync(string fromFolder, int toEventId);
    Task RegisterImageAsync(int eventId, string fileName);
    Task SyncEventImagesAsync(int eventId, IEnumerable<string> currentFileNames);
    Task DeleteEventImagesAsync(int eventId);
}

public class FileService(IWebHostEnvironment env, EventsDbContext db, ILogger<FileService> logger) : IFileService
{
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
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(targetDir, fileName);
            if (File.Exists(destFile)) File.Delete(destFile);
            File.Move(file, destFile);

            await RegisterImageAsync(toEventId, fileName);
        }

        Directory.Delete(sourceDir, true);
        await Task.CompletedTask;
    }

    public async Task RegisterImageAsync(int eventId, string fileName)
    {
        var exists = await db.EventImages.AnyAsync(i => i.EventId == eventId && i.FileName == fileName);
        if (!exists)
        {
            db.EventImages.Add(new EventImage { EventId = eventId, FileName = fileName });
            await db.SaveChangesAsync();
        }
    }

    public async Task SyncEventImagesAsync(int eventId, IEnumerable<string> currentFileNames)
    {
        var registeredImages = await db.EventImages
            .Where(i => i.EventId == eventId)
            .ToListAsync();

        var toRemove = registeredImages
            .Where(i => !currentFileNames.Contains(i.FileName, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (toRemove.Count > 0)
        {
            var eventsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources", "Events"));
            var dir = Path.Combine(eventsRoot, eventId.ToString());

            foreach (var img in toRemove)
            {
                try
                {
                    var filePath = Path.Combine(dir, img.FileName);
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error deleting physical file {FileName} for event {EventId}", img.FileName, eventId);
                }
            }

            db.EventImages.RemoveRange(toRemove);
            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteEventImagesAsync(int eventId)
    {
        var images = await db.EventImages.Where(i => i.EventId == eventId).ToListAsync();
        if (images.Count > 0)
        {
            db.EventImages.RemoveRange(images);
            await db.SaveChangesAsync();
        }

        try
        {
            var eventsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources", "Events"));
            var dir = Path.Combine(eventsRoot, eventId.ToString());
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting image directory for event {EventId}", eventId);
        }
    }
}
