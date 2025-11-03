using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ARS.Models;

[Table("Flights")]
public class Flight
{
    [Key]
    public int FlightID { get; set; }

    [Required]
    [StringLength(20)]
    public string FlightNumber { get; set; } = string.Empty;

    [Required]
    public int OriginCityID { get; set; }

    [Required]
    public int DestinationCityID { get; set; }

    [Required]
    public DateTime DepartureTime { get; set; }

    [Required]
    public DateTime ArrivalTime { get; set; }

    [Required]
    public int Duration { get; set; } // Duration in minutes

    [Required]
    [StringLength(50)]
    public string AircraftType { get; set; } = string.Empty;

    [Required]
    public int TotalSeats { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseFare { get; set; }

    // Navigation properties
    [ForeignKey("OriginCityID")]
    public virtual City? OriginCity { get; set; }

    [ForeignKey("DestinationCityID")]
    public virtual City? DestinationCity { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
