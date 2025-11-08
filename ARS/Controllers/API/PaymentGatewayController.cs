using ARS.Data;
using ARS.DTO;
using ARS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentGatewayController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentGatewayController> _logger;

        public PaymentGatewayController(ApplicationDbContext context, ILogger<PaymentGatewayController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private static string GenRef() =>
            $"PG{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

        /// <summary>
        /// Khởi tạo thanh toán: tạo Payment (Pending) và trả về URL giả lập gateway.
        /// </summary>
        [HttpPost("initiate")]
        public async Task<IActionResult> Initiate([FromBody] PaymentGatewayRequestDTO dto)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationID == dto.ReservationID);

            if (reservation is null)
                return NotFound("Reservation không tồn tại.");

            if (dto.Amount <= 0)
                return BadRequest("Amount phải > 0.");

            // Tạo bản ghi Payment trạng thái Pending
            var payment = new Payment
            {
                ReservationID = dto.ReservationID,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                TransactionStatus = "Pending",
                PaymentDate = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Tạo URL callback giả lập (success & fail) để test trên Swagger
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var successUrl = $"{baseUrl}/api/PaymentGateway/mock-callback?paymentId={payment.PaymentID}&result=success";
            var failUrl    = $"{baseUrl}/api/PaymentGateway/mock-callback?paymentId={payment.PaymentID}&result=fail";

            return Ok(new
            {
                payment.PaymentID,
                reservation.ReservationID,
                payment.TransactionStatus,
                successUrl,
                failUrl
            });
        }

        /// <summary>
        /// Endpoint mô phỏng callback từ Payment Gateway (Success / Fail).
        /// </summary>
        [HttpGet("mock-callback")]
        public async Task<IActionResult> MockCallback([FromQuery] int paymentId, [FromQuery] string result)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

            if (payment is null)
                return NotFound("Payment không tồn tại.");

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationID == payment.ReservationID);

            if (reservation is null)
                return NotFound("Reservation không tồn tại.");

            if (payment.TransactionStatus == "Completed" || payment.TransactionStatus == "Failed")
            {
                return BadRequest("Giao dịch này đã được xử lý trước đó.");
            }

            result = result?.ToLowerInvariant() ?? "";

            if (result == "success")
            {
                payment.TransactionStatus = "Completed";
                payment.TransactionRefNo = GenRef();
                payment.PaymentDate = DateTime.UtcNow;

                // Logic đơn giản:
                // Nếu reservation đang Blocked hoặc Pending => chuyển sang Confirmed khi thanh toán thành công.
                if (reservation.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) ||
                    reservation.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    reservation.Status = "Confirmed";
                    if (string.IsNullOrWhiteSpace(reservation.ConfirmationNumber))
                    {
                        reservation.ConfirmationNumber = $"ARS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
                    }
                    reservation.BlockingNumber = null;
                }
            }
            else if (result == "fail")
            {
                payment.TransactionStatus = "Failed";
                payment.TransactionRefNo = GenRef();

                // Có thể giữ nguyên trạng thái reservation (Blocked / Pending).
                // Tuỳ yêu cầu dự án, có thể auto-cancel.
            }
            else
            {
                return BadRequest("result phải là 'success' hoặc 'fail'.");
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                payment.PaymentID,
                payment.TransactionStatus,
                payment.TransactionRefNo,
                ReservationID = reservation.ReservationID,
                ReservationStatus = reservation.Status
            });
        }
    }
}
