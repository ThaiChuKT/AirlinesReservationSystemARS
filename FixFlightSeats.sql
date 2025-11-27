-- Fix Flight Seats Issue
-- This script ensures all flights use the same seat layout and have correct seat counts

-- Step 1: Check current SeatLayouts
SELECT * FROM SeatLayouts;

-- Step 2: Count seats per layout
SELECT SeatLayoutId, COUNT(*) as SeatCount 
FROM Seats 
GROUP BY SeatLayoutId;

-- Step 3: Assign all flights to use the first/default SeatLayout
-- Get the first SeatLayout ID
SET @defaultLayoutId = (SELECT SeatLayoutId FROM SeatLayouts ORDER BY SeatLayoutId LIMIT 1);

-- Update all flights to use this layout
UPDATE Flights 
SET SeatLayoutId = @defaultLayoutId
WHERE SeatLayoutId IS NULL OR SeatLayoutId != @defaultLayoutId;

-- Step 4: Update TotalSeats for all flights to match the seat count in the layout
UPDATE Flights f
SET f.TotalSeats = (
    SELECT COUNT(*) 
    FROM Seats s 
    WHERE s.SeatLayoutId = f.SeatLayoutId
)
WHERE f.SeatLayoutId IS NOT NULL;

-- Step 5: Verify the changes
SELECT FlightID, FlightNumber, SeatLayoutId, TotalSeats 
FROM Flights 
ORDER BY FlightID;

-- Step 6: Clean up old FlightSeats to regenerate them correctly
-- TRUNCATE TABLE FlightSeats;

-- Step 7: Verify Seats table has correct data
SELECT SeatLayoutId, COUNT(*) as TotalSeats, 
       MIN(RowNumber) as MinRow, MAX(RowNumber) as MaxRow
FROM Seats
GROUP BY SeatLayoutId;
