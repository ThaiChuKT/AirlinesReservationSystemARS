namespace ARS.Models.DTO
{
    public class CityDTO
    {
        public int CityID { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string AirportCode { get; set; } = string.Empty;
    }
}