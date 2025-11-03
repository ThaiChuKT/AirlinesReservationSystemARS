using System.ComponentModel.DataAnnotations;

namespace ARS.Models.DTO;

public class FlightResultDTO
{
    public int FlightID { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    
    // Origin information
    public string OriginCity { get; set; } = string.Empty;
    public string OriginAirport { get; set; } = string.Empty;
    
    // Destination information
    public string DestinationCity { get; set; } = string.Empty;
    public string DestinationAirport { get; set; } = string.Empty;
    
    // Time information
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int Duration { get; set; } // in minutes
    
    // Flight details
    public string AircraftType { get; set; } = string.Empty;
    public decimal BaseFare { get; set; }
    public decimal TotalPrice { get; set; }
    public int AvailableSeats { get; set; }
    
    // Schedule information
    public int ScheduleID { get; set; }
    public DateTime DepartureDate { get; set; }
}