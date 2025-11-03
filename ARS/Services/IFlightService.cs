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
}