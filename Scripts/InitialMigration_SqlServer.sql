-- =========================================================================
-- AgriSync India: EF Core Code-First SQL Server Migration Script
-- Database: AgriSyncIndiaDb
-- =========================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'AgriSyncIndiaDb')
BEGIN
    CREATE DATABASE AgriSyncIndiaDb;
END
GO

USE AgriSyncIndiaDb;
GO

-- 1. Table: Zones
IF OBJECT_ID('dbo.Zones', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Zones (
        ZoneId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        CropType NVARCHAR(100) NOT NULL,
        TargetTemp FLOAT NOT NULL,
        TargetHumidity FLOAT NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Optimal',
        Description NVARCHAR(250) NULL,
        AreaSqMeters FLOAT NOT NULL DEFAULT 1000,
        IconClass NVARCHAR(50) NOT NULL DEFAULT 'bi-flower1'
    );
END
GO

-- 2. Table: SensorLogs
IF OBJECT_ID('dbo.SensorLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SensorLogs (
        LogId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ZoneId INT NOT NULL,
        Temperature FLOAT NOT NULL,
        Humidity FLOAT NOT NULL,
        VPD FLOAT NOT NULL,
        SoilMoisture FLOAT NOT NULL,
        SolarRadiation FLOAT NOT NULL DEFAULT 0,
        SoilTemperature FLOAT NOT NULL DEFAULT 20,
        WindSpeed FLOAT NOT NULL DEFAULT 0,
        TimestampIST DATETIME2 NOT NULL,
        CONSTRAINT FK_SensorLogs_Zones_ZoneId FOREIGN KEY (ZoneId) 
            REFERENCES dbo.Zones (ZoneId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_SensorLogs_TimestampIST ON dbo.SensorLogs (TimestampIST);
    CREATE NONCLUSTERED INDEX IX_SensorLogs_ZoneId_TimestampIST ON dbo.SensorLogs (ZoneId, TimestampIST);
END
GO

-- 3. Table: Actuators
IF OBJECT_ID('dbo.Actuators', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Actuators (
        ActuatorId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        ZoneId INT NOT NULL,
        IsAutoMode BIT NOT NULL DEFAULT 1,
        CurrentStatus NVARCHAR(20) NOT NULL DEFAULT 'OFF',
        LastTriggered DATETIME2 NOT NULL,
        DeviceType NVARCHAR(50) NOT NULL DEFAULT 'Ventilation',
        PowerRatingKw FLOAT NOT NULL DEFAULT 1.5,
        CONSTRAINT FK_Actuators_Zones_ZoneId FOREIGN KEY (ZoneId) 
            REFERENCES dbo.Zones (ZoneId) ON DELETE CASCADE
    );
END
GO

-- 4. Table: AutomationRules
IF OBJECT_ID('dbo.AutomationRules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AutomationRules (
        RuleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConditionVariable NVARCHAR(50) NOT NULL,
        Operator NVARCHAR(10) NOT NULL,
        ThresholdValue FLOAT NOT NULL,
        ActionTarget NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        Description NVARCHAR(200) NULL,
        CreatedAt DATETIME2 NOT NULL
    );
END
GO

-- 5. Table: Users
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        Role NVARCHAR(50) NOT NULL DEFAULT 'Farm Manager',
        AffiliatedFarmCluster NVARCHAR(150) NULL,
        CreatedAt DATETIME2 NOT NULL,
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );
END
GO

-- 6. Table: FarmClusters
IF OBJECT_ID('dbo.FarmClusters', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FarmClusters (
        ClusterId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        Region NVARCHAR(100) NOT NULL,
        Latitude FLOAT NOT NULL DEFAULT 28.6139,
        Longitude FLOAT NOT NULL DEFAULT 77.2090,
        TotalAcreage FLOAT NOT NULL DEFAULT 25.0,
        PrimaryCrops NVARCHAR(200) NOT NULL,
        Description NVARCHAR(300) NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Active',
        CreatedAt DATETIME2 NOT NULL
    );
END
GO

-- 7. Table: ClimateAlerts
IF OBJECT_ID('dbo.ClimateAlerts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClimateAlerts (
        AlertId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ZoneId INT NULL,
        Severity NVARCHAR(20) NOT NULL DEFAULT 'Warning',
        Title NVARCHAR(150) NOT NULL,
        Message NVARCHAR(500) NOT NULL,
        AgronomistRecommendation NVARCHAR(250) NULL,
        TimestampIST DATETIME2 NOT NULL,
        IsAcknowledged BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_ClimateAlerts_Zones_ZoneId FOREIGN KEY (ZoneId) 
            REFERENCES dbo.Zones (ZoneId) ON DELETE SET NULL
    );
END
GO

-- 7. Stored Procedure: Fetch 24-Hour Logs
IF OBJECT_ID('dbo.Get24HourZoneLogs', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Get24HourZoneLogs;
GO

CREATE PROCEDURE dbo.Get24HourZoneLogs
    @ZoneId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Cutoff DATETIME2 = DATEADD(HOUR, -24, GETDATE());

    SELECT 
        LogId, ZoneId, Temperature, Humidity, VPD, SoilMoisture, 
        SolarRadiation, SoilTemperature, WindSpeed, TimestampIST
    FROM dbo.SensorLogs
    WHERE ZoneId = @ZoneId AND TimestampIST >= @Cutoff
    ORDER BY TimestampIST ASC;
END
GO
