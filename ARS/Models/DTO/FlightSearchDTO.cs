using System.ComponentModel.DataAnnotations;

namespace ARS.Models.DTO
{
    public class FlightSearchDTO
    {
        [Required(ErrorMessage = "Origin city is required")]
        public int OriginCityId { get; set; }

        [Required(ErrorMessage = "Destination city is required")]
        public int DestinationCityId { get; set; }

        [Required(ErrorMessage = "Departure date is required")]
        public DateTime DepartureDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        [Range(1, 9, ErrorMessage = "Number of adults must be between 1 and 9")]
        public int NumAdults { get; set; } = 1;

        [Range(0, 9, ErrorMessage = "Number of children must be between 0 and 9")]
        public int NumChildren { get; set; } = 0;

        [Range(0, 9, ErrorMessage = "Number of seniors must be between 0 and 9")]
        public int NumSeniors { get; set; } = 0;

        [Required(ErrorMessage = "Flight class is required")]
        public string Class { get; set; } = "Economy";
    }
}