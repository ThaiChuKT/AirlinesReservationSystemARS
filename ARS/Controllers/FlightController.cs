using Microsoft.AspNetCore.Mvc;
using ARS.Models.DTO;
using ARS.Services;
using ARS.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ARS.Controllers
{
    public class FlightController : Controller
    {
        private readonly IFlightService _flightService;

        public FlightController(IFlightService flightService)
        {
            _flightService = flightService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cities = await _flightService.GetAllCitiesAsync();
            ViewBag.Cities = new SelectList(cities, "CityID", "CityName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FlightSearchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var cities = await _flightService.GetAllCitiesAsync();
                ViewBag.Cities = new SelectList(cities, "CityID", "CityName");
                return View(model);
            }

            // Convert ViewModel to DTO for search
            var searchDto = new FlightSearchDTO
            {
                OriginCityId = model.OriginCityID,
                DestinationCityId = model.DestinationCityID,
                DepartureDate = model.TravelDate.ToDateTime(TimeOnly.MinValue),
                NumAdults = model.Passengers,
                NumChildren = 0,
                NumSeniors = 0,
                Class = model.Class
            };

            var result = await _flightService.SearchFlightsAsync(searchDto);

            // Return to view with results
            ViewBag.SearchResults = result;
            var cities2 = await _flightService.GetAllCitiesAsync();
            ViewBag.Cities = new SelectList(cities2, "CityID", "CityName");
            return View(model);
        }

        [HttpPost]
        [Route("api/Flight/search")]
        public async Task<ActionResult<FlightSearchResponseDTO>> SearchFlights([FromBody] FlightSearchDTO searchDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new FlightSearchResponseDTO
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Flights = new List<FlightResultDTO>()
                });
            }

            var result = await _flightService.SearchFlightsAsync(searchDto);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/Flight/{id}")]
        public async Task<ActionResult<FlightResultDTO>> GetFlightById(int id, [FromQuery] DateTime? departureDate)
        {
            var flight = await _flightService.GetFlightByIdAsync(id, departureDate);
            
            if (flight == null)
            {
                if (departureDate.HasValue)
                    return NotFound(new { message = $"Không tìm thấy chuyến bay với ID {id} vào ngày {departureDate.Value:yyyy-MM-dd}" });
                else
                    return NotFound(new { message = $"Không tìm thấy chuyến bay với ID {id}" });
            }

            return Ok(flight);
        }

        [HttpGet]
        [Route("api/Flight/cities")]
        public async Task<ActionResult<List<CityDTO>>> GetCities()
        {
            var cities = await _flightService.GetAllCitiesAsync();
            return Ok(cities);
        }

        [HttpGet]
        [Route("api/Flight")]
        public async Task<ActionResult<List<FlightResultDTO>>> GetAllFlights()
        {
            var flights = await _flightService.GetAllFlightsAsync();
            
            if (flights == null || !flights.Any())
                return Ok(new { message = "Không có chuyến bay nào", flights = new List<FlightResultDTO>() });

            return Ok(new 
            { 
                message = $"Tìm thấy {flights.Count} chuyến bay",
                totalFlights = flights.Count,
                flights = flights
            });
        }

        [HttpPost]
        [Route("api/Flight/calculate-price")]
        public async Task<ActionResult<decimal>> CalculatePrice([FromBody] PriceCalculationDTO dto)
        {
            var totalPrice = await _flightService.CalculateTotalPriceAsync(
                dto.FlightId,
                dto.DepartureDate,
                dto.NumAdults,
                dto.NumChildren,
                dto.NumSeniors,
                dto.Class
            );

            return Ok(new { totalPrice });
        }
    }

    public class PriceCalculationDTO
    {
        public int FlightId { get; set; }
        public DateTime DepartureDate { get; set; }
        public int NumAdults { get; set; }
        public int NumChildren { get; set; }
        public int NumSeniors { get; set; }
        public string Class { get; set; } = "Economy";
    }
}
