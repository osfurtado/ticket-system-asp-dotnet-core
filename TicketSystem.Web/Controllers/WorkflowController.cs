using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.Workflow;

namespace TicketSystem.Web.Controllers
{
    public class WorkflowController : Controller
    {
        private readonly AppDbContext _context;

        public WorkflowController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Workflow
        public async Task<IActionResult> Index()
        {
            return View(await _context.Workflows.ToListAsync());
        }

        // GET: Workflow/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workflowModel = await _context.Workflows
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workflowModel == null)
            {
                return NotFound();
            }

            return View(workflowModel);
        }

        // GET: Workflow/Create
        public IActionResult Create()
        {
            var viewModel = new WorkflowCreateViewModel();
            viewModel.Statuses.Add(new WorkflowStatusViewModel());
            return View(viewModel);
        }

        // POST: Workflow/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create( WorkflowCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            if (viewModel.Statuses != null && viewModel.Statuses.Any())
            {
                foreach (var status in viewModel.Statuses)
                {
                    status.IsInicial = false;
                    status.IsFinal = false;
                }

                viewModel.Statuses.First().IsInicial = true;
                viewModel.Statuses.Last().IsFinal = true;
            }

            var novoWorkflow = new WorkflowModel
            {
                Name = viewModel.Name,
                // Converte a lista de StatusViewModel para a entidade WorkflowStatus
                Statuses = viewModel.Statuses?.Select(s => new WorkflowStatus
                {
                    Name = s.Name,
                    IsInicial = s.IsInicial,
                    IsFinal = s.IsFinal
                    // O EF Core vai preencher o Id e o WorkflowId automaticamente!
                }).ToList() ?? new List<WorkflowStatus>()
            };

            try
            {
                _context.Workflows.Add(novoWorkflow);
                await _context.SaveChangesAsync();

                // Mensagem de sucesso opcional para mostrar na próxima tela
                TempData["SuccessMessage"] = "Workflow criado com sucesso!";

                return RedirectToAction(nameof(Index)); // Redireciona para a listagem
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fail to save Workflow on the database", ex.Message);
                // Em caso de erro no banco, logar o erro e devolver a view
                ModelState.AddModelError("", "Failed to save the Workflow in the Database");
                return View(viewModel);
            }
        }

        // GET: Workflow/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workflowModel = await _context.Workflows.FindAsync(id);
            if (workflowModel == null)
            {
                return NotFound();
            }
            return View(workflowModel);
        }

        // POST: Workflow/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] WorkflowModel workflowModel)
        {
            if (id != workflowModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(workflowModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkflowModelExists(workflowModel.Id))
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
            return View(workflowModel);
        }

        // GET: Workflow/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workflowModel = await _context.Workflows
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workflowModel == null)
            {
                return NotFound();
            }

            return View(workflowModel);
        }

        // POST: Workflow/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var workflowModel = await _context.Workflows.FindAsync(id);
            if (workflowModel != null)
            {
                _context.Workflows.Remove(workflowModel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WorkflowModelExists(int id)
        {
            return _context.Workflows.Any(e => e.Id == id);
        }
    }
}
