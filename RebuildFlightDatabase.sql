-- ============================================
-- Rebuild Flight Database with Fresh Data
-- Creates flights through end of 2025
-- ============================================

-- Step 1: Clean up existing flight-related data
-- Must delete in correct order due to foreign keys
DELETE FROM ReservationLegs WHERE ReservationLegID > 0;
DELETE FROM FlightSeats WHERE FlightSeatId > 0;
DELETE FROM Reservations WHERE ReservationID > 0;
DELETE FROM Schedules WHERE ScheduleID > 0;
DELETE FROM Flights WHERE FlightID > 0;

-- Reset auto-increment counters
ALTER TABLE Flights AUTO_INCREMENT = 1;
ALTER TABLE Schedules AUTO_INCREMENT = 1;
ALTER TABLE Reservations AUTO_INCREMENT = 1;
ALTER TABLE ReservationLegs AUTO_INCREMENT = 1;
ALTER TABLE FlightSeats AUTO_INCREMENT = 1;

-- Step 2: Ensure we have cities (keep existing ones)
-- Just verify key cities exist, don't delete existing data

-- Step 3: Get the default SeatLayout ID
SET @defaultLayoutId = (SELECT SeatLayoutId FROM SeatLayouts ORDER BY SeatLayoutId LIMIT 1);

-- If no SeatLayout exists, we need to create one with the standard configuration
-- Check if we need to create it
SET @layoutCount = (SELECT COUNT(*) FROM SeatLayouts);

-- Step 4: Create comprehensive flight schedule
-- Major routes with multiple daily flights

-- Get City IDs for major routes
SET @hkgId = (SELECT CityID FROM Cities WHERE AirportCode = 'HKG' LIMIT 1);
SET @sinId = (SELECT CityID FROM Cities WHERE AirportCode = 'SIN' LIMIT 1);
SET @tpeId = (SELECT CityID FROM Cities WHERE AirportCode = 'TPE' LIMIT 1);
SET @nrtId = (SELECT CityID FROM Cities WHERE AirportCode = 'NRT' LIMIT 1);
SET @bkkId = (SELECT CityID FROM Cities WHERE AirportCode = 'BKK' LIMIT 1);
SET @mnlId = (SELECT CityID FROM Cities WHERE AirportCode = 'MNL' LIMIT 1);
SET @icnId = (SELECT CityID FROM Cities WHERE AirportCode = 'ICN' LIMIT 1);
SET @pvgId = (SELECT CityID FROM Cities WHERE AirportCode = 'PVG' LIMIT 1);

-- Insert Flights (30+ routes with different times)
INSERT INTO Flights (FlightNumber, OriginCityID, DestinationCityID, DepartureTime, ArrivalTime, Duration, AircraftType, TotalSeats, BaseFare, SeatLayoutId)
VALUES
-- Hong Kong <-> Singapore (4 daily flights each way)
('AR101', @hkgId, @sinId, '2025-01-01 06:30:00', '2025-01-01 10:15:00', '03:45:00', 'Airbus A320', 180, 450.00, @defaultLayoutId),
('AR102', @sinId, @hkgId, '2025-01-01 11:30:00', '2025-01-01 15:15:00', '03:45:00', 'Airbus A320', 180, 450.00, @defaultLayoutId),
('AR103', @hkgId, @sinId, '2025-01-01 12:00:00', '2025-01-01 15:45:00', '03:45:00', 'Boeing 737', 180, 480.00, @defaultLayoutId),
('AR104', @sinId, @hkgId, '2025-01-01 16:30:00', '2025-01-01 20:15:00', '03:45:00', 'Boeing 737', 180, 480.00, @defaultLayoutId),
('AR105', @hkgId, @sinId, '2025-01-01 16:00:00', '2025-01-01 19:45:00', '03:45:00', 'Airbus A321', 180, 500.00, @defaultLayoutId),
('AR106', @sinId, @hkgId, '2025-01-01 20:30:00', '2025-01-01 00:15:00', '03:45:00', 'Airbus A321', 180, 500.00, @defaultLayoutId),
('AR107', @hkgId, @sinId, '2025-01-01 20:00:00', '2025-01-01 23:45:00', '03:45:00', 'Boeing 787', 240, 550.00, @defaultLayoutId),
('AR108', @sinId, @hkgId, '2025-01-01 07:00:00', '2025-01-01 10:45:00', '03:45:00', 'Boeing 787', 240, 550.00, @defaultLayoutId),

