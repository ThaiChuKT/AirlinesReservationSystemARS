using System.ComponentModel.DataAnnotations;

namespace ARS.Models.DTO;

public class FlightSearchResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<FlightResultDTO> Flights { get; set; } = new List<FlightResultDTO>();
    public int TotalFlights { get; set; }
    public FlightSearchDTO? SearchCriteria { get; set; }
}