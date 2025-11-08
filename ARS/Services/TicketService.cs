using System.Reflection;
using ARS.Data;
using ARS.Models;
using Microsoft.EntityFrameworkCore;

namespace ARS.Services
{
    public class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;

        public TicketService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool ok, string? error)> BlockAsync(int reservationId)
        {
            var res = await _context.Reservations
                .Include(r => r.Flight)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (res is null) return (false, "Reservation not found");

            if (res.Status == "Confirmed")
                return (false, "Reservation already confirmed");

            res.Status = "Blocked";
            res.BlockingNumber ??= GenerateBlockingNumber();

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool ok, string? error)> ConfirmAsync(int reservationId)
        {
            var res = await _context.Reservations
                .Include(r => r.Flight)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (res is null) return (false, "Reservation not found");

            if (res.Status == "Confirmed")
                return (true, null); // idempotent

            // Tính ghế đang giữ bởi các vé đã Confirm cho cùng chuyến & ngày
            int alreadyTaken = await _context.Reservations
                .Where(r => r.FlightID == res.FlightID
                         && r.TravelDate == res.TravelDate
                         && r.Status == "Confirmed")
                .Select(r => r.NumAdults + r.NumChildren + r.NumSeniors)
                .SumAsync();

            int needSeats = res.NumAdults + res.NumChildren + res.NumSeniors;

            // Lấy capacity động từ Flight (ưu tiên thuộc tính có sẵn). Fallback = 180.
            int capacity = GetCapacity(res.Flight) ?? 180;

            if (alreadyTaken + needSeats > capacity)
                return (false, $"Not enough seats. capacity={capacity}, taken={alreadyTaken}, need={needSeats}");

            res.Status = "Confirmed";
            await _context.SaveChangesAsync();
            return (true, null);
        }

        private static string GenerateBlockingNumber()
            => $"BLK{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

        /// <summary>
        /// Tìm capacity theo tên thuộc tính phổ biến để không cần đổi model/bảng hiện có.
        /// Ưu tiên: Capacity -> SeatCapacity -> TotalSeats -> Seats
        /// </summary>
        private static int? GetCapacity(Flight? flight)
        {
            if (flight is null) return null;

            static int? TryProp(object obj, string name)
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (p is null) return null;
                var v = p.GetValue(obj);
                return v is int i ? i : v as int?;
            }

            return TryProp(flight, "Capacity")
                ?? TryProp(flight, "SeatCapacity")
                ?? TryProp(flight, "TotalSeats")
                ?? TryProp(flight, "Seats");
        }
    }
}
