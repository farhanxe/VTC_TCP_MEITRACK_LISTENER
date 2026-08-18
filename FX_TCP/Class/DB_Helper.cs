using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FX_TCP.Class;

namespace FX_TCP
{

    public class Tracking_Data
    {
        public Guid PK_Vehicle { get; set; }
        public string GpsIMEINumber { get; set; }
        public Nullable<bool> Internal_ShowTemperature { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public string EngineStatus { get; set; }
        public double Course { get; set; }
        public double Temperature { get; set; }
        public double Fuel { get; set; }
        public double Speed { get; set; }
        public decimal Distance { get; set; }
        public DateTime UpdateTime { get; set; }
        public DateTime ServerTime { get; set; }
        public Nullable<int> RemainingCash { get; set; }
    }



    public class DB_Helper_Data
    {

        public string GpsIMEINumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public string EngineStatus { get; set; }
        public double Course { get; set; }
        public double Temperature { get; set; }
        public double Fuel { get; set; }
        public double Speed { get; set; }
        public string Distance { get; set; }
        public string Status_PostionValidity { get; set; }
        public int Status_SateliteCount { get; set; }
        public int Status_GSMSignalStrength { get; set; }
        public int RemainingCash { get; set; }
        public DateTime UpdateTime { get; set; }

        public string EventCode { get; set; }
    }

    public class DB_Helper : IDisposable
    {
        VTS_Entities db = new VTS_Entities();


