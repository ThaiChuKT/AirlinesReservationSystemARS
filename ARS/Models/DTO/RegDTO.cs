using System.ComponentModel.DataAnnotations;

namespace ARS.Models.DTO
{
    public class RegDTO
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public char Gender { get; set; }

        [Required]
        [Range(1, 150)]
        public int Age { get; set; }

        [Required]
        public string CreditCardNumber { get; set; } = string.Empty;

        public int SkyMiles { get; set; } = 0;

        public string Role { get; set; } = "User";
    }
}