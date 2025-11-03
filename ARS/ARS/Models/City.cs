using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ARS.Models;

[Table("Cities")]
public class City
{
    [Key]
    public int CityID { get; set; }

    [Required]
    [StringLength(100)]
    public string CityName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string AirportCode { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<Flight> OriginFlights { get; set; } = new List<Flight>();
    public virtual ICollection<Flight> DestinationFlights { get; set; } = new List<Flight>();
}
