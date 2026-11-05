-- =========================================================================
-- AgriSync India: Automated Climate Monitoring System for Smart Agriculture
-- SQL Server Stored Procedures & Schema Setup
-- =========================================================================

-- Stored Procedure: Fetch 24-Hour Historical Sensor Logs for a Zone
IF OBJECT_ID('dbo.Get24HourZoneLogs', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Get24HourZoneLogs;
GO

CREATE PROCEDURE dbo.Get24HourZoneLogs
    @ZoneId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CutoffTime DATETIME = DATEADD(HOUR, -24, GETDATE());

    SELECT 
        LogId,
        ZoneId,
        Temperature,
        Humidity,
        VPD,
        SoilMoisture,
        SolarRadiation,
        SoilTemperature,
        WindSpeed,
        TimestampIST
    FROM 
        dbo.SensorLogs
    WHERE 
        ZoneId = @ZoneId
        AND TimestampIST >= @CutoffTime
    ORDER BY 
        TimestampIST ASC;
END;
GO

-- Stored Procedure: Fetch Farm-Wide Climate Summary Stats
IF OBJECT_ID('dbo.GetAggregatedClimateStats', 'P') IS NOT NULL
    DROP PROCEDURE dbo.GetAggregatedClimateStats;
GO

CREATE PROCEDURE dbo.GetAggregatedClimateStats
    @ZoneId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CutoffTime DATETIME = DATEADD(HOUR, -24, GETDATE());

    SELECT 
        ZoneId,
        AVG(Temperature) AS AvgTemperature,
        MIN(Temperature) AS MinTemperature,
        MAX(Temperature) AS MaxTemperature,
        AVG(Humidity) AS AvgHumidity,
        AVG(VPD) AS AvgVPD,
        AVG(SoilMoisture) AS AvgSoilMoisture
    FROM 
        dbo.SensorLogs
    WHERE 
        (@ZoneId IS NULL OR ZoneId = @ZoneId)
        AND TimestampIST >= @CutoffTime
    GROUP BY 
        ZoneId;
END;
GO
