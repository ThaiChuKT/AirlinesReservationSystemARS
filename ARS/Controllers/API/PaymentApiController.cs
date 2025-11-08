using ARS.DTO;
using ARS.Data;
using ARS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PaymentApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentCreateDTO dto)
        {
            // bảo đảm Reservation tồn tại
            var exists = await _context.Reservations.AnyAsync(r => r.ReservationID == dto.ReservationID);
            if (!exists) return BadRequest("ReservationID does not exist.");

            var payment = new Payment
            {
                ReservationID = dto.ReservationID,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                TransactionStatus = "Pending",
                PaymentDate = DateTime.UtcNow
                // TransactionRefNo: để null, sẽ điền khi succeeded/failed
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByReservation), new { reservationId = payment.ReservationID }, payment);
        }

        [HttpGet("by-reservation/{reservationId:int}")]
        public async Task<IActionResult> GetByReservation(int reservationId)
        {
            var list = await _context.Payments
                .Where(p => p.ReservationID == reservationId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("{id:int}/succeeded")]
        public async Task<IActionResult> MarkSucceeded(int id, [FromQuery] string? reference = null)
        {
            var pay = await _context.Payments.FindAsync(id);
            if (pay is null) return NotFound();

            pay.TransactionStatus = "Completed";           // hoặc "Succeeded" tùy convention của bạn
            pay.TransactionRefNo = string.IsNullOrWhiteSpace(reference) ? pay.TransactionRefNo : reference;
            // đảm bảo có PaymentDate
            if (pay.PaymentDate == default) pay.PaymentDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/failed")]
        public async Task<IActionResult> MarkFailed(int id, [FromQuery] string? reference = null)
        {
            var pay = await _context.Payments.FindAsync(id);
            if (pay is null) return NotFound();

            pay.TransactionStatus = "Failed";
            if (!string.IsNullOrWhiteSpace(reference))
                pay.TransactionRefNo = reference;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
