using System.ComponentModel.DataAnnotations;

namespace ARS.Models;

public class Reservation
{
    [Key]
    public int ReservationId { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Add other reservation properties as needed
}