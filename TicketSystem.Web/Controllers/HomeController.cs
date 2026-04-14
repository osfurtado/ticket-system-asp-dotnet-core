using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.Home;

namespace TicketSystem.Web.Controllers;


public class HomeController : Controller
{

    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Ticket");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var model = new LandingPageViewModel
        {
            // Tickets Stats
            TotalTickets = await _context.Tickets.CountAsync(),
            ClosedTickets = await _context.Tickets.CountAsync(t => t.CurrentStatus == "Closed"),

            // Project Stats
            TotalProjects = await _context.Projects.CountAsync(p => !p.IsDeleted),
            ActiveProjects = await _context.Projects.CountAsync(p => !p.IsDeleted && (p.EndDate == null || p.EndDate > today)),

            // User Stats
            TotalUsers = await _context.Users.CountAsync()
        };

        model.OpenTickets = model.TotalTickets - model.ClosedTickets;
        model.ClosedProjects = model.TotalProjects - model.ActiveProjects;

        // TODO: For While Mockdata

        model.UsersByRole = new Dictionary<string, int>
        {
            { "Admin", 2 },
            { "Manager", 5 },
            { "Member", model.TotalUsers - 7 > 0 ? model.TotalUsers - 7 : 0 }
        };

        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> LoginOnly()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
