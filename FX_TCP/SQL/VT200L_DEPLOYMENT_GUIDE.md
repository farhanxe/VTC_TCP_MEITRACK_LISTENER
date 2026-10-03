# VT200L Port 6066 Deployment Guide

## Overview
This guide covers the deployment of the new VT200L GPS tracker listener on port 6066.

## Device Model
- **Device**: VT200L GPS Tracker
- **Protocol**: VT200L GPRS Protocol (Standard Version)
- **Port**: 6066
- **Start Delimiter**: `&&` (from tracker to server)
- **Server Reply**: `$$` (from server to tracker)

## Key Features

### 1. Protocol Differences from Existing Ports
- **Port 6062**: Old protocol (legacy devices)
- **Port 6063/6065**: MeiTrack T711L protocol (`$$` delimiter, `,AAA,` marker)
- **Port 6066**: VT200L protocol (`&&` delimiter, requires acknowledgment with same pack-no)

### 2. VT200L Protocol Specifics
```
Format: &&<pack-no><pack-len>,<ID>,<cmd>,<alm-code>,<alm-data>,<date-time>,<fix_flag>,<latitude>,<longitude>,...

Example:
&&<153,868618052108909,000,0,,210526063453,A,19.956285,99.860008,14,1.3,0,61,390,7429,...

Server Reply Format:
$$<pack-no><len>,<IMEI>,<cmd>,1<checksum>\r\n
```

### 3. Data Fields Captured
- **GPS**: Latitude, Longitude, Altitude, Speed, Course
- **Device Status**: Engine Status (derived from system-sta), GSM Signal Strength
- **Sensors**: Temperature (from temp-sensor field), Fuel (from AD1 voltage)
- **Events**: Alarm codes (0=interval, 53=RFID/iButton, etc.)
- **GPS Quality**: Satellite count, HDOP, Position validity

### 4. Engine Status Detection
VT200L uses `system-sta` hex field:
- **Bit 3 = 1**: External power connected (Engine ON)
- Example: `0x000000BC` = Binary `10111100` → Bit 3 = 1 → Engine ON

### 5. Fuel Sensor Parsing
Format: `<ext-V|bat-V|ad1-V|...|adn-V>` (e.g., `0508|01A0|01C8|0000`)
- **AD1 (index 2)** = Fuel sensor voltage in hex
- `0x01C8` = 456 decimal → 456/100 = 4.56V
- **Fuel %** = (4.56V / 5.0V) × 100% = 91.2%
- **Fuel Liters** = (4.56V / 5.0V) × 50L = 45.6 liters
- **Note**: Adjust `MAX_VOLTAGE` and `TANK_CAPACITY_LITERS` constants in code for your sensors

