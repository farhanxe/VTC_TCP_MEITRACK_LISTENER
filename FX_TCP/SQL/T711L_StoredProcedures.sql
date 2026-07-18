USE [3rdEyE]
GO

-- ============================================================
--  T711L_Insert_Insert
--  Called when: Device IMEI exists in VehicleTrackingInformation
--               but VehicleTracking row needs first-time population
--               OR as the new-device first-packet handler for 6063.
--  Params: clean, strongly-typed — no varchar abuse for numbers.
-- ============================================================
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

    -- Update live-tracking row (single row per vehicle)
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


    -- Archive to partitioned DeviceData database (10-day windows, June 2026)
    IF      (@UpdateTime >= '2026-06-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_06_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-06-11' AND @UpdateTime < '2026-06-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_06_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-06-01' AND @UpdateTime < '2026-06-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_06_x1].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-05-21' AND @UpdateTime < '2026-06-01')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_05_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-05-11' AND @UpdateTime < '2026-05-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_05_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-05-01' AND @UpdateTime < '2026-05-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_05_x1].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-04-21' AND @UpdateTime < '2026-05-01')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_04_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-04-11' AND @UpdateTime < '2026-04-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_04_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-04-01' AND @UpdateTime < '2026-04-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_04_x1].dbo.DeviceData
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


-- ============================================================
--  T711L_Update_Insert
--  Called when: newer packet arrives for a known vehicle.
--  Updates VehicleTracking live row + archives to DeviceData.
--  @IsLocationChanged = 1 also updates Location_ChangedAt.
-- ============================================================
IF OBJECT_ID('dbo.T711L_Update_Insert', 'P') IS NOT NULL
    DROP PROCEDURE dbo.T711L_Update_Insert
GO

CREATE PROCEDURE [dbo].[T711L_Update_Insert]
(
    @PK_Vehicle             uniqueidentifier,
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
    @RemainingCash          int             = 0,
    @IsLocationChanged      bit             = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now datetime = GETDATE();

    -- ── Update VehicleTracking live row ────────────────────────────────────
    -- Position is only updated when GPS is valid (A or 1)
    IF (@Status_PostionValidity = 'A' OR @Status_PostionValidity = '1')
    BEGIN
        IF @IsLocationChanged = 1
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
                ServerTime                  = @Now,
                Location_ChangedAt          = @UpdateTime
            WHERE PK_Vehicle = @PK_Vehicle;
        ELSE
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
    ELSE
    BEGIN
        -- GPS invalid: update everything except lat/lon
        UPDATE VehicleTracking SET
            WillInsert                  = 1,
            Altitude                    = @Altitude,
            EngineStatus                = @EngineStatus,
            Course                      = @Course,
            Temperature                 = @Temperature,
            Fuel                        = @Fuel,
            Speed                       = @Speed,
            Distance                    = @Distance,
            EventCode                   = @EventCode,
            Status_PostionValidity      = @Status_PostionValidity,
            Status_SateliteCount        = @Status_SateliteCount,
            Status_GSMSignalStrength    = @Status_GSMSignalStrength,
            RemainingCash               = @RemainingCash,
            UpdateTime                  = @UpdateTime,
            ServerTime                  = @Now
        WHERE PK_Vehicle = @PK_Vehicle;
    END


    -- ── Archive to partitioned DeviceData ─────────────────────────────────
    IF      (@UpdateTime >= '2026-06-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_06_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-06-11' AND @UpdateTime < '2026-06-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_06_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-06-01' AND @UpdateTime < '2026-06-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_06_x1].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-05-21' AND @UpdateTime < '2026-06-01')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_05_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-05-11' AND @UpdateTime < '2026-05-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_05_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-05-01' AND @UpdateTime < '2026-05-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_05_x1].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-04-21' AND @UpdateTime < '2026-05-01')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_04_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-04-11' AND @UpdateTime < '2026-04-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_04_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-04-01' AND @UpdateTime < '2026-04-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_04_x1].dbo.DeviceData
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
        SELECT 'T711L-UpdateInsert-OK-UpdateTime out of range' AS RESPONSE;
        RETURN;
    END

    SELECT 'T711L-UpdateInsert-OK' AS RESPONSE;
END
GO


-- ============================================================
--  T711L__Insert   (double underscore — heartbeat/duplicate)
--  Called when: packet timestamp <= stored UpdateTime.
--  Does NOT update VehicleTracking live row.
--  Archives to DeviceData only (keeps full history).
-- ============================================================
IF OBJECT_ID('dbo.T711L__Insert', 'P') IS NOT NULL
    DROP PROCEDURE dbo.T711L__Insert
GO

CREATE PROCEDURE [dbo].[T711L__Insert]
(
    @PK_Vehicle             uniqueidentifier,
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

    DECLARE @Now datetime = GETDATE();

    -- Archive to partitioned DeviceData only (no live-row update)
    IF      (@UpdateTime >= '2026-06-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_06_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-06-11' AND @UpdateTime < '2026-06-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_06_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-06-01' AND @UpdateTime < '2026-06-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_06_x1].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-05-21' AND @UpdateTime < '2026-06-01')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_05_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-05-11' AND @UpdateTime < '2026-05-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_05_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-05-01' AND @UpdateTime < '2026-05-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_05_x1].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-04-21' AND @UpdateTime < '2026-05-01')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_04_x3].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-04-11' AND @UpdateTime < '2026-04-21')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_04_x2].dbo.DeviceData
            (FK_Vehicle,GpsIMEINumber,Latitude,Longitude,Altitude,EngineStatus,Course,
             Temperature,Fuel,Speed,Distance,EventCode,RemainingCash,
             Status_PostionValidity,Status_SateliteCount,Status_GSMSignalStrength,
             Mileage,UpdateTime,ServerTime)
        VALUES
            (@PK_Vehicle,@GpsIMEINumber,@Latitude,@Longitude,@Altitude,@EngineStatus,@Course,
             @Temperature,@Fuel,@Speed,@Distance,@EventCode,@RemainingCash,
             @Status_PostionValidity,@Status_SateliteCount,@Status_GSMSignalStrength,
             @Mileage,@UpdateTime,@Now);

    ELSE IF (@UpdateTime >= '2026-04-01' AND @UpdateTime < '2026-04-11')
        INSERT INTO [3rdEyE_TrackingDataBase_2026_04_x1].dbo.DeviceData
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
        SELECT 'T711L-_Insert-OK-UpdateTime out of range' AS RESPONSE;
        RETURN;
    END

    SELECT 'T711L-_Insert-OK' AS RESPONSE;
END
GO
