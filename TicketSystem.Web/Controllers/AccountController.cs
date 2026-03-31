using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Web.Models.Account;
using TicketSystem.Web.Models.Users;

namespace TicketSystem.Web.Controllers
{
    public class AccountController : Controller
    {

        private UserManager<AppUser> _userManager;
        private SignInManager<AppUser> _signInManager;
        public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager)
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
        }

        public IActionResult Login(string returnUrl)
        {
            return View(new LoginModel()
            {
                Username = string.Empty,
                Password = string.Empty,
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                AppUser? user = await _userManager.FindByNameAsync(model.Username);
                if (user != null)
                {
                    await _signInManager.SignOutAsync();
                    var result = await _signInManager.PasswordSignInAsync(
                    user, model.Password, false, false);
                    if (result.Succeeded)
                    {
                        return Redirect(model.ReturnUrl ?? "/");
                    }
                }
                ModelState.AddModelError("", "Username oder Passwort ungültig");
            }
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Logout(string returnUrl = "/")
        {
            await _signInManager.SignOutAsync();
            return Redirect(returnUrl);
        }

        public IActionResult AccessDenied(string returnUrl)
        {
            return View("AccessDenied", returnUrl);
        }

        // GET: Account/Register
        [AllowAnonymous]
        public IActionResult Register() => View();

        // POST: Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Note que IsActive é false por padrão no Model
                var user = new AppUser { UserName = model.Username, Name = model.Name };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // NÃO fazemos o SignIn aqui. O usuário deve aguardar a aprovação do Admin.
                    TempData["SuccessMessage"] = "Registro concluído com sucesso! Aguarde o administrador ativar sua conta.";
                    return RedirectToAction("Login", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: Account/Profile
        [Authorize] // Qualquer usuário logado pode acessar
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new ProfileViewModel { Name = user.Name, Username = user.UserName };
            return View(model);
        }

        // POST: Account/Profile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.Name = model.Name;
            user.UserName = model.Username;
            await _userManager.UpdateAsync(user);

            // Atualizar senha se preenchido
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            }

            // Atualiza o cookie de login para refletir possíveis mudanças
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Profile updated sucessfully!";
            return RedirectToAction(nameof(Profile));
        }
    }
}
