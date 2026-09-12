using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Route("users")]
[Authorize(Roles = Roles.Admin)]
public class UserController(
    IToastNotification toastNotification,
    IExportService exportService,
    IImportService importService) : Controller
{
    private readonly string[] _allowedFileExtensions = [".csv", ".txt", ".xlsx"];

    [HttpGet("export")]
    public async Task<IActionResult> ExportUsers([FromQuery] List<string>? selectedRoles = null)
    {
        try
        {
            selectedRoles ??= [Roles.Guest];
            var (bytes, fileName) = await exportService.ExportUsersToExcelAsync(selectedRoles);
            Response.Cookies.Append("fileDownload", "true", new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax });
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: fileName);
        }
        catch (Exception)
        {
            toastNotification.AddErrorToastMessage("An error occurred while exporting users.");
            return RedirectToPage("/Admin/Users");
        }
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportUsers(
        [FromForm] IFormFile? userList,
        [FromForm] int skipRows = 0,
        [FromForm] string delimiter = ",",
        [FromForm] List<string>? selectedRoles = null,
        [FromQuery] string? search = null)
    {
        if (userList is null || userList.Length == 0)
            return BadRequest("No file uploaded");

        // Ensure it's a valid file type
        var extension = Path.GetExtension(userList.FileName).ToLowerInvariant();
        if (!_allowedFileExtensions.Contains(extension))
            return BadRequest("Only CSV/Text and Excel files are allowed");

        if (!delimiter.HasValue(false)) delimiter = ",";

        selectedRoles ??= [Roles.Guest];
        var importedCount = 0;
        List<string> importErrors = [];

        try
        {
            await using var stream = userList.OpenReadStream();
            (importedCount, importErrors) = extension == ".xlsx"
                ? await importService.ImportUsersFromExcelAsync(stream, skipRows, selectedRoles)
                : await importService.ImportUsersFromCsvAsync(stream, skipRows, delimiter, selectedRoles);
        }
        catch (Exception ex)
        {
            importErrors.Add($"Error reading file: {ex.Message}");
        }

        if (importErrors.Count == 0)
            toastNotification.AddSuccessToastMessage($"{importedCount} users imported successfully");
        else toastNotification.AddWarningToastMessage($"Import completed with issues:{Environment.NewLine}{string.Join(Environment.NewLine, importErrors)}");

        TempData[ViewDataKeys.ImportErrors] = importErrors;
        return RedirectToPage("/admin/users", new { search });
    }

}
