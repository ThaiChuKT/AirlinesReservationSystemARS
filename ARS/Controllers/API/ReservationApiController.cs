using ARS.Data;
using ARS.DTO;
using ARS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReservationApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static string GenerateConfirmationNumber()
            => $"ARS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReservationCreateDTO dto)
        {
            // Validate FK cơ bản
            var flightExists   = await _context.Flights.AnyAsync(f => f.FlightID == dto.FlightID);
            var scheduleExists = await _context.Schedules.AnyAsync(s => s.ScheduleID == dto.ScheduleID);
            var userExists     = await _context.Users.AnyAsync(u => u.UserID == dto.UserID);

            if (!flightExists || !scheduleExists || !userExists)
                return BadRequest("Invalid FK: FlightID/ScheduleID/UserID does not exist.");

            var reservation = new Reservation
            {
                UserID = dto.UserID,
                FlightID = dto.FlightID,
                ScheduleID = dto.ScheduleID,
                BookingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TravelDate = dto.TravelDate,
                Status = "Pending",
                NumAdults = dto.NumAdults,
                NumChildren = dto.NumChildren,
                NumSeniors = dto.NumSeniors,
                Class = dto.Class,
                ConfirmationNumber = GenerateConfirmationNumber()
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = reservation.ReservationID }, reservation);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Flight).ThenInclude(f => f.OriginCity)
                .Include(r => r.Flight).ThenInclude(f => f.DestinationCity)
                .Include(r => r.Schedule)
                .Include(r => r.Payments)
                .Include(r => r.Refunds)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            return res is null ? NotFound() : Ok(res);
        }

        [HttpGet("by-user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var list = await _context.Reservations
                .Include(r => r.Flight).ThenInclude(f => f.OriginCity)
                .Include(r => r.Flight).ThenInclude(f => f.DestinationCity)
                .Include(r => r.Schedule)
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.BookingDate)
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res is null) return NotFound();

            res.Status = status;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
