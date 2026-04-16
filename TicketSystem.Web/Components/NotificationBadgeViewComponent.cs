using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Web.Models;
using TicketSystem.Web.Models.Account;
using Microsoft.EntityFrameworkCore;

namespace TicketSystem.Web.Components
{
    public class NotificationBadgeViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public NotificationBadgeViewComponent(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = _userManager.GetUserId(HttpContext.User);

            // If user is not logged in, show 0
            if (string.IsNullOrEmpty(userId)) return View(0);

            var unreadCount = await _context.Messages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

            return View(unreadCount);
        }
    }
}