        public string PushDeviceData_PORT_6062(DB_Helper_Data new_data)
        {

            if (new_data.UpdateTime > DateTime.Now.AddMinutes(5))
            {
                return "Got Invalid Updatetime from device GpsIMEINumber: " + new_data.GpsIMEINumber.ToString() + ". UpdateTime : " + new_data.UpdateTime;
            }


            try
            {
                #region AuditLog
                AuditLog.auditLog((JsonConvert.SerializeObject(new_data)).ToString(), "12");
                #endregion
            }
            catch (Exception ex)
            {

            }

            try
            {
                string reponseFromProcedure = "";
                //double _distance = 0;
                new_data.RemainingCash = 0;
                var old_data = (from v in db.VehicleTrackingInformations.Where(v => v.GpsIMEINumber == new_data.GpsIMEINumber)
                                join vt in db.VehicleTrackings on v.PK_Vehicle equals vt.PK_Vehicle
                                select new
                                {
                                    v.PK_Vehicle,
                                    v.Internal_ShowTemperature,
                                    vt.Latitude,
                                    vt.Longitude,
                                    vt.Altitude,
                                    vt.EngineStatus,
                                    vt.Course,
                                    vt.Temperature,
                                    vt.Fuel,
                                    vt.Speed,
                                    vt.Distance,
                                    vt.UpdateTime,
                                    vt.ServerTime
                                }).FirstOrDefault();
                if (old_data == null)
                {
                    try
                    {
                        reponseFromProcedure = reponseFromProcedure + db.Database.SqlQuery<string>("EXEC dbo.PushDeviceData_Insert_Insert '" + new_data.GpsIMEINumber + "','" + new_data.UpdateTime.ToString() + "','" + new_data.Latitude + "','" + new_data.Longitude + "','" + new_data.Altitude + "','" + new_data.EngineStatus + "','" + new_data.Course + "','" + new_data.Temperature + "','" + new_data.Fuel + "','" + new_data.Speed + "','" + new_data.Distance + "','" + new_data.EventCode + "','" + new_data.Status_PostionValidity + "','" + new_data.Status_SateliteCount + "','" + new_data.Status_GSMSignalStrength + "','" + new_data.RemainingCash + "'").FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        #region AuditLog
                        AuditLog.auditLog(ex.Message, "103");
                        #endregion
                    }
                }
                else
                {
                    if (new_data.UpdateTime > old_data.UpdateTime)
                    {
                        //# Temperature Calculation
                        //if (old_data.Internal_ShowTemperature == true)
                        //{
                        //    if (new_data.Temperature == 0)
                        //    {
                        //        new_data.Temperature = old_data.Temperature;
                        //    }
                        //    else if (new_data.Temperature > 50)
                        //    {
                        //        new_data.Temperature = old_data.Temperature;
                        //    }
                        //    else
                        //    {
                        //        //# keep as it is
                        //    }
                        //}
                        //else
                        //{
                        //    new_data.Temperature = 0;
                        //}

                        //reponseFromProcedure = reponseFromProcedure + db.Database.SqlQuery<string>("EXEC dbo.PushDeviceData_Update_Insert '" + old_data.PK_Vehicle + "','" + new_data.GpsIMEINumber + "','" + new_data.UpdateTime.ToString() + "','" + new_data.Latitude + "','" + new_data.Longitude + "','" + new_data.Altitude + "','" + new_data.EngineStatus + "','" + new_data.Course + "','" + new_data.Temperature + "','" + new_data.Fuel + "','" + new_data.Speed + "','" + new_data.Distance + "','" + new_data.EventCode + "','" + new_data.Status_PostionValidity + "','" + new_data.Status_SateliteCount + "','" + new_data.Status_GSMSignalStrength + "','" + new_data.RemainingCash + "'").FirstOrDefault();
                        if (distanceInKmBetweenEarthCoordinates(old_data.Latitude, old_data.Longitude, new_data.Latitude, new_data.Longitude) > CommonClass.Distance_Change_Range_In_KM_6062)
                        {
                            reponseFromProcedure = reponseFromProcedure + db.Database.SqlQuery<string>("EXEC dbo.PushDeviceData_Update_Insert '" + old_data.PK_Vehicle + "','" + new_data.GpsIMEINumber + "','" + new_data.UpdateTime.ToString() + "','" + new_data.Latitude + "','" + new_data.Longitude + "','" + new_data.Altitude + "','" + new_data.EngineStatus + "','" + new_data.Course + "','" + new_data.Temperature + "','" + new_data.Fuel + "','" + new_data.Speed + "','" + new_data.Distance + "','" + new_data.EventCode + "','" + new_data.Status_PostionValidity + "','" + new_data.Status_SateliteCount + "','" + new_data.Status_GSMSignalStrength + "','" + new_data.RemainingCash + "'," + 1).FirstOrDefault();
                        }
                        else
                        {
                            reponseFromProcedure = reponseFromProcedure + db.Database.SqlQuery<string>("EXEC dbo.PushDeviceData_Update_Insert '" + old_data.PK_Vehicle + "','" + new_data.GpsIMEINumber + "','" + new_data.UpdateTime.ToString() + "','" + new_data.Latitude + "','" + new_data.Longitude + "','" + new_data.Altitude + "','" + new_data.EngineStatus + "','" + new_data.Course + "','" + new_data.Temperature + "','" + new_data.Fuel + "','" + new_data.Speed + "','" + new_data.Distance + "','" + new_data.EventCode + "','" + new_data.Status_PostionValidity + "','" + new_data.Status_SateliteCount + "','" + new_data.Status_GSMSignalStrength + "','" + new_data.RemainingCash + "'," + 0).FirstOrDefault();
                        }
                    }
                    else
                    {
                        reponseFromProcedure = reponseFromProcedure + db.Database.SqlQuery<string>("EXEC dbo.PushDeviceData__Insert '" + old_data.PK_Vehicle + "','" + new_data.GpsIMEINumber + "','" + new_data.UpdateTime.ToString() + "','" + new_data.Latitude + "','" + new_data.Longitude + "','" + new_data.Altitude + "','" + new_data.EngineStatus + "','" + new_data.Course + "','" + new_data.Temperature + "','" + new_data.Fuel + "','" + new_data.Speed + "','" + new_data.Distance + "','" + new_data.EventCode + "','" + new_data.Status_PostionValidity + "','" + new_data.Status_SateliteCount + "','" + new_data.Status_GSMSignalStrength + "','" + new_data.RemainingCash + "'").FirstOrDefault();
                    }
                }
                return reponseFromProcedure;

            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "100");
                #endregion

                return "Exception: " + ex.Message;
            }

        }
        // ─────────────────────────────────────────────────────────────────────
        //  PORT 6063  –  MeiTrack T711L GPRS
        //  Calls NEW dedicated T711L_* stored procedures (not the shared ones).
        //  All numeric params typed as decimal/int — no varchar conversion.
        // ─────────────────────────────────────────────────────────────────────
        public string PushDeviceData_PORT_6063(DB_Helper_Data new_data)
        {
            // ── Timestamp sanity fix ──────────────────────────────────────────
            // If device time is in the FUTURE (> server time), clamp it to server time.
            // This handles devices with wrong RTC dates.
            DateTime serverNow = DateTime.Now;
            
            if (new_data.UpdateTime > serverNow)
            {
                // Device time is in future - replace with server time
                AuditLog.auditLog(new_data.GpsIMEINumber,
                    string.Format("FutureFix: DeviceTime={0} → ServerTime={1}",
                        new_data.UpdateTime, serverNow),
                    "6063_FUTURE_FIX");

                new_data.UpdateTime = serverNow;
            }
            else
            {
                // Check if date is way off in the past (> 7 days old)
                TimeSpan drift = serverNow - new_data.UpdateTime;
                if (drift.TotalDays > 7)
                {
                    // Keep HH:mm:ss from device, use today's date from server
                    DateTime fixed_time = new DateTime(
                        serverNow.Year, serverNow.Month, serverNow.Day,
                        new_data.UpdateTime.Hour,
                        new_data.UpdateTime.Minute,
                        new_data.UpdateTime.Second,
                        DateTimeKind.Local);

                    AuditLog.auditLog(new_data.GpsIMEINumber,
                        string.Format("PastDateFix: DeviceTime={0} → FixedTime={1}",
                            new_data.UpdateTime, fixed_time),
                        "6063_PAST_FIX");

                    new_data.UpdateTime = fixed_time;
                }
            }

            new_data.RemainingCash = 0;
            try
            {
                var old_data = (from v in db.VehicleTrackingInformations
                                    .Where(v => v.GpsIMEINumber == new_data.GpsIMEINumber)
                                join vt in db.VehicleTrackings on v.PK_Vehicle equals vt.PK_Vehicle
                                select new { v.PK_Vehicle, vt.Latitude, vt.Longitude, vt.UpdateTime })
                               .FirstOrDefault();

                string   sql;
                object[] prms;

                if (old_data == null)
                {
                    sql  = "EXEC dbo.T711L_Insert_Insert " + T711L_InsertParamNames();
                    prms = T711L_InsertParams(new_data);
                }
                else if (new_data.UpdateTime > old_data.UpdateTime)
                {
                    bool moved = distanceInKmBetweenEarthCoordinates(
                        old_data.Latitude, old_data.Longitude,
                        new_data.Latitude, new_data.Longitude)
                        > CommonClass.Distance_Change_Range_In_KM_6063;

                    sql  = "EXEC dbo.T711L_Update_Insert " + T711L_UpdateParamNames();
                    prms = T711L_UpdateParams(old_data.PK_Vehicle, new_data, moved ? 1 : 0);
                }
                else
                {
                    sql  = "EXEC dbo.T711L__Insert " + T711L_HeartbeatParamNames();
                    prms = T711L_HeartbeatParams(old_data.PK_Vehicle, new_data);
                }

                string response = db.Database.SqlQuery<string>(sql, prms).FirstOrDefault() ?? "T711L-OK";
                AuditLog.auditLog(new_data.GpsIMEINumber, "DB: " + response, "6063_DB");
                return response;
            }
            catch (Exception ex)
            {
                AuditLog.auditLog(new_data.GpsIMEINumber, ex.Message, "6063_DB_ERR");
                return "Exception: " + ex.Message;
            }
        }

