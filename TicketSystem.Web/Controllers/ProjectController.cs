using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.Project;
using TicketSystem.Web.Models.ProjectManagement;

namespace TicketSystem.Web.Controllers
{
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
                OpenTickets = p.Tickets.Count(t => t.CurrentStatus != "Closed")
            }).ToListAsync();

            return View(projectsList);
        }

        // GET: Project/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ProjectCreateViewModel
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today), 
                WorkflowsList = new SelectList(await _context.Workflows.ToListAsync(), "Id", "Name")
            };
            return View(viewModel);
        }

        // POST: Project/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var project = new ProjectModel
                {
                    Title = model.Title,
                    Description = model.Description,
                    StartDate = model.StartDate,
                    WorkflowId = model.WorkflowId,
                    IsDeleted = false
                    // EndDate null by Default, set when project is closed
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Project created successfully!";
                return RedirectToAction(nameof(Index));
            }

            model.WorkflowsList = new SelectList(await _context.Workflows.ToListAsync(), "Id", "Name", model.WorkflowId);
            return View(model);
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
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

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
                    IsBlocked = t.BlockedByTickets.Any()
                }).ToList()
            };

            boardViewModel.UsersList = new SelectList(_context.Users.ToList(), "Id", "UserName");


            return View(boardViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
            {
                return NotFound();
            }

            // Verifica se já não está encerrado para evitar reescrita da data
            if (project.EndDate.HasValue)
            {
                TempData["ErrorMessage"] = "Este projeto já se encontra encerrado.";
                return RedirectToAction(nameof(Details), new { id = project.Id });
            }

            // Define a data de fim como a data de hoje
            project.EndDate = DateOnly.FromDateTime(DateTime.Today);

            _context.Projects.Update(project);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"O projeto '{project.Title}' foi encerrado com sucesso.";

            // Redireciona para a listagem (ou de volta para os detalhes, como preferir)
            return RedirectToAction(nameof(Index));
        }

        // POST: Project/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // [Authorize(Roles = "Admin")] // Descomente se usar Roles no Identity
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null) return NotFound();

            // Dupla verificação de segurança no backend
            if (!project.EndDate.HasValue)
            {
                TempData["ErrorMessage"] = "Only closed Projects can be deleted";
                return RedirectToAction(nameof(Index));
            }

            // Soft Delete
            project.IsDeleted = true;
            _context.Projects.Update(project);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"The Project '{project.Title}' was successfully deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
