using System.ComponentModel.DataAnnotations;

namespace ARS.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Required]
        [StringLength(1)]
        public char Gender { get; set; } // M, F, O

        public int? Age { get; set; }

        [StringLength(20)]
        public string? CreditCardNumber { get; set; }

        public int SkyMiles { get; set; } = 0;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "Customer"; // Customer, Admin

        // Navigation properties
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
