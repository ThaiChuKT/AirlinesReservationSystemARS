using ARS.Data;
using ARS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARS.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ApplicationDbContext context, ILogger<PaymentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Payment/Create?reservationId=...
        // Hiển thị form thanh toán cho một reservation cụ thể (nếu bạn làm View)
        [HttpGet]
        public async Task<IActionResult> Create(int reservationId)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Flight).ThenInclude(f => f.OriginCity)
                .Include(r => r.Flight).ThenInclude(f => f.DestinationCity)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null)
            {
                return NotFound();
            }

            // View này bạn có thể tạo sau (hiện tại controller không ảnh hưởng API nào)
            return View(reservation);
        }

        // POST: /Payment/Create
        // Thanh toán trực tiếp (không qua gateway mock), dùng cho demo đơn giản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int reservationId, decimal amount, string paymentMethod)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null)
            {
                return NotFound();
            }

            if (amount <= 0)
            {
                ModelState.AddModelError(nameof(amount), "Số tiền phải lớn hơn 0.");
                // nếu sau này bạn có View mạnh hơn, có thể truyền model riêng
                return View(reservation);
            }

            // Tạo payment hoàn thành luôn (manual, không dùng gateway)
            var payment = new Payment
            {
                ReservationID = reservationId,
                Amount = amount,
                PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Manual" : paymentMethod,
                PaymentDate = DateTime.UtcNow,
                TransactionStatus = "Completed",
                TransactionRefNo = $"MANUAL{DateTime.UtcNow:yyyyMMddHHmmss}"
            };

            _context.Payments.Add(payment);

            // Nếu reservation chưa Confirmed thì confirm luôn khi thanh toán thành công
            if (!reservation.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                reservation.Status = "Confirmed";

                if (string.IsNullOrWhiteSpace(reservation.ConfirmationNumber))
                {
                    reservation.ConfirmationNumber =
                        $"ARS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
                }

                reservation.BlockingNumber = null;
            }

            await _context.SaveChangesAsync();

            // Sau khi thanh toán xong, chuyển đến trang xác nhận
            return RedirectToAction(nameof(Confirmation), new { id = payment.PaymentID });
        }

        // GET: /Payment/Confirmation/{id}
        // Hiển thị thông tin thanh toán & vé
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Reservation)
                    .ThenInclude(r => r.Flight)
                .Include(p => p.Reservation)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.PaymentID == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }
    }
}
