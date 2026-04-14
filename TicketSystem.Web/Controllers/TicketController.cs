using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Security.Claims;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.ProjectManagement;
using TicketSystem.Web.Models.Ticket;
using TicketSystem.Web.Models.Ticket.ViewModels;

namespace TicketSystem.Web.Controllers
{
    [Authorize]
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
            var query = _context.Tickets
                                .AsNoTracking()
                                .Where(t => !t.Project!.IsDeleted)
                                .AsQueryable();

            // Apply Filters
            query = ApplyFilters(query, model);

            // To ViewModel
            model.Tickets = await query
                .Select(t => new TicketListViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Project = t.Project!.Title,
                    CreatedBy = t.CreatedBy!.UserName!,
                    CreatedAt = DateOnly.FromDateTime(t.CreatedAt),
                    Assignee = t.Assignee != null ? t.Assignee.UserName! : "Not Assigned",
                    CurrentStatus = t.CurrentStatus,
                    CanChange = (isAdmin ||
                         t.CreatorId == currentUserId ||
                         t.AssigneeId == currentUserId ||
                         t.Project.Members.Any(m => m.MemberId == currentUserId && m.RoleInProject == "Manager"))
                         &&
                         t.CurrentStatus != "Closed"
                         &&
                         t.Project.EndDate == null,
                    CanChangeStatus = !t.BlockedByTickets.Any(td => td.BlockingTicket!.CurrentStatus != "Closed") && t.AssigneeId != null
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
            if (id == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var ticketModel = await _context.Tickets
                .Include(t => t.Assignee)
                .Include(t => t.ClosedBy)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                    .ThenInclude(p => p.Members)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.Creator)
                .Include(t => t.Attachments)
                .ThenInclude(a => a.UploadedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticketModel == null || ticketModel.Project!.IsDeleted) return NotFound();

            // Business Rules for Permissions
            bool isProjectActive = ticketModel.Project!.EndDate == null;
            bool isNotClosed = ticketModel.CurrentStatus != "Closed";
            bool hasRole = isAdmin || ticketModel.CreatorId == currentUserId || ticketModel.AssigneeId == currentUserId ||
                   ticketModel.Project.Members.Any(m => m.MemberId == currentUserId && m.RoleInProject == "Manager");

            bool canEditBase = isProjectActive && isNotClosed && hasRole;

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
                CanEdit = canEditBase,
                CanManageDependencies = canEditBase,
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

        // GET CREATE TICKET: Fetches the Create Form to put inside the Modal
        public async Task<IActionResult> CreatePartial(int? projectId = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var projectQuery = _context.Projects
                                    .Where(p => !p.IsDeleted && p.EndDate == null);

            if (!isAdmin)
            {
                projectQuery = projectQuery.Where(p => p.Members.Any(m => m.MemberId == currentUserId));
            }


            var model = new TicketCreateViewModel
            {
                // Populate dropdowns for the modal
                Projects = await projectQuery
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Title })
                    .ToListAsync(),

                ProjectId = projectId ?? 0,
                // To be Populated by Ajax
                UsersList = new List<SelectListItem>()
            };

            if (projectId.HasValue)
            {
                model.UsersList = await _context.ProjectMembers
                    .Where(pm => pm.ProjectId == projectId.Value)
                    .Select(pm => new SelectListItem { Value = pm.MemberId, Text = pm.Member!.UserName })
                    .ToListAsync();
            }

