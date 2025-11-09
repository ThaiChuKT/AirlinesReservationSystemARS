using ARS.Services;
using Microsoft.AspNetCore.Mvc;

namespace ARS.Controllers
{
    public class NotificationController : Controller
    {
        private readonly IEmailService _emailService;

        public NotificationController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // GET: /Notification/Emails
        public IActionResult Emails()
        {
            var emails = _emailService.GetAll()
                .OrderByDescending(e => e.SentAt)
                .ToList();

            return View(emails);
        }
    }
}