-- Hong Kong <-> Taipei (3 daily flights each way)
('AR201', @hkgId, @tpeId, '2025-01-01 08:00:00', '2025-01-01 09:50:00', '01:50:00', 'Airbus A320', 180, 280.00, @defaultLayoutId),
('AR202', @tpeId, @hkgId, '2025-01-01 10:30:00', '2025-01-01 12:20:00', '01:50:00', 'Airbus A320', 180, 280.00, @defaultLayoutId),
('AR203', @hkgId, @tpeId, '2025-01-01 14:00:00', '2025-01-01 15:50:00', '01:50:00', 'Boeing 737', 180, 300.00, @defaultLayoutId),
('AR204', @tpeId, @hkgId, '2025-01-01 16:30:00', '2025-01-01 18:20:00', '01:50:00', 'Boeing 737', 180, 300.00, @defaultLayoutId),
('AR205', @hkgId, @tpeId, '2025-01-01 19:00:00', '2025-01-01 20:50:00', '01:50:00', 'Airbus A321', 180, 320.00, @defaultLayoutId),
('AR206', @tpeId, @hkgId, '2025-01-01 21:30:00', '2025-01-01 23:20:00', '01:50:00', 'Airbus A321', 180, 320.00, @defaultLayoutId),

-- Hong Kong <-> Tokyo (2 daily flights each way)
('AR301', @hkgId, @nrtId, '2025-01-01 09:00:00', '2025-01-01 14:30:00', '05:30:00', 'Boeing 787', 240, 650.00, @defaultLayoutId),
('AR302', @nrtId, @hkgId, '2025-01-01 15:30:00', '2025-01-01 20:00:00', '04:30:00', 'Boeing 787', 240, 650.00, @defaultLayoutId),
('AR303', @hkgId, @nrtId, '2025-01-01 18:00:00', '2025-01-01 23:30:00', '05:30:00', 'Airbus A350', 300, 700.00, @defaultLayoutId),
('AR304', @nrtId, @hkgId, '2025-01-01 10:00:00', '2025-01-01 14:30:00', '04:30:00', 'Airbus A350', 300, 700.00, @defaultLayoutId),

-- Singapore <-> Bangkok (3 daily flights each way)
('AR401', @sinId, @bkkId, '2025-01-01 07:00:00', '2025-01-01 09:30:00', '02:30:00', 'Airbus A320', 180, 350.00, @defaultLayoutId),
('AR402', @bkkId, @sinId, '2025-01-01 10:30:00', '2025-01-01 13:00:00', '02:30:00', 'Airbus A320', 180, 350.00, @defaultLayoutId),
('AR403', @sinId, @bkkId, '2025-01-01 13:00:00', '2025-01-01 15:30:00', '02:30:00', 'Boeing 737', 180, 370.00, @defaultLayoutId),
('AR404', @bkkId, @sinId, '2025-01-01 16:30:00', '2025-01-01 19:00:00', '02:30:00', 'Boeing 737', 180, 370.00, @defaultLayoutId),
('AR405', @sinId, @bkkId, '2025-01-01 19:00:00', '2025-01-01 21:30:00', '02:30:00', 'Airbus A321', 180, 390.00, @defaultLayoutId),
('AR406', @bkkId, @sinId, '2025-01-01 22:30:00', '2025-01-01 01:00:00', '02:30:00', 'Airbus A321', 180, 390.00, @defaultLayoutId),

