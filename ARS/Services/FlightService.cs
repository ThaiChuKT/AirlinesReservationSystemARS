using Microsoft.EntityFrameworkCore;
using ARS.Data;
using ARS.Models.DTO;

namespace ARS.Services
{
    public class FlightService : IFlightService
    {
        private readonly ApplicationDbContext _context;

        public FlightService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FlightSearchResponseDTO> SearchFlightsAsync(FlightSearchDTO searchDto)
        {
            try
            {
                // Normalize date to midnight for comparison
                var searchDate = DateOnly.FromDateTime(searchDto.DepartureDate);

                var query = from f in _context.Flights
                            join s in _context.Schedules on f.FlightID equals s.FlightID
                            join origin in _context.Cities on f.OriginCityID equals origin.CityID
                            join dest in _context.Cities on f.DestinationCityID equals dest.CityID
                            where f.OriginCityID == searchDto.OriginCityId
                               && f.DestinationCityID == searchDto.DestinationCityId
                               && EF.Functions.DateDiffDay(s.Date, searchDate) == 0
                               && s.Status == "Scheduled"
                            select new { Flight = f, Schedule = s, Origin = origin, Destination = dest };

                var results = await query.ToListAsync();

                if (!results.Any())
                {
                    return new FlightSearchResponseDTO
                    {
                        Success = false,
                        Message = "Không tìm thấy chuyến bay phù hợp",
                        Flights = new List<FlightResultDTO>()
                    };
                }

                var flightResults = new List<FlightResultDTO>();
                int totalPassengers = searchDto.NumAdults + searchDto.NumChildren + searchDto.NumSeniors;

                foreach (var result in results)
                {
                    int availableSeats = result.Flight.TotalSeats;
                    if (availableSeats < totalPassengers)
                        continue;

                    decimal totalPrice = await CalculateTotalPriceAsync(
                        result.Flight.FlightID,
                        searchDto.DepartureDate,
                        searchDto.NumAdults,
                        searchDto.NumChildren,
                        searchDto.NumSeniors,
                        searchDto.Class
                    );

                    var flightDto = new FlightResultDTO
                    {
                        FlightID = result.Flight.FlightID,
                        FlightNumber = result.Flight.FlightNumber,
                        OriginCity = result.Origin.CityName,
                        OriginAirport = result.Origin.AirportCode,
                        DestinationCity = result.Destination.CityName,
                        DestinationAirport = result.Destination.AirportCode,
                        DepartureTime = result.Flight.DepartureTime,
                        ArrivalTime = result.Flight.ArrivalTime,
                        Duration = result.Flight.Duration,
                        AircraftType = result.Flight.AircraftType,
                        BaseFare = result.Flight.BaseFare,
                        TotalPrice = totalPrice,
                        AvailableSeats = availableSeats,
                        ScheduleID = result.Schedule.ScheduleID,
                        DepartureDate = result.Schedule.Date.ToDateTime(TimeOnly.MinValue)
                    };

                    flightResults.Add(flightDto);
                }

                if (!flightResults.Any())
                {
                    return new FlightSearchResponseDTO
                    {
                        Success = false,
                        Message = "Không có chuyến bay nào có đủ ghế trống",
                        Flights = new List<FlightResultDTO>()
                    };
                }

                return new FlightSearchResponseDTO
                {
                    Success = true,
                    Message = $"Tìm thấy {flightResults.Count} chuyến bay",
                    Flights = flightResults,
                    TotalResults = flightResults.Count
                };
            }
            catch (Exception ex)
            {
                return new FlightSearchResponseDTO
                {
                    Success = false,
                    Message = $"Lỗi tìm kiếm: {ex.Message}",
                    Flights = new List<FlightResultDTO>()
                };
            }
        }

        public async Task<FlightResultDTO?> GetFlightByIdAsync(int flightId, DateTime departureDate)
        {
            var searchDate = DateOnly.FromDateTime(departureDate);

            var result = await (from f in _context.Flights
                               join s in _context.Schedules on f.FlightID equals s.FlightID
                               join origin in _context.Cities on f.OriginCityID equals origin.CityID
                               join dest in _context.Cities on f.DestinationCityID equals dest.CityID
                               where f.FlightID == flightId 
                                  && EF.Functions.DateDiffDay(s.Date, searchDate) == 0
                               select new { Flight = f, Schedule = s, Origin = origin, Destination = dest })
                               .FirstOrDefaultAsync();

            if (result == null)
                return null;

            return new FlightResultDTO
            {
                FlightID = result.Flight.FlightID,
                FlightNumber = result.Flight.FlightNumber,
                OriginCity = result.Origin.CityName,
                OriginAirport = result.Origin.AirportCode,
                DestinationCity = result.Destination.CityName,
                DestinationAirport = result.Destination.AirportCode,
                DepartureTime = result.Flight.DepartureTime,
                ArrivalTime = result.Flight.ArrivalTime,
                Duration = result.Flight.Duration,
                AircraftType = result.Flight.AircraftType,
                BaseFare = result.Flight.BaseFare,
                AvailableSeats = result.Flight.TotalSeats,
                ScheduleID = result.Schedule.ScheduleID,
                DepartureDate = result.Schedule.Date.ToDateTime(TimeOnly.MinValue)
            };
        }

        public async Task<List<CityDTO>> GetAllCitiesAsync()
        {
            return await _context.Cities
                .Select(c => new CityDTO
                {
                    CityID = c.CityID,
                    CityName = c.CityName,
                    Country = c.Country,
                    AirportCode = c.AirportCode
                })
                .OrderBy(c => c.CityName)
                .ToListAsync();
        }

        public async Task<decimal> CalculateTotalPriceAsync(int flightId, DateTime departureDate,
            int numAdults, int numChildren, int numSeniors, string flightClass)
        {
            var flight = await _context.Flights.FindAsync(flightId);
            if (flight == null)
                return 0;

            decimal baseFare = flight.BaseFare;

            // Class multiplier
            decimal classMultiplier = flightClass.ToLower() switch
            {
                "economy" => 1.0m,
                "premium economy" => 1.5m,
                "business" => 2.0m,
                "first" => 3.0m,
                _ => 1.0m
            };

            // Calculate per passenger type
            decimal adultPrice = baseFare * classMultiplier * numAdults;
            decimal childPrice = baseFare * classMultiplier * 0.75m * numChildren; // 25% discount
            decimal seniorPrice = baseFare * classMultiplier * 0.9m * numSeniors; // 10% discount

            return adultPrice + childPrice + seniorPrice;
        }
    }
}