        // ── T711L SP parameter helpers ────────────────────────────────────────

        private static string T711L_InsertParamNames() =>
            "@GpsIMEINumber,@UpdateTime,@Latitude,@Longitude," +
            "@Altitude,@EngineStatus,@Course,@Temperature," +
            "@Fuel,@Speed,@Distance,@Mileage,@EventCode," +
            "@Status_PostionValidity,@Status_SateliteCount," +
            "@Status_GSMSignalStrength,@RemainingCash";

        private static object[] T711L_InsertParams(DB_Helper_Data d)
        {
            return new object[]
            {
                new System.Data.SqlClient.SqlParameter("@GpsIMEINumber",          d.GpsIMEINumber),
                new System.Data.SqlClient.SqlParameter("@UpdateTime",             d.UpdateTime),
                new System.Data.SqlClient.SqlParameter("@Latitude",              (decimal)d.Latitude),
                new System.Data.SqlClient.SqlParameter("@Longitude",             (decimal)d.Longitude),
                new System.Data.SqlClient.SqlParameter("@Altitude",              (decimal)d.Altitude),
                new System.Data.SqlClient.SqlParameter("@EngineStatus",           d.EngineStatus ?? "0"),
                new System.Data.SqlClient.SqlParameter("@Course",                (decimal)d.Course),
                new System.Data.SqlClient.SqlParameter("@Temperature",           (decimal)d.Temperature),
                new System.Data.SqlClient.SqlParameter("@Fuel",                  (decimal)d.Fuel),
                new System.Data.SqlClient.SqlParameter("@Speed",                 (decimal)d.Speed),
                new System.Data.SqlClient.SqlParameter("@Distance",              decimal.Parse(d.Distance ?? "0", System.Globalization.CultureInfo.InvariantCulture)),
                new System.Data.SqlClient.SqlParameter("@Mileage",               DBNull.Value) { IsNullable = true },
                new System.Data.SqlClient.SqlParameter("@EventCode",             d.EventCode ?? "0"),
                new System.Data.SqlClient.SqlParameter("@Status_PostionValidity",d.Status_PostionValidity ?? "V"),
                new System.Data.SqlClient.SqlParameter("@Status_SateliteCount",  d.Status_SateliteCount),
                new System.Data.SqlClient.SqlParameter("@Status_GSMSignalStrength", d.Status_GSMSignalStrength),
                new System.Data.SqlClient.SqlParameter("@RemainingCash",         d.RemainingCash)
            };
        }

