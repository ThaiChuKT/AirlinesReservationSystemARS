using System.ComponentModel.DataAnnotations;

namespace ARS.Models.DTO;

public class FlightSearchResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<FligthResultDTO> Flights { get; set; } = new List<FligthResultDTO>();
    public int TotalFlights { get; set; }
    public FlightSeachDTO? SearchCriteria { get; set; }
}
