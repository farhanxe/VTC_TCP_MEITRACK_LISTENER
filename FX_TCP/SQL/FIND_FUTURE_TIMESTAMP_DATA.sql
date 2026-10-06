-- ============================================================================
--  FIND DATA WITH WRONG TIMESTAMPS (FUTURE TIMES)
--  
--  This script finds all records where device UpdateTime is in the FUTURE
--  compared to when they were actually stored (ServerTime)
--  
--  Execute on [3rdEyE] database
-- ============================================================================

USE [3rdEyE]
GO

PRINT '================================================================'
PRINT 'CHECKING VehicleTracking TABLE FOR FUTURE TIMESTAMPS'
PRINT '================================================================'
PRINT ''

-- ============================================================================
--  1. VehicleTracking Table - Records with Future UpdateTime
-- ============================================================================
SELECT 
    PK_Vehicle,
    GpsIMEINumber,
    UpdateTime AS DeviceTime,
    ServerTime AS ActualTime,
    DATEDIFF(HOUR, ServerTime, UpdateTime) AS HoursDifference,
    CASE 
        WHEN UpdateTime > DATEADD(HOUR, 1, ServerTime) THEN '🔴 FUTURE TIME'
        ELSE '✅ OK'
    END AS Status,
    Latitude,
    Longitude,
    EngineStatus,
    Speed
FROM VehicleTracking
WHERE UpdateTime > DATEADD(MINUTE, 5, ServerTime)  -- Device time >5 min ahead of server
ORDER BY DATEDIFF(MINUTE, ServerTime, UpdateTime) DESC;

PRINT ''
PRINT 'Found records in VehicleTracking where UpdateTime is in the FUTURE'
PRINT ''

-- ============================================================================
--  2. DeviceData Table - TODAY's Records with Future Timestamps
-- ============================================================================
PRINT '================================================================'
PRINT 'CHECKING DeviceData TABLE (TODAY) FOR FUTURE TIMESTAMPS'
PRINT '================================================================'
PRINT ''

SELECT TOP 100
    PK_RowData,
    FK_Vehicle,
    GpsIMEINumber,
    UpdateTime AS DeviceTime,
    ServerTime AS ActualTime,
    DATEDIFF(HOUR, ServerTime, UpdateTime) AS HoursDifference,
    CASE 
        WHEN UpdateTime > DATEADD(HOUR, 1, ServerTime) THEN '🔴 FUTURE TIME'
        ELSE '✅ OK'
    END AS Status,
    Latitude,
    Longitude,
    EngineStatus,
    Speed
FROM [3rdEyE_TrackingDataBase_2026_10_x1].dbo.DeviceData
WHERE 
    CAST(ServerTime AS DATE) = CAST(GETDATE() AS DATE)  -- Only today
    AND UpdateTime > DATEADD(MINUTE, 5, ServerTime)     -- Future timestamps
ORDER BY ServerTime DESC;

PRINT ''
PRINT 'Found TODAY records in DeviceData where UpdateTime is in the FUTURE'
PRINT ''

-- ============================================================================
--  3. Count by IMEI - Which devices have wrong clocks?
-- ============================================================================
PRINT '================================================================'
PRINT 'DEVICES WITH WRONG CLOCK (Grouped by IMEI)'
PRINT '================================================================'
PRINT ''

SELECT 
    GpsIMEINumber,
    COUNT(*) AS WrongTimestampCount,
    MIN(DATEDIFF(HOUR, ServerTime, UpdateTime)) AS MinHoursDiff,
    MAX(DATEDIFF(HOUR, ServerTime, UpdateTime)) AS MaxHoursDiff,
    AVG(DATEDIFF(HOUR, ServerTime, UpdateTime)) AS AvgHoursDiff,
    MIN(ServerTime) AS FirstSeen,
    MAX(ServerTime) AS LastSeen
FROM [3rdEyE_TrackingDataBase_2026_10_x1].dbo.DeviceData
WHERE 
    CAST(ServerTime AS DATE) = CAST(GETDATE() AS DATE)  -- Only today
    AND UpdateTime > DATEADD(MINUTE, 5, ServerTime)     -- Future timestamps
GROUP BY GpsIMEINumber
ORDER BY WrongTimestampCount DESC;

PRINT ''
PRINT 'Shows which devices have the most wrong timestamps TODAY'
PRINT ''

-- ============================================================================
--  4. SPECIFIC IMEI CHECK (Replace with your IMEI)
-- ============================================================================
PRINT '================================================================'
PRINT 'CHECKING SPECIFIC IMEI: 861490079986445'
PRINT '================================================================'
PRINT ''

DECLARE @TargetIMEI varchar(50) = '861490079986445';