            return PartialView("_CreateTicketModalPartial", model);
        }

        // Endpoint to be called by Ajax to populate the projects:
        [HttpGet]
        public async Task<IActionResult> GetProjectMembers(int projectId)
        {
            var members = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId)
                .Select(pm => new { value = pm.MemberId, text = pm.Member!.UserName })
                .ToListAsync();

            return Json(members);
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
                bool isBlocked = await _context.TicketDependencies
                         .AnyAsync(td => td.BlockedTicketId == id && td.BlockingTicket!.CurrentStatus != "Closed") || viewModel.AssigneeId == null;
                
                viewModel.CanChangeStatus = !isBlocked;

                var ticket = await _context.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

                if (ticket != null)
                {
                    viewModel.UsersList = await _context.ProjectMembers
                                                        .Where(pm => pm.ProjectId == ticket.ProjectId)
                                                        .Select(pm => new SelectListItem { Value = pm.MemberId, Text = pm.Member!.UserName })
                                                        .ToListAsync();
                    await PopulateEditDropdownsAsync(viewModel);
                }
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var t = await _context.Tickets
                            .Include(t => t.Project)
                                .ThenInclude(p => p.Members)
                            .Include(t => t.BlockedByTickets) 
                            .FirstOrDefaultAsync(t => t.Id == id);

            if (t == null) return NotFound();

            // Edit Business Rules
            bool isProjectActive = t.Project!.EndDate == null;
            bool isNotClosed = t.CurrentStatus != "Closed";
            bool hasRole = isAdmin || t.CreatorId == currentUserId || t.AssigneeId == currentUserId ||
                   t.Project.Members.Any(m => m.MemberId == currentUserId && m.RoleInProject == "Manager");

            bool canEdit = isProjectActive && isNotClosed && hasRole;

            if (!canEdit)
            {
                return Forbid(); 
            }

            // Status Change Rule
            bool isBlocked = await _context.TicketDependencies
                                     .AnyAsync(td => td.BlockedTicketId == id && td.BlockingTicket!.CurrentStatus != "Closed") || t.AssigneeId == null;

            // Get TicketData
            var ticketData = new TicketEditViewModel
                                {
                                    Id = t.Id,
                                    Title = t.Title,
                                    Description = t.Description,
                                    CurrentStatus = t.CurrentStatus,
                                    AssigneeId = t.AssigneeId,
                                    WorkflowId = t.Project!.WorkflowId,
                                    ProjectId = t.ProjectId,
                                    CanChangeStatus = !isBlocked
            };


            // List of Workflow Statuses and Users for Dropdowns
            await PopulateEditDropdownsAsync(ticketData);

            return PartialView("_EditTicketModalPartial", ticketData);
        }

        [HttpGet]
        public async Task<IActionResult> AddDependencyPartial(int ticketId)
        {
            var ticket = await _context.Tickets
                                        .Include(t => t.Project)
                                            .ThenInclude(p => p.Members)
                                        .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

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

            bool isProjectActive = ticket.Project!.EndDate == null;
            bool isNotClosed = ticket.CurrentStatus != "Closed";
            bool hasRole = isAdmin || ticket.CreatorId == currentUserId || ticket.AssigneeId == currentUserId ||
                   ticket.Project.Members.Any(m => m.MemberId == currentUserId && m.RoleInProject == "Manager");

            var model = new TicketAddDependencyViewModel
            {
                BlockedTicketId = ticketId,
                AvailableTickets = availableTicketsList,
                TicketsBlockingMe = ticketsBlockingMe!,
                TicketsIBlock = ticketsIBlock!,
                CanManageDependencies = isProjectActive && isNotClosed && hasRole
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
        public async Task<IActionResult> CloseTicket(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var ticket = await _context.Tickets
                .Include(t => t.BlockedByTickets)
                    .ThenInclude(td => td.BlockingTicket)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

             bool isBlocked = ticket.BlockedByTickets.Any(td => td.BlockingTicket!.CurrentStatus != "Closed") || ticket.AssigneeId == null;

            if (!isBlocked && ticket.CurrentStatus != "Closed")
            {
                ticket.CurrentStatus = "Closed";
                ticket.ClosedById = currentUserId;
                ticket.ClosedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
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


        // ------- PRIVATE METHOD ---------------
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


            var usersList = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == model.ProjectId)
                .Select(pm => new SelectListItem { Value = pm.MemberId, Text = pm.Member!.UserName })
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


        private bool CanChangeTicket(TicketModel ticket, ProjectModel project)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!project.EndDate.HasValue && ticket.AssigneeId != null && ticket.CurrentStatus != "Closed" && !ticket.BlockedByTickets.Any() && (User.IsInRole("Admin") || project.Members.Any(pm => pm.RoleInProject == "Manager" && pm.MemberId == currentUserId) || currentUserId == ticket.CreatorId || currentUserId == ticket.AssigneeId))
            {
                return true;
            }
            return false;
        }

        private bool CanCreateTicket(ProjectModel project)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!project.EndDate.HasValue && (User.IsInRole("Admin") || project.Members.Any(pm => pm.MemberId == currentUserId)))
            {
                return true;
            }
            return false;
        }

        private bool CanAssignTicket(TicketModel ticket, ProjectModel project)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!project.EndDate.HasValue && ticket.CurrentStatus != "Closed" && (User.IsInRole("Admin") || project.Members.Any(pm => pm.RoleInProject == "Manager" && pm.MemberId == currentUserId) || currentUserId == ticket.CreatorId))
            {
                return true;
            }
            return false;
        }

    }
}
