using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.Account;
using TicketSystem.Web.Models.Home;

namespace TicketSystem.Web.Controllers;


public class HomeController : Controller
{

    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public HomeController(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity!.IsAuthenticated) return RedirectToAction("Dashboard", "Home");

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

        model.UsersByRole = await _context.Roles
            .Select(role => new RoleUsageViewModel
            {
                RoleName = role.Name ?? string.Empty,
                UserCount = _context.UserRoles.Count(ur => ur.RoleId == role.Id)
            }).ToListAsync();

        return View(model);
    }



    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            // Authenticated endpoint but user couldn't be resolved - challenge the auth system
            return Challenge();
        }

        // Check if user has the global App "Admin" role
        var isAppAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        var viewModel = new DashboardViewModel
        {
            IsAppAdmin = isAppAdmin
        };

        // 1. Fetch User-Specific Metrics
        viewModel.UnreadMessagesCount = await _context.Messages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

        viewModel.MyProjectsCount = await _context.ProjectMembers
            .CountAsync(pm => pm.MemberId == userId && pm.Project != null && !pm.Project.IsDeleted);

        viewModel.MyAssignedTicketsCount = await _context.Tickets
            .CountAsync(t => t.AssigneeId == userId && t.CurrentStatus != "Closed");

        // Find tickets assigned to the user that are waiting on another ticket
        viewModel.MyBlockedTicketsCount = await _context.Tickets
            .Include(t => t.BlockedByTickets)
            .ThenInclude(d => d.BlockingTicket)
            .Where(t => t.AssigneeId == userId && t.CurrentStatus != "Closed")
            .CountAsync(t => t.BlockedByTickets.Any(b => b.BlockingTicket != null && b.BlockingTicket.CurrentStatus != "Closed"));

        // 2. Fetch Lists for Quick Views (Limit to 5 for performance)
        viewModel.RecentAssignedTickets = await _context.Tickets
            .Include(t => t.Project)
            .Where(t => t.AssigneeId == userId && t.CurrentStatus != "Closed")
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .ToListAsync();

        viewModel.MyRecentProjects = await _context.ProjectMembers
                                                    .Include(pm => pm.Project)
                                                    .Where(pm => pm.MemberId == userId && pm.Project != null && !pm.Project.IsDeleted)
                                                    .Select(pm => pm.Project!)
                                                    .OrderByDescending(p => p.StartDate)
                                                    .Take(5)
                                                    .ToListAsync();

        // 3. Fetch Global Metrics (Only if Admin, to save database processing)
        if (isAppAdmin)
        {
            viewModel.TotalAppUsers = await _context.Users.CountAsync(u => u.IsActive);
            viewModel.TotalActiveProjects = await _context.Projects.CountAsync(p => !p.IsDeleted);
            viewModel.TotalOpenTicketsGlobally = await _context.Tickets.CountAsync(t => t.CurrentStatus != "Closed");
        }

        return View(viewModel);
    
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
