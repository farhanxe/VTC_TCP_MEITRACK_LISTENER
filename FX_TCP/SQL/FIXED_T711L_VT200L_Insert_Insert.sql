-- ============================================================================
--  FIXED STORED PROCEDURES FOR PORT 6065 (T711L) AND PORT 6066 (VT200L)
--  
--  ISSUE FIXED: VehicleTracking table not updating for first-time devices
--  SOLUTION: Added INSERT logic when row doesn't exist, UPDATE when it does
--  
--  Execute this script on [3rdEyE] database
--  Date: 2026-09-30
-- ============================================================================

USE [3rdEyE]
GO

-- ============================================================================
--  T711L_Insert_Insert (PORT 6065)
--  FIXED: Now creates VehicleTracking row if it doesn't exist
-- ============================================================================

IF OBJECT_ID('dbo.T711L_Insert_Insert', 'P') IS NOT NULL
    DROP PROCEDURE dbo.T711L_Insert_Insert
GO

CREATE PROCEDURE [dbo].[T711L_Insert_Insert]
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

    -- ══════════════════════════════════════════════════════════════════════
    --  FIXED: Check if VehicleTracking row exists, INSERT or UPDATE
    -- ══════════════════════════════════════════════════════════════════════
    IF NOT EXISTS (SELECT 1 FROM VehicleTracking WHERE PK_Vehicle = @PK_Vehicle)
    BEGIN
        -- First packet from this device - INSERT new row
        INSERT INTO VehicleTracking (
            PK_Vehicle,
            WillInsert,
            Latitude,
            Longitude,
            Altitude,
            EngineStatus,
            Course,
            Temperature,
            Fuel,
            Speed,
            Distance,
            Mileage,
            EventCode,
            Status_PostionValidity,
            Status_SateliteCount,
            Status_GSMSignalStrength,
            RemainingCash,
            UpdateTime,
            ServerTime
        )
        VALUES (
            @PK_Vehicle,
            1,
            @Latitude,
            @Longitude,
            @Altitude,
            @EngineStatus,
            @Course,
            @Temperature,
            @Fuel,
            @Speed,
            @Distance,
            @Mileage,
            @EventCode,
            @Status_PostionValidity,
            @Status_SateliteCount,
            @Status_GSMSignalStrength,
            @RemainingCash,
            @UpdateTime,
            @Now
        );
    END
    ELSE
    BEGIN
        -- Row exists - UPDATE it
        UPDATE VehicleTracking SET
            WillInsert                  = 1,
            Latitude                    = @Latitude,
            Longitude                   = @Longitude,
            Altitude                    = @Altitude,
            EngineStatus                = @EngineStatus,
            Course                      = @Course,
            Temperature                 = @Temperature,
            Fuel                        = @Fuel,
            Speed                       = @Speed,
            Distance                    = @Distance,
            Mileage                     = ISNULL(@Mileage, Mileage),
            EventCode                   = @EventCode,
            Status_PostionValidity      = @Status_PostionValidity,
            Status_SateliteCount        = @Status_SateliteCount,
            Status_GSMSignalStrength    = @Status_GSMSignalStrength,
            RemainingCash               = @RemainingCash,
            UpdateTime                  = @UpdateTime,
            ServerTime                  = @Now
        WHERE PK_Vehicle = @PK_Vehicle;
    END


    -- Archive to partitioned DeviceData database (10-day windows, 2026)
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

    ELSE IF (@UpdateTime >= '2026-08-21' AND @UpdateTime < '2026-09-01')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_08_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-08-11' AND @UpdateTime < '2026-08-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_08_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-08-01' AND @UpdateTime < '2026-08-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_08_x1].dbo.DeviceData
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
        SELECT 'T711L-InsertInsert-OK-UpdateTime out of range' AS RESPONSE;
        RETURN;
    END

    SELECT 'T711L-InsertInsert-OK' AS RESPONSE;
END
GO


-- ============================================================================
--  VT200L_Insert_Insert (PORT 6066)
--  FIXED: Now creates VehicleTracking row if it doesn't exist
-- ============================================================================

