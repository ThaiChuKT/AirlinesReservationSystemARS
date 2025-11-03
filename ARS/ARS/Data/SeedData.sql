-- Insert sample cities
INSERT INTO `City` (`CityName`, `Country`, `AirportCode`) VALUES
('Hà Nội', 'Việt Nam', 'HAN'),
('Hồ Chí Minh', 'Việt Nam', 'SGN'),
('Đà Nẵng', 'Việt Nam', 'DAD'),
('Nha Trang', 'Việt Nam', 'CXR'),
('Phú Quốc', 'Việt Nam', 'PQC'),
('Bangkok', 'Thái Lan', 'BKK'),
('Singapore', 'Singapore', 'SIN'),
('Tokyo', 'Nhật Bản', 'NRT'),
('Seoul', 'Hàn Quốc', 'ICN'),
('Hong Kong', 'Trung Quốc', 'HKG');

-- Insert sample flights (HAN -> SGN)
INSERT INTO `Flight` (`FlightNumber`, `OriginCityID`, `DestinationCityID`, `DepartureTime`, `ArrivalTime`, `Duration`, `AircraftType`, `TotalSeats`, `BaseFare`) VALUES
('VN101', 1, 2, '2025-11-04 06:00:00', '2025-11-04 08:15:00', 135, 'Airbus A321', 180, 1500000),
('VN103', 1, 2, '2025-11-04 09:30:00', '2025-11-04 11:45:00', 135, 'Boeing 787', 250, 1800000),
('VN105', 1, 2, '2025-11-04 14:00:00', '2025-11-04 16:15:00', 135, 'Airbus A321', 180, 1600000),
('VN107', 1, 2, '2025-11-04 18:30:00', '2025-11-04 20:45:00', 135, 'Boeing 787', 250, 2000000);

-- Insert sample flights (SGN -> HAN)
INSERT INTO `Flight` (`FlightNumber`, `OriginCityID`, `DestinationCityID`, `DepartureTime`, `ArrivalTime`, `Duration`, `AircraftType`, `TotalSeats`, `BaseFare`) VALUES
('VN102', 2, 1, '2025-11-04 07:00:00', '2025-11-04 09:15:00', 135, 'Airbus A321', 180, 1500000),
('VN104', 2, 1, '2025-11-04 10:30:00', '2025-11-04 12:45:00', 135, 'Boeing 787', 250, 1800000),
('VN106', 2, 1, '2025-11-04 15:00:00', '2025-11-04 17:15:00', 135, 'Airbus A321', 180, 1600000),
('VN108', 2, 1, '2025-11-04 19:30:00', '2025-11-04 21:45:00', 135, 'Boeing 787', 250, 2000000);

-- Insert sample flights (HAN -> DAD)
INSERT INTO `Flight` (`FlightNumber`, `OriginCityID`, `DestinationCityID`, `DepartureTime`, `ArrivalTime`, `Duration`, `AircraftType`, `TotalSeats`, `BaseFare`) VALUES
('VN201', 1, 3, '2025-11-04 08:00:00', '2025-11-04 09:20:00', 80, 'Airbus A320', 150, 1200000),
('VN203', 1, 3, '2025-11-04 13:00:00', '2025-11-04 14:20:00', 80, 'Airbus A320', 150, 1300000);

-- Insert sample flights (SGN -> DAD)
INSERT INTO `Flight` (`FlightNumber`, `OriginCityID`, `DestinationCityID`, `DepartureTime`, `ArrivalTime`, `Duration`, `AircraftType`, `TotalSeats`, `BaseFare`) VALUES
('VN301', 2, 3, '2025-11-04 09:00:00', '2025-11-04 10:15:00', 75, 'Airbus A320', 150, 1100000),
('VN303', 2, 3, '2025-11-04 16:00:00', '2025-11-04 17:15:00', 75, 'Airbus A320', 150, 1200000);

-- Insert schedules for the next 7 days
INSERT INTO `Schedule` (`FlightID`, `Date`, `Status`)
SELECT 
    f.FlightID,
    DATE_ADD(CURDATE(), INTERVAL d.day DAY) + INTERVAL HOUR(f.DepartureTime) HOUR + INTERVAL MINUTE(f.DepartureTime) MINUTE,
    'Scheduled'
FROM `Flight` f
CROSS JOIN (
    SELECT 0 AS day UNION ALL
    SELECT 1 UNION ALL
    SELECT 2 UNION ALL
    SELECT 3 UNION ALL
    SELECT 4 UNION ALL
    SELECT 5 UNION ALL
    SELECT 6
) d;
