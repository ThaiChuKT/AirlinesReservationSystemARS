using System.ComponentModel.DataAnnotations;

namespace ARS.Models.DTO;

public class ScheduleDTO{
    public int ScheduleId {get; set;}
    public int FlightId {get; set;}
    public DateTime Date {get; set;}
    public string Status {get; set;} = string.Empty;
}