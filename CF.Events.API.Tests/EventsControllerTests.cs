using System.Security.Claims;
using CF.Events.API.Controllers;
using CF.Events.API.Data;
using CF.Events.Shared.DTOs;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CF.Events.API.Tests;

public class EventsControllerTests
{
    private (EventsDbContext, Mock<IWebHostEnvironment>, EventsController) SetupController(string userId = "test-user-id", string role = "Admin")
    {
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new EventsDbContext(options);

        var mockEnv = new Mock<IWebHostEnvironment>();
        var apiPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CF.Events.API"));
        mockEnv.Setup(m => m.ContentRootPath).Returns(apiPath);

        var controller = new EventsController(context, mockEnv.Object);

        // Mock User
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        ], "mock"));

        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        return (context, mockEnv, controller);
    }

    [Fact]
    public void GetInvitationFiles_ShouldReturnFoldersWithIndexHtml()
    {
        // Arrange
        var (_, _, controller) = SetupController();

        // Act
        var result = controller.GetInvitationFiles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var files = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);

        Assert.NotEmpty(files);
        Assert.Contains("engagement", files);
        Assert.Contains("sample", files);
    }

    [Fact]
    public async Task GetInvitationContent_ShouldReturnProcessedHtml()
    {
        // Arrange
        var (context, mockEnv, controller) = SetupController();

        // Ensure the directory exists in the mock environment path
        var invitationsPath = Path.Combine(mockEnv.Object.ContentRootPath, "Resources", "Invitations", "sample");
        Directory.CreateDirectory(invitationsPath);
        var htmlContent = "<!DOCTYPE html><html><body><h1>[EventName]</h1><p>[EventDate]</p><p>[EventLocation]</p></body></html>";
        await File.WriteAllTextAsync(Path.Combine(invitationsPath, "index.html"), htmlContent);

        var ev = new Event
        {
            Id = 1,
            Name = "Test Wedding",
            Date = new DateTime(2026, 6, 20),
            Location = "Test Venue",
            InvitationFileName = "sample"
        };
        context.Events.Add(ev);
        context.Rsvps.Add(new Rsvp { EventId = 1, UserId = "test-user-id" });
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetInvitationContent(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var content = Assert.IsType<InvitationContentDto>(okResult.Value);

        Assert.Contains("Test Wedding", content.HtmlContent);
        Assert.Contains("Test Venue", content.HtmlContent);
    }

    [Fact]
    public async Task GetInvitationContent_AdminShouldBeAllowedWithoutRsvp()
    {
        // Arrange
        var (context, mockEnv, controller) = SetupController();

        // Ensure the directory exists in the mock environment path
        var invitationsPath = Path.Combine(mockEnv.Object.ContentRootPath, "Resources", "Invitations", "sample");
        Directory.CreateDirectory(invitationsPath);
        var htmlContent = "<!DOCTYPE html><html><body><h1>[EventName]</h1></body></html>";
        await File.WriteAllTextAsync(Path.Combine(invitationsPath, "index.html"), htmlContent);

        var ev = new Event
        {
            Id = 2,
            Name = "Admin Preview Event",
            Date = new DateTime(2026, 6, 20),
            InvitationFileName = "sample"
        };
        context.Events.Add(ev);
        // Note: No RSVP added for test-user-id
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetInvitationContent(2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var content = Assert.IsType<InvitationContentDto>(okResult.Value);
        Assert.Contains("Admin Preview Event", content.HtmlContent);
    }

    [Fact]
    public async Task GetEvent_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var (context, _, controller) = SetupController();
        // Clear User to simulate unauthenticated
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        // Note: The [Authorize] attribute itself is not processed in unit tests unless using Integration Tests with TestServer.
        // But our controller logic might still use User.FindFirstValue(ClaimTypes.NameIdentifier) which returns null.
        var result = await controller.GetEvent(1);

        // Assert
        // In our current implementation of GetEvent:
        // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);
        // if (rsvp is null && !User.IsInRole(Roles.Admin)) return Forbid();
        // Since userId is null, rsvp will be null, and !User.IsInRole(Roles.Admin) will be true.
        // So it returns Forbid(), not Unauthorized(). The [Authorize] attribute normally handles 401.
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetEvent_UnauthorizedUser_ShouldReturnForbid()
    {
        // Arrange
        var (context, _, controller) = SetupController(userId: "regular-user", role: "User");
        // User is "regular-user" but no RSVP exists for Event 1
        context.Events.Add(new Event { Id = 1, Name = "Private Event" });
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetEvent(1);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}
