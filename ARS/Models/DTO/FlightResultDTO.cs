using System.ComponentModel.DataAnnotations;

namespace ARS.Models.DTO;

public class FlightResultDTO{
     public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public int OriginCityId { get; set; }
    public string OriginCityName { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = string.Empty;
    public string OriginAirportCode { get; set; } = string.Empty;
    public int DestinationCityId { get; set; }
    public string DestinationCityName { get; set; } = string.Empty;
    public string DestinationCountry { get; set; } = string.Empty;
    public string DestinationAirportCode { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int Duration { get; set; }
    public string AircraftType { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal BaseFare { get; set; }
    public decimal TotalPrice { get; set; }
    public List<ScheduleDTO> Schedules { get; set; } = new List<ScheduleDTO>();
}