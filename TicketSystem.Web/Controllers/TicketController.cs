using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading.Tasks;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.Project;
using TicketSystem.Web.Models.Ticket;

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
        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Tickets
                                    .Include(t => t.Assignee)
                                    .Include(t => t.ClosedBy)
                                    .Include(t => t.CreatedBy)
                                    .Include(t => t.Project)
                                    .Select(t => new DisplayTicketViewModel
                                    {
                                        Id = t.Id,
                                        Title = t.Title,
                                        Description = t.Description,
                                        Project = t.Project != null ? t.Project.Title : "No Project",
                                        CreatedBy = t.CreatedBy != null ? t.CreatedBy.UserName! : "Unknown",
                                        CreatedAt = t.CreatedAt,
                                        Assignee = t.Assignee != null ? t.Assignee.UserName! : "Not Assigned",
                                        AssignedAt = t.AssignedAt,
                                        ClosedBy = t.ClosedBy != null ? t.ClosedBy.UserName! : "-",
                                        ClosedAt = t.ClosedAt,
                                        CurrentStatus = t.CurrentStatus
                                    }).ToListAsync();


            return View(tickets);
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
                ProjectName = ticketModel.Project != null ? ticketModel.Project.Title : "No Project",
                CreatorName = ticketModel.CreatedBy != null ? ticketModel.CreatedBy.UserName! : "Unknown",
                AssigneeName = ticketModel.Assignee?.UserName,
                CreatedAt = ticketModel.CreatedAt,

                Comments = ticketModel.Comments
                            .OrderByDescending(c => c.CreatedAt)
                            .Select(c => new CommentViewModel
                            {
                                CreatorName = c.Creator?.UserName ?? "Unknown",
                                Content = c.Content,
                                CreatedAt = c.CreatedAt
                            }).ToList(),

                Attachments = ticketModel.Attachments
                    .OrderByDescending(a => a.UploadedAt)
                    .Select(a => new AttachmentViewModel
                    {
                        Id = a.Id,
                        Filename = a.Filename,
                        UploadedByName = a.UploadedBy?.UserName ?? "Unknown",
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

        // GET: Ticket/Create
        public async Task<IActionResult> Create()
        {
            var users = await _context.Users.ToListAsync();
            var projects = await _context.Projects.ToListAsync();

            var viewModel = new CreateTicketViewModel
            {
                Projects = projects.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Title
                }),
            };
            return View(viewModel);
        }

        // POST: Ticket/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTicketViewModel viewModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            var projects = await _context.Projects.FindAsync(viewModel.ProjectId);
            if (projects == null)
            {
                return NotFound();
            }

            
            /* TO MOVE
            var workflowStatuses = await _context.WorkflowStatuses.ToListAsync();

            var wfStatusProject = workflowStatuses.Where(w => w.WorkflowId == projects.WorkflowId);

            string firstStatus = wfStatusProject.Where(s => s.IsInicial).Select(s => s.Name).ToList()[0];*/


            if (ModelState.IsValid)
            {
                

                var ticket = new TicketModel
                {
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    ProjectId = viewModel.ProjectId,
                    CreatorId = userId,
                    CreatedAt = DateTime.Now,
                    CurrentStatus = "Open"
                };

                _context.Add(ticket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            viewModel.Projects = new SelectList(_context.Projects, "Id", "Title", viewModel.ProjectId);

            return View(viewModel);
        }

        // GET: Ticket/Edit/5
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Description,AssigneeId")] TicketModel ticketModel)
        {
            if (id != ticketModel.Id)
            {
                return NotFound();
            }

            var ticketFromDB = await _context.Tickets.FindAsync(id);

            if (ticketFromDB == null)
            {
                return NotFound();
            }

            ticketFromDB.Description = ticketModel.Description;
            ticketFromDB.AssigneeId = ticketModel.AssigneeId;
            ticketFromDB.AssignedAt = DateTime.Now;

            ModelState.Clear();

            TryValidateModel(ticketFromDB);

            if (ModelState.IsValid)
            {
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketModelExists(ticketModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AssigneeId"] = new SelectList(_context.Users, "Id", "Id", ticketModel.AssigneeId);

            return View(ticketModel);
        }

        // GET: Ticket/Delete/5
        public async Task<IActionResult> Delete(int? id)
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
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticketModel == null)
            {
                return NotFound();
            }

            return View(ticketModel);
        }

        // POST: Ticket/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticketModel = await _context.Tickets.FindAsync(id);
            if (ticketModel != null)
            {
                _context.Tickets.Remove(ticketModel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketModelExists(int id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }
    }
}
