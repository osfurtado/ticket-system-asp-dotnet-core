using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Security.Claims;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.Ticket;
using TicketSystem.Web.Models.Ticket.ViewModels;

namespace TicketSystem.Web.Controllers
{
    public class TicketController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public TicketController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Ticket
        public async Task<IActionResult> Index(TicketFilterViewModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            // Build the Query
            var query = _context.Tickets.AsNoTracking().AsQueryable();

            // Apply Filters
            query = ApplyFilters(query, model);

            // To ViewModel
            model.Tickets = await query
                .Select(t => new TicketListViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Project = t.Project != null ? t.Project.Title : "No Project",
                    CreatedBy = t.CreatedBy != null ? t.CreatedBy.UserName! : "Unknown",
                    CreatedAt = DateOnly.FromDateTime(t.CreatedAt),
                    Assignee = t.Assignee != null ? t.Assignee.UserName! : "Not Assigned",
                    CurrentStatus = t.CurrentStatus,
                    // Logic handled at the DB level
                    CanChange = isAdmin || currentUserId == t.CreatorId || currentUserId == t.AssigneeId
                })
                .ToListAsync();

            // Populate Dropdowns (Ideally cached or loaded more efficiently)
            model.ProjectList = await _context.Projects.Select(p => p.Title).OrderBy(t => t).ToListAsync();
            model.CreatedByList = await _context.Users.Select(u => u.UserName ?? "No Username").OrderBy(t => t).ToListAsync();
            model.AssigneeList = model.CreatedByList; 