### 6. Temperature Sensor Parsing
Format: `010109` or `010109D9`
- First 2 chars = sensor number (`01` = sensor #1)
- Next 4 chars = hex temperature value (`0109` = 265 decimal)
- **Temperature** = 265 / 10 = 26.5°C
- **Negative temps**: If value > 32767, subtract 65536 (e.g., `0xFFFF` = -0.1°C)

### 7. Special Events
- **Event 0**: Interval report (normal periodic data)
- **Event 53**: RFID/iButton card swipe (includes card data in alm-data field)
- **Event 1**: Ignition ON
- **Event 9**: Ignition OFF

### 8. Server Acknowledgment
VT200L **requires** server acknowledgment with the **same pack-no**:
```
Tracker sends: &&<153,868618052108909,000,...
Server replies: $$<22,868618052108909,000,1<checksum>\r\n
                  ^--- same pack-no
```

## Deployment Steps

### Step 1: Run SQL Script
Execute the stored procedures SQL script on your SQL Server:

```sql
-- Connect to your 3rdEyE database
USE [3rdEyE]
GO

-- Run the script
-- File: FX_TCP/SQL/VT200L_StoredProcedures.sql
```

This creates three stored procedures:
- `VT200L_Insert_Insert` - First packet from new device
- `VT200L_Update_Insert` - Normal update (newer timestamp)
- `VT200L__Insert` - Heartbeat/duplicate (older timestamp)

**Important**: The SQL script includes partitioning for August 2026. If deploying in a different time period, update the date ranges in all three procedures.

### Step 2: Update Database Partitions (if needed)
If you're deploying beyond August 2026, you need to:
1. Create new partitioned databases (e.g., `3rdEyE_TrackingDataBase_2026_09_x1`, `x2`, `x3`)
2. Update the stored procedures with new date ranges
3. Follow the existing pattern in `T711L_StoredProcedures.sql` for reference

### Step 3: Verify Configuration
Check `App.config` for these settings (already added):

```xml
<appSettings>
  <!-- Port number -->
  <add key="PORT_for_6066" value="6066" />
  
  <!-- Distance threshold for location change detection (km) -->
  <add key="Distance_Change_Range_In_KM_6066" value="0.05" />
  
  <!-- UTC offset in seconds (21600 = UTC+6 for Bangladesh) -->
  <add key="UTC_Offset_Seconds_6066" value="21600" />
  
  <!-- Dedup: skip DB if same data within N seconds -->
  <add key="Dedup_Seconds_6066" value="10" />
  
  <!-- Queue batch size for DB writer thread -->
  <add key="Queue_BatchSize_6066" value="50" />
</appSettings>
```

### Step 4: Build and Deploy
1. **Build the solution** in Visual Studio
2. **Test locally** first with a test device
3. **Deploy to production** server

### Step 5: Open Port 6066
Ensure your firewall allows **TCP port 6066** inbound:

```powershell
# Windows Firewall
New-NetFirewallRule -DisplayName "VT200L GPS Port 6066" -Direction Inbound -LocalPort 6066 -Protocol TCP -Action Allow
```

### Step 6: Configure VT200L Devices
Configure your VT200L devices to send data to your server:

**SMS Commands:**
```
# Set server IP and port
100,<server_ip>,6066

# Set reporting interval (e.g., 30 seconds)
1 01,30

# Enable GPS
102,1
```

Refer to VT200L device manual for complete configuration commands.

### Step 7: Start Application
1. Launch the application
2. From the MDI parent form, open **frm6066** form
3. Monitor the connection and packet counters
4. Check the live log for incoming packets

## Monitoring

### UI Elements
The frm6066 form provides:
- **Connection count**: Active TCP connections
- **Packet rate**: Packets/second
- **Queue depth**: Pending DB writes
- **Dedup counter**: Duplicate packets skipped
- **DB latency**: Milliseconds per DB write
- **CPU/Memory**: System resource usage
- **Live log**: Real-time packet parsing with color coding
- **Connection list**: Active devices with IMEI, IP, timestamps

### Log Levels
- **Green**: Database success
- **Cyan**: Parsed packets
- **Orange**: Warnings (disconnections)
- **Red**: Errors (parse failures, DB errors)
- **Gray**: Debug info (ACKs, dedups)

### Key Metrics to Monitor
1. **Parse errors**: Should be 0 or very low
2. **DB errors**: Should be 0
3. **Queue depth**: Should stay under 100 (spikes are normal)
4. **Dedup count**: Depends on device reporting interval
5. **DB latency**: Should be under 200ms (over 500ms = warning)

## Troubleshooting

### Problem: No packets received
**Check:**
- Firewall allows port 6066
- Device configured with correct server IP and port
- Device has internet connectivity
- Form is running and listening

### Problem: Parse errors
**Check:**
- Device firmware version (VT200L protocol may vary slightly)
- Packet format matches expected structure
- Check audit log for actual packet content

### Problem: DB errors
**Check:**
- SQL Server connectivity
- Stored procedures exist
- IMEI is registered in `VehicleTrackingInformation` table
- Date partitions exist for current date range

### Problem: High queue depth
**Check:**
- DB latency (slow database connection)
- Increase `Queue_BatchSize_6066` in config
- Check SQL Server performance

### Problem: Temperature/Fuel showing 0
**Check:**
- Device has temperature/fuel sensors connected
- Sensor wiring correct (refer to VT200L manual)
- Adjust parsing constants in code if sensor specs differ:
  - `MAX_VOLTAGE` (default 5.0V)
  - `TANK_CAPACITY_LITERS` (default 50L)
  - `TEMP_ANALOG_INDEX` (default 2)

## Database Schema

### Tables Used
1. **VehicleTrackingInformation**: Device registration (IMEI → PK_Vehicle mapping)
2. **VehicleTracking**: Live tracking data (one row per vehicle, updated in real-time)
3. **DeviceData** (partitioned): Historical data archive (all packets stored)

### Required Fields in VehicleTrackingInformation
```sql
-- Ensure your devices are registered
SELECT * FROM VehicleTrackingInformation WHERE GpsIMEINumber = '<your_imei>'

-- If not found, insert:
INSERT INTO VehicleTrackingInformation (PK_Vehicle, GpsIMEINumber, ...)
VALUES (NEWID(), '<your_imei>', ...)
```

### Checking Data
```sql
-- Live data
SELECT * FROM VehicleTracking WHERE GpsIMEINumber = '<your_imei>'

-- Historical data (current partition)
SELECT TOP 100 * FROM [3rdEyE_TrackingDataBase_2026_08_x2].dbo.DeviceData
WHERE GpsIMEINumber = '<your_imei>'
ORDER BY UpdateTime DESC
```

## Performance Tuning

### For High Volume (>500 devices)
1. Increase `Queue_BatchSize_6066` to 100 or 200
2. Reduce `Dedup_Seconds_6066` to 30 seconds
3. Monitor CPU/Memory usage
4. Consider multiple listeners on different ports with load balancing

### For Low Latency Requirements
1. Reduce `Dedup_Seconds_6066` to 0 (disabled)
2. Set `Queue_BatchSize_6066` to 10-20
3. Optimize SQL Server indexes on DeviceData tables

## Architecture Notes

### Thread Model
- **Main Thread**: UI updates, metrics display
- **Accept Thread**: TCP connection handling
- **Receive Threads**: One per connected client (async I/O)
- **Writer Thread**: Background DB queue processor (decoupled from TCP)

### Why Background Writer?
The writer thread ensures:
- **TCP never blocks** on DB operations
- **Burst handling**: Can absorb spikes in packet rate
- **Failed DB writes** don't drop TCP connections
- **Better throughput**: Batch processing reduces overhead

### Deduplication Strategy
Dedupe cache tracks per-IMEI:
- Last saved timestamp
- Last position (lat/lon)
- Last engine status
- Last event code
- Last temperature

**Skip DB write** if ALL unchanged within `Dedup_Seconds_6066`.
**Always save** if:
- Vehicle moved beyond `Distance_Change_Range_In_KM_6066`
- Engine status changed
- Temperature changed (>0.1°C)
- Event code changed (e.g., RFID swipe)

## Testing

### Test with Sample Packet
You can use a TCP client (e.g., netcat, telnet, or C# test app) to send a test packet:

```
&&<153,868618052108909,000,0,,210526063453,A,19.956285,99.860008,14,1.3,0,61,390,7429,520|3|3908|02672666,28,000000BC,02,00,0508|01A0|0000|0000,1,010000,010109D9
```

Expected server reply:
```
$$<22,868618052108909,000,16F
```

## Maintenance

### Regular Tasks
1. **Monitor parse/DB errors daily**
2. **Check queue depth during peak hours**
3. **Review audit logs weekly** (AuditLog table)
4. **Archive old DeviceData partitions** (monthly)
5. **Update date partitions** in stored procedures (monthly, before month end)

### Monthly Partition Update
Before each new month, update all three stored procedures with new date ranges. Example for September 2026:

```sql
IF (@UpdateTime >= '2026-09-21')
    INSERT INTO [3rdEyE_TrackingDataBase_2026_09_x3].dbo.DeviceData ...
ELSE IF (@UpdateTime >= '2026-09-11' AND @UpdateTime < '2026-09-21')
    INSERT INTO [3rdEyE_TrackingDataBase_2026_09_x2].dbo.DeviceData ...
-- ... etc
```

## Support

### Audit Logs
All events are logged to `AuditLog` table with codes:
- `6066_PARSED`: Successfully parsed packet
- `6066_DB`: Database write response
- `6066_DB_ERR`: Database error
- `6066_PACKET_ERR`: Parse error
- `6066_FUTURE_FIX`: Device time fixed (was in future)
- `6066_PAST_FIX`: Device time fixed (was >7 days old)
- `SETUP_6066`: Server startup
- `SHUTDOWN_6066`: Server shutdown

### Contact
For protocol questions or customization, refer to:
- VT200L device manual
- Original protocol documentation (included in deployment package)
- Your GPS device vendor support

## Version History
- **v1.0** (2026-08-18): Initial VT200L port 6066 implementation
  - Full VT200L protocol support
  - Temperature and fuel sensor parsing
  - RFID/iButton event handling
  - Background queue writer
  - Per-IMEI deduplication
  - Server acknowledgment with checksum

## License
Internal use only. Part of 3rdEyE Vehicle Tracking System.
