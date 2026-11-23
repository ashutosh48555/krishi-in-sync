using Microsoft.AspNetCore.Mvc;

namespace AgriSyncIndia.Controllers;

public partial class AccountController
{
    /// <summary>Styled 403 page. Point <c>AccessDeniedPath</c> here to use it.</summary>
    [HttpGet("AccessDenied")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    /// <summary>Styled 404 page. Use with <c>UseStatusCodePagesWithReExecute("/Account/NotFound")</c>.</summary>
    [HttpGet("NotFound")]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }
}