        private static string T711L_UpdateParamNames() =>
            "@PK_Vehicle,@GpsIMEINumber,@UpdateTime,@Latitude,@Longitude," +
            "@Altitude,@EngineStatus,@Course,@Temperature," +
            "@Fuel,@Speed,@Distance,@Mileage,@EventCode," +
            "@Status_PostionValidity,@Status_SateliteCount," +
            "@Status_GSMSignalStrength,@RemainingCash,@IsLocationChanged";

        private static object[] T711L_UpdateParams(Guid pk, DB_Helper_Data d, int locationChanged)
        {
            return new object[]
            {
                new System.Data.SqlClient.SqlParameter("@PK_Vehicle",            pk),
                new System.Data.SqlClient.SqlParameter("@GpsIMEINumber",         d.GpsIMEINumber),
                new System.Data.SqlClient.SqlParameter("@UpdateTime",            d.UpdateTime),
                new System.Data.SqlClient.SqlParameter("@Latitude",             (decimal)d.Latitude),
                new System.Data.SqlClient.SqlParameter("@Longitude",            (decimal)d.Longitude),
                new System.Data.SqlClient.SqlParameter("@Altitude",             (decimal)d.Altitude),
                new System.Data.SqlClient.SqlParameter("@EngineStatus",          d.EngineStatus ?? "0"),
                new System.Data.SqlClient.SqlParameter("@Course",               (decimal)d.Course),
                new System.Data.SqlClient.SqlParameter("@Temperature",          (decimal)d.Temperature),
                new System.Data.SqlClient.SqlParameter("@Fuel",                 (decimal)d.Fuel),
                new System.Data.SqlClient.SqlParameter("@Speed",                (decimal)d.Speed),
                new System.Data.SqlClient.SqlParameter("@Distance",             decimal.Parse(d.Distance ?? "0", System.Globalization.CultureInfo.InvariantCulture)),
                new System.Data.SqlClient.SqlParameter("@Mileage",              DBNull.Value) { IsNullable = true },
                new System.Data.SqlClient.SqlParameter("@EventCode",            d.EventCode ?? "0"),
                new System.Data.SqlClient.SqlParameter("@Status_PostionValidity",d.Status_PostionValidity ?? "V"),
                new System.Data.SqlClient.SqlParameter("@Status_SateliteCount", d.Status_SateliteCount),
                new System.Data.SqlClient.SqlParameter("@Status_GSMSignalStrength", d.Status_GSMSignalStrength),
                new System.Data.SqlClient.SqlParameter("@RemainingCash",        d.RemainingCash),
                new System.Data.SqlClient.SqlParameter("@IsLocationChanged",    locationChanged == 1)
            };
        }

