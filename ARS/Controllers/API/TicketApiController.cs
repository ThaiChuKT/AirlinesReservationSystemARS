using ARS.Data;
using ARS.DTO;
using ARS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TicketApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static string GenBlockingNumber()
            => $"BLK{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

        private static string GenConfirmationNumber()
            => $"ARS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

        // POST: api/Ticket/block
        [HttpPost("block")]
        public async Task<IActionResult> Block([FromBody] BlockTicketDTO dto)
        {
            // Validate FK
            var userOk = await _context.Users.AnyAsync(u => u.UserID == dto.UserID);
            var flightOk = await _context.Flights.AnyAsync(f => f.FlightID == dto.FlightID);
            var scheduleOk = await _context.Schedules.AnyAsync(
                s => s.ScheduleID == dto.ScheduleID && s.FlightID == dto.FlightID);

            if (!userOk || !flightOk || !scheduleOk)
                return BadRequest("UserID/FlightID/ScheduleID không hợp lệ.");

            var reservation = new Reservation
            {
                UserID = dto.UserID,
                FlightID = dto.FlightID,
                ScheduleID = dto.ScheduleID,
                BookingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TravelDate = dto.TravelDate,
                Status = "Blocked",
                NumAdults = dto.NumAdults,
                NumChildren = dto.NumChildren,
                NumSeniors = dto.NumSeniors,
                Class = dto.Class,
                BlockingNumber = GenBlockingNumber(),
                // DB yêu cầu NOT NULL => cấp luôn, sau này vẫn dùng lại khi confirm
                ConfirmationNumber = GenConfirmationNumber()
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtRoute(
                routeName: "GetReservationForTicket",
                routeValues: new { id = reservation.ReservationID },
                value: reservation);
        }

        [HttpGet("reservation/{id:int}", Name = "GetReservationForTicket")]
        public async Task<IActionResult> GetReservationForTicket(int id)
        {
            var res = await _context.Reservations
                .Include(r => r.Flight).ThenInclude(f => f.OriginCity)
                .Include(r => r.Flight).ThenInclude(f => f.DestinationCity)
                .Include(r => r.Schedule)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            return res is null ? NotFound() : Ok(res);
        }

        // POST: api/Ticket/confirm
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmTicketDTO dto)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationID == dto.ReservationID);

            if (reservation is null)
                return NotFound("Reservation không tồn tại.");

            if (reservation.UserID != dto.UserID)
                return Forbid();

            // Chỉ cho confirm từ Blocked/Pending
            if (!reservation.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) &&
                !reservation.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest($"Không thể confirm khi trạng thái hiện tại là '{reservation.Status}'.");
            }

            // Nếu vì lý do gì chưa có confirmationNumber thì cấp luôn
            if (string.IsNullOrWhiteSpace(reservation.ConfirmationNumber))
                reservation.ConfirmationNumber = GenConfirmationNumber();

            reservation.Status = "Confirmed";
            reservation.BlockingNumber = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                reservation.ReservationID,
                reservation.Status,
                reservation.ConfirmationNumber
            });
        }
    }
}