-- Singapore <-> Tokyo (2 daily flights each way)
('AR501', @sinId, @nrtId, '2025-01-01 10:00:00', '2025-01-01 17:30:00', '07:30:00', 'Boeing 787', 240, 800.00, @defaultLayoutId),
('AR502', @nrtId, @sinId, '2025-01-01 18:30:00', '2025-01-01 01:00:00', '06:30:00', 'Boeing 787', 240, 800.00, @defaultLayoutId),
('AR503', @sinId, @nrtId, '2025-01-01 22:00:00', '2025-01-01 05:30:00', '07:30:00', 'Airbus A350', 300, 850.00, @defaultLayoutId),
('AR504', @nrtId, @sinId, '2025-01-01 11:00:00', '2025-01-01 17:30:00', '06:30:00', 'Airbus A350', 300, 850.00, @defaultLayoutId),

-- Bangkok <-> Manila (2 daily flights each way)
('AR601', @bkkId, @mnlId, '2025-01-01 08:00:00', '2025-01-01 12:30:00', '04:30:00', 'Airbus A320', 180, 400.00, @defaultLayoutId),
('AR602', @mnlId, @bkkId, '2025-01-01 13:30:00', '2025-01-01 18:00:00', '04:30:00', 'Airbus A320', 180, 400.00, @defaultLayoutId),
('AR603', @bkkId, @mnlId, '2025-01-01 17:00:00', '2025-01-01 21:30:00', '04:30:00', 'Boeing 737', 180, 420.00, @defaultLayoutId),
('AR604', @mnlId, @bkkId, '2025-01-01 22:30:00', '2025-01-01 03:00:00', '04:30:00', 'Boeing 737', 180, 420.00, @defaultLayoutId),

-- Seoul <-> Tokyo (2 daily flights each way)
('AR701', @icnId, @nrtId, '2025-01-01 09:00:00', '2025-01-01 11:30:00', '02:30:00', 'Airbus A320', 180, 450.00, @defaultLayoutId),
('AR702', @nrtId, @icnId, '2025-01-01 12:30:00', '2025-01-01 15:00:00', '02:30:00', 'Airbus A320', 180, 450.00, @defaultLayoutId),
('AR703', @icnId, @nrtId, '2025-01-01 16:00:00', '2025-01-01 18:30:00', '02:30:00', 'Boeing 737', 180, 470.00, @defaultLayoutId),
('AR704', @nrtId, @icnId, '2025-01-01 19:30:00', '2025-01-01 22:00:00', '02:30:00', 'Boeing 737', 180, 470.00, @defaultLayoutId),

-- Shanghai <-> Hong Kong (3 daily flights each way)
('AR801', @pvgId, @hkgId, '2025-01-01 07:00:00', '2025-01-01 10:00:00', '03:00:00', 'Airbus A320', 180, 400.00, @defaultLayoutId),
('AR802', @hkgId, @pvgId, '2025-01-01 11:00:00', '2025-01-01 14:00:00', '03:00:00', 'Airbus A320', 180, 400.00, @defaultLayoutId),
('AR803', @pvgId, @hkgId, '2025-01-01 14:00:00', '2025-01-01 17:00:00', '03:00:00', 'Boeing 737', 180, 420.00, @defaultLayoutId),
('AR804', @hkgId, @pvgId, '2025-01-01 18:00:00', '2025-01-01 21:00:00', '03:00:00', 'Boeing 737', 180, 420.00, @defaultLayoutId),
('AR805', @pvgId, @hkgId, '2025-01-01 19:00:00', '2025-01-01 22:00:00', '03:00:00', 'Airbus A321', 180, 440.00, @defaultLayoutId),
('AR806', @hkgId, @pvgId, '2025-01-01 23:00:00', '2025-01-01 02:00:00', '03:00:00', 'Airbus A321', 180, 440.00, @defaultLayoutId),

