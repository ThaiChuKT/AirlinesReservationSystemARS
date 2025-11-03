using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ARS.Models;

[Table("Schedules")]
public class Schedule
{
    [Key]
    public int ScheduleID { get; set; }

    [Required]
    public int FlightID { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Scheduled"; // Scheduled, Delayed, Cancelled, Completed

    // Navigation properties
    [ForeignKey("FlightID")]
    public virtual Flight? Flight { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
