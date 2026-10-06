-- ============================================================================
--  TIMESTAMP FIX FOR T711L AND VT200L STORED PROCEDURES
--  
--  ISSUE: Device GPS time can be wrong (e.g., 23:59 when server time is 14:40)
--  SOLUTION: Check if UpdateTime is >5 minutes in future, replace with ServerTime
--  
--  Execute this script on [3rdEyE] database
--  Date: 2026-10-04
-- ============================================================================

USE [3rdEyE]
GO

PRINT '================================================================'
PRINT 'FIXING T711L_Insert_Insert WITH TIMESTAMP VALIDATION'
PRINT '================================================================'
GO

ALTER PROCEDURE [dbo].[T711L_Insert_Insert]
(
    @GpsIMEINumber          varchar(50),
    @UpdateTime             datetime,
    @Latitude               decimal(18,6),
    @Longitude              decimal(18,6),
    @Altitude               decimal(18,2)   = 0,
    @EngineStatus           varchar(5)      = '0',
    @Course                 decimal(18,2)   = 0,
    @Temperature            decimal(18,2)   = 0,
    @Fuel                   decimal(18,2)   = 0,
    @Speed                  decimal(18,2)   = 0,
    @Distance               decimal(18,5)   = 0,
    @Mileage                decimal(18,0)   = NULL,
    @EventCode              varchar(5)      = '0',
    @Status_PostionValidity varchar(1)      = 'V',
    @Status_SateliteCount   int             = 0,
    @Status_GSMSignalStrength int           = 0,
    @RemainingCash          int             = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PK_Vehicle uniqueidentifier =
        (SELECT TOP 1 PK_Vehicle
         FROM   VehicleTrackingInformation
         WHERE  GpsIMEINumber = @GpsIMEINumber);

    IF @PK_Vehicle IS NULL
    BEGIN
        SELECT 'T711L-InsertInsert-No vehicle found for IMEI: ' + @GpsIMEINumber AS RESPONSE;
        RETURN;
    END

    DECLARE @Now datetime = GETDATE();
    DECLARE @OriginalUpdateTime datetime = @UpdateTime;

    -- ══════════════════════════════════════════════════════════════════════
    --  TIMESTAMP FIX: If device time is >5 minutes in future, use server time
    --  Example: Device sends 23:59:59 when server time is 14:40:00
    --  This is 9+ hours in future → replace with server time
    -- ══════════════════════════════════════════════════════════════════════
    IF @UpdateTime > DATEADD(MINUTE, 5, @Now)
    BEGIN
        -- Device clock is WRONG (future time) - use server time instead
        SET @UpdateTime = @Now;
        
        -- Log the fix for audit
        PRINT 'T711L TIMESTAMP FIX: IMEI=' + @GpsIMEINumber + 
              ' DeviceTime=' + CONVERT(varchar, @OriginalUpdateTime, 120) +
              ' → ServerTime=' + CONVERT(varchar, @Now, 120);
    END

    -- Check if UpdateTime is way in the past (>7 days old)
    IF @UpdateTime < DATEADD(DAY, -7, @Now)
    BEGIN
        -- Device date is way off in past - use server time
        SET @UpdateTime = @Now;
        
        PRINT 'T711L PAST DATE FIX: IMEI=' + @GpsIMEINumber +
              ' DeviceTime=' + CONVERT(varchar, @OriginalUpdateTime, 120) +
              ' → ServerTime=' + CONVERT(varchar, @Now, 120);
    END

    -- ══════════════════════════════════════════════════════════════════════
    --  VehicleTracking table: INSERT or UPDATE
    -- ══════════════════════════════════════════════════════════════════════
    IF NOT EXISTS (SELECT 1 FROM VehicleTracking WHERE PK_Vehicle = @PK_Vehicle)
    BEGIN
        INSERT INTO VehicleTracking (
            PK_Vehicle, WillInsert, Latitude, Longitude, Altitude,
            EngineStatus, Course, Temperature, Fuel, Speed, Distance, Mileage,
            EventCode, Status_PostionValidity, Status_SateliteCount,
            Status_GSMSignalStrength, RemainingCash, UpdateTime, ServerTime
        )
        VALUES (
            @PK_Vehicle, 1, @Latitude, @Longitude, @Altitude,
            @EngineStatus, @Course, @Temperature, @Fuel, @Speed, @Distance, @Mileage,
            @EventCode, @Status_PostionValidity, @Status_SateliteCount,
            @Status_GSMSignalStrength, @RemainingCash, @UpdateTime, @Now
        );
    END
    ELSE
    BEGIN
        UPDATE VehicleTracking SET
            WillInsert = 1, Latitude = @Latitude, Longitude = @Longitude, Altitude = @Altitude,
            EngineStatus = @EngineStatus, Course = @Course, Temperature = @Temperature,
            Fuel = @Fuel, Speed = @Speed, Distance = @Distance, Mileage = ISNULL(@Mileage, Mileage),
            EventCode = @EventCode, Status_PostionValidity = @Status_PostionValidity,
            Status_SateliteCount = @Status_SateliteCount,
            Status_GSMSignalStrength = @Status_GSMSignalStrength,
            RemainingCash = @RemainingCash, UpdateTime = @UpdateTime, ServerTime = @Now
        WHERE PK_Vehicle = @PK_Vehicle;
    END

    -- ══════════════════════════════════════════════════════════════════════
    --  Archive to partitioned DeviceData (using FIXED UpdateTime)
    -- ══════════════════════════════════════════════════════════════════════
    IF      (@UpdateTime >= '2026-10-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_10_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE IF (@UpdateTime >= '2026-10-11' AND @UpdateTime < '2026-10-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_10_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE IF (@UpdateTime >= '2026-10-01' AND @UpdateTime < '2026-10-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_10_x1].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE IF (@UpdateTime >= '2026-09-21' AND @UpdateTime < '2026-10-01')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_09_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE IF (@UpdateTime >= '2026-09-11' AND @UpdateTime < '2026-09-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_09_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE IF (@UpdateTime >= '2026-09-01' AND @UpdateTime < '2026-09-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_09_x1].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE
    BEGIN
        SELECT 'T711L-InsertInsert-OK-UpdateTime out of range (' + CONVERT(varchar, @UpdateTime, 120) + ')' AS RESPONSE;
        RETURN;
    END

    SELECT 'T711L-InsertInsert-OK' AS RESPONSE;
END
GO

PRINT '✅ T711L_Insert_Insert FIXED!'
GO

-- ============================================================================
--  VT200L_Insert_Insert FIX
-- ============================================================================

PRINT '================================================================'
PRINT 'FIXING VT200L_Insert_Insert WITH TIMESTAMP VALIDATION'
PRINT '================================================================'
GO

ALTER PROCEDURE [dbo].[VT200L_Insert_Insert]
(
    @GpsIMEINumber          varchar(50),
    @UpdateTime             datetime,
    @Latitude               decimal(18,6),
    @Longitude              decimal(18,6),
    @Altitude               decimal(18,2)   = 0,
    @EngineStatus           varchar(5)      = '0',
    @Course                 decimal(18,2)   = 0,
    @Temperature            decimal(18,2)   = 0,
    @Fuel                   decimal(18,2)   = 0,
    @Speed                  decimal(18,2)   = 0,
    @Distance               decimal(18,5)   = 0,
    @Mileage                decimal(18,0)   = NULL,
    @EventCode              varchar(5)      = '0',
    @Status_PostionValidity varchar(1)      = 'V',
    @Status_SateliteCount   int             = 0,
    @Status_GSMSignalStrength int           = 0,
    @RemainingCash          int             = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PK_Vehicle uniqueidentifier =
        (SELECT TOP 1 PK_Vehicle
         FROM   VehicleTrackingInformation
         WHERE  GpsIMEINumber = @GpsIMEINumber);

    IF @PK_Vehicle IS NULL
    BEGIN
        SELECT 'VT200L-InsertInsert-No vehicle found for IMEI: ' + @GpsIMEINumber AS RESPONSE;
        RETURN;
    END

    DECLARE @Now datetime = GETDATE();
    DECLARE @OriginalUpdateTime datetime = @UpdateTime;

    -- ══════════════════════════════════════════════════════════════════════
    --  TIMESTAMP FIX: If device time is >5 minutes in future, use server time
    -- ══════════════════════════════════════════════════════════════════════
    IF @UpdateTime > DATEADD(MINUTE, 5, @Now)
    BEGIN
        SET @UpdateTime = @Now;
        
        PRINT 'VT200L TIMESTAMP FIX: IMEI=' + @GpsIMEINumber + 
              ' DeviceTime=' + CONVERT(varchar, @OriginalUpdateTime, 120) +
              ' → ServerTime=' + CONVERT(varchar, @Now, 120);
    END

    IF @UpdateTime < DATEADD(DAY, -7, @Now)
    BEGIN
        SET @UpdateTime = @Now;
        
        PRINT 'VT200L PAST DATE FIX: IMEI=' + @GpsIMEINumber +
              ' DeviceTime=' + CONVERT(varchar, @OriginalUpdateTime, 120) +
              ' → ServerTime=' + CONVERT(varchar, @Now, 120);
    END

    -- ══════════════════════════════════════════════════════════════════════
    --  VehicleTracking table: INSERT or UPDATE
    -- ══════════════════════════════════════════════════════════════════════
    IF NOT EXISTS (SELECT 1 FROM VehicleTracking WHERE PK_Vehicle = @PK_Vehicle)
    BEGIN
        INSERT INTO VehicleTracking (
            PK_Vehicle, WillInsert, Latitude, Longitude, Altitude,
            EngineStatus, Course, Temperature, Fuel, Speed, Distance, Mileage,
            EventCode, Status_PostionValidity, Status_SateliteCount,
            Status_GSMSignalStrength, RemainingCash, UpdateTime, ServerTime
        )
        VALUES (
            @PK_Vehicle, 1, @Latitude, @Longitude, @Altitude,
            @EngineStatus, @Course, @Temperature, @Fuel, @Speed, @Distance, @Mileage,
            @EventCode, @Status_PostionValidity, @Status_SateliteCount,
            @Status_GSMSignalStrength, @RemainingCash, @UpdateTime, @Now
        );
    END
    ELSE
    BEGIN
        UPDATE VehicleTracking SET
            WillInsert = 1, Latitude = @Latitude, Longitude = @Longitude, Altitude = @Altitude,
            EngineStatus = @EngineStatus, Course = @Course, Temperature = @Temperature,
            Fuel = @Fuel, Speed = @Speed, Distance = @Distance, Mileage = ISNULL(@Mileage, Mileage),
            EventCode = @EventCode, Status_PostionValidity = @Status_PostionValidity,
            Status_SateliteCount = @Status_SateliteCount,
            Status_GSMSignalStrength = @Status_GSMSignalStrength,
            RemainingCash = @RemainingCash, UpdateTime = @UpdateTime, ServerTime = @Now
        WHERE PK_Vehicle = @PK_Vehicle;
    END

    -- ══════════════════════════════════════════════════════════════════════
    --  Archive to partitioned DeviceData (using FIXED UpdateTime)
    -- ══════════════════════════════════════════════════════════════════════
    IF      (@UpdateTime >= '2026-10-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_10_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE IF (@UpdateTime >= '2026-10-11' AND @UpdateTime < '2026-10-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_10_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE IF (@UpdateTime >= '2026-10-01' AND @UpdateTime < '2026-10-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_10_x1].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE IF (@UpdateTime >= '2026-09-21' AND @UpdateTime < '2026-10-01')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_09_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE IF (@UpdateTime >= '2026-09-11' AND @UpdateTime < '2026-09-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_09_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE IF (@UpdateTime >= '2026-09-01' AND @UpdateTime < '2026-09-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_09_x1].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);
    ELSE
    BEGIN
        SELECT 'VT200L-InsertInsert-OK-UpdateTime out of range (' + CONVERT(varchar, @UpdateTime, 120) + ')' AS RESPONSE;
        RETURN;
    END

    SELECT 'VT200L-InsertInsert-OK' AS RESPONSE;
END
GO

PRINT '✅ VT200L_Insert_Insert FIXED!'
GO

-- ============================================================================
--  VERIFICATION
-- ============================================================================
PRINT ''
PRINT '================================================================'
PRINT '✅ TIMESTAMP VALIDATION ADDED TO BOTH STORED PROCEDURES!'
PRINT '================================================================'
PRINT ''
PRINT 'FIXED:'
PRINT '  • T711L_Insert_Insert - Now validates UpdateTime before storing'
PRINT '  • VT200L_Insert_Insert - Now validates UpdateTime before storing'
PRINT ''
PRINT 'LOGIC:'
PRINT '  • If device time > 5 minutes in future → use server time'
PRINT '  • If device time > 7 days in past → use server time'
PRINT '  • Otherwise use device time as-is'
PRINT ''
PRINT 'EXAMPLE:'
PRINT '  Device sends: 2026-10-04 23:59:59 (11:59 PM)'
PRINT '  Server time:  2026-10-04 14:40:00 (2:40 PM)'
PRINT '  Stored as:    2026-10-04 14:40:00 (server time used!)'
PRINT ''

SELECT 
    name AS ProcedureName,
    modify_date AS LastModified
FROM sys.procedures
WHERE name IN ('T711L_Insert_Insert', 'VT200L_Insert_Insert')
ORDER BY name
GO