-- Shanghai <-> Singapore (2 daily flights each way)
('AR901', @pvgId, @sinId, '2025-01-01 08:00:00', '2025-01-01 13:30:00', '05:30:00', 'Boeing 787', 240, 600.00, @defaultLayoutId),
('AR902', @sinId, @pvgId, '2025-01-01 14:30:00', '2025-01-01 20:00:00', '05:30:00', 'Boeing 787', 240, 600.00, @defaultLayoutId),
('AR903', @pvgId, @sinId, '2025-01-01 20:00:00', '2025-01-01 01:30:00', '05:30:00', 'Airbus A350', 300, 650.00, @defaultLayoutId),
('AR904', @sinId, @pvgId, '2025-01-01 09:00:00', '2025-01-01 14:30:00', '05:30:00', 'Airbus A350', 300, 650.00, @defaultLayoutId);

-- Step 5: Create schedules for all flights from November 28, 2025 through December 31, 2025
-- This will create daily schedules for each flight

DELIMITER $$

CREATE TEMPORARY PROCEDURE IF NOT EXISTS GenerateSchedules()
BEGIN
    DECLARE done INT DEFAULT FALSE;
    DECLARE flightIdVar INT;
    DECLARE currentDate DATE;
    DECLARE endDate DATE;
    
    DECLARE flight_cursor CURSOR FOR 
        SELECT FlightID FROM Flights;
    
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
    
    SET currentDate = '2025-11-28';
    SET endDate = '2025-12-31';
    
    OPEN flight_cursor;
    
    flight_loop: LOOP
        FETCH flight_cursor INTO flightIdVar;
        IF done THEN
            LEAVE flight_loop;
        END IF;
        
        -- Generate daily schedules for this flight
        SET currentDate = '2025-11-28';
        WHILE currentDate <= endDate DO
            INSERT INTO Schedules (FlightID, Date, Status)
            VALUES (flightIdVar, currentDate, 'Scheduled');
            
            SET currentDate = DATE_ADD(currentDate, INTERVAL 1 DAY);
        END WHILE;
    END LOOP;
    
    CLOSE flight_cursor;
END$$

DELIMITER ;

CALL GenerateSchedules();
DROP TEMPORARY PROCEDURE IF EXISTS GenerateSchedules;

-- Step 6: Update TotalSeats for all flights based on SeatLayout
UPDATE Flights f
SET f.TotalSeats = (
    SELECT COUNT(*) 
    FROM Seats s 
    WHERE s.SeatLayoutId = f.SeatLayoutId
)
WHERE f.SeatLayoutId IS NOT NULL;

-- Step 7: Verify the results
SELECT 
    'Flights Created' as Info,
    COUNT(*) as Count
FROM Flights
UNION ALL
SELECT 
    'Schedules Created' as Info,
    COUNT(*) as Count
FROM Schedules
UNION ALL
SELECT 
    'Date Range' as Info,
    CONCAT(MIN(Date), ' to ', MAX(Date)) as Count
FROM Schedules;

-- Show flight summary by route
SELECT 
    CONCAT(oc.AirportCode, ' → ', dc.AirportCode) as Route,
    COUNT(*) as DailyFlights,
    MIN(BaseFare) as MinFare,
    MAX(BaseFare) as MaxFare
FROM Flights f
JOIN Cities oc ON f.OriginCityID = oc.CityID
JOIN Cities dc ON f.DestinationCityID = dc.CityID
GROUP BY oc.AirportCode, dc.AirportCode
ORDER BY Route;

-- Show total schedules by date range
SELECT 
    COUNT(*) as TotalSchedules,
    COUNT(DISTINCT FlightID) as UniqueFlights,
    COUNT(DISTINCT Date) as DaysOfService
FROM Schedules;

SELECT '✅ Flight database rebuild complete!' as Status;
