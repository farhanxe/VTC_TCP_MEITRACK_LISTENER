using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Newtonsoft.Json;



namespace FX_TCP
{

    public class ListConvertedData
    {
        public string EventCode { get; set; }
        public string LAT { get; set; }
        public string LONG { get; set; }
        public string Date_Time { get; set; }
        public string PositionStatus { get; set; }
        public string Number_Of_Satellite { get; set; }
        public string GSM_Signal_Strength { get; set; }
        public string SPEED { get; set; }
        public string Direction { get; set; }
        public string HDOP { get; set; }
        public string Altitude { get; set; }
        public string Milage { get; set; }
        public string RunTime { get; set; }
        public string Base_Station_Info { get; set; }
        public string IO_Port_Status { get; set; }
        public string Analog { get; set; }
        public string GEO_Fence { get; set; }
    }

    public class DataSpliter
    {
        public string Identifier { get; set; }
        public string DataLength { get; set; }
        public string IMEI { get; set; }
        public string protocolVersion { get; set; }
        public string LONG_LAT_Packet_Length { get; set; }
        public string Remaining_Cache { get; set; }
        public List<ListConvertedData> ListData { get; set; }
        public string Checksum { get; set; }
    }

    public class DataRetriveHelper
    {
        //public int SL { get; set; }
        public string ByteValue { get; set; }

    }












    public class HEX_Spliter : IDisposable
    {
        public byte[] Raw_Data { get; set; }


        private string LittleEndian(int StartIndexNo, int size)
        {
            string returnValue = null;

            byte[] DataByte = new byte[size];
            for (int i = 0; i < size; i++)
            {
                DataByte[i] = Raw_Data[StartIndexNo + i];
            }

            switch (size)
            {
                case 2:
                    returnValue = ((DataByte[1] << 8) | DataByte[0]).ToString();
                    break;

                case 4:
                    returnValue = ((DataByte[3] << 24) | (DataByte[2] << 16) | (DataByte[1] << 8) | DataByte[0]).ToString();
                    break;

                default:
                    break;
            }

            return returnValue;
        }







        //Test, it for R&D
        private string BigEndian(int StartIndexNo, int size)
        {
            try
            {
                string returnValue = null;

                byte[] DataByte = new byte[size];

                for (int i = 0; i < size; i++)
                {
                    DataByte[i] = Raw_Data[StartIndexNo + i];
                }

                switch (size)
                {
                    case 2:
                        returnValue = ((DataByte[1]) | DataByte[0] << 8).ToString();
                        break;
                    /*
                case 4:
                    returnValue = ((DataByte[3] << 24) | (DataByte[2] << 16) | (DataByte[1] << 8) | DataByte[0]).ToString();
                    break;

                case 8:
                    returnValue = ((DataByte[6] << 48) | (DataByte[5] << 40) | (DataByte[4] << 32) | DataByte[0]| (DataByte[3] << 24) | (DataByte[2] << 16) | (DataByte[1] << 8) | DataByte[0]).ToString();
                    break;
                    */

                    default:
                        break;
                }


                return returnValue;
            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "100");
                #endregion

                return null;
            }

        }



        public string GetSplitData()
        {
            DataSpliter dp = new DataSpliter();

            #region Identifier, 1 byte, ASCII
            byte[] identifierByte = new byte[1];
            identifierByte[0] = Raw_Data[2];
            #endregion

            #region DataLength, 3 byte, ASCII
            byte[] DataLengthByte = new byte[3];
            DataLengthByte[0] = Raw_Data[3];
            DataLengthByte[1] = Raw_Data[4];
            DataLengthByte[2] = Raw_Data[5];
            #endregion

            #region IMEI, 15 byte, ASCII
            byte[] IMEIByte = new byte[15];
            for (int i = 0; i < 15; i++)
            {
                IMEIByte[i] = Raw_Data[7 + i];
            }
            #endregion


            dp.Identifier = Encoding.ASCII.GetString(identifierByte).ToString();
            dp.DataLength = Encoding.ASCII.GetString(DataLengthByte).ToString();
            dp.IMEI = Encoding.ASCII.GetString(IMEIByte).ToString();
            dp.protocolVersion = LittleEndian(27, 2);
            dp.LONG_LAT_Packet_Length = LittleEndian(29, 2);
            dp.Remaining_Cache = LittleEndian(31, 4);


            int Data_Packet_Number = (Convert.ToInt32(dp.DataLength) - 34) / Convert.ToInt32(dp.LONG_LAT_Packet_Length);

            #region List_Data
            //index start from 35

            List<ListConvertedData> SplitDataList = new List<ListConvertedData>();

            for (int DataPackets = 0; DataPackets < Data_Packet_Number; DataPackets++)
            {
                try
                {
                    ListConvertedData lst = new ListConvertedData();
                    lst.EventCode = Raw_Data[(52 * DataPackets) + 35].ToString();


                    lst.LAT = (Convert.ToDouble(LittleEndian((52 * DataPackets) + 36, 4)) / 1000000).ToString();
                    lst.LONG = (Convert.ToDouble(LittleEndian((52 * DataPackets) + 40, 4)) / 1000000).ToString();
                    lst.Date_Time = (Convert.ToDouble(LittleEndian((52 * DataPackets) + 44, 4))).ToString();
          
                    lst.PositionStatus = Raw_Data[(52 * DataPackets) + 48].ToString();


                    lst.Number_Of_Satellite = Raw_Data[(52 * DataPackets) + 49].ToString();
                    lst.GSM_Signal_Strength = Raw_Data[(52 * DataPackets) + 50].ToString();
                    lst.SPEED = (Convert.ToDouble(LittleEndian((52 * DataPackets) + 51, 2))).ToString();
                    
                    lst.Direction = (Convert.ToDouble(LittleEndian((52 * DataPackets) + 53, 2))).ToString();
                    lst.HDOP = (Convert.ToDouble(LittleEndian((52 * DataPackets) + 55, 2))).ToString();
                    lst.Altitude = (Convert.ToDouble(LittleEndian((52 * DataPackets) + 57, 2))).ToString();
                    lst.Milage = (Convert.ToDouble(LittleEndian((52 * DataPackets) + 59, 4))).ToString();
                    lst.RunTime = (Convert.ToDouble(LittleEndian((52 * DataPackets) + 63, 4))).ToString();
                    lst.Base_Station_Info = (Convert.ToDouble(LittleEndian((52 * DataPackets) + 67, 8))).ToString();//will be. BigEnding
                    lst.IO_Port_Status = (Convert.ToDouble(BigEndian((52 * DataPackets) + 75, 2))).ToString();
                    lst.Analog = "";
                    lst.GEO_Fence = "";

                    SplitDataList.Add(lst);
                }
                catch (Exception ex)
                {
                    #region AuditLog
                    AuditLog.auditLog(dp.IMEI + " - " + ex.Message, "100");
                    #endregion
                }

            }

            #endregion


            #region Checksum

            byte[] Checksum_Byte = new byte[2];
            for (int i = 0; i < 2; i++)
            {
                Checksum_Byte[i] = Raw_Data[Raw_Data.Count() - 4 + i];
            }
            #endregion


            dp.Checksum = Encoding.ASCII.GetString(Checksum_Byte).ToString();

            dp.ListData = SplitDataList.ToList();

            string output = JsonConvert.SerializeObject(dp);


            #region AuditLog
            AuditLog.auditLog(output, "115");
            #endregion

            return output;

        }

        public static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }

        public void Dispose()
        {


        }


    }



}