IF OBJECT_ID('dbo.VT200L_Insert_Insert', 'P') IS NOT NULL
    DROP PROCEDURE dbo.VT200L_Insert_Insert
GO

CREATE PROCEDURE [dbo].[VT200L_Insert_Insert]
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

    -- ══════════════════════════════════════════════════════════════════════
    --  FIXED: Check if VehicleTracking row exists, INSERT or UPDATE
    -- ══════════════════════════════════════════════════════════════════════
    IF NOT EXISTS (SELECT 1 FROM VehicleTracking WHERE PK_Vehicle = @PK_Vehicle)
    BEGIN
        -- First packet from this device - INSERT new row
        INSERT INTO VehicleTracking (
            PK_Vehicle,
            WillInsert,
            Latitude,
            Longitude,
            Altitude,
            EngineStatus,
            Course,
            Temperature,
            Fuel,
            Speed,
            Distance,
            Mileage,
            EventCode,
            Status_PostionValidity,
            Status_SateliteCount,
            Status_GSMSignalStrength,
            RemainingCash,
            UpdateTime,
            ServerTime
        )
        VALUES (
            @PK_Vehicle,
            1,
            @Latitude,
            @Longitude,
            @Altitude,
            @EngineStatus,
            @Course,
            @Temperature,
            @Fuel,
            @Speed,
            @Distance,
            @Mileage,
            @EventCode,
            @Status_PostionValidity,
            @Status_SateliteCount,
            @Status_GSMSignalStrength,
            @RemainingCash,
            @UpdateTime,
            @Now
        );
    END
    ELSE
    BEGIN
        -- Row exists - UPDATE it
        UPDATE VehicleTracking SET
            WillInsert                  = 1,
            Latitude                    = @Latitude,
            Longitude                   = @Longitude,
            Altitude                    = @Altitude,
            EngineStatus                = @EngineStatus,
            Course                      = @Course,
            Temperature                 = @Temperature,
            Fuel                        = @Fuel,
            Speed                       = @Speed,
            Distance                    = @Distance,
            Mileage                     = ISNULL(@Mileage, Mileage),
            EventCode                   = @EventCode,
            Status_PostionValidity      = @Status_PostionValidity,
            Status_SateliteCount        = @Status_SateliteCount,
            Status_GSMSignalStrength    = @Status_GSMSignalStrength,
            RemainingCash               = @RemainingCash,
            UpdateTime                  = @UpdateTime,
            ServerTime                  = @Now
        WHERE PK_Vehicle = @PK_Vehicle;
    END


    -- Archive to partitioned DeviceData database (10-day windows, 2026)
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

    ELSE IF (@UpdateTime >= '2026-08-21' AND @UpdateTime < '2026-09-01')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_08_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-08-11' AND @UpdateTime < '2026-08-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_08_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-08-01' AND @UpdateTime < '2026-08-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_08_x1].dbo.DeviceData
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
        SELECT 'VT200L-InsertInsert-OK-UpdateTime out of range' AS RESPONSE;
        RETURN;
    END

    SELECT 'VT200L-InsertInsert-OK' AS RESPONSE;
END
GO


-- ============================================================================
--  VERIFICATION QUERY
--  Run this after executing the script to verify procedures were created
-- ============================================================================
PRINT ''
PRINT '✅ FIXED STORED PROCEDURES CREATED SUCCESSFULLY!'
PRINT ''
PRINT 'Procedures updated:'
PRINT '  • T711L_Insert_Insert (Port 6065)'
PRINT '  • VT200L_Insert_Insert (Port 6066)'
PRINT ''
PRINT 'Change: Added INSERT logic when VehicleTracking row does not exist'
PRINT ''
PRINT 'Verify with:'
PRINT '  SELECT name, modify_date FROM sys.procedures'
PRINT '  WHERE name IN (''T711L_Insert_Insert'', ''VT200L_Insert_Insert'')'
PRINT ''

-- Verification
SELECT 
    name AS ProcedureName,
    modify_date AS LastModified
FROM sys.procedures
WHERE name IN ('T711L_Insert_Insert', 'VT200L_Insert_Insert')
ORDER BY name
GO
