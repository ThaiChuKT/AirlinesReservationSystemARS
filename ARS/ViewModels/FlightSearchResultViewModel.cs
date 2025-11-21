using ARS.Models;

namespace ARS.ViewModels
{
    public class FlightSearchResultViewModel
    {
        public FlightSearchViewModel SearchCriteria { get; set; } = new FlightSearchViewModel();
        // For One-way searches this contains matching flights
        public List<FlightResultItem> Flights { get; set; } = new List<FlightResultItem>();

        // For round-trip
        public List<FlightResultItem> OutboundFlights { get; set; } = new List<FlightResultItem>();
        public List<FlightResultItem> ReturnFlights { get; set; } = new List<FlightResultItem>();

        // For multi-city: a list of result lists, one per leg
        public List<List<FlightResultItem>> LegsResults { get; set; } = new List<List<FlightResultItem>>();
    }

    public class FlightResultItem
    {
        public int FlightID { get; set; }
        public string FlightNumber { get; set; } = string.Empty;
        public string OriginCity { get; set; } = string.Empty;
        public string OriginAirportCode { get; set; } = string.Empty;
        public string DestinationCity { get; set; } = string.Empty;
        public string DestinationAirportCode { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int Duration { get; set; }
        public string AircraftType { get; set; } = string.Empty;
        public int AvailableSeats { get; set; }
        public decimal BasePrice { get; set; }
        public decimal FinalPrice { get; set; }
        public int? ScheduleID { get; set; }
    }
}
