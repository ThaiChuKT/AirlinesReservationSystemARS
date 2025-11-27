using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ARS.Data;
using ARS.Models;
using Microsoft.AspNetCore.Identity;

namespace ARS.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public PaymentController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Payment/Index?reservationId=7
        public async Task<IActionResult> Index(int reservationId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Please login to make a payment.";
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Payment?reservationId={reservationId}" });
            }

            var reservation = await _context.Reservations
                .Include(r => r.Flight)
                    .ThenInclude(f => f.OriginCity)
                .Include(r => r.Flight)
                    .ThenInclude(f => f.DestinationCity)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null)
            {
                return NotFound();
            }

            // Allow access if user owns reservation OR user is admin
            var isAdmin = User.IsInRole("Admin");
            if (reservation.UserID != currentUser.Id && !isAdmin)
            {
                return Forbid();
            }

            // For now, redirect back to details with a message
            TempData["InfoMessage"] = "Payment functionality is coming soon. This reservation is confirmed.";
            return RedirectToAction("Details", "Reservation", new { id = reservationId });
        }
    }
}
