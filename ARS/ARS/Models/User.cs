using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ARS.Models;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "char(1)")]
    public char Gender { get; set; }

    [Required]
    public int Age { get; set; }

    [Required]
    public string CreditCardNumber { get; set; } = string.Empty;

    public int SkyMiles { get; set; } = 0;

    [Required]
    public string Role { get; set; } = "User";

    // Navigation properties
    // public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}