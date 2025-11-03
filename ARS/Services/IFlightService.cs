using ARS.Models;
using ARS.Models.DTO;

namespace ARS.Services
{
    public interface IFlightService
    {
        Task<FlightSearchResponseDTO> SearchFlightsAsync(FlightSearchDTO searchDto);
        Task<FlightResultDTO?> GetFlightByIdAsync(int flightId, DateTime departureDate);
        Task<List<CityDTO>> GetAllCitiesAsync();
        Task<decimal> CalculateTotalPriceAsync(int flightId, DateTime departureDate, 
            int numAdults, int numChildren, int numSeniors, string flightClass);
    }

    public class CityDTO
    {
        public int CityID { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string AirportCode { get; set; } = string.Empty;
    }
}