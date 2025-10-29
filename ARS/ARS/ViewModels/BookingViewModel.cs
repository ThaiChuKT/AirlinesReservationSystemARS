using System.ComponentModel.DataAnnotations;

namespace ARS.ViewModels
{
    public class BookingViewModel
    {
        public int FlightID { get; set; }
        public int? ScheduleID { get; set; }
        public string FlightNumber { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateOnly TravelDate { get; set; }
        public decimal BasePrice { get; set; }
        public decimal TotalPrice { get; set; }

        [Required]
        [Range(0, 10)]
        [Display(Name = "Number of Adults")]
        public int NumAdults { get; set; } = 1;

        [Range(0, 10)]
        [Display(Name = "Number of Children")]
        public int NumChildren { get; set; } = 0;

        [Range(0, 10)]
        [Display(Name = "Number of Seniors")]
        public int NumSeniors { get; set; } = 0;

        [Required]
        [Display(Name = "Class")]
        public string Class { get; set; } = "Economy";

        // User information (if not logged in)
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }
    }
}
