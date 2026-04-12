using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.Account;
using TicketSystem.Web.Models.Users;

namespace TicketSystem.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        public UsersController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        // GET: Users
        public async Task<IActionResult> Index()
        {
            var viewModel = await BuildViewModelAsync();
            return View(viewModel);
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel CreateForm)
        {
            if (!ModelState.IsValid)
                return View("Index", await BuildViewModelAsync(CreateForm, showCreateModal: true));

            var usernameExists = await _userManager.FindByNameAsync(CreateForm.Username);
            if (usernameExists != null)
            {
                ModelState.AddModelError("CreateForm.Username", "Username already exists");
                return View("Index", await BuildViewModelAsync(CreateForm, showCreateModal: true));
            }

            var user = new AppUser
            {
                Name = CreateForm.Name,
                UserName = CreateForm.Username,
                IsActive = CreateForm.IsActive
            };

            var result = await _userManager.CreateAsync(user, CreateForm.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View("Index", await BuildViewModelAsync(CreateForm, showCreateModal: true));
            }

            var role = await _roleManager.FindByIdAsync(CreateForm.RoleId);
            if (role?.Name != null)
                await _userManager.AddToRoleAsync(user, role.Name);

            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Impede que o Admin se desative acidentalmente
            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser.Id)
            {
                TempData["ErrorMessage"] = "Você não pode inativar o seu próprio usuário.";
                return RedirectToAction(nameof(Index));
            }

            // Alterna o estado
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Edit/5 : 
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            

            var editForm = new EditUserViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.UserName ?? string.Empty,
                IsActive = user.IsActive,
                RoleId = (await _roleManager.FindByNameAsync(userRoles.FirstOrDefault() ?? string.Empty))?.Id ?? string.Empty,
            };

            var viewModel = await BuildViewModelAsync(editForm: editForm, showEditModal: true);

            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.CurrentUserId = currentUser.Id;

            return View("Index", viewModel);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel EditForm)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (!ModelState.IsValid)
            {
                ViewBag.CurrentUserId = currentUser.Id;
                return View("Index", await BuildViewModelAsync(editForm: EditForm, showEditModal: true));
            }

            var user = await _userManager.FindByIdAsync(EditForm.Id);

            if (user == null) return NotFound();

            // Check username conflict (excluding the current user)
            var usernameExists = await _userManager.FindByNameAsync(EditForm.Username);
            if (usernameExists != null && usernameExists.Id != EditForm.Id)
            {
                ModelState.AddModelError("EditForm.Username", "Username already exists");
                return View("Index", await BuildViewModelAsync(editForm: EditForm, showEditModal: true));
            }

            if (EditForm.Id == currentUser?.Id)
            {
                ModelState.Remove("RoleId");
                ModelState.Remove("IsActive"); 
            }

            // Update User Properties
            user.Name = EditForm.Name;
            user.UserName = EditForm.Username;

            if (user.Id != currentUser.Id)
            {
                user.IsActive = EditForm.IsActive;
            }

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View("Index", await BuildViewModelAsync(editForm: EditForm, showEditModal: true));
            }

            // Update User Roles

            if (user.Id != currentUser.Id)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                var newRole = await _roleManager.FindByIdAsync(EditForm.RoleId);
                if (newRole?.Name != null)
                    await _userManager.AddToRoleAsync(user, newRole.Name);
            }

            return RedirectToAction(nameof(Index));
        }


        // Reusable methods
        // Get All Users:
        private async Task<List<UserListViewModel>> GetUserListAsync()
        {
            var users = await _userManager.Users.ToListAsync<AppUser>();
            var userViewModels = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserListViewModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    Username = user.UserName ?? "Unknown",
                    RoleName = roles.FirstOrDefault() ?? "without role",
                    IsActive = user.IsActive
                });
            }

            return userViewModels;
        }

        private async Task PrepareIndexViewBagAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.CurrentUserId = currentUser?.Id;
            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Id", "Name");
            ViewBag.ShowCreateModal = true;
        }

        private async Task<UserManagementViewModel> BuildViewModelAsync(
                                                        CreateUserViewModel? createForm = null,
                                                        EditUserViewModel? editForm = null,
                                                        bool showCreateModal = false,
                                                        bool showEditModal = false)
        {
            var users = await _userManager.Users.ToListAsync<AppUser>();
            var userViewModels = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserListViewModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    Initials = AvatarHelper.GetInitials(user.Name ?? "Default"),
                    Username = user.UserName ?? "Unknown",
                    RoleName = roles.FirstOrDefault() ?? "Without role",
                    IsActive = user.IsActive
                });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.CurrentUserId = currentUser?.Id;
            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Id", "Name");

            return new UserManagementViewModel
            {
                Users = userViewModels,
                CreateForm = createForm ?? new CreateUserViewModel(),
                EditForm = editForm ?? new EditUserViewModel(),
                ShowCreateModal = showCreateModal,
                ShowEditModal = showEditModal
            };
        }

    }
}
