using System.ComponentModel.DataAnnotations;

namespace ARS.ViewModels
{
    public class FlightSearchViewModel
    {
        [Required(ErrorMessage = "Please select origin city")]
        [Display(Name = "From")]
        public int OriginCityID { get; set; }

        [Required(ErrorMessage = "Please select destination city")]
        [Display(Name = "To")]
        public int DestinationCityID { get; set; }

        [Required(ErrorMessage = "Please select travel date")]
        [Display(Name = "Travel Date")]
        [DataType(DataType.Date)]
        public DateOnly TravelDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

        [Display(Name = "Number of Passengers")]
        [Range(1, 10, ErrorMessage = "Number of passengers must be between 1 and 10")]
        public int Passengers { get; set; } = 1;

        [Display(Name = "Class")]
        public string Class { get; set; } = "Economy";
    }
}
