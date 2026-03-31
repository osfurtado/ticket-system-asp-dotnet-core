using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using TicketSystem.Web.Models.Account;

namespace TicketSystem.Web.Models
{
    public static class EnsureDatabase
    {
        // Definimos as Roles num array para facilitar a iteração e futura manutenção
        private static readonly string[] _roles = { "Admin", "Tester", "Developer" };

        // Dados do utilizador a ser criado
        private const string AdminUserName = "ofurtado";
        private const string AdminFullName = "Osvaldo Furtado";
        private const string DefaultPassword = "Secret123$";

        public static void Migrate(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (ctx.Database.GetPendingMigrations().Any())
            {
                ctx.Database.Migrate();
            }
        }

        public static async Task SeedDefaultAccounts(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            // 1. Seed 3 Roles (Admin, Tester, Developer)
            foreach (var roleName in _roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new AppRole() { Name = roleName };
                    await roleManager.CreateAsync(role);
                }
            }

            // 2. Seed apenas do utilizador 'ofurtado'
            var adminUser = await userManager.FindByNameAsync(AdminUserName);
            if (adminUser == null)
            {
                adminUser = new AppUser()
                {
                    UserName = AdminUserName,
                    // Assumindo que a sua classe AppUser tem a propriedade Name. 
                    // Se for FirstName/LastName, ajuste de acordo.
                    Name = AdminFullName
                };

                var createResult = await userManager.CreateAsync(adminUser, DefaultPassword);

                // Apenas adicionamos a Role se a criação do utilizador for bem-sucedida
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
