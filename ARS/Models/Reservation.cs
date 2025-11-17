using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ARS.Models
{
    public class Reservation
    {
        [Key]
        public int ReservationID { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserID { get; set; }

        [Required]
        [ForeignKey("Flight")]
        public int FlightID { get; set; }

        [Required]
        [ForeignKey("Schedule")]
        public int ScheduleID { get; set; }

        [Required]
        public DateOnly BookingDate { get; set; }

        [Required]
        public DateOnly TravelDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Confirmed"; // Confirmed, Cancelled, Completed, Rescheduled

        [Required]
        public int NumAdults { get; set; } = 1;

        public int NumChildren { get; set; } = 0;

        public int NumSeniors { get; set; } = 0;

        [Required]
        [StringLength(50)]
        public string Class { get; set; } = "Economy"; // Economy, Business, First

        [Required]
        [StringLength(50)]
        public string ConfirmationNumber { get; set; } = string.Empty;

        [StringLength(50)]
        public string? BlockingNumber { get; set; }

    // Seat assignment (optional)
    public int? SeatId { get; set; }
    public virtual Seat? Seat { get; set; }

    // Legacy label for compatibility (e.g. "12A")
    [StringLength(10)]
    public string? SeatLabel { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Flight? Flight { get; set; }
        public virtual Schedule? Schedule { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    }
}
