using System.ComponentModel.DataAnnotations;

namespace ARS.Models.DTO;

public class FlightSeachDTO {
    [Required]
    public int OriginCityId { get; set; }

    [Required]
    public int DestinationCityId { get; set; }

    [Required]
    public DateTime DepartureDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    [Range(1, 9)]
    public int NumAdults { get; set; } = 1;

    [Range(0, 9)]
    public int NumChildren { get; set; } = 0;

    [Range(0, 9)]
    public int NumSeniors { get; set; } = 0;

    public string? Class { get; set; } = "Economy";
}