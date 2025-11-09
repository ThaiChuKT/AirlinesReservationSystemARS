using ARS.Data;
using ARS.DTO;
using ARS.Models;
using ARS.Services;
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
        private readonly IEmailService _emailService;

        public PaymentGatewayController(
            ApplicationDbContext context,
            ILogger<PaymentGatewayController> logger,
            IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        private static string GenRef() =>
            $"PG{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

        [HttpPost("initiate")]
        public async Task<IActionResult> Initiate([FromBody] PaymentGatewayRequestDTO dto)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Flight)
                .FirstOrDefaultAsync(r => r.ReservationID == dto.ReservationID);

            if (reservation is null)
                return NotFound("Reservation không tồn tại.");

            if (dto.Amount <= 0)
                return BadRequest("Amount phải > 0.");

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

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/api/PaymentGateway/mock-callback?paymentId={payment.PaymentID}&result=success";
            var failUrl = $"{baseUrl}/api/PaymentGateway/mock-callback?paymentId={payment.PaymentID}&result=fail";

            return Ok(new
            {
                payment.PaymentID,
                reservation.ReservationID,
                payment.TransactionStatus,
                successUrl,
                failUrl
            });
        }

        [HttpGet("mock-callback")]
        public async Task<IActionResult> MockCallback([FromQuery] int paymentId, [FromQuery] string result)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

            if (payment is null)
                return NotFound("Payment không tồn tại.");

            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Flight).ThenInclude(f => f.OriginCity)
                .Include(r => r.Flight).ThenInclude(f => f.DestinationCity)
                .FirstOrDefaultAsync(r => r.ReservationID == payment.ReservationID);

            if (reservation is null)
                return NotFound("Reservation không tồn tại.");

            if (payment.TransactionStatus is "Completed" or "Failed")
            {
                return BadRequest("Giao dịch này đã được xử lý trước đó.");
            }

            result = result?.ToLowerInvariant() ?? "";

            if (result == "success")
            {
                payment.TransactionStatus = "Completed";
                payment.TransactionRefNo = GenRef();
                payment.PaymentDate = DateTime.UtcNow;

                if (reservation.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) ||
                    reservation.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    reservation.Status = "Confirmed";

                    if (string.IsNullOrWhiteSpace(reservation.ConfirmationNumber))
                    {
                        reservation.ConfirmationNumber =
                            $"ARS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
                    }

                    reservation.BlockingNumber = null;
                }

                // Gửi email mock xác nhận
                if (!string.IsNullOrWhiteSpace(reservation.User?.Email))
                {
                    var subject = $"[ARS] Booking Confirmed - {reservation.ConfirmationNumber}";
                    var body =
                        $"Xin chào {reservation.User.FirstName} {reservation.User.LastName},\n\n" +
                        $"Thanh toán cho đặt chỗ #{reservation.ReservationID} đã THÀNH CÔNG.\n" +
                        $"Mã xác nhận: {reservation.ConfirmationNumber}\n" +
                        $"Chặng bay: {reservation.Flight?.OriginCity?.CityName} -> {reservation.Flight?.DestinationCity?.CityName}\n" +
                        $"Giờ khởi hành: {reservation.Flight?.DepartureTime}\n" +
                        $"Mã giao dịch: {payment.TransactionRefNo}\n" +
                        $"Trạng thái vé: {reservation.Status}\n\n" +
                        $"(Đây là email mô phỏng phục vụ demo ARS.)";

                    await _emailService.SendAsync(reservation.User.Email, subject, body);
                }
            }
            else if (result == "fail")
            {
                payment.TransactionStatus = "Failed";
                payment.TransactionRefNo = GenRef();
                payment.PaymentDate = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(reservation.User?.Email))
                {
                    var subject = "[ARS] Thanh toán thất bại";
                    var body =
                        $"Xin chào {reservation.User.FirstName} {reservation.User.LastName},\n\n" +
                        $"Thanh toán cho đặt chỗ #{reservation.ReservationID} đã THẤT BẠI.\n" +
                        $"Vui lòng thử lại hoặc dùng phương thức thanh toán khác.\n\n" +
                        $"(Đây là email mô phỏng phục vụ demo ARS.)";

                    await _emailService.SendAsync(reservation.User.Email, subject, body);
                }
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