            return View(model);
        }

        // GET: Ticket/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticketModel = await _context.Tickets
                .Include(t => t.Assignee)
                .Include(t => t.ClosedBy)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.Creator)
                .Include(t => t.Attachments)
                .ThenInclude(a => a.UploadedBy)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (ticketModel == null)
            {
                return NotFound();
            }

            var viewModel = new TicketDetailsViewModel
            {
                Id = ticketModel.Id,
                Title = ticketModel.Title,
                Description = ticketModel.Description,
                CurrentStatus = ticketModel.CurrentStatus,
                Project = ticketModel.Project,
                CreatorName = ticketModel.CreatedBy != null ? ticketModel.CreatedBy.UserName! : "Unknown",
                AssigneeName = ticketModel.Assignee?.UserName,
                CreatedAt = ticketModel.CreatedAt,

                Comments = ticketModel.Comments
                            .OrderByDescending(c => c.CreatedAt)
                            .Select(c => new CommentViewModel
                            {
                                CreatorName = c.Creator?.Name ?? "Unknown",
                                Content = c.Content,
                                CreatedAt = c.CreatedAt
                            }).ToList(),

                Attachments = ticketModel.Attachments
                    .OrderByDescending(a => a.UploadedAt)
                    .Select(a => new AttachmentViewModel
                    {
                        Id = a.Id,
                        Filename = a.Filename,
                        UploadedByName = a.UploadedBy?.Name ?? "Unknown",
                        UploadedAt = a.UploadedAt
                    }).ToList(),


                NewComment = new AddCommentViewModel { TicketId = ticketModel.Id},
                NewAttachment = new UploadAttachmentViewModel { TicketId = ticketModel.Id }
            };
            

            return View(viewModel);
        }

        // POST: /Tickets/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(AddCommentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Details), new { id = model.TicketId });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(); 
            }

            var comment = new TicketComment
            {
                TicketId = model.TicketId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow, 
                CreatorId = currentUserId
            };

            _context.TicketComments.Add(comment);
            await _context.SaveChangesAsync();
            return Redirect($"{Url.Action("Details", new { id = model.TicketId })}#comments");
        }

        // POST: /Tickets/UploadAttachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(UploadAttachmentViewModel model)
        {
            if (!ModelState.IsValid || model.File == null || model.File.Length == 0)
            {
                TempData["ErrorMessage"] = "Select please a valid file";
                return RedirectToAction(nameof(Details), new { id = model.TicketId });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "tickets", model.TicketId.ToString());

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.File.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(fileStream);
            }

            var attachment = new TicketAttachment
            {
                TicketId = model.TicketId,
                Filename = uniqueFileName, 
                UploadedById = currentUserId,
                UploadedAt = DateTime.UtcNow
            };

            _context.TicketAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "File added successfully!";
            return RedirectToAction(nameof(Details), new { id = model.TicketId });

        }


        // GET: /Tickets/DownloadAttachment/5
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var attachment = await _context.TicketAttachments.FindAsync(id);
            if (attachment == null)
            {
                return NotFound();
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "tickets", attachment.TicketId.ToString(), attachment.Filename);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Arquivo físico não encontrado no servidor.");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            var originalFileName = attachment.Filename.Substring(attachment.Filename.IndexOf('_') + 1);

            return File(fileBytes, "application/octet-stream", originalFileName);
        }


        // POST: Ticket/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                // If validation fails, repopulate lists and return the partial view with errors
                viewModel.Projects = await _context.Projects.Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Title }).ToListAsync();
                viewModel.UsersList = await _context.Users.Select(u => new SelectListItem { Value = u.Id, Text = u.UserName }).ToListAsync();

                return PartialView("_CreateTicketModalPartial", viewModel);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(viewModel.ProjectId);
            if (project == null)
            {
                return NotFound();
            }

            if (project.EndDate.HasValue)
            {
                return Unauthorized("Cannot create a ticket for a project that has already ended.");
            }

            var ticket = new TicketModel
            {
                Title = viewModel.Title,
                Description = viewModel.Description,
                ProjectId = viewModel.ProjectId,
                AssigneeId = viewModel.AssigneeId,
                CreatorId = userId,
                CreatedAt = DateTime.Now,
                CurrentStatus = "Open"
            };

            _context.Add(ticket);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // CREATE PARTIAL: Fetches the Create Form to put inside the Modal
        public async Task<IActionResult> CreatePartial()
        {
            var model = new TicketCreateViewModel
            {
                // Populate dropdowns for the modal
                Projects = await _context.Projects
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Title })
                    .ToListAsync(),

                UsersList = await _context.Users
                    .Select(u => new SelectListItem { Value = u.Id, Text = u.UserName })
                    .ToListAsync()
            };

            return PartialView("_CreateTicketModalPartial", model);
        }

        // GET: Ticket/Edit/5 : TODO : Not Used
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticketModel = await _context.Tickets
                            .Include(t => t.Project)
                            .Include(t => t.CreatedBy)
                            .FirstOrDefaultAsync(m => m.Id == id); 
            if (ticketModel == null)
            {
                return NotFound();
            }
            ViewData["AssigneeId"] = new SelectList(_context.Users, "Id", "UserName", ticketModel.AssigneeId);
            return View(ticketModel);
        }

        // POST: Ticket/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TicketEditViewModel viewModel)
        {
            // Check Ticket
            if (id != viewModel.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateEditDropdownsAsync(viewModel);
                return PartialView("_EditTicketModalPartial", viewModel);
            }

            var ticketFromDB = await _context.Tickets.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == viewModel.Id);

            if (ticketFromDB == null)
            {
                return NotFound();
            }

            // Rejects if Project is Ended or Ticket is Closed
            if (ticketFromDB.Project!.EndDate.HasValue || ticketFromDB.ClosedAt.HasValue)
            {
                return Json(new { success = false, message = "Not Allowed: Project is Ended or Ticket is Closed." });
            }

            // Can only update status if the ticket is assigned to someone
            if (viewModel.CurrentStatus != "Open" & string.IsNullOrEmpty(ticketFromDB.AssigneeId))
            {
                return Json(new { success = false, message = "Ticket must be Assigned" });
            }

            ticketFromDB.Title = viewModel.Title;
            ticketFromDB.Description = viewModel.Description;
            ticketFromDB.AssigneeId = viewModel.AssigneeId;
            ticketFromDB.CurrentStatus = viewModel.CurrentStatus;
            if (viewModel.AssigneeId != null)
            {
                ticketFromDB.AssignedAt = DateTime.Now;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TicketModelExists(viewModel.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> EditPartial(int id)
        {
            // Get TicketData
            var ticketData = await _context.Tickets
                                .Where(t => t.Id == id)
                                .Select(t => new TicketEditViewModel
                                {
                                    Id = t.Id,
                                    Title = t.Title,
                                    Description = t.Description,
                                    CurrentStatus = t.CurrentStatus,
                                    AssigneeId = t.AssigneeId,
                                    WorkflowId = t.Project!.WorkflowId
                                })
                                .FirstOrDefaultAsync();

            if(ticketData ==null) return NotFound();
            if(ticketData.WorkflowId == 0) return NotFound("Workflow not found");

            // List of Workflow Statuses and Users for Dropdowns
            await PopulateEditDropdownsAsync(ticketData);

            return PartialView("_EditTicketModalPartial", ticketData);
        }

        [HttpGet]
        public async Task<IActionResult> AddDependencyPartial(int ticketId)
        {
            var ticket = await _context.Set<TicketModel>().FindAsync(ticketId);
            if (ticket == null) return NotFound();

            var ticketsBlockingMe = await _context.Set<TicketDependency>()
                                                .Where(td => td.BlockedTicketId == ticketId)
                                                .Select(td => td.BlockingTicket)
                                                .ToListAsync();

            var ticketsIBlock = await _context.Set<TicketDependency>()
                                                .Where(td => td.BlockingTicketId == ticketId)
                                                .Select(td => td.BlockedTicket)
                                                .ToListAsync();

            var existingDependenciesIds = ticketsBlockingMe.Select(t => t.Id).ToList();

            var candidateTickets = await _context.Set<TicketModel>()
                                                .Where(t => t.ProjectId == ticket.ProjectId
                                                         && t.Id != ticketId
                                                         && !existingDependenciesIds.Contains(t.Id))
                                                .ToListAsync();

            var availableTicketsList = new List<SelectListItem>();

            foreach (var candidate in candidateTickets)
            {
                if (!await CheckCircularDependency(ticketId, candidate.Id))
                {
                    availableTicketsList.Add(new SelectListItem
                    {
                        Value = candidate.Id.ToString(),
                        Text = $"#{candidate.Id} - {candidate.Title}"
                    });
                }
            }

            var model = new TicketAddDependencyViewModel
            {
                BlockedTicketId = ticketId,
                AvailableTickets = availableTicketsList,
                TicketsBlockingMe = ticketsBlockingMe!,
                TicketsIBlock = ticketsIBlock!
            };

            return PartialView("_AddDependencyTicketPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDependency(TicketAddDependencyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Details), new { id = model.BlockedTicketId });
            }

            var blockedTicket = await _context.Set<TicketModel>().FindAsync(model.BlockedTicketId);
            var blockingTicket = await _context.Set<TicketModel>().FindAsync(model.BlockingTicketId);

            if (blockedTicket == null || blockingTicket == null || blockedTicket.ProjectId != blockingTicket.ProjectId)
            {
                TempData["Error"] = "Invalid Tickets or they belong to different projects";
                return RedirectToAction(nameof(Details), new { id = model.BlockedTicketId });
            }

            if (await CheckCircularDependency(model.BlockedTicketId, model.BlockingTicketId))
            {
                TempData["Error"] = "It is not possible to add dependency. They would generate circular dependency";
                return RedirectToAction(nameof(Details), new { id = model.BlockedTicketId });
            }

            var dependency = new TicketDependency
            {
                BlockedTicketId = model.BlockedTicketId,
                BlockingTicketId = model.BlockingTicketId
            };

            _context.Add(dependency);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Dependency added successfully.";
            return RedirectToAction(nameof(Details), new { id = model.BlockedTicketId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDependency(int blockedId, int blockingId)
        {
            var dependency = await _context.Set<TicketDependency>()
                                .FirstOrDefaultAsync(td => td.BlockedTicketId == blockedId && td.BlockingTicketId == blockingId);

            if (dependency != null)
            {
                _context.Remove(dependency);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Dependency Removed Successfully.";
            }

            return RedirectToAction(nameof(Details), new { id = blockedId });
        }


        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateTicketStatusRequest request)
        {
            if (request == null || request.TicketId <= 0 || string.IsNullOrEmpty(request.NewStatus))
            {
                return BadRequest(new { success = false, message = "Invalid Data." });
            }

            var ticket = await _context.Tickets.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == request.TicketId);

            if (ticket == null)
            {
                return NotFound(new { success = false, message = "Ticket not found." });
            }

            // Rejects if Project is Ended or Ticket is Closed
            if (ticket.Project!.EndDate.HasValue || ticket.ClosedAt.HasValue)
            {
                return Json(new { success = false, message = "Not Allowed: Project is Ended or Ticket is Closed." });
            }

            // Can only update status if the ticket is assigned to someone
            if (string.IsNullOrEmpty(ticket.AssigneeId))
            {
                return Json(new { success = false, message = "Ticket must be Assigned" });
            }

            // Update status
            ticket.CurrentStatus = request.NewStatus;

            if (request.NewStatus.ToLower() == "closed") { ticket.ClosedAt = DateTime.Now; }

            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Status updated successfully" });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, TicketAssignViewModel viewModel)
        {
            if(id != viewModel.Id)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);

            if(ticket == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var usersList = await _context.Users
                            .Select(u => new SelectListItem { Value = u.Id, Text = u.UserName })
                            .ToListAsync();
                viewModel.UsersList = usersList;
                return PartialView("_EditTicketModalPartial", viewModel);
            }

            if (ticket.Project!.EndDate.HasValue || ticket.ClosedAt.HasValue)
            {
                return Json(new { success = false, message = "Not Allowed: Project is Ended or Ticket is Closed." });
            }

            ticket.AssigneeId = viewModel.AssigneeId;
            ticket.AssignedAt = DateTime.Now;

            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        [HttpGet]
        public async Task<IActionResult> AssignPartial(int id)
        {
            // Checks
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);

            if(ticket == null) return NotFound();

            var usersList = await _context.Users
                    .Select(u => new SelectListItem { Value = u.Id, Text = u.UserName })
                    .ToListAsync();

            var viewModel = new TicketAssignViewModel
            {
                Id = ticket.Id,
                AssigneeId = ticket.AssigneeId,
                UsersList = usersList
            };

            return PartialView("_AssignTicketModalPartial", viewModel);
        }

        private bool TicketModelExists(int id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }

        private IQueryable<TicketModel> ApplyFilters(IQueryable<TicketModel> query, TicketFilterViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.SearchTitle))
                query = query.Where(t => t.Title.Contains(model.SearchTitle));

            if (model.SearchDate.HasValue)
                query = query.Where(t => t.CreatedAt.Date == model.SearchDate.Value.Date);

            if (!string.IsNullOrWhiteSpace(model.SearchStatus))
            {
                query = model.SearchStatus switch
                {
                    "Open" => query.Where(t => t.CurrentStatus == "Open"),
                    "Closed" => query.Where(t => t.CurrentStatus == "Closed"),
                    _ => query.Where(t => t.CurrentStatus != "Open" && t.CurrentStatus != "Close")
                };
            }
            if (!string.IsNullOrEmpty(model.SearchTitle))
                query = query.Where(t => t.Title.Contains(model.SearchTitle));

            if (model.SearchDate.HasValue)
                query = query.Where(t => t.CreatedAt.Date == model.SearchDate.Value.Date);

            if (!string.IsNullOrEmpty(model.SearchCreatedBy))
                query = query.Where(t => t.CreatedBy.UserName == model.SearchCreatedBy);

            if (!string.IsNullOrEmpty(model.SearchProject))
                query = query.Where(t => t.Project.Title == model.SearchProject);

            if (!string.IsNullOrEmpty(model.SearchAssignee))
                query = query.Where(t => t.Assignee.UserName == model.SearchAssignee);
            
            return query;
        }

        private async Task PopulateEditDropdownsAsync(TicketEditViewModel model)
        {
            var statusList = await _context.WorkflowStatuses
                                .Where(ws => ws.WorkflowId == model.WorkflowId)
                                .Select(ws => new SelectListItem { Value = ws.Name, Text = ws.Name }).ToListAsync();

            var usersList = await _context.Users
                                .Select(u => new SelectListItem { Value = u.Id, Text = u.UserName })
                                .ToListAsync();


            model.StatusList = statusList;
            model.UsersList = usersList;

        }

        private async Task<bool> CheckCircularDependency(int blockedId, int blockingId)
        {
            var queue = new Queue<int>();
            var visited = new HashSet<int>();

            queue.Enqueue(blockingId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();

                if (currentId == blockedId)
                    return true;

                if (!visited.Contains(currentId))
                {
                    visited.Add(currentId);
                    var blockers = await _context.Set<TicketDependency>()
                        .Where(td => td.BlockedTicketId == currentId)
                        .Select(td => td.BlockingTicketId)
                        .ToListAsync();

                    foreach (var blocker in blockers)
                    {
                        queue.Enqueue(blocker);
                    }
                }
            }
            return false;
        }
    }
}
