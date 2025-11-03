-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Nov 03, 2025 at 01:21 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `arsdatabase`
--

-- --------------------------------------------------------

--
-- Table structure for table `aspnetroleclaims`
--

CREATE TABLE `aspnetroleclaims` (
  `Id` int(11) NOT NULL,
  `RoleId` int(11) NOT NULL,
  `ClaimType` longtext DEFAULT NULL,
  `ClaimValue` longtext DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `aspnetroles`
--

CREATE TABLE `aspnetroles` (
  `Id` int(11) NOT NULL,
  `Name` varchar(256) DEFAULT NULL,
  `NormalizedName` varchar(256) DEFAULT NULL,
  `ConcurrencyStamp` longtext DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `aspnetroles`
--

INSERT INTO `aspnetroles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES
(1, 'Admin', 'ADMIN', NULL),
(2, 'Customer', 'CUSTOMER', NULL);

-- --------------------------------------------------------

--
-- Table structure for table `aspnetuserclaims`
--

CREATE TABLE `aspnetuserclaims` (
  `Id` int(11) NOT NULL,
  `UserId` int(11) NOT NULL,
  `ClaimType` longtext DEFAULT NULL,
  `ClaimValue` longtext DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `aspnetuserlogins`
--

CREATE TABLE `aspnetuserlogins` (
  `LoginProvider` varchar(255) NOT NULL,
  `ProviderKey` varchar(255) NOT NULL,
  `ProviderDisplayName` longtext DEFAULT NULL,
  `UserId` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `aspnetuserroles`
--

CREATE TABLE `aspnetuserroles` (
  `UserId` int(11) NOT NULL,
  `RoleId` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `aspnetuserroles`
--

INSERT INTO `aspnetuserroles` (`UserId`, `RoleId`) VALUES
(1, 2);

-- --------------------------------------------------------

--
-- Table structure for table `aspnetusers`
--

CREATE TABLE `aspnetusers` (
  `Id` int(11) NOT NULL,
  `UserName` varchar(256) DEFAULT NULL,
  `NormalizedUserName` varchar(256) DEFAULT NULL,
  `Email` varchar(256) DEFAULT NULL,
  `NormalizedEmail` varchar(256) DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL DEFAULT 0,
  `PasswordHash` longtext DEFAULT NULL,
  `SecurityStamp` longtext DEFAULT NULL,
  `ConcurrencyStamp` longtext DEFAULT NULL,
  `PhoneNumber` longtext DEFAULT NULL,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL DEFAULT 0,
  `TwoFactorEnabled` tinyint(1) NOT NULL DEFAULT 0,
  `LockoutEnd` datetime(6) DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL DEFAULT 0,
  `AccessFailedCount` int(11) NOT NULL DEFAULT 0,
  `FirstName` varchar(100) NOT NULL,
  `LastName` varchar(100) NOT NULL,
  `Phone` varchar(20) DEFAULT NULL,
  `Address` varchar(500) DEFAULT NULL,
  `Gender` char(1) NOT NULL,
  `Age` int(11) DEFAULT NULL,
  `CreditCardNumber` varchar(20) DEFAULT NULL,
  `SkyMiles` int(11) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `aspnetusers`
--

INSERT INTO `aspnetusers` (`Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`, `FirstName`, `LastName`, `Phone`, `Address`, `Gender`, `Age`, `CreditCardNumber`, `SkyMiles`) VALUES
(1, 'scwar69@gmail.com', 'SCWAR69@GMAIL.COM', 'scwar69@gmail.com', 'SCWAR69@GMAIL.COM', 0, 'AQAAAAIAAYagAAAAEE1geISp6sQIZeqj04hXorx4o68DcKx+KExXpfnx88mwi1UwX/0Ovkj62fKNEBeUEw==', 'RY5ALWUZNK6GGKTWKDWHQAJRN4EQPP65', 'd70a74e8-e930-4f51-ae9c-bfd865fe7316', NULL, 0, 0, NULL, 1, 0, 'Shiro', 'White', '0825113336', '123 Street', 'M', 13, NULL, 0);

-- --------------------------------------------------------

--
-- Table structure for table `aspnetusertokens`
--

CREATE TABLE `aspnetusertokens` (
  `UserId` int(11) NOT NULL,
  `LoginProvider` varchar(255) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `Value` longtext DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `cities`
--

CREATE TABLE `cities` (
  `CityID` int(11) NOT NULL,
  `CityName` varchar(100) NOT NULL,
  `Country` varchar(100) NOT NULL,
  `AirportCode` varchar(10) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `cities`
--

INSERT INTO `cities` (`CityID`, `CityName`, `Country`, `AirportCode`) VALUES
(1, 'Manila', 'Philippines', 'MNL'),
(2, 'Cebu', 'Philippines', 'CEB'),
(3, 'Tokyo', 'Japan', 'NRT'),
(4, 'Singapore', 'Singapore', 'SIN'),
(5, 'Hong Kong', 'Hong Kong', 'HKG');

-- --------------------------------------------------------

--
-- Table structure for table `flights`
--

CREATE TABLE `flights` (
  `FlightID` int(11) NOT NULL,
  `FlightNumber` varchar(20) NOT NULL,
  `OriginCityID` int(11) NOT NULL,
  `DestinationCityID` int(11) NOT NULL,
  `DepartureTime` datetime(6) NOT NULL,
  `ArrivalTime` datetime(6) NOT NULL,
  `Duration` int(11) NOT NULL,
  `AircraftType` varchar(50) NOT NULL,
  `TotalSeats` int(11) NOT NULL,
  `BaseFare` decimal(10,2) NOT NULL,
  `PolicyID` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `flights`
--

INSERT INTO `flights` (`FlightID`, `FlightNumber`, `OriginCityID`, `DestinationCityID`, `DepartureTime`, `ArrivalTime`, `Duration`, `AircraftType`, `TotalSeats`, `BaseFare`, `PolicyID`) VALUES
(100, 'ARS100', 1, 2, '2025-11-15 08:00:00.000000', '2025-11-15 09:00:00.000000', 60, 'A320', 150, 50.00, 2),
(101, 'ARS200', 1, 3, '2025-11-16 10:30:00.000000', '2025-11-16 18:00:00.000000', 450, 'B787', 300, 320.00, 3),
(102, 'ARS300', 2, 4, '2025-11-17 14:00:00.000000', '2025-11-17 16:30:00.000000', 150, 'A321', 180, 120.00, 2);

-- --------------------------------------------------------

--
-- Table structure for table `payments`
--

CREATE TABLE `payments` (
  `PaymentID` int(11) NOT NULL,
  `ReservationID` int(11) NOT NULL,
  `Amount` decimal(10,2) NOT NULL,
  `PaymentDate` datetime(6) NOT NULL,
  `PaymentMethod` varchar(50) NOT NULL,
  `TransactionStatus` varchar(50) NOT NULL,
  `TransactionRefNo` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `pricingpolicies`
--

CREATE TABLE `pricingpolicies` (
  `PolicyID` int(11) NOT NULL,
  `Description` varchar(500) NOT NULL,
  `DaysBeforeDeparture` int(11) NOT NULL,
  `PriceMultiplier` decimal(5,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `pricingpolicies`
--

INSERT INTO `pricingpolicies` (`PolicyID`, `Description`, `DaysBeforeDeparture`, `PriceMultiplier`) VALUES
(1, 'Early Bird (30+ days)', 30, 0.80),
(2, 'Standard (15-29 days)', 15, 1.00),
(3, 'Late Booking (7-14 days)', 7, 1.20),
(4, 'Last Minute (0-6 days)', 0, 1.50);

-- --------------------------------------------------------

--
-- Table structure for table `refunds`
--

CREATE TABLE `refunds` (
  `RefundID` int(11) NOT NULL,
  `ReservationID` int(11) NOT NULL,
  `RefundAmount` decimal(10,2) NOT NULL,
  `RefundDate` datetime(6) NOT NULL,
  `RefundPercentage` decimal(5,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `reservations`
--

CREATE TABLE `reservations` (
  `ReservationID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `FlightID` int(11) NOT NULL,
  `ScheduleID` int(11) NOT NULL,
  `BookingDate` date NOT NULL,
  `TravelDate` date NOT NULL,
  `Status` varchar(50) NOT NULL,
  `NumAdults` int(11) NOT NULL,
  `NumChildren` int(11) NOT NULL,
  `NumSeniors` int(11) NOT NULL,
  `Class` varchar(50) NOT NULL,
  `ConfirmationNumber` varchar(50) NOT NULL,
  `BlockingNumber` varchar(50) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `reservations`
--

INSERT INTO `reservations` (`ReservationID`, `UserID`, `FlightID`, `ScheduleID`, `BookingDate`, `TravelDate`, `Status`, `NumAdults`, `NumChildren`, `NumSeniors`, `Class`, `ConfirmationNumber`, `BlockingNumber`) VALUES
(1, 1, 100, 1000, '2025-11-03', '2025-11-15', 'Pending', 1, 0, 0, 'Economy', 'ARS202511031907405640', 'BLK20251103190740');

-- --------------------------------------------------------

--
-- Table structure for table `schedules`
--

CREATE TABLE `schedules` (
  `ScheduleID` int(11) NOT NULL,
  `FlightID` int(11) NOT NULL,
  `Date` date NOT NULL,
  `Status` varchar(50) NOT NULL,
  `CityID` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `schedules`
--

INSERT INTO `schedules` (`ScheduleID`, `FlightID`, `Date`, `Status`, `CityID`) VALUES
(1000, 100, '2025-11-15', 'Scheduled', 2),
(1001, 101, '2025-11-16', 'Scheduled', 3),
(1002, 102, '2025-11-17', 'Scheduled', 4);

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `UserID` int(11) NOT NULL,
  `FirstName` varchar(100) NOT NULL,
  `LastName` varchar(100) NOT NULL,
  `Email` varchar(200) NOT NULL,
  `Password` varchar(255) NOT NULL,
  `Phone` varchar(20) DEFAULT NULL,
  `Address` varchar(500) DEFAULT NULL,
  `Gender` varchar(1) NOT NULL,
  `Age` int(11) DEFAULT NULL,
  `CreditCardNumber` varchar(20) DEFAULT NULL,
  `SkyMiles` int(11) NOT NULL,
  `Role` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `users`
--

INSERT INTO `users` (`UserID`, `FirstName`, `LastName`, `Email`, `Password`, `Phone`, `Address`, `Gender`, `Age`, `CreditCardNumber`, `SkyMiles`, `Role`) VALUES
(1, 'Admin', 'User', 'admin@ars.com', 'Admin@123', NULL, NULL, 'M', NULL, NULL, 0, 'Admin');

-- --------------------------------------------------------

--
-- Table structure for table `__efmigrationshistory`
--

CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `__efmigrationshistory`
--

INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`) VALUES
('20251029121905_InitialCreate', '9.0.10'),
('20251103114859_InitialIdentity', '9.0.10'),
('20251103120432_SeedFlights', '9.0.10');

--
-- Indexes for dumped tables
--

--
-- Indexes for table `aspnetroleclaims`
--
ALTER TABLE `aspnetroleclaims`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`);

--
-- Indexes for table `aspnetroles`
--
ALTER TABLE `aspnetroles`
  ADD PRIMARY KEY (`Id`),
  ADD UNIQUE KEY `RoleNameIndex` (`NormalizedName`);

--
-- Indexes for table `aspnetuserclaims`
--
ALTER TABLE `aspnetuserclaims`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `IX_AspNetUserClaims_UserId` (`UserId`);

--
-- Indexes for table `aspnetuserlogins`
--
ALTER TABLE `aspnetuserlogins`
  ADD PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  ADD KEY `IX_AspNetUserLogins_UserId` (`UserId`);

--
-- Indexes for table `aspnetuserroles`
--
ALTER TABLE `aspnetuserroles`
  ADD PRIMARY KEY (`UserId`,`RoleId`),
  ADD KEY `IX_AspNetUserRoles_RoleId` (`RoleId`);

--
-- Indexes for table `aspnetusers`
--
ALTER TABLE `aspnetusers`
  ADD PRIMARY KEY (`Id`),
  ADD UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  ADD KEY `EmailIndex` (`NormalizedEmail`);

--
-- Indexes for table `aspnetusertokens`
--
ALTER TABLE `aspnetusertokens`
  ADD PRIMARY KEY (`UserId`,`LoginProvider`,`Name`);

--
-- Indexes for table `cities`
--
ALTER TABLE `cities`
  ADD PRIMARY KEY (`CityID`),
  ADD UNIQUE KEY `IX_Cities_AirportCode` (`AirportCode`);

--
-- Indexes for table `flights`
--
ALTER TABLE `flights`
  ADD PRIMARY KEY (`FlightID`),
  ADD UNIQUE KEY `IX_Flights_FlightNumber` (`FlightNumber`),
  ADD KEY `IX_Flights_DestinationCityID` (`DestinationCityID`),
  ADD KEY `IX_Flights_OriginCityID` (`OriginCityID`),
  ADD KEY `IX_Flights_PolicyID` (`PolicyID`);

--
-- Indexes for table `payments`
--
ALTER TABLE `payments`
  ADD PRIMARY KEY (`PaymentID`),
  ADD KEY `IX_Payments_ReservationID` (`ReservationID`);

--
-- Indexes for table `pricingpolicies`
--
ALTER TABLE `pricingpolicies`
  ADD PRIMARY KEY (`PolicyID`);

--
-- Indexes for table `refunds`
--
ALTER TABLE `refunds`
  ADD PRIMARY KEY (`RefundID`),
  ADD KEY `IX_Refunds_ReservationID` (`ReservationID`);

--
-- Indexes for table `reservations`
--
ALTER TABLE `reservations`
  ADD PRIMARY KEY (`ReservationID`),
  ADD UNIQUE KEY `IX_Reservations_ConfirmationNumber` (`ConfirmationNumber`),
  ADD KEY `IX_Reservations_FlightID` (`FlightID`),
  ADD KEY `IX_Reservations_ScheduleID` (`ScheduleID`),
  ADD KEY `IX_Reservations_UserID` (`UserID`);

--
-- Indexes for table `schedules`
--
ALTER TABLE `schedules`
  ADD PRIMARY KEY (`ScheduleID`),
  ADD KEY `IX_Schedules_CityID` (`CityID`),
  ADD KEY `IX_Schedules_FlightID` (`FlightID`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`UserID`),
  ADD UNIQUE KEY `IX_Users_Email` (`Email`);

--
-- Indexes for table `__efmigrationshistory`
--
ALTER TABLE `__efmigrationshistory`
  ADD PRIMARY KEY (`MigrationId`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `aspnetroleclaims`
--
ALTER TABLE `aspnetroleclaims`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `aspnetroles`
--
ALTER TABLE `aspnetroles`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `aspnetuserclaims`
--
ALTER TABLE `aspnetuserclaims`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `aspnetusers`
--
ALTER TABLE `aspnetusers`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `cities`
--
ALTER TABLE `cities`
  MODIFY `CityID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT for table `flights`
--
ALTER TABLE `flights`
  MODIFY `FlightID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=103;

--
-- AUTO_INCREMENT for table `payments`
--
ALTER TABLE `payments`
  MODIFY `PaymentID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `pricingpolicies`
--
ALTER TABLE `pricingpolicies`
  MODIFY `PolicyID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT for table `refunds`
--
ALTER TABLE `refunds`
  MODIFY `RefundID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `reservations`
--
ALTER TABLE `reservations`
  MODIFY `ReservationID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `schedules`
--
ALTER TABLE `schedules`
  MODIFY `ScheduleID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=1003;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `UserID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `aspnetroleclaims`
--
ALTER TABLE `aspnetroleclaims`
  ADD CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE;

--
-- Constraints for table `aspnetuserclaims`
--
ALTER TABLE `aspnetuserclaims`
  ADD CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE;

--
-- Constraints for table `aspnetuserlogins`
--
ALTER TABLE `aspnetuserlogins`
  ADD CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE;

--
-- Constraints for table `aspnetuserroles`
--
ALTER TABLE `aspnetuserroles`
  ADD CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE;

--
-- Constraints for table `aspnetusertokens`
--
ALTER TABLE `aspnetusertokens`
  ADD CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE;

--
-- Constraints for table `flights`
--
ALTER TABLE `flights`
  ADD CONSTRAINT `FK_Flights_Cities_DestinationCityID` FOREIGN KEY (`DestinationCityID`) REFERENCES `cities` (`CityID`),
  ADD CONSTRAINT `FK_Flights_Cities_OriginCityID` FOREIGN KEY (`OriginCityID`) REFERENCES `cities` (`CityID`),
  ADD CONSTRAINT `FK_Flights_PricingPolicies_PolicyID` FOREIGN KEY (`PolicyID`) REFERENCES `pricingpolicies` (`PolicyID`) ON DELETE SET NULL;

--
-- Constraints for table `payments`
--
ALTER TABLE `payments`
  ADD CONSTRAINT `FK_Payments_Reservations_ReservationID` FOREIGN KEY (`ReservationID`) REFERENCES `reservations` (`ReservationID`) ON DELETE CASCADE;

--
-- Constraints for table `refunds`
--
ALTER TABLE `refunds`
  ADD CONSTRAINT `FK_Refunds_Reservations_ReservationID` FOREIGN KEY (`ReservationID`) REFERENCES `reservations` (`ReservationID`) ON DELETE CASCADE;

--
-- Constraints for table `reservations`
--
ALTER TABLE `reservations`
  ADD CONSTRAINT `FK_Reservations_AspNetUsers_UserID` FOREIGN KEY (`UserID`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_Reservations_Flights_FlightID` FOREIGN KEY (`FlightID`) REFERENCES `flights` (`FlightID`),
  ADD CONSTRAINT `FK_Reservations_Schedules_ScheduleID` FOREIGN KEY (`ScheduleID`) REFERENCES `schedules` (`ScheduleID`),
  ADD CONSTRAINT `FK_Reservations_Users_UserID` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE;

--
-- Constraints for table `schedules`
--
ALTER TABLE `schedules`
  ADD CONSTRAINT `FK_Schedules_Cities_CityID` FOREIGN KEY (`CityID`) REFERENCES `cities` (`CityID`) ON DELETE SET NULL,
  ADD CONSTRAINT `FK_Schedules_Flights_FlightID` FOREIGN KEY (`FlightID`) REFERENCES `flights` (`FlightID`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
