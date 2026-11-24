using System.Diagnostics;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgriSyncIndia.Controllers;

public class HomeController : Controller
{
    public async Task<IActionResult> Index([FromServices] DashboardViewModelBuilder builder)
        => View(await builder.BuildAsync(HttpContext.Lang(), null, HttpContext));

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