        private static string T711L_HeartbeatParamNames() =>
            "@PK_Vehicle,@GpsIMEINumber,@UpdateTime,@Latitude,@Longitude," +
            "@Altitude,@EngineStatus,@Course,@Temperature," +
            "@Fuel,@Speed,@Distance,@Mileage,@EventCode," +
            "@Status_PostionValidity,@Status_SateliteCount," +
            "@Status_GSMSignalStrength,@RemainingCash";

        private static object[] T711L_HeartbeatParams(Guid pk, DB_Helper_Data d)
        {
            return new object[]
            {
                new System.Data.SqlClient.SqlParameter("@PK_Vehicle",            pk),
                new System.Data.SqlClient.SqlParameter("@GpsIMEINumber",         d.GpsIMEINumber),
                new System.Data.SqlClient.SqlParameter("@UpdateTime",            d.UpdateTime),
                new System.Data.SqlClient.SqlParameter("@Latitude",             (decimal)d.Latitude),
                new System.Data.SqlClient.SqlParameter("@Longitude",            (decimal)d.Longitude),
                new System.Data.SqlClient.SqlParameter("@Altitude",             (decimal)d.Altitude),
                new System.Data.SqlClient.SqlParameter("@EngineStatus",          d.EngineStatus ?? "0"),
                new System.Data.SqlClient.SqlParameter("@Course",               (decimal)d.Course),
                new System.Data.SqlClient.SqlParameter("@Temperature",          (decimal)d.Temperature),
                new System.Data.SqlClient.SqlParameter("@Fuel",                 (decimal)d.Fuel),
                new System.Data.SqlClient.SqlParameter("@Speed",                (decimal)d.Speed),
                new System.Data.SqlClient.SqlParameter("@Distance",             decimal.Parse(d.Distance ?? "0", System.Globalization.CultureInfo.InvariantCulture)),
                new System.Data.SqlClient.SqlParameter("@Mileage",              DBNull.Value) { IsNullable = true },
                new System.Data.SqlClient.SqlParameter("@EventCode",            d.EventCode ?? "0"),
                new System.Data.SqlClient.SqlParameter("@Status_PostionValidity",d.Status_PostionValidity ?? "V"),
                new System.Data.SqlClient.SqlParameter("@Status_SateliteCount", d.Status_SateliteCount),
                new System.Data.SqlClient.SqlParameter("@Status_GSMSignalStrength", d.Status_GSMSignalStrength),
                new System.Data.SqlClient.SqlParameter("@RemainingCash",        d.RemainingCash)
            };
        }
        public string CleanDeviceData()
        {
            string reponseFromProcedure = "";
            try
            {
                reponseFromProcedure = db.Database.SqlQuery<string>("EXEC dbo.CleanDeviceData").FirstOrDefault();

                #region AuditLog
                AuditLog.auditLog(reponseFromProcedure, "105");//# Please add your own generated number replacing 100
                #endregion


                return reponseFromProcedure;
            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "106");//# Please add your own generated number replacing 100
                #endregion

                return "Exception: " + ex.Message;
            }
        }




        public double degreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public double distanceInKmBetweenEarthCoordinates(double lat1, double lon1, double lat2, double lon2)
        {
            var earthRadiusKm = 6371;

            var dLat = degreesToRadians(lat2 - lat1);
            var dLon = degreesToRadians(lon2 - lon1);

            lat1 = degreesToRadians(lat1);
            lat2 = degreesToRadians(lat2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }

        public void Dispose()
        {
            try
            {
                db.Dispose();
            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "55");
                #endregion
            }
        }


    }
}
