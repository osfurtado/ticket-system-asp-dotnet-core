using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.CurrentUserId = currentUser?.Id;

            return View(userViewModels);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync<AppRole>(), "Id", "Name");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var usernameExists = await _userManager.FindByNameAsync(model.Username);

                if(usernameExists != null)
                {
                    ModelState.AddModelError("Username", "username already exists");
                    ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Id", "Name");
                    return View(model);
                }

                var user = new AppUser { Name = model.Name, UserName = model.Username, IsActive = model.IsActive };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    var role = await _roleManager.FindByIdAsync(model.RoleId);
                    if (role != null)
                    {
                        await _userManager.AddToRoleAsync(user, role.Name);
                    }
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Se falhou, recarrega as roles
            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Id", "Name");
            return View(model);
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

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Name == userRoles.FirstOrDefault());

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.UserName,
                IsActive = user.IsActive,
                RoleId = role?.Id
            };

            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.CurrentUserId = currentUser.Id;

            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Id", "Name", role?.Id);
            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (model.Id == currentUser?.Id)
            {
                ModelState.Remove("RoleId");
                ModelState.Remove("IsActive"); 
            }


            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Id", "Name", model.RoleId);
                ViewBag.CurrentUserId = currentUser.Id;
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            
            if (user == null) return NotFound();

            // Atualiza dados básicos
            user.Name = model.Name;
            user.UserName = model.Username;

            if (user.Id != currentUser.Id)
            {
                user.IsActive = model.IsActive;

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!string.IsNullOrEmpty(model.RoleId))
                {
                    var newRole = await _roleManager.FindByIdAsync(model.RoleId);
                    if (newRole != null) await _userManager.AddToRoleAsync(user, newRole.Name);
                }
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
