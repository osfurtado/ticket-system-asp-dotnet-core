using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TicketSystem.Web.Models.Account;
using TicketSystem.Web.Models.ProjectManagement;
using TicketSystem.Web.Models.Ticket;
using TicketSystem.Web.Models.Workflow;

namespace TicketSystem.Web.Models
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, AppDbContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            // 1. Seed App Roles
            string[] appRoles = { "Admin", "Manager", "Tester", "Developer" };
            foreach (var role in appRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new AppRole { Name = role });
                }
            }

            // 2. Seed Users
            var usersToSeed = new List<(string Username, string Email, string Role, string Name)>
        {
            ("admin1", "admin1@app.com", "Admin", "System Admin"),
            ("manager1", "manager1@app.com", "Manager", "Alice Manager"),
            ("manager2", "manager2@app.com", "Manager", "Bob Manager"),
            ("tester1", "tester1@app.com", "Tester", "Charlie Tester"),
            ("tester2", "tester2@app.com", "Tester", "Diana Tester"),
            ("tester3", "tester3@app.com", "Tester", "Eve Tester"),
            ("dev1", "dev1@app.com", "Developer", "Frank Dev"),
            ("dev2", "dev2@app.com", "Developer", "Grace Dev"),
            ("dev3", "dev3@app.com", "Developer", "Hank Dev")
        };

            foreach (var u in usersToSeed)
            {
                if (await userManager.FindByEmailAsync(u.Email) == null)
                {
                    var user = new AppUser { UserName = u.Username, Email = u.Email, Name = u.Name, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(user, "Password123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, u.Role);
                    }
                }
            }

            // Wait for users to be committed to context if not already
            await context.SaveChangesAsync();

            // Retrieve seeded users to use their IDs
            var allUsers = await context.Users.ToListAsync();
            var managers = allUsers.Where(u => u.UserName.StartsWith("manager")).ToList();
            var devs = allUsers.Where(u => u.UserName.StartsWith("dev")).ToList();
            var testers = allUsers.Where(u => u.UserName.StartsWith("tester")).ToList();

            // Proceed only if we haven't seeded projects yet
            if (!context.Set<ProjectModel>().Any())
            {
                for (int i = 1; i <= 5; i++)
                {
                    var projectCreator = managers[i % managers.Count]; // Alternate between managers

                    // 3. Create Workflow and Statuses for the Project
                    var workflow = new WorkflowModel
                    {
                        Name = $"Workflow Project {i}",
                        Statuses = new List<WorkflowStatus>
                    {
                        new WorkflowStatus { Name = "Open", IsInicial = true, IsFinal = false, OrderIndex = 0 },
                        new WorkflowStatus { Name = "In Development", IsInicial = false, IsFinal = false, OrderIndex = 1 },
                        new WorkflowStatus { Name = "Code Review", IsInicial = false, IsFinal = false, OrderIndex = 2 },
                        new WorkflowStatus { Name = "Testing", IsInicial = false, IsFinal = false, OrderIndex = 3 },
                        new WorkflowStatus { Name = "Closed", IsInicial = false, IsFinal = true, OrderIndex = 4 }
                    }
                    };
                    context.Set<WorkflowModel>().Add(workflow);
                    await context.SaveChangesAsync(); // Save to generate IDs

                    // 4. Create Project
                    var project = new ProjectModel
                    {
                        Title = $"Project Alpha {i}",
                        Description = $"This is the description for seeded project {i}.",
                        CreatedById = projectCreator.Id,
                        StartDate = DateOnly.FromDateTime(DateTime.Now),
                        WorkflowId = workflow.Id,
                        IsDeleted = false
                    };
                    context.Set<ProjectModel>().Add(project);
                    await context.SaveChangesAsync(); // Save to generate IDs

                    // 5. Create Project Members (4 per project)
                    // We add the Creator as Manager, 1 Moderator (a dev), 2 Members (a dev and a tester)
                    var members = new List<ProjectMember>
                {
                    new ProjectMember { ProjectId = project.Id, MemberId = projectCreator.Id, RoleInProject = "Manager" },
                    new ProjectMember { ProjectId = project.Id, MemberId = devs[i % devs.Count].Id, RoleInProject = "Moderator" },
                    new ProjectMember { ProjectId = project.Id, MemberId = devs[(i + 1) % devs.Count].Id, RoleInProject = "Member" },
                    new ProjectMember { ProjectId = project.Id, MemberId = testers[i % testers.Count].Id, RoleInProject = "Member" }
                };
                    context.Set<ProjectMember>().AddRange(members);
                    await context.SaveChangesAsync();

                    // 6. Create Tickets (4 per project) distributed across different statuses
                    var statuses = workflow.Statuses.OrderBy(s => s.OrderIndex).ToList();
                    var tickets = new List<TicketModel>
                {
                    new TicketModel {
                        Title = $"Setup Architecture P{i}", Description = "Initial setup",
                        ProjectId = project.Id, CreatorId = projectCreator.Id, AssigneeId = members[1].MemberId,
                        CurrentStatus = statuses[1].Name, CreatedAt = DateTime.Now
                    },
                    new TicketModel {
                        Title = $"Implement Auth P{i}", Description = "JWT Setup",
                        ProjectId = project.Id, CreatorId = members[1].MemberId, AssigneeId = members[2].MemberId,
                        CurrentStatus = statuses[2].Name, CreatedAt = DateTime.Now
                    },
                    new TicketModel {
                        Title = $"Write Unit Tests P{i}", Description = "Test coverage",
                        ProjectId = project.Id, CreatorId = members[2].MemberId, AssigneeId = members[3].MemberId,
                        CurrentStatus = statuses[3].Name, CreatedAt = DateTime.Now
                    },
                    new TicketModel {
                        Title = $"Design Database P{i}", Description = "Schema design",
                        ProjectId = project.Id, CreatorId = projectCreator.Id, AssigneeId = projectCreator.Id,
                        CurrentStatus = statuses[4].Name, CreatedAt = DateTime.Now, ClosedAt = DateTime.Now, ClosedById = projectCreator.Id
                    }
                };
                    context.Set<TicketModel>().AddRange(tickets);
                    await context.SaveChangesAsync();

                    // 7. Create Dependencies (Tickets Blocking Tickets)
                    // E.g., Ticket 2 is blocked by Ticket 1. Ticket 3 is blocked by Ticket 2.
                    var dependencies = new List<TicketDependency>
                {
                    new TicketDependency { BlockedTicketId = tickets[1].Id, BlockingTicketId = tickets[0].Id },
                    new TicketDependency { BlockedTicketId = tickets[2].Id, BlockingTicketId = tickets[1].Id }
                };
                    context.Set<TicketDependency>().AddRange(dependencies);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