SELECT TOP 20
    PK_RowData,
    GpsIMEINumber,
    UpdateTime AS DeviceTime,
    ServerTime AS ActualTime,
    DATEDIFF(MINUTE, ServerTime, UpdateTime) AS MinutesDifference,
    CASE 
        WHEN UpdateTime > DATEADD(HOUR, 8, ServerTime) THEN '🔴 9+ HOURS AHEAD'
        WHEN UpdateTime > DATEADD(HOUR, 1, ServerTime) THEN '🟡 1-9 HOURS AHEAD'
        WHEN UpdateTime > DATEADD(MINUTE, 5, ServerTime) THEN '🟠 5+ MIN AHEAD'
        ELSE '✅ CORRECT'
    END AS Status,
    Latitude,
    Longitude,
    EngineStatus,
    Speed,
    EventCode
FROM [3rdEyE_TrackingDataBase_2026_10_x1].dbo.DeviceData
WHERE GpsIMEINumber = @TargetIMEI
ORDER BY ServerTime DESC;

PRINT ''
PRINT 'Shows last 20 records for specific IMEI'
PRINT ''

-- ============================================================================
--  5. SUMMARY STATISTICS
-- ============================================================================
PRINT '================================================================'
PRINT 'SUMMARY STATISTICS (TODAY)'
PRINT '================================================================'
PRINT ''

SELECT 
    'Total Today Records' AS Metric,
    COUNT(*) AS Count
FROM [3rdEyE_TrackingDataBase_2026_10_x1].dbo.DeviceData
WHERE CAST(ServerTime AS DATE) = CAST(GETDATE() AS DATE)

UNION ALL

SELECT 
    'Records with Future Time' AS Metric,
    COUNT(*) AS Count
FROM [3rdEyE_TrackingDataBase_2026_10_x1].dbo.DeviceData
WHERE 
    CAST(ServerTime AS DATE) = CAST(GETDATE() AS DATE)
    AND UpdateTime > DATEADD(MINUTE, 5, ServerTime)

UNION ALL

SELECT 
    'Records with Correct Time' AS Metric,
    COUNT(*) AS Count
FROM [3rdEyE_TrackingDataBase_2026_10_x1].dbo.DeviceData
WHERE 
    CAST(ServerTime AS DATE) = CAST(GETDATE() AS DATE)
    AND UpdateTime <= DATEADD(MINUTE, 5, ServerTime)

UNION ALL

SELECT 
    'Affected Devices (IMEI Count)' AS Metric,
    COUNT(DISTINCT GpsIMEINumber) AS Count
FROM [3rdEyE_TrackingDataBase_2026_10_x1].dbo.DeviceData
WHERE 
    CAST(ServerTime AS DATE) = CAST(GETDATE() AS DATE)
    AND UpdateTime > DATEADD(MINUTE, 5, ServerTime);

PRINT ''
PRINT '================================================================'
PRINT '✅ QUERY COMPLETE'
PRINT '================================================================'
PRINT ''
PRINT 'INTERPRETATION:'
PRINT '  🔴 FUTURE TIME   = UpdateTime is >1 hour ahead of ServerTime (WRONG!)'
PRINT '  🟡 1-9 HOURS     = Device clock is several hours ahead'
PRINT '  🟠 5+ MIN AHEAD  = Device clock is slightly ahead'
PRINT '  ✅ CORRECT       = UpdateTime matches ServerTime (±5 min)'
PRINT ''
PRINT 'ACTION REQUIRED:'
PRINT '  1. Execute TIMESTAMP_FIX_STORED_PROCEDURES.sql to prevent future bad data'
PRINT '  2. Consider fixing existing bad data (UPDATE UpdateTime = ServerTime WHERE...)'
PRINT ''

-- ============================================================================
--  6. OPTIONAL: FIX EXISTING DATA (UNCOMMENT TO RUN)
-- ============================================================================
/*
PRINT '================================================================'
PRINT '⚠️  FIXING EXISTING DATA IN VehicleTracking'
PRINT '================================================================'
PRINT ''

-- Backup first!
SELECT * 
INTO VehicleTracking_Backup_Before_TimestampFix
FROM VehicleTracking
WHERE UpdateTime > DATEADD(MINUTE, 5, ServerTime);

PRINT 'Backup created: VehicleTracking_Backup_Before_TimestampFix'
PRINT ''

-- Fix the data
UPDATE VehicleTracking
SET UpdateTime = ServerTime
WHERE UpdateTime > DATEADD(MINUTE, 5, ServerTime);

PRINT 'VehicleTracking table FIXED: UpdateTime set to ServerTime for future timestamps'
PRINT ''
PRINT '⚠️  NOTE: DeviceData historical tables NOT modified (too many records)'
PRINT '   Only live tracking table (VehicleTracking) was fixed'
PRINT ''
*/

GO
