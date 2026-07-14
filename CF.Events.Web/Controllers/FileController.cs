using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CF.Events.Web.Controllers;

[Route("file")]
[Authorize]
public class FileController(IFileService fileService, ILogger<FileController> logger) : Controller
{
    [HttpPost("upload-image/{folderName}")]
    public async Task<IActionResult> UploadImage([FromRoute] string folderName, IFormFile image)
    {
        try
        {
            if (image.Length == 0)
                return Json(new { success = 0 });

            var url = await fileService.SaveImageAsync(folderName, image);

            return Json(new
            {
                success = 1,
                file = new { url }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading image for folder {FolderName}", folderName);
            return Json(new { success = 0 });
        }
    }
}
