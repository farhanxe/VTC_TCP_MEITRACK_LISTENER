using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FX_TCP.Class;

namespace FX_TCP
{
    public partial class frm6062 : Form
    {

        bool IsLiveMode = false;
        //DB_Helper dbHelper = new DB_Helper();

        private byte[] _buffer = new byte[1024];
        public List<SocketT2h> __ClientSockets { get; set; }
        List<string> _names = new List<string>();
        private Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


        public bool CheckValue(int number)
        {
            //When Engine Status = 1, then data comes 200

            string binary = Convert.ToString(number, 2);

            binary = binary.Substring(binary.Length - 5);

            char[] array = binary.ToCharArray();

            bool result = false;

            if (array[1] == '1')
            {
                result = true;
            }

            return result;

        }

        public bool IsConnected(Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (Exception ex)
            {

                #region AuditLog
                AuditLog.auditLog(ex.Message, "2");
                #endregion
                return false;
            }
        }

        public frm6062()
        {
            try
            {
                InitializeComponent();
                CheckForIllegalCrossThreadCalls = false;
                __ClientSockets = new List<SocketT2h>();
            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "1");
                #endregion
            }

        }

        private void frm6062_Load(object sender, EventArgs e)
        {
            try
            {
                SetupServer();
            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "3");
                #endregion
            }
        }

        private void SetupServer()
        {
            try
            {
                int PORT_No = Convert.ToInt32(ConfigurationManager.AppSettings["PORT_for_6062"]);
                CommonClass.Max_Connected_Socket_6062 = Convert.ToInt32(ConfigurationManager.AppSettings["Max_Connected_Socket_6062"]);
                CommonClass.Distance_Change_Range_In_KM_6062 = Convert.ToDouble(ConfigurationManager.AppSettings["Distance_Change_Range_In_KM_6062"]);

                int LiveMode = Convert.ToInt32(ConfigurationManager.AppSettings["IsLiveMode"]);

                if (LiveMode == 1)
                {
                    IsLiveMode = true;
                }
                else
                {
                    IsLiveMode = false;
                }




                lblMsg.Text = "Setting up server . . .";
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT_No));//6062//5353
                _serverSocket.Listen(1);
                _serverSocket.BeginAccept(new AsyncCallback(AppceptCallback), null);
            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "4");
                #endregion
            }
        }

        private void connectionClose(Socket _socket)
        {
            try
            {
                _socket.Close();
            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "5");
                #endregion
            }
        }

        private void AppceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = _serverSocket.EndAccept(ar);

                int cnt = list_Client.Items.Count;
                __ClientSockets.Add(new SocketT2h(socket, cnt.ToString()));
                //list_Client.Items.Add(cnt.ToString() + ". " + socket.RemoteEndPoint.ToString());

                lblMsg.Text = "" + __ClientSockets.Count.ToString() + " | Client connected. . .";

                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                _serverSocket.BeginAccept(new AsyncCallback(AppceptCallback), null);
            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "5");
                #endregion
            }


        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                //using
                Socket socket = (Socket)ar.AsyncState;
                {

                    if (socket.Connected)
                    {
                        int received;
                        try
                        {
                            received = socket.EndReceive(ar);
                        }
                        catch (Exception ex)
                        {
                            for (int i = 0; i < __ClientSockets.Count; i++)
                            {
                                if (__ClientSockets[i]._Socket.RemoteEndPoint.ToString().Equals(socket.RemoteEndPoint.ToString()))
                                {
                                    //__ClientSockets.RemoveAt(i);
                                    //lblMsg.Text = "" + __ClientSockets.Count.ToString();
                                }
                            }

                            #region AuditLog

                            if (IsLiveMode == true)
                            {
                                AuditLog.auditLog(ex.Message, "6");
                            }

                            #endregion

                            return;
                        }
                        if (received != 0)
                        {
                            byte[] dataBuf = new byte[received];
                            Array.Copy(_buffer, dataBuf, received);
                            string text = Encoding.ASCII.GetString(dataBuf);

                            text = text.Replace('\0', ' ');
                            text = text.Trim();


                            string reponse = string.Empty;

                            string[] splitData = text.Split(',');

                            int io = Convert.ToInt16(splitData[17], 16);

                            string io_Bit = Convert.ToString(io, 2);

                            int io_Bit_length = io_Bit.Length;

                            try
                            {
                                #region Insert_DataBase

                                //DB_Helper dbHelper = new DB_Helper();
                                using (DB_Helper dbHelper = new DB_Helper())
                                {
                                    DB_Helper_Data helperData = new DB_Helper_Data();
                                    helperData.GpsIMEINumber = splitData[1].ToString();
                                    helperData.Latitude = Convert.ToDouble(splitData[4].ToString());
                                    helperData.Longitude = Convert.ToDouble(splitData[5].ToString());
                                    helperData.Altitude = 0;

                                    helperData.RemainingCash = -1;

                                    //Event Code
                                    try
                                    {
                                        helperData.EventCode = splitData[3].ToString();
                                    }
                                    catch (Exception)
                                    {
                                        helperData.EventCode = "E";
                                    }
                                    //Positioning status
                                    try
                                    {
                                        helperData.Status_PostionValidity = splitData[7].ToString();
                                    }
                                    catch (Exception)
                                    {
                                        helperData.Status_PostionValidity = "E";
                                    }
                                    //Number of satellites
                                    try
                                    {
                                        helperData.Status_SateliteCount = Convert.ToInt32(splitData[8].ToString());
                                    }
                                    catch (Exception)
                                    {
                                        helperData.Status_SateliteCount = -1;
                                    }
                                    //GSM signal strength
                                    try
                                    {
                                        helperData.Status_GSMSignalStrength = Convert.ToInt32(splitData[9].ToString());
                                    }
                                    catch (Exception)
                                    {
                                        helperData.Status_GSMSignalStrength = -1;
                                    }


                                    try
                                    {
                                        string io_value = "";
                                        io_value = splitData[17].ToString();

                                        if (CheckValue(Convert.ToInt32(io_value)) == true)
                                        {
                                            helperData.EngineStatus = "1";
                                        }
                                        else
                                        {
                                            helperData.EngineStatus = "0";
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        helperData.EngineStatus = "0";

                                        #region AuditLog
                                        AuditLog.auditLog(helperData.GpsIMEINumber, ex.Message, "8");
                                        #endregion
                                    }

                                    int Year = 0;
                                    int Month = 0;
                                    int Day = 0;
                                    int Hour = 0;
                                    int Minute = 0;
                                    int Second = 0;

                                    DateTime actionDateTime;

                                    try
                                    {
                                        Year = 2000 + Convert.ToInt32(splitData[6].ToString().Trim().Substring(0, 2));
                                        Month = Convert.ToInt32(splitData[6].ToString().Trim().Substring(2, 2));
                                        Day = Convert.ToInt32(splitData[6].ToString().Trim().Substring(4, 2));

                                        Hour = Convert.ToInt32(splitData[6].ToString().Trim().Substring(6, 2));
                                        Minute = Convert.ToInt32(splitData[6].ToString().Trim().Substring(8, 2));
                                        Second = Convert.ToInt32(splitData[6].ToString().Trim().Substring(10, 2));

                                        actionDateTime = new DateTime(Year, Month, Day, Hour, Minute, Second).AddSeconds(21600);
                                        helperData.UpdateTime = actionDateTime;

                                    }
                                    catch (Exception ex)
                                    {
                                        #region AuditLog
                                        AuditLog.auditLog(helperData.GpsIMEINumber, ex.Message, "8");
                                        #endregion
                                    }

                                    helperData.Course = Convert.ToDouble(splitData[11].ToString());


                                    #region Get_Temperature_Data

                                    //string split_Temperature_Data = "0.0";
                                    //string temp_Temperature = "0.0";
                                    double actualTemperature = 0.0;

                                    try
                                    {
                                        var temperature_data = splitData[23].ToString();
                                        if (!string.IsNullOrEmpty(temperature_data))
                                        {
                                            var sensor1_data = temperature_data.Substring(2, 4);
                                            actualTemperature = Int32.Parse(sensor1_data, System.Globalization.NumberStyles.HexNumber) / 100;//Convert.ToDouble(d2);
                                        }

                                        //# Old codes
                                        //split_Temperature_Data = splitData[23].ToString();//   23   //splitData[18].ToString().Split('|')

                                        //try
                                        //{
                                        //    temp_Temperature = split_Temperature_Data.ToString();
                                        //}
                                        //catch (Exception ex)
                                        //{
                                        //    temp_Temperature = "0.0";
                                        //}

                                        //if ((string.IsNullOrEmpty(temp_Temperature) == false) || temp_Temperature != "0.0")
                                        //{
                                        //    temp_Temperature.Replace('0', ' ');

                                        //    temp_Temperature = temp_Temperature.Trim();

                                        //    int num = Int32.Parse(temp_Temperature, System.Globalization.NumberStyles.HexNumber);

                                        //    actualTemperature = num / 100.0;
                                        //}
                                    }
                                    catch (Exception ex)
                                    {
                                        #region AuditLog
                                        AuditLog.auditLog(helperData.GpsIMEINumber, ex.Message, "8.1");
                                        #endregion
                                    }

                                    #endregion


                                    helperData.Temperature = actualTemperature;




                                    double analogFuel = 0.01;

                                    try
                                    {
                                        //0000|0000|0000|019D|04AC

                                        string[] analogData = splitData[18].ToString().Split('|');
                                        int num = Int32.Parse(analogData[0], System.Globalization.NumberStyles.HexNumber);
                                        // AuditLog.auditLog(splitData[18].ToString(), "FUEL_String");
                                        analogFuel = Convert.ToDouble(num.ToString());
                                        //  AuditLog.auditLog(analogFuel.ToString(), "FUEL_String");

                                    }
                                    catch (Exception ex)
                                    {
                                        #region AuditLog
                                        AuditLog.auditLog(helperData.GpsIMEINumber, ex.Message, "FUEL");
                                        #endregion
                                    }


                                    helperData.Fuel = analogFuel;

                                    helperData.Speed = Convert.ToDouble(splitData[10].ToString());
                                    helperData.Distance = "0";


                                    #region AuditLog
                                    //if (IsLiveMode == true)
                                    {
                                        AuditLog.auditLog(helperData.GpsIMEINumber, (JsonConvert.SerializeObject(helperData)).ToString(), "8");
                                    }
                                    #endregion


                                    string Response_from_DB = dbHelper.PushDeviceData_PORT_6062(helperData);


                                    #region AuditLog
                                    if (IsLiveMode == true)
                                    {
                                        AuditLog.auditLog(helperData.GpsIMEINumber, "Response From DB : " + Response_from_DB, "8");
                                    }
                                    #endregion


                                    //Sendata(socket, "@@E27," + splitData[1].ToString() + ",F09,3*CA" + Environment.NewLine);

                                }//Using


                                #endregion
                            }
                            catch (Exception ex)
                            {
                                #region AuditLog
                                AuditLog.auditLog(ex.Message, "8");
                                #endregion
                            }

                            for (int i = 0; i < __ClientSockets.Count; i++)
                            {
                                try
                                {
                                    if (socket.RemoteEndPoint.ToString().Equals(__ClientSockets[i]._Socket.RemoteEndPoint.ToString()))
                                    {
                                        //txtReceive.AppendText(Environment.NewLine + __ClientSockets[i]._Name + ": " + text.Trim().ToString());

                                    }
                                }
                                catch (Exception ex)
                                {

                                }
                            }


                            reponse = "Done" + text;

                            Sendata(socket, reponse);

                        }
                        else
                        {

                            for (int i = 0; i < __ClientSockets.Count; i++)
                            {
                                try
                                {

                                    if (__ClientSockets[i]._Socket.RemoteEndPoint.ToString().Equals(socket.RemoteEndPoint.ToString()))
                                    {
                                        __ClientSockets.RemoveAt(i);
                                        //lblMsg.Text = "" + __ClientSockets.Count.ToString();
                                    }
                                }
                                catch (Exception ex)
                                {

                                }

                            }


                        }
                    }

                }


                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);

            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "13");
                #endregion
            }
        }
        void Sendata(Socket socket, string noidung)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(noidung);
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                _serverSocket.BeginAccept(new AsyncCallback(AppceptCallback), null);
            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "14");
                #endregion
            }
        }
        private void SendCallback(IAsyncResult AR)
        {
            try
            {
                Socket socket = (Socket)AR.AsyncState;
                socket.EndSend(AR);
            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "15");
                #endregion
            }
        }

        private void frm6062_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                connectionClose(_serverSocket);
            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "12");
                #endregion
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            txtReceive.Text = "";

            try
            {
                List<SocketT2h> listSocketss = __ClientSockets;

                int i = 0;

                try
                {

                    foreach (var item in listSocketss)
                    {
                        try
                        {
                            Console.WriteLine(item._Socket.Connected.ToString());


                            Console.WriteLine(IsConnected(item._Socket).ToString());

                            if (IsConnected(item._Socket) == false)
                            {
                                //tcpClient.GetStream().Close();
                                //tcpClient.Close();
                                try
                                {
                                    item._Socket.Close();
                                    //__ClientSockets.RemoveAt(i);
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                            //item._Socket.Disconnect(true);
                        }
                        catch (Exception ex)
                        {
                        }
                        i++;
                    }


                }
                catch (Exception ex)
                {

                }

                list_Client.Refresh();


            }
            catch (Exception ex)
            {
                #region AuditLog
                AuditLog.auditLog(ex.Message, "177");
                #endregion

            }


        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }
    }


}
