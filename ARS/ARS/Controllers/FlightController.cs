using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ARS.Data;
using ARS.Models;
using ARS.Models.DTO;

namespace ARS.Controllers;

public class FlightController : Controller
{
    private readonly ApplicationDbContext _context;

    public FlightController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Hiển thị trang tìm kiếm chuyến bay
    /// </summary>
    [HttpGet]
    public IActionResult Search()
    {
        return View();
    }

    /// <summary>
    /// Tìm kiếm chuyến bay theo Origin, Destination và Date
    /// </summary>
    /// <param name="searchDto">Thông tin tìm kiếm</param>
    /// <returns>Danh sách chuyến bay phù hợp</returns>
    [HttpPost("api/Flight/search")]
    public async Task<ActionResult<FlightSearchResponseDTO>> SearchFlights([FromBody] FlightSeachDTO searchDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new FlightSearchResponseDTO
                {
                    Success = false,
                    Message = "Dữ liệu tìm kiếm không hợp lệ",
                    Flights = new List<FligthResultDTO>(),
                    TotalFlights = 0
                });
            }

            // Truy vấn flights theo origin và destination
            var query = _context.Flights
                .Include(f => f.OriginCity)
                .Include(f => f.DestinationCity)
                .Include(f => f.Schedules)
                .Where(f => f.OriginCityID == searchDto.OriginCityId 
                         && f.DestinationCityID == searchDto.DestinationCityId);

            var flights = await query.ToListAsync();

            // Lọc flights có schedule phù hợp với ngày tìm kiếm
            var flightResults = new List<FligthResultDTO>();

            foreach (var flight in flights)
            {
                // Lấy schedule cho ngày được chọn
                var schedules = flight.Schedules
                    .Where(s => s.Date.Date == searchDto.DepartureDate.Date 
                             && s.Status == "Scheduled")
                    .ToList();

                if (schedules.Any())
                {
                    // Tính số ghế đã đặt cho mỗi schedule
                    var totalPassengers = searchDto.NumAdults + searchDto.NumChildren + searchDto.NumSeniors;
                    var availableSeats = flight.TotalSeats;

                    // TODO: Truy vấn số ghế đã đặt từ Reservations khi có table Reservation
                    // var bookedSeats = await _context.Reservations
                    //     .Where(r => schedules.Select(s => s.ScheduleID).Contains(r.ScheduleID))
                    //     .SumAsync(r => r.NumAdults + r.NumChildren + r.NumSeniors);
                    // availableSeats = flight.TotalSeats - bookedSeats;

                    // Kiểm tra còn đủ ghế không
                    if (availableSeats >= totalPassengers)
                    {
                        // Tính giá dựa trên class và số hành khách
                        decimal totalPrice = CalculateTotalPrice(
                            flight.BaseFare,
                            searchDto.NumAdults,
                            searchDto.NumChildren,
                            searchDto.NumSeniors,
                            searchDto.Class
                        );

                        var flightResult = new FligthResultDTO
                        {
                            FlightId = flight.FlightID,
                            FlightNumber = flight.FlightNumber,
                            OriginCityId = flight.OriginCityID,
                            OriginCityName = flight.OriginCity?.CityName ?? "",
                            OriginCountry = flight.OriginCity?.Country ?? "",
                            OriginAirportCode = flight.OriginCity?.AirportCode ?? "",
                            DestinationCityId = flight.DestinationCityID,
                            DestinationCityName = flight.DestinationCity?.CityName ?? "",
                            DestinationCountry = flight.DestinationCity?.Country ?? "",
                            DestinationAirportCode = flight.DestinationCity?.AirportCode ?? "",
                            DepartureTime = flight.DepartureTime,
                            ArrivalTime = flight.ArrivalTime,
                            Duration = flight.Duration,
                            AircraftType = flight.AircraftType,
                            TotalSeats = flight.TotalSeats,
                            AvailableSeats = availableSeats,
                            BaseFare = flight.BaseFare,
                            TotalPrice = totalPrice,
                            Schedules = schedules.Select(s => new ScheduleDTO
                            {
                                ScheduleId = s.ScheduleID,
                                FlightId = s.FlightID,
                                Date = s.Date,
                                Status = s.Status
                            }).ToList()
                        };

                        flightResults.Add(flightResult);
                    }
                }
            }

            // Sắp xếp theo giá hoặc thời gian khởi hành
            flightResults = flightResults.OrderBy(f => f.DepartureTime).ToList();

            return Ok(new FlightSearchResponseDTO
            {
                Success = true,
                Message = flightResults.Any() 
                    ? $"Tìm thấy {flightResults.Count} chuyến bay phù hợp" 
                    : "Không tìm thấy chuyến bay phù hợp",
                Flights = flightResults,
                TotalFlights = flightResults.Count,
                SearchCriteria = searchDto
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new FlightSearchResponseDTO
            {
                Success = false,
                Message = $"Lỗi khi tìm kiếm chuyến bay: {ex.Message}",
                Flights = new List<FligthResultDTO>(),
                TotalFlights = 0
            });
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết một chuyến bay
    /// </summary>
    [HttpGet("api/Flight/{id}")]
    public async Task<ActionResult<FligthResultDTO>> GetFlightById(int id)
    {
        try
        {
            var flight = await _context.Flights
                .Include(f => f.OriginCity)
                .Include(f => f.DestinationCity)
                .Include(f => f.Schedules)
                .FirstOrDefaultAsync(f => f.FlightID == id);

            if (flight == null)
            {
                return NotFound(new { message = "Không tìm thấy chuyến bay" });
            }

            var flightResult = new FligthResultDTO
            {
                FlightId = flight.FlightID,
                FlightNumber = flight.FlightNumber,
                OriginCityId = flight.OriginCityID,
                OriginCityName = flight.OriginCity?.CityName ?? "",
                OriginCountry = flight.OriginCity?.Country ?? "",
                OriginAirportCode = flight.OriginCity?.AirportCode ?? "",
                DestinationCityId = flight.DestinationCityID,
                DestinationCityName = flight.DestinationCity?.CityName ?? "",
                DestinationCountry = flight.DestinationCity?.Country ?? "",
                DestinationAirportCode = flight.DestinationCity?.AirportCode ?? "",
                DepartureTime = flight.DepartureTime,
                ArrivalTime = flight.ArrivalTime,
                Duration = flight.Duration,
                AircraftType = flight.AircraftType,
                TotalSeats = flight.TotalSeats,
                AvailableSeats = flight.TotalSeats,
                BaseFare = flight.BaseFare,
                TotalPrice = flight.BaseFare,
                Schedules = flight.Schedules.Select(s => new ScheduleDTO
                {
                    ScheduleId = s.ScheduleID,
                    FlightId = s.FlightID,
                    Date = s.Date,
                    Status = s.Status
                }).ToList()
            };

            return Ok(flightResult);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi khi lấy thông tin chuyến bay: {ex.Message}" });
        }
    }

    /// <summary>
    /// Lấy danh sách tất cả các thành phố
    /// </summary>
    [HttpGet("api/Flight/cities")]
    public async Task<ActionResult<List<City>>> GetAllCities()
    {
        try
        {
            var cities = await _context.Cities.ToListAsync();
            return Ok(cities);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi khi lấy danh sách thành phố: {ex.Message}" });
        }
    }

    /// <summary>
    /// Tính tổng giá vé dựa trên số hành khách và hạng vé
    /// </summary>
    private decimal CalculateTotalPrice(
        decimal baseFare,
        int numAdults,
        int numChildren,
        int numSeniors,
        string? flightClass)
    {
        decimal classMultiplier = flightClass?.ToLower() switch
        {
            "business" => 2.0m,
            "first" => 3.0m,
            "premium economy" => 1.5m,
            _ => 1.0m // Economy
        };

        // Trẻ em giảm 25%, người cao tuổi giảm 10%
        decimal adultPrice = baseFare * classMultiplier * numAdults;
        decimal childPrice = baseFare * classMultiplier * 0.75m * numChildren;
        decimal seniorPrice = baseFare * classMultiplier * 0.9m * numSeniors;

        return adultPrice + childPrice + seniorPrice;
    }
}
