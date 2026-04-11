using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.Account;
using TicketSystem.Web.Models.ProjectManagement;
using TicketSystem.Web.Models.Users;
using TicketSystem.Web.Models.Workflow;

namespace TicketSystem.Web.Controllers
{
    [Authorize(Roles ="Admin")]
    public class ProjectManagementController : Controller
    {
        private readonly AppDbContext _context;

        public ProjectManagementController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Project
        public async Task<IActionResult> Index()
        {
            var viewModel = await BuildViewModelAsync();
            return View(viewModel);
        }

        // GET: Project/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            var project = await _context.Projects
                .Include(p => p.CreatedBy)
                .Include(p => p.Members)
                    .ThenInclude(pm => pm.Member)
                .Include(p => p.Workflow)
                    .ThenInclude(w => w!.Statuses) 
                .Include(p => p.Tickets)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);


            if (project == null) return NotFound();

            var existingMemberIds = project.Members.Select(m => m.MemberId).ToList();
            var availableUsers = await _context.Users
                .Where(u => u.IsActive && !existingMemberIds.Contains(u.Id))
                .Select(u => new SelectListItem { Value = u.Id, Text = u.Name })
                .ToListAsync();

            var availableRoles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Manager", Text = "Manager" },
                new SelectListItem { Value = "Member", Text = "Member" }
            };

            var viewModel = new ProjectDetailsViewModel
            {
                // Home
                CurrentUserId = currentUserId,
                ProjectId = project.Id,
                Title = project.Title,
                Description = project.Description,
                CreatedByName = project.CreatedBy?.Name ?? "Unknown",
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                TotalTickets = project.Tickets?.Count() ?? 0,
                TotalOpenedTickets = project.Tickets?.Count(t => t.CurrentStatus != "Closed") ?? 0,

                // Members
                ExistingMembers = project.Members.Select(m => new ProjectMemberItemViewModel
                {
                    MemberId = m.MemberId,
                    MemberName = m.Member?.Name ?? "Unknown",
                    RoleInProject = m.RoleInProject,
                    Initials = AvatarHelper.GetInitials(m.Member?.Name ?? "Default")
                }).ToList(),
                AddMemberForm = new AddProjectMemberViewModel
                {
                    ProjectId = project.Id,
                    AvailableUsers = availableUsers,
                    AvailableRoles = availableRoles
                },

                // Workflow
                WorkflowId = project.WorkflowId,
                WorkflowName = project.Workflow?.Name ?? "Default Workflow", 
                WorkflowStatuses = project.Workflow!.Statuses!.Select(s => new WorkflowStatusItemViewModel
                {
                    StatusId = s.Id,
                    Name = s.Name,
                    IsInicial = s.IsInicial,
                    IsFinal = s.IsFinal
                }).ToList(),
                AddStatusForm = new AddWorkflowStatusViewModel
                {
                    ProjectId = project.Id,
                    WorkflowId = project.WorkflowId
                }
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(CreateProjectVM CreateForm)
        {

            if (!ModelState.IsValid)
                return View("Index", await BuildViewModelAsync(CreateForm, showCreateModal: true));

            // Workflow Statuses
            var Statuses = new List<WorkflowStatus>
            {
                new WorkflowStatus { Name = "Open", IsInicial = true, IsFinal = false },
                new WorkflowStatus { Name = "Closed", IsInicial = false, IsFinal = true }
            };

            // Workflow
            var newWorkflow = new WorkflowModel
            {
                Name = $"{CreateForm.Title} - Workflow",
                Statuses = new List<WorkflowStatus>
                {
                    new WorkflowStatus { Name = "Open", IsInicial = true, IsFinal = false },
                    new WorkflowStatus { Name = "Closed", IsInicial = false, IsFinal = true }
                }
            };

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = new ProjectModel
            {
                Title = CreateForm.Title,
                Description = CreateForm.Description,
                StartDate = DateOnly.FromDateTime(DateTime.Now),
                CreatedById = currentUserId!,
                Workflow = newWorkflow,
                IsDeleted = false
            };

            // Create Membership
            var membership = new ProjectMember
            {
                Project = project,
                MemberId = currentUserId!,
                RoleInProject = "Manager"
            };

            _context.Add(project);
            _context.Add(membership);
            await _context.SaveChangesAsync();
            await BuildViewModelAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Project/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null) return NotFound();

            var editForm = new EditProjectVM
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description
            };

            var viewModel = await BuildViewModelAsync(editForm: editForm, showEditModal: true);
            return View("Index", viewModel);

        }

        // POST: Project/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditProjectVM editForm)
        {
            if (id != editForm.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View("Index", await BuildViewModelAsync(editForm: editForm, showEditModal: true));
            }

            var projectModel = await _context.Projects.FindAsync(editForm.Id);

            if (projectModel == null) return NotFound();

            projectModel.Title = editForm.Title;
            projectModel.Description = editForm.Description;

            try
            {
                _context.Update(projectModel);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectModelExists(projectModel.Id))
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

        // GET: Project/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var projectModel = await _context.Projects
                .Include(p => p.Workflow)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (projectModel == null)
            {
                return NotFound();
            }

            return View(projectModel);
        }

        // POST: Project/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var projectModel = await _context.Projects.FindAsync(id);
            
            if (projectModel != null)
            {
                projectModel.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectModelExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }


        // --- POST ACTIONS FOR TABS ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(AddProjectMemberViewModel model)
        {
            if (ModelState.IsValid)
            {
                var newMember = new ProjectMember
                {
                    ProjectId = model.ProjectId,
                    MemberId = model.SelectedUserId,
                    RoleInProject = model.SelectedRole
                };

                _context.ProjectMembers.Add(newMember);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), "ProjectManagement", new { id = model.ProjectId }, "members");
            }
            return RedirectToAction(nameof(Details), "ProjectManagement", new { id = model.ProjectId }, "members");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeMemberRole(int projectId, string memberId, string newRole)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (memberId == currentUserId)
            {
                return RedirectToAction(nameof(Details), "ProjectManagement", new { id = projectId }, "members");
            }

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.MemberId == memberId);

            if (member != null)
            {
                member.RoleInProject = newRole;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), "ProjectManagement", new { id = projectId }, "members");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int projectId, string memberId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // SECURITY CHECK: Prevent users from removing themselves
            if (memberId == currentUserId)
            {
                return RedirectToAction(nameof(Details), "ProjectManagement", new { id = projectId }, "members");
            }

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.MemberId == memberId);

            if (member != null)
            {
                _context.ProjectMembers.Remove(member);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), "ProjectManagement", new { id = projectId }, "members");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateInviteLink(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();

            // Generate a new unique token if one doesn't exist, or replace the old one
            project.InviteToken = Guid.NewGuid();
            await _context.SaveChangesAsync();

            // Build the full absolute URL for the invite link
            var inviteUrl = Url.Action(
                action: nameof(JoinProject),
                controller: "ProjectManagement",
                values: new { token = project.InviteToken },
                protocol: Request.Scheme);

            return Json(new { success = true, url = inviteUrl });
        }


        [HttpGet]
        public async Task<IActionResult> JoinProject(Guid token)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return Challenge(); // Redirect to login if not authenticated

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.InviteToken == token);

            if (project == null)
            {
                // Token is invalid or revoked
                return NotFound("Invalid or expired invite link.");
            }

            // Check if the user is already a member
            var isAlreadyMember = project.Members.Any(m => m.MemberId == currentUserId);

            if (!isAlreadyMember)
            {
                // Add them as a regular Member
                var newMember = new ProjectMember
                {
                    ProjectId = project.Id,
                    MemberId = currentUserId,
                    RoleInProject = "Member"
                };

                _context.ProjectMembers.Add(newMember);
                await _context.SaveChangesAsync();
            }

            // Redirect them to the project details page (Members tab)
            return RedirectToAction(nameof(Details), "ProjectManagement", new { id = project.Id }, "members");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWorkflowStatus(AddWorkflowStatusViewModel model)
        {
            if (ModelState.IsValid)
            {
                var newStatus = new WorkflowStatus
                {
                    WorkflowId = model.WorkflowId,
                    Name = model.Name,
                    IsInicial = model.IsInicial,
                    IsFinal = model.IsFinal
                };

                _context.WorkflowStatuses.Add(newStatus);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), "ProjectManagement", new { id = model.ProjectId }, "workflow");
            }
            return RedirectToAction(nameof(Details), "ProjectManagement", new { id = model.ProjectId }, "workflow");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveWorkflowStatus(int projectId, int statusId)
        {
            var status = await _context.WorkflowStatuses.FindAsync(statusId);

            // SECURITY CHECK: Ensure the status exists and is NOT a locked system status
            if (status != null && !status.IsInicial && !status.IsFinal)
            {
                _context.WorkflowStatuses.Remove(status);
                await _context.SaveChangesAsync();
            }

            // Redirect back directly to the Workflow tab using the URL fragment
            return RedirectToAction(nameof(Details), "ProjectManagement", new { id = projectId }, "workflow");
        }


        [HttpPost]
        // Note: When sending JSON via JS fetch, [FromBody] is required to parse the payload.
        // We omit [ValidateAntiForgeryToken] here unless you explicitly configure your JS fetch to send the token in a custom header (like 'RequestVerificationToken').
        public async Task<IActionResult> UpdateStatusOrder([FromBody] List<StatusOrderDto> newOrder)
        {
            if (newOrder == null || !newOrder.Any())
            {
                return BadRequest("No order data provided.");
            }

            // Extract the IDs from the incoming payload to query them efficiently
            var statusIds = newOrder.Select(o => o.Id).ToList();

            // Fetch only the statuses that are being updated
            var statusesToUpdate = await _context.WorkflowStatuses
                .Where(s => statusIds.Contains(s.Id))
                .ToListAsync();

            foreach (var status in statusesToUpdate)
            {
                // Extra safety check: never reorder Initial or Final statuses
                if (!status.IsInicial && !status.IsFinal)
                {
                    // Find the matching order index from our incoming DTO list
                    var orderInfo = newOrder.First(o => o.Id == status.Id);
                    status.OrderIndex = orderInfo.OrderIndex;
                }
            }

            await _context.SaveChangesAsync();

            // Return an HTTP 200 OK result because this was called via AJAX (no page reload)
            return Ok(new { success = true, message = "Order updated successfully." });
        }

        private async Task<PMCreateEditViewModel> BuildViewModelAsync(
                                                        CreateProjectVM? createForm = null,
                                                        EditProjectVM? editForm = null,
                                                        bool showCreateModal = false,
                                                        bool showEditModal = false)
        {
            var projects = await _context.Projects.Select(p => new ProjectListVM
            {
                Id = p.Id,
                Title = p.Title,
                StartDate = p.StartDate,
                EndDate =  p.EndDate,
                CreatedBy = p.CreatedBy!.UserName ?? "Unknown"
            }).ToListAsync();

            return new PMCreateEditViewModel
            {
                Projects = projects,
                CreateForm = createForm ?? new CreateProjectVM(),
                EditForm = editForm ?? new EditProjectVM(),
                ShowCreateModal = showCreateModal,
                ShowEditModal = showEditModal
            };
        }

    }
}
