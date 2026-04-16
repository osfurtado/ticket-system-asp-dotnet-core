using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Security.Claims;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.Project;
using TicketSystem.Web.Models.ProjectManagement;
using TicketSystem.Web.Models.Ticket;

namespace TicketSystem.Web.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly AppDbContext _context;

        public ProjectController(AppDbContext context)
        {
            _context = context;
        }


        // GET: Project/Index
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var projectsList = await _context.Projects
            .Where(p => !p.IsDeleted)
            .Select(p => new ProjectListViewModel
            {
                Id = p.Id,
                Title = p.Title,
                DescriptionSnippet = p.Description.Length > 100 ? p.Description.Substring(0, 100) + "..." : p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                WorkflowName = p.Workflow != null ? p.Workflow.Name : "Without Workflow",
                TotalTickets = p.Tickets.Count(),
                OpenTickets = p.Tickets.Count(t => t.CurrentStatus != "Closed"),
                IsCurrentUserMember = p.Members.Any(m => m.MemberId == userId)
            }).ToListAsync();

            return View(projectsList);
        }

        // GET: Project/Details/5 (Kanban board)
        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Workflow)
                .ThenInclude(w => w.Statuses)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.Assignee)
                .Include(p => p.Tickets).ThenInclude(t => t.Comments)
                .Include(p => p.Tickets).ThenInclude(t => t.BlockedByTickets)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var boardViewModel = new ProjectBoardViewModel
            {
                ProjectId = project.Id,
                ProjectTitle = project.Title,
                WorkflowName = project.Workflow?.Name ?? "",
                EndDate = project.EndDate,
                WorkflowStatuses = project.Workflow?.Statuses
                                                .OrderByDescending(s => s.IsInicial)
                                                .ThenBy(s => s.IsFinal)
                                                .ThenBy(s => s.OrderIndex)
                                                .Select(s => s.Name).ToList() ?? new List<string>(),
                Tickets = project.Tickets.Select(t => new TicketCardViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    CurrentStatus = t.CurrentStatus,
                    AssigneeName = t.Assignee?.UserName ?? "Not assigned",
                    CommentsCount = t.Comments.Count,
                    AttachmentsCount = t.Attachments?.Count ?? 0,
                    IsClosed = t.ClosedAt.HasValue,
                    IsBlocked = t.BlockedByTickets.Any(td => td.BlockedTicketId == t.Id && td.BlockingTicket!.CurrentStatus != "Closed"),
                    IsLocked = !CanChangeTicketStatus(currentUserId!, t, project),
                    CanAssign = CanAssignTicket(currentUserId!, t, project)
                }).ToList(),
                CanCreateTicket = CanCreateTicket(currentUserId!, project),
                CanManage = User.IsInRole("Admin") || project.Members.Any(pm => pm.MemberId == currentUserId! && (pm.RoleInProject == "Manager" || pm.RoleInProject == "Moderator" ))
                
            };

            boardViewModel.UsersList = new SelectList(_context.Users.ToList(), "Id", "UserName");


            return View(boardViewModel);
        }


        private bool CanChangeTicketStatus(string currentUserId, TicketModel ticket, ProjectModel project)
        {
            
            if (!project.EndDate.HasValue && ticket.AssigneeId != null && ticket.CurrentStatus != "Closed" && !ticket.BlockedByTickets.Any(td => td.BlockedTicketId == ticket.Id && td.BlockingTicket!.CurrentStatus != "Closed") && (User.IsInRole("Admin") || project.Members.Any(pm => pm.RoleInProject == "Manager" && pm.MemberId == currentUserId) || currentUserId == ticket.CreatorId || currentUserId == ticket.AssigneeId))
            {
                return true;
            }
            return false;
        }

        private bool CanCreateTicket(string currentUserId, ProjectModel project)
        {
            if (!project.EndDate.HasValue && (User.IsInRole("Admin") || project.Members.Any(pm => pm.MemberId == currentUserId)))
            {
                return true;
            }
            return false;
        }

        private bool CanAssignTicket(string currentUserId, TicketModel ticket, ProjectModel project)
        {
            
            if (!project.EndDate.HasValue && ticket.CurrentStatus != "Closed" && (User.IsInRole("Admin") || project.Members.Any(pm => pm.RoleInProject == "Manager" && pm.MemberId == currentUserId) || currentUserId == ticket.CreatorId))
            {
                return true;
            }
            return false;
        }
    }
}
