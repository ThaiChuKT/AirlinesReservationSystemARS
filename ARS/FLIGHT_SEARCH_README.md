# Chức năng Tìm kiếm Chuyến bay (Flight Search)

## Tổng quan
Chức năng tìm kiếm chuyến bay cho phép người dùng tìm kiếm các chuyến bay dựa trên:
- Điểm khởi hành (Origin City)
- Điểm đến (Destination City)
- Ngày khởi hành (Departure Date)
- Số lượng hành khách (Người lớn, Trẻ em, Người cao tuổi)
- Hạng vé (Economy, Premium Economy, Business, First Class)

## Cấu trúc

### 1. Models

#### City.cs
```
- CityID: int (Primary Key)
- CityName: string
- Country: string
- AirportCode: string
```

#### Flight.cs
```
- FlightID: int (Primary Key)
- FlightNumber: string
- OriginCityID: int (Foreign Key -> Cities)
- DestinationCityID: int (Foreign Key -> Cities)
- DepartureTime: DateTime
- ArrivalTime: DateTime
- Duration: int (phút)
- AircraftType: string
- TotalSeats: int
- BaseFare: decimal
```

#### Schedule.cs
```
- ScheduleID: int (Primary Key)
- FlightID: int (Foreign Key -> Flights)
- Date: DateTime
- Status: string (Scheduled, Delayed, Cancelled, Completed)
```

### 2. DTOs

#### FlightSeachDTO.cs (Request)
```
- OriginCityId: int
- DestinationCityId: int
- DepartureDate: DateTime
- ReturnDate: DateTime? (optional)
- NumAdults: int (1-9)
- NumChildren: int (0-9)
- NumSeniors: int (0-9)
- Class: string (Economy, Premium Economy, Business, First)
```

#### FligthResultDTO.cs (Response Item)
```
- FlightId: int
- FlightNumber: string
- Origin/Destination Info (City names, airport codes, countries)
- DepartureTime/ArrivalTime: DateTime
- Duration: int
- AircraftType: string
- TotalSeats/AvailableSeats: int
- BaseFare/TotalPrice: decimal
- Schedules: List<ScheduleDTO>
```

#### FlightSearchResponseDTO.cs (Response Wrapper)
```
- Success: bool
- Message: string
- Flights: List<FligthResultDTO>
- TotalFlights: int
- SearchCriteria: FlightSeachDTO
```

### 3. Controller

#### FlightController.cs

**MVC Endpoints:**
- `GET /Flight/Search` - Hiển thị trang tìm kiếm

**API Endpoints:**
- `POST /api/Flight/search` - Tìm kiếm chuyến bay
- `GET /api/Flight/{id}` - Lấy thông tin chi tiết chuyến bay
- `GET /api/Flight/cities` - Lấy danh sách tất cả thành phố

### 4. View

#### Search.cshtml
- Form tìm kiếm với các trường nhập liệu
- Hiển thị kết quả tìm kiếm động với AJAX
- Tính năng chọn chuyến bay

## Logic tính giá

### Class Multiplier:
- Economy: 1.0x
- Premium Economy: 1.5x
- Business: 2.0x
- First Class: 3.0x

### Passenger Multiplier:
- Người lớn: 100% giá vé
- Trẻ em: 75% giá vé
- Người cao tuổi: 90% giá vé

**Công thức:**
```
TotalPrice = (BaseFare × ClassMultiplier) × 
             (NumAdults + NumChildren × 0.75 + NumSeniors × 0.9)
```

## Cài đặt và Sử dụng

### 1. Tạo Database và Tables

Chạy script SQL trong file `Data/SeedData.sql`:

```bash
# Kết nối MySQL và chạy script
mysql -u your_username -p your_database < Data/SeedData.sql
```

Hoặc sử dụng EF Core Migrations:

```bash
# Add migration
dotnet ef migrations add AddFlightSearchTables

# Update database
dotnet ef database update
```

### 2. Cấu hình Connection String

Trong `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ARS;User=root;Password=yourpassword;"
  }
}
```

### 3. Chạy ứng dụng

```bash
dotnet run
```

### 4. Truy cập

- Trang tìm kiếm: `https://localhost:5001/Flight/Search`
- API Search: `POST https://localhost:5001/api/Flight/search`

## API Usage Examples

### Tìm kiếm chuyến bay

**Request:**
```bash
POST /api/Flight/search
Content-Type: application/json

{
  "originCityId": 1,
  "destinationCityId": 2,
  "departureDate": "2025-11-04",
  "numAdults": 2,
  "numChildren": 1,
  "numSeniors": 0,
  "class": "Economy"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Tìm thấy 4 chuyến bay phù hợp",
  "flights": [
    {
      "flightId": 1,
      "flightNumber": "VN101",
      "originCityName": "Hà Nội",
      "originAirportCode": "HAN",
      "destinationCityName": "Hồ Chí Minh",
      "destinationAirportCode": "SGN",
      "departureTime": "2025-11-04T06:00:00",
      "arrivalTime": "2025-11-04T08:15:00",
      "duration": 135,
      "aircraftType": "Airbus A321",
      "availableSeats": 180,
      "baseFare": 1500000,
      "totalPrice": 3750000,
      "schedules": [...]
    }
  ],
  "totalFlights": 4,
  "searchCriteria": {...}
}
```

### Lấy danh sách thành phố

**Request:**
```bash
GET /api/Flight/cities
```

**Response:**
```json
[
  {
    "cityID": 1,
    "cityName": "Hà Nội",
    "country": "Việt Nam",
    "airportCode": "HAN"
  },
  {
    "cityID": 2,
    "cityName": "Hồ Chí Minh",
    "country": "Việt Nam",
    "airportCode": "SGN"
  }
]
```

## TODO / Cải tiến

- [ ] Thêm filter theo giá, thời gian
- [ ] Sắp xếp kết quả (giá thấp nhất, thời gian sớm nhất, thời gian ngắn nhất)
- [ ] Tích hợp với PricingPolicy để tính giá động
- [ ] Tính toán ghế trống chính xác dựa trên Reservations
- [ ] Thêm chức năng tìm chuyến bay khứ hồi
- [ ] Cache danh sách cities
- [ ] Pagination cho kết quả tìm kiếm
- [ ] Lọc chuyến bay theo hãng hàng không

## Lỗi thường gặp

1. **Không tìm thấy chuyến bay**: Kiểm tra có schedule nào trong ngày được chọn không
2. **Không load được cities**: Kiểm tra database connection và dữ liệu trong bảng Cities
3. **Giá vé = 0**: Kiểm tra BaseFare trong bảng Flights

## Liên hệ

Nếu có vấn đề, vui lòng tạo issue hoặc liên hệ team phát triển.
