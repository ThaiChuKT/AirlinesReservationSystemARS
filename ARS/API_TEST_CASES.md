# Flight Search API Test Cases

## 1. Test POST /api/Flight/search - Tìm chuyến bay Hà Nội -> Sài Gòn

### Test Case 1: Tìm kiếm cơ bản (1 người lớn, Economy)
```http
POST https://localhost:5001/api/Flight/search
Content-Type: application/json

{
  "originCityId": 1,
  "destinationCityId": 2,
  "departureDate": "2025-11-04",
  "numAdults": 1,
  "numChildren": 0,
  "numSeniors": 0,
  "class": "Economy"
}
```

### Test Case 2: Gia đình (2 người lớn, 2 trẻ em, Business)
```http
POST https://localhost:5001/api/Flight/search
Content-Type: application/json

{
  "originCityId": 1,
  "destinationCityId": 2,
  "departureDate": "2025-11-05",
  "numAdults": 2,
  "numChildren": 2,
  "numSeniors": 0,
  "class": "Business"
}
```

### Test Case 3: Người cao tuổi (2 người cao tuổi, Premium Economy)
```http
POST https://localhost:5001/api/Flight/search
Content-Type: application/json

{
  "originCityId": 2,
  "destinationCityId": 1,
  "departureDate": "2025-11-06",
  "numAdults": 0,
  "numChildren": 0,
  "numSeniors": 2,
  "class": "Premium Economy"
}
```

### Test Case 4: Tìm chuyến bay Hà Nội -> Đà Nẵng
```http
POST https://localhost:5001/api/Flight/search
Content-Type: application/json

{
  "originCityId": 1,
  "destinationCityId": 3,
  "departureDate": "2025-11-04",
  "numAdults": 1,
  "numChildren": 0,
  "numSeniors": 0,
  "class": "Economy"
}
```

### Test Case 5: Không tìm thấy chuyến bay (route không tồn tại)
```http
POST https://localhost:5001/api/Flight/search
Content-Type: application/json

{
  "originCityId": 1,
  "destinationCityId": 6,
  "departureDate": "2025-11-04",
  "numAdults": 1,
  "numChildren": 0,
  "numSeniors": 0,
  "class": "Economy"
}
```

### Test Case 6: Validation Error (Missing required fields)
```http
POST https://localhost:5001/api/Flight/search
Content-Type: application/json

{
  "originCityId": 1,
  "numAdults": 1
}
```

## 2. Test GET /api/Flight/cities - Lấy danh sách thành phố

```http
GET https://localhost:5001/api/Flight/cities
```

Expected Response:
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
  },
  ...
]
```

## 3. Test GET /api/Flight/{id} - Lấy chi tiết chuyến bay

```http
GET https://localhost:5001/api/Flight/1
```

Expected Response:
```json
{
  "flightId": 1,
  "flightNumber": "VN101",
  "originCityId": 1,
  "originCityName": "Hà Nội",
  "originCountry": "Việt Nam",
  "originAirportCode": "HAN",
  "destinationCityId": 2,
  "destinationCityName": "Hồ Chí Minh",
  "destinationCountry": "Việt Nam",
  "destinationAirportCode": "SGN",
  "departureTime": "2025-11-04T06:00:00",
  "arrivalTime": "2025-11-04T08:15:00",
  "duration": 135,
  "aircraftType": "Airbus A321",
  "totalSeats": 180,
  "availableSeats": 180,
  "baseFare": 1500000,
  "totalPrice": 1500000,
  "schedules": [...]
}
```

## Price Calculation Examples

### Economy Class
- 1 Adult: 1,500,000 × 1.0 × 1 = **1,500,000 VNĐ**
- 2 Adults: 1,500,000 × 1.0 × 2 = **3,000,000 VNĐ**
- 2 Adults + 2 Children: 1,500,000 × 1.0 × (2 + 2×0.75) = **5,250,000 VNĐ**
- 2 Seniors: 1,500,000 × 1.0 × 2 × 0.9 = **2,700,000 VNĐ**

### Business Class
- 1 Adult: 1,500,000 × 2.0 × 1 = **3,000,000 VNĐ**
- 2 Adults + 2 Children: 1,500,000 × 2.0 × (2 + 2×0.75) = **10,500,000 VNĐ**

### Premium Economy
- 2 Seniors: 1,500,000 × 1.5 × 2 × 0.9 = **4,050,000 VNĐ**

### First Class
- 1 Adult: 1,500,000 × 3.0 × 1 = **4,500,000 VNĐ**

## Testing with PowerShell

```powershell
# Test 1: Search flights
$body = @{
    originCityId = 1
    destinationCityId = 2
    departureDate = "2025-11-04"
    numAdults = 1
    numChildren = 0
    numSeniors = 0
    class = "Economy"
} | ConvertTo-Json

Invoke-WebRequest -Uri "https://localhost:5001/api/Flight/search" `
    -Method POST `
    -Body $body `
    -ContentType "application/json" `
    -SkipCertificateCheck

# Test 2: Get cities
Invoke-WebRequest -Uri "https://localhost:5001/api/Flight/cities" `
    -Method GET `
    -SkipCertificateCheck

# Test 3: Get flight by ID
Invoke-WebRequest -Uri "https://localhost:5001/api/Flight/1" `
    -Method GET `
    -SkipCertificateCheck
```

## Testing with cURL

```bash
# Test 1: Search flights
curl -X POST https://localhost:5001/api/Flight/search \
  -H "Content-Type: application/json" \
  -d '{
    "originCityId": 1,
    "destinationCityId": 2,
    "departureDate": "2025-11-04",
    "numAdults": 1,
    "numChildren": 0,
    "numSeniors": 0,
    "class": "Economy"
  }' \
  --insecure

# Test 2: Get cities
curl -X GET https://localhost:5001/api/Flight/cities --insecure

# Test 3: Get flight by ID
curl -X GET https://localhost:5001/api/Flight/1 --insecure
```
