using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.Account;
using TicketSystem.Web.Models.Project.Proposta;
using TicketSystem.Web.Models.Ticket;

namespace TicketSystem.Web.Controllers
{

    public class UpdateTicketStatusRequest
    {
        public int TicketId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }

    public class PropostaTicketController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public PropostaTicketController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: Ticket/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketCreateViewModel model)
        {
            // Se a validação falhar, redirecionamos de volta para o quadro do projeto com erro
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Preencha os campos obrigatórios do ticket.";
                return RedirectToAction("Details", "PropostaProject", new { id = model.ProjectId });
            }

            var project = await _context.Projects
                                        .Include(p => p.Workflow)
                                        .ThenInclude(w => w.Statuses)
                                        .FirstOrDefaultAsync(p => p.Id == model.ProjectId);

            if (project == null) return NotFound();

            // REGRA DE NEGÓCIO: Bloquear se o projeto estiver encerrado
            if (project.EndDate.HasValue)
            {
                TempData["ErrorMessage"] = "Não é possível criar tickets num projeto que já foi encerrado.";
                return RedirectToAction("Details", "PropostaProject", new { id = model.ProjectId });
            }

            // 1. Pega o usuário logado
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();


            if (project.Workflow == null || !project.Workflow.Statuses.Any())
            {
                TempData["ErrorMessage"] = "Erro: O projeto não possui um Workflow válido configurado.";
                return RedirectToAction("Details", "PropostaProject", new { id = model.ProjectId });
            }

            // Regra de Negócio: O status inicial é o primeiro status cadastrado no Workflow
            var initialStatus = project.Workflow.Statuses.OrderBy(s => s.Id).First().Name;

            // 3. Cria a entidade Ticket
            var ticket = new TicketModel
            {
                Title = model.Title,
                Description = model.Description,
                ProjectId = model.ProjectId,
                CreatorId = currentUser.Id,
                CreatedAt = DateTime.Now, // Data do sistema (Auditoria)
                CurrentStatus = initialStatus,
                AssigneeId = model.AssigneeId,
                AssignedAt = !string.IsNullOrEmpty(model.AssigneeId) ? DateTime.Now : null
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Ticket #{ticket.Id} criado com sucesso!";

            // Redireciona de volta para o Kanban
            return RedirectToAction("Details", "PropostaProject", new { id = model.ProjectId });
        }

        [HttpPost]
        // O [FromBody] permite que a action leia o JSON enviado pelo fetch do JavaScript
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateTicketStatusRequest request)
        {
            if (request == null || request.TicketId <= 0 || string.IsNullOrEmpty(request.NewStatus))
            {
                return BadRequest(new { success = false, message = "Dados inválidos." });
            }

            var ticket = await _context.Tickets.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == request.TicketId);

            if (ticket == null)
            {
                return NotFound(new { success = false, message = "Ticket não encontrado." });
            }

            // BLINDAGEM DE NEGÓCIO: Rejeita se o projeto ou o ticket estiverem encerrados
            if (ticket.Project!.EndDate.HasValue || ticket.ClosedAt.HasValue)
            {
                return Json(new { success = false, message = "Ação não permitida em projetos ou tickets encerrados." });
            }


            // NOVA REGRA DE NEGÓCIO: Só pode mover se estiver atribuído
            // Nota: Se o status que estiver a receber for o primeiro do Workflow, pode querer permitir. 
            // Mas assumindo que qualquer movimento exige um responsável:
            if (string.IsNullOrEmpty(ticket.AssigneeId))
            {
                return Json(new { success = false, message = "O ticket precisa ser atribuído a um utilizador antes de avançar!" });
            }

            // Atualiza o status
            ticket.CurrentStatus = request.NewStatus;

            // Se o status for "Concluído" (ou equivalente no seu workflow), você pode preencher o ClosedAt e ClosedById aqui.
            // if (request.NewStatus.ToLower() == "concluído") { ticket.ClosedAt = DateTime.Now; }

            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();

            // Retorna sucesso em formato JSON para o JavaScript
            return Json(new { success = true, message = "Status atualizado com sucesso!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int TicketId, string AssigneeId, int ProjectId)
        {
            var ticket = await _context.Tickets.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == TicketId);

            if (ticket != null && ticket.Project != null && !ticket.Project.EndDate.HasValue)
            {
                ticket.AssigneeId = AssigneeId;
                ticket.AssignedAt = DateTime.Now;

                _context.Tickets.Update(ticket);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Utilizador atribuído com sucesso.";
            }

            return RedirectToAction("Details", "PropostaProject", new { id = ProjectId });
        }

    }
}
