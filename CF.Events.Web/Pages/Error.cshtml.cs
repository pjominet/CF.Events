using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CF.Events.Web.Pages;

[AllowAnonymous]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel
{
    public string Title { get; private set; } = "Something went wrong";
    public string Detail { get; private set; } = "An unexpected error occurred while processing your request.";

    public void OnGet(int? code)
    {
        switch (code)
        {
            case 404:
                Title = "Page not found";
                Detail = "The page you are looking for doesn't exist or has been moved.";
                break;
            case 403:
                Title = "Access denied";
                Detail = "You do not have permission to access this resource.";
                break;
            case 401:
                Title = "Unauthorized";
                Detail = "You need to sign in to access this resource.";
                break;
        }
    }
}
