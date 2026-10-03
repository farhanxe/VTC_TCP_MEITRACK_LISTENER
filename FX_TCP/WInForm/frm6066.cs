using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FX_TCP.Class;

namespace FX_TCP
{
    public partial class frm6066 : Form
    {
        // ── Configuration ──────────────────────────────────────────────────────
        private bool   _isLiveMode;
        private int    _portNo;
        private int    _utcOffsetSeconds;
        private double _distChangeRangeKm;
        private int    _dedupSeconds;
        private int    _batchSize;

        // ── Network ────────────────────────────────────────────────────────────
        private Socket _serverSocket;
        private readonly ConcurrentDictionary<string, ClientState> _clients
            = new ConcurrentDictionary<string, ClientState>();

        // ── Write queue ────────────────────────────────────────────────────────
        private readonly ConcurrentQueue<DB_Helper_Data> _writeQueue
            = new ConcurrentQueue<DB_Helper_Data>();
        private Thread   _writerThread;
        private volatile bool _writerRunning = false;

        // ── Per-IMEI dedup cache ───────────────────────────────────────────────
        private readonly ConcurrentDictionary<string, DedupEntry> _dedupCache
            = new ConcurrentDictionary<string, DedupEntry>();

        private struct DedupEntry
        {
            public DateTime LastSaved;
            public double   Lat;
            public double   Lon;
            public string   Engine;
            public string   EventCode;
            public double   Temperature;
        }

        // ── Counters ───────────────────────────────────────────────────────────
        private long _totalReceived    = 0;
        private long _totalProcessed   = 0;
        private long _totalDbErrors    = 0;
        private long _totalParseErrors = 0;
        private long _totalDeduped     = 0;
        private long _prevReceived     = 0;

        // ── System metrics ─────────────────────────────────────────────────────
        private PerformanceCounter _cpuCounter;
        private DateTime           _startTime;
        private long               _lastDbMs = 0;
        private const int          LOG_MAX   = 500;

        // ── Per-client state ───────────────────────────────────────────────────
        private class ClientState
        {
            public Socket        Socket      { get; }
            public string        EndPoint    { get; }
            public byte[]        Buffer      { get; } = new byte[4096];
            public StringBuilder Accumulator { get; } = new StringBuilder(4096);
            public DateTime      ConnectedAt { get; } = DateTime.Now;
            public string        LastIMEI    { get; set; } = "";
            public DateTime      LastPacket  { get; set; } = DateTime.Now;
            public long          PacketCount { get; set; } = 0;
            public string        LastPackNo  { get; set; } = "";
            public ClientState(Socket s)
            {
                Socket   = s;
                EndPoint = s.RemoteEndPoint?.ToString() ?? "unknown";
            }
        }

        // ── Constructor ────────────────────────────────────────────────────────
        public frm6066()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        // ── Form Load ──────────────────────────────────────────────────────────
        private void frm6066_Load(object sender, EventArgs e)
        {
            _startTime = DateTime.Now;
            try { _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); }
            catch { }
            try
            {
                LoadConfig();
                StartWriterThread();
                SetupServer();
                UpdateModeBadge();
                timerUI.Start();
                AppendLog(string.Format(
                    "VT200L Server started on port {0}  |  Dedup={1}s  BatchSize={2}",
                    _portNo, _dedupSeconds, _batchSize), LogLevel.Info);
            }
            catch (Exception ex)
            {
                AuditLog.auditLog(ex.Message, "frm6066_Load");
                SetStatusError("FATAL: " + ex.Message);
                AppendLog("FATAL: " + ex.Message, LogLevel.Error);
            }
        }

        // ── Load settings ──────────────────────────────────────────────────────
        private void LoadConfig()
        {
            _portNo = int.Parse(
                ConfigurationManager.AppSettings["PORT_for_6066"] ?? "6066");
            _isLiveMode = (ConfigurationManager.AppSettings["IsLiveMode"] ?? "0") == "1";
            _distChangeRangeKm = double.Parse(
                ConfigurationManager.AppSettings["Distance_Change_Range_In_KM_6066"] ?? "0.05",
                CultureInfo.InvariantCulture);
            _utcOffsetSeconds = int.Parse(
                ConfigurationManager.AppSettings["UTC_Offset_Seconds_6066"] ?? "0");
            _dedupSeconds = int.Parse(
                ConfigurationManager.AppSettings["Dedup_Seconds_6066"] ?? "10");
            _batchSize = int.Parse(
                ConfigurationManager.AppSettings["Queue_BatchSize_6066"] ?? "50");
            CommonClass.Distance_Change_Range_In_KM_6066 = _distChangeRangeKm;
        }

        // ── TCP server ──────────────────────────────────────────────────────────
        private void SetupServer()
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _portNo));
            _serverSocket.Listen(500);
            _serverSocket.BeginAccept(AcceptCallback, null);
            picIndicator.BackColor = Color.LimeGreen;
            SetStatus("Listening on :" + _portNo, Color.LightGreen);
            AuditLog.auditLog("VT200L TCP server started on port " + _portNo, "SETUP_6066");
        }

        // ── Background DB writer thread ────────────────────────────────────────
        private void StartWriterThread()
        {
            _writerRunning = true;
            _writerThread  = new Thread(WriterLoop)
            {
                IsBackground = true,
                Name         = "VT200L-DBWriter",
                Priority     = ThreadPriority.BelowNormal
            };
            _writerThread.Start();
        }

        private void WriterLoop()
        {
            while (_writerRunning)
            {
                try
                {
                    int processed = 0;
                    while (processed < _batchSize && _writeQueue.TryDequeue(out DB_Helper_Data data))
                    {
                        SaveToDatabase(data);
                        processed++;
                    }

                    if (processed == 0)
                        Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    AuditLog.auditLog(ex.Message, "WRITER_LOOP_ERR_6066");
                    Thread.Sleep(500);
                }
            }
        }

        private void SaveToDatabase(DB_Helper_Data d)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using (var dbHelper = new DB_Helper())
                {
                    string response = dbHelper.PushDeviceData_PORT_6066(d);
                    sw.Stop();
                    Interlocked.Exchange(ref _lastDbMs, sw.ElapsedMilliseconds);
                    Interlocked.Increment(ref _totalProcessed);
                    // Only log slow DB operations to reduce UI spam
                    if (sw.ElapsedMilliseconds > 200)
                        AppendLog(string.Format("[DB-SLOW] {0}  ({1} ms)  Q:{2}",
                            response, sw.ElapsedMilliseconds, _writeQueue.Count), LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Interlocked.Increment(ref _totalDbErrors);
                AppendLog("[DB-ERR] " + ex.Message, LogLevel.Error);
                AuditLog.auditLog(d.GpsIMEINumber, ex.Message, "6066_DB_ERR");
            }
        }

        // ── Deduplication ──────────────────────────────────────────────────────
        private bool IsDuplicate(DB_Helper_Data d)
        {
            // Always save non-heartbeat events (53=RFID/iButton, etc.)
            // 0=interval report → deduplicate
            if (d.EventCode != "0")
                return false;

            if (!_dedupCache.TryGetValue(d.GpsIMEINumber, out DedupEntry last))
                return false;

            if ((d.UpdateTime - last.LastSaved).TotalSeconds > _dedupSeconds)
                return false;

            if (d.EngineStatus != last.Engine)
                return false;

            if (d.EventCode != last.EventCode)
                return false;

            if (Math.Abs(d.Temperature - last.Temperature) >= 0.1)
                return false;

            double distKm = new DB_Helper().distanceInKmBetweenEarthCoordinates(
                last.Lat, last.Lon, d.Latitude, d.Longitude);
            if (distKm > _distChangeRangeKm)
                return false;

            return true;
        }

        private void UpdateDedupCache(DB_Helper_Data d)
        {
            _dedupCache[d.GpsIMEINumber] = new DedupEntry
            {
                LastSaved   = d.UpdateTime,
                Lat         = d.Latitude,
                Lon         = d.Longitude,
                Engine      = d.EngineStatus,
                EventCode   = d.EventCode,
                Temperature = d.Temperature
            };
        }

        // ── Accept callback ────────────────────────────────────────────────────
        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket cs = _serverSocket.EndAccept(ar);
                cs.NoDelay = true;
                var state = new ClientState(cs);
                _clients[state.EndPoint] = state;
                Interlocked.Increment(ref FX_TCP.Class.PublicClass.ActiveConnection_6066);
                cs.BeginReceive(state.Buffer, 0, state.Buffer.Length,
                    SocketFlags.None, ReceiveCallback, state);
                AppendLog("Connected: " + state.EndPoint, LogLevel.Info);
            }
            catch (ObjectDisposedException) { return; }
            catch (Exception ex) { AuditLog.auditLog(ex.Message, "ACCEPT_ERR_6066"); }
            finally
            {
                try { _serverSocket.BeginAccept(AcceptCallback, null); }
                catch { }
            }
        }

        // ── Receive callback ───────────────────────────────────────────────────
        private void ReceiveCallback(IAsyncResult ar)
        {
            ClientState state  = (ClientState)ar.AsyncState;
            Socket      socket = state.Socket;
            try
            {
                if (!socket.Connected) { CloseClient(state); return; }
                int received;
                try   { received = socket.EndReceive(ar); }
                catch { CloseClient(state); return; }
                if (received == 0) { CloseClient(state); return; }

                Interlocked.Increment(ref _totalReceived);
                state.LastPacket = DateTime.Now;
                state.PacketCount++;

                state.Accumulator.Append(
                    Encoding.ASCII.GetString(state.Buffer, 0, received));
                ProcessAccumulator(state);

                socket.BeginReceive(state.Buffer, 0, state.Buffer.Length,
                    SocketFlags.None, ReceiveCallback, state);
            }
            catch (Exception ex)
            {
                AuditLog.auditLog(ex.Message, "RECV_ERR_6066");
                CloseClient(state);
            }
        }

        // ── TCP stream framing ─────────────────────────────────────────────────
        private void ProcessAccumulator(ClientState state)
        {
            string raw = state.Accumulator.ToString();
            int idx;
            while ((idx = raw.IndexOf("\r\n", StringComparison.Ordinal)) >= 0)
            {
                string packet = raw.Substring(0, idx).Trim();
                raw = raw.Substring(idx + 2);
                if (!string.IsNullOrWhiteSpace(packet))
                    ProcessPacket(state, packet);
            }
            state.Accumulator.Clear();
            if (raw.Length > 0) state.Accumulator.Append(raw);
        }

        // ── Parse VT200L packet ────────────────────────────────────────────────
        private void ProcessPacket(ClientState state, string packet)
        {
            try
            {
                // VT200L format: &&<pack-no><pack-len>,<ID>,<cmd>,<alm-code>,...
                if (!packet.StartsWith("&&"))
                {
                    AppendLog("[SKIP] Not VT200L: " + Truncate(packet, 60), LogLevel.Debug);
                    return;
                }

                // Extract pack-no (first character after &&)
                string packNo = packet.Length > 2 ? packet.Substring(2, 1) : "?";
                state.LastPackNo = packNo;

                // Find first comma to split header from data
                int commaPos = packet.IndexOf(',');
                if (commaPos < 0)
                {
                    Interlocked.Increment(ref _totalParseErrors);
                    AppendLog("[ERR] No comma found: " + Truncate(packet, 60), LogLevel.Error);
                    return;
                }

                // Remove header (&&<pack-no><pack-len>,)
                string dataStr = packet.Substring(commaPos + 1);

                // Remove checksum if present (last 2 hex chars before \r\n)
                int lastComma = dataStr.LastIndexOf(',');
                if (lastComma > 0 && dataStr.Length - lastComma <= 5)
                    dataStr = dataStr.Substring(0, lastComma);

                string[] f = dataStr.Split(',');
                if (f.Length < 20)
                {
                    Interlocked.Increment(ref _totalParseErrors);
                    AppendLog("[ERR] Too few fields (" + f.Length + "): "
                        + Truncate(packet, 60), LogLevel.Error);
                    return;
                }

                var d = new DB_Helper_Data();
                
                // Field mapping (VT200L protocol)
                d.GpsIMEINumber            = SafeField(f, 0, "");        // <ID>
                state.LastIMEI             = d.GpsIMEINumber;
                string cmd                 = SafeField(f, 1, "000");     // <cmd>
                d.EventCode                = SafeField(f, 2, "0");       // <alm-code>
                string almData             = SafeField(f, 3, "");        // <alm-data> (RFID/iButton)
                string dateTime            = SafeField(f, 4, "");        // <date-time>
                d.Status_PostionValidity   = SafeField(f, 5, "V");       // <fix_flag>
                
                double.TryParse(SafeField(f, 6, "0"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double lat);
                double.TryParse(SafeField(f, 7, "0"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double lon);
                d.Latitude                 = lat;
                d.Longitude                = lon;
                
                d.Status_SateliteCount     = ParseInt(SafeField(f, 8, "0"));     // <sat-quantity>
                // f[9] = HDOP (not stored)
                d.Speed                    = ParseDouble(SafeField(f, 10, "0")); // <speed>
                d.Course                   = ParseDouble(SafeField(f, 11, "0")); // <course>
                d.Altitude                 = ParseDouble(SafeField(f, 12, "0")); // <altitude>
                // f[13] = odometer (not used here)
                // f[14] = MCC|MNC|LAC|CI
                d.Status_GSMSignalStrength = ParseInt(SafeField(f, 15, "0"));    // <CSQ-quality>
                
                string systemSta           = SafeField(f, 16, "0");      // <system-sta>
                d.EngineStatus             = DeriveEngineStatusVT200L(systemSta);
                
                string inSta               = SafeField(f, 17, "0");      // <in-sta>
                string outSta              = SafeField(f, 18, "0");      // <out-sta>
                string voltages            = SafeField(f, 19, "");       // <ext-V|bat-V|ad1-V|...|adn-V>
                
                d.Fuel                     = ParseFuelFromVoltageVT200L(voltages);
                
                // Temperature (optional field, may be at index 21 or 22)
                string tempSensor = "";
                if (f.Length > 21) tempSensor = SafeField(f, 21, "");
                if (f.Length > 22 && string.IsNullOrEmpty(tempSensor)) tempSensor = SafeField(f, 22, "");
                d.Temperature              = ParseTemperatureVT200L(tempSensor);
                
                d.UpdateTime               = ParseVT200LDateTime(dateTime, _utcOffsetSeconds);
                d.Distance                 = "0";
                d.RemainingCash            = 0;

                AppendLog(string.Format(
                    "[RX] IMEI:{0} Ev:{1} Lat:{2:F4} Lon:{3:F4} Spd:{4} Eng:{5} GPS:{6} Sats:{7} Temp:{8:F1} Q:{9}",
                    d.GpsIMEINumber, d.EventCode, d.Latitude, d.Longitude,
                    d.Speed, d.EngineStatus, d.Status_PostionValidity,
                    d.Status_SateliteCount, d.Temperature, _writeQueue.Count), LogLevel.Debug); // Changed to Debug to reduce UI spam

                // Server acknowledgment (VT200L requires reply with same pack-no)
                SendVT200LAck(state.Socket, packNo, d.GpsIMEINumber, cmd);

                // Deduplication
                if (IsDuplicate(d))
                {
                    Interlocked.Increment(ref _totalDeduped);
                    AppendLog("[DEDUP] " + d.GpsIMEINumber, LogLevel.Debug);
                    return;
                }

                UpdateDedupCache(d);
                _writeQueue.Enqueue(d);

                if (_isLiveMode)
                    AuditLog.auditLog(d.GpsIMEINumber,
                        JsonConvert.SerializeObject(d), "6066_PARSED");
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalParseErrors);
                AppendLog("[PARSE-ERR] " + ex.Message, LogLevel.Error);
                AuditLog.auditLog(ex.Message + " | " + Truncate(packet, 100),
                    "6066_PACKET_ERR");
            }
        }

        // ── Send VT200L acknowledgment ─────────────────────────────────────────
        private void SendVT200LAck(Socket socket, string packNo, string imei, string cmd)
        {
            try
            {
                // Format: $$<pack-no><pack-len>,<ID>,<cmd-code>,<cmd-data><checksum>\r\n
                // For acknowledgment: $$<pack-no><len>,<IMEI>,<cmd>,1<checksum>\r\n
                string dataStr = string.Format("{0},{1},1", imei, cmd);
                int dataLen = dataStr.Length;
                string preChecksum = string.Format("$${0}{1},{2}", packNo, dataLen, dataStr);
                
                string checksum = CalculateChecksum(preChecksum);
                string response = preChecksum + checksum + "\r\n";
                
                byte[] data = Encoding.ASCII.GetBytes(response);
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, socket);
                
                AppendLog("[ACK] " + response.Replace("\r\n", ""), LogLevel.Debug);
            }
            catch (Exception ex)
            {
                AuditLog.auditLog(ex.Message, "SEND_ACK_ERR_6066");
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndSend(ar);
            }
            catch (Exception ex)
            {
                AuditLog.auditLog(ex.Message, "SEND_CALLBACK_ERR_6066");
            }
        }

        // ── Calculate checksum (XOR of all bytes) ──────────────────────────────
        private string CalculateChecksum(string data)
        {
            byte checksum = 0;
            foreach (char c in data)
                checksum ^= (byte)c;
            return checksum.ToString("X2");
        }

        // ── UI timer (1 s) ─────────────────────────────────────────────────────
        private int _uiTickCount = 0;
        private void timerUI_Tick(object sender, EventArgs e)
        {
            _uiTickCount++;
            try
            {
                // Prune dead sockets (every tick)
                var dead = new List<ClientState>();
                foreach (var kv in _clients)
                    if (!IsSocketAlive(kv.Value.Socket)) dead.Add(kv.Value);
                foreach (var d in dead) CloseClient(d);

                // Stat tiles (every tick)
                lblConnVal.Text     = _clients.Count.ToString();
                lblRxVal.Text       = FormatBig(_totalReceived);
                lblOkVal.Text       = FormatBig(_totalProcessed);
                lblParseErrVal.Text = _totalParseErrors.ToString();
                lblDbErrVal.Text    = _totalDbErrors.ToString();
                lblUptimeVal.Text   = FormatUptime(DateTime.Now - _startTime);

                tileParseErr.BackColor = _totalParseErrors > 0
                    ? Color.FromArgb(200, 60, 0)  : Color.FromArgb(50, 50, 60);
                tileDbErr.BackColor    = _totalDbErrors > 0
                    ? Color.FromArgb(180, 20, 20) : Color.FromArgb(50, 50, 60);

                // CPU / MEM (every tick)
                float cpu = 0;
                try { cpu = _cpuCounter?.NextValue() ?? 0; } catch { }
                lblCpuVal.Text  = cpu.ToString("0.0") + " %";
                pbCPU.Value     = (int)Math.Min(cpu, 100);
                pbCPU.ForeColor = cpu > 80 ? Color.Red : cpu > 50 ? Color.Orange : Color.LimeGreen;

                long memMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                lblMemVal.Text = memMb + " MB";
                pbMem.Value    = (int)Math.Min(memMb, 100);

                // Packet rate (every tick)
                long nowRx    = Interlocked.Read(ref _totalReceived);
                long rate     = nowRx - _prevReceived;
                _prevReceived = nowRx;
                lblRateVal.Text      = rate + " /s";
                lblRateVal.ForeColor = rate > 50 ? Color.Orange : Color.LightGreen;

                // DB latency (every tick)
                long dbMs         = Interlocked.Read(ref _lastDbMs);
                lblDbLatVal.Text  = dbMs > 0 ? dbMs + " ms" : "-- ms";
                lblDbLatVal.ForeColor = dbMs > 500 ? Color.Red
                                      : dbMs > 200 ? Color.Orange : Color.Plum;

                // Server time (every tick)
                lblServerTime.Text = "Server: " + DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");

                // PERFORMANCE FIX: Connection list is EXPENSIVE - only refresh every 5 seconds
                if (_uiTickCount % 5 == 0)
                    RefreshConnectionList();

                // Status bar (every tick)
                SetStatus(string.Format(
                    "VT200L Port:{0}  Conn:{1}  Rate:{2}/s  Q:{3}  Deduped:{4}  CPU:{5:0}%  Mem:{6}MB",
                    _portNo, _clients.Count, rate,
                    _writeQueue.Count,
                    _totalDeduped, cpu, memMb), Color.LightGreen);
            }
            catch (Exception ex) { AuditLog.auditLog(ex.Message, "TIMER_ERR_6066"); }
        }

        // ── Connection ListView ────────────────────────────────────────────────
        private void RefreshConnectionList()
        {
            lvConnections.BeginUpdate();
            lvConnections.Items.Clear();
            foreach (var kv in _clients)
            {
                var cs  = kv.Value;
                bool ok = IsSocketAlive(cs.Socket);
                var lvi = new ListViewItem(
                    string.IsNullOrEmpty(cs.LastIMEI) ? "?" : cs.LastIMEI);
                lvi.SubItems.Add(cs.EndPoint);
                lvi.SubItems.Add(cs.ConnectedAt.ToString("HH:mm:ss"));
                lvi.SubItems.Add(cs.LastPacket.ToString("HH:mm:ss"));
                lvi.SubItems.Add(ok ? "● OK" : "✕ Dead");
                lvi.ForeColor = ok ? Color.LightGreen : Color.OrangeRed;
                lvi.BackColor = Color.FromArgb(18, 18, 30);
                lvi.UseItemStyleForSubItems = true;
                lvConnections.Items.Add(lvi);
            }
            lvConnections.EndUpdate();
        }

        // ── Live log ───────────────────────────────────────────────────────────
        private enum LogLevel { Debug, Info, Packet, Success, Warning, Error }
        private void AppendLog(string msg, LogLevel level)
        {
            if (rtbLog == null) return;
            
            // PERFORMANCE FIX: Skip Debug level logs in production to prevent UI flooding
            // At high packet rates, logging every packet freezes the UI
            if (level == LogLevel.Debug)
                return;
            
            try
            {
                Color col;
                switch (level)
                {
                    case LogLevel.Success: col = Color.LimeGreen;   break;
                    case LogLevel.Packet:  col = Color.DeepSkyBlue; break;
                    case LogLevel.Warning: col = Color.Orange;       break;
                    case LogLevel.Error:   col = Color.OrangeRed;    break;
                    case LogLevel.Debug:   col = Color.DimGray;      break;
                    default:               col = Color.LightGray;    break;
                }
                string line = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg + "\n";
                if (rtbLog.InvokeRequired)
                    rtbLog.BeginInvoke(new Action(() => WriteLog(line, col)));
                else
                    WriteLog(line, col);
            }
            catch { }
        }

        private void WriteLog(string line, Color col)
        {
            // OPTIMIZED: Reduce UI update frequency to prevent freezing
            // Only trim log every 100 entries instead of every entry
            if (rtbLog.Lines.Length > LOG_MAX && rtbLog.Lines.Length % 100 == 0)
            {
                try
                {
                    // Quick trim: remove first 25% of lines
                    int linesToRemove = LOG_MAX / 4;
                    int removePos = 0;
                    for (int i = 0; i < linesToRemove && removePos < rtbLog.TextLength; i++)
                    {
                        removePos = rtbLog.Text.IndexOf('\n', removePos) + 1;
                        if (removePos <= 0) break;
                    }
                    if (removePos > 0)
                    {
                        rtbLog.Select(0, removePos);
                        rtbLog.SelectedText = "";
                    }
                }
                catch { }
            }
            
            rtbLog.SelectionStart  = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor  = col;
            rtbLog.AppendText(line);
            rtbLog.ScrollToCaret();
        }

        private void lblClearBtn_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
            Interlocked.Exchange(ref _totalReceived,    0);
            Interlocked.Exchange(ref _totalProcessed,   0);
            Interlocked.Exchange(ref _totalDbErrors,    0);
            Interlocked.Exchange(ref _totalParseErrors, 0);
            Interlocked.Exchange(ref _totalDeduped,     0);
            _prevReceived = 0;
            AppendLog("Counters reset.", LogLevel.Info);
        }

        // ── Form closing ───────────────────────────────────────────────────────
        private void frm6066_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                timerUI.Stop();
                _writerRunning = false;
                _cpuCounter?.Dispose();
                foreach (var kv in _clients) CloseClient(kv.Value);
                _clients.Clear();
                try { _serverSocket?.Shutdown(SocketShutdown.Both); } catch { }
                try { _serverSocket?.Close(); }                        catch { }
                AuditLog.auditLog("frm6066 closed. QueueRemaining=" +
                    _writeQueue.Count, "SHUTDOWN_6066");
            }
            catch (Exception ex) { AuditLog.auditLog(ex.Message, "CLOSE_FORM_ERR_6066"); }
        }

        // ── Close one client ───────────────────────────────────────────────────
        private void CloseClient(ClientState state)
        {
            try
            {
                _clients.TryRemove(state.EndPoint, out _);
                Interlocked.Decrement(ref FX_TCP.Class.PublicClass.ActiveConnection_6066);
                try { state.Socket.Shutdown(SocketShutdown.Both); } catch { }
                try { state.Socket.Close(); }                        catch { }
                AppendLog("Disconnected: " + state.EndPoint +
                    (string.IsNullOrEmpty(state.LastIMEI) ? "" : "  IMEI:" + state.LastIMEI),
                    LogLevel.Warning);
            }
            catch (Exception ex) { AuditLog.auditLog(ex.Message, "CLOSE_ERR_6066"); }
        }

        // ── Badge / status helpers ─────────────────────────────────────────────
        private void UpdateModeBadge()
        {
            lblPortBadge.Text      = "PORT " + _portNo;
            lblModeBadge.Text      = _isLiveMode ? "LIVE" : "DEBUG";
            lblModeBadge.BackColor = _isLiveMode ? Color.OrangeRed : Color.DimGray;
        }

        private void SetStatus(string msg, Color col)
        {
            if (lblStatus.GetCurrentParent()?.InvokeRequired == true)
                lblStatus.GetCurrentParent().Invoke(new Action(() =>
                { lblStatus.Text = msg; lblStatus.ForeColor = col; }));
            else { lblStatus.Text = msg; lblStatus.ForeColor = col; }
        }

        private void SetStatusError(string msg)
        {
            picIndicator.BackColor = Color.Red;
            SetStatus(msg, Color.OrangeRed);
        }

        // ── VT200L protocol parsing helpers ────────────────────────────────────
        
        // Parse VT200L date-time: "210526063453" = 2021-05-26 06:34:53
        private static DateTime ParseVT200LDateTime(string raw, int utcOffsetSec)
        {
            try
            {
                raw = raw.Trim();
                if (raw.Length < 12) return DateTime.Now;
                return new DateTime(
                    2000 + int.Parse(raw.Substring(0, 2)),
                    int.Parse(raw.Substring(2, 2)),
                    int.Parse(raw.Substring(4, 2)),
                    int.Parse(raw.Substring(6, 2)),
                    int.Parse(raw.Substring(8, 2)),
                    int.Parse(raw.Substring(10, 2)),
                    DateTimeKind.Utc).AddSeconds(utcOffsetSec);
            }
            catch { return DateTime.Now; }
        }

        // Parse engine status from system-sta hex value
        // Bit3=1: external power connected (engine ON)
        private static string DeriveEngineStatusVT200L(string systemStaHex)
        {
            try
            {
                string hex = systemStaHex.Trim();
                if (string.IsNullOrEmpty(hex)) return "0";
                int val = Convert.ToInt32(hex, 16);
                // Bit 3 = external power connected
                return ((val & 0x08) != 0) ? "1" : "0";
            }
            catch { return "0"; }
        }

        // Parse fuel from voltage string: "0508|01A0|0000|0000"
        // AD1 (index 2) is fuel sensor: 0x01C8 = 456 dec, 456/100 = 4.56V
        // Fuel % = (4.56/5)*100 = 91.2%
        // Fuel liters = (4.56/5)*50 = 45.6 (assuming 50L tank)
        private static double ParseFuelFromVoltageVT200L(string voltages)
        {
            try
            {
                string[] parts = voltages.Trim().Split('|');
                if (parts.Length <= 2) return 0;
                
                string ad1Hex = parts[2].Trim();
                if (string.IsNullOrEmpty(ad1Hex) || ad1Hex == "0000") return 0;
                
                int ad1Val = Convert.ToInt32(ad1Hex, 16);
                double ad1Voltage = ad1Val / 100.0;
                
                // Assuming 5V = 100%, 50L tank
                // Adjust these constants based on your actual sensor and tank
                const double MAX_VOLTAGE = 5.0;
                const double TANK_CAPACITY_LITERS = 50.0;
                
                double fuelLiters = (ad1Voltage / MAX_VOLTAGE) * TANK_CAPACITY_LITERS;
                return fuelLiters;
            }
            catch { return 0; }
        }

        // Parse temperature: "010109D9" or "010109"
        // Format: 01 = sensor#1, 0109 = hex value
        // 0x0109 = 265 dec, temp = 265/10 = 26.5°C
        // If highest bit is 1, temperature is negative
        private static double ParseTemperatureVT200L(string tempSensor)
        {
            try
            {
                if (string.IsNullOrEmpty(tempSensor) || tempSensor.Length < 6)
                    return 0;
                
                // Extract hex value (skip first 2 chars = sensor number)
                string tempHex = tempSensor.Substring(2, 4);
                int tempVal = Convert.ToInt32(tempHex, 16);
                
                // Check if negative (highest bit of 16-bit value)
                if (tempVal > 32767)
                    tempVal -= 65536;
                
                return tempVal / 10.0;
            }
            catch { return 0; }
        }

        private static bool IsSocketAlive(Socket s)
        {
            try { return !(s.Poll(1, SelectMode.SelectRead) && s.Available == 0); }
            catch { return false; }
        }

        private static string SafeField(string[] arr, int idx, string def)
        {
            return (arr != null && idx >= 0 && idx < arr.Length) ? arr[idx] : def;
        }

        private static int ParseInt(string s)
        {
            return int.TryParse(s, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int val) ? val : 0;
        }

        private static double ParseDouble(string s)
        {
            return double.TryParse(s, NumberStyles.Float,
                CultureInfo.InvariantCulture, out double val) ? val : 0;
        }

        private static string Truncate(string s, int maxLen)
        {
            return (s.Length <= maxLen) ? s : s.Substring(0, maxLen) + "...";
        }

        private static string FormatBig(long n)
        {
            if (n >= 1000000) return (n / 1000000.0).ToString("0.0") + "M";
            if (n >= 1000)    return (n / 1000.0).ToString("0.0") + "K";
            return n.ToString();
        }

        private static string FormatUptime(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return string.Format("{0}d {1:00}h {2:00}m", (int)ts.TotalDays, ts.Hours, ts.Minutes);
            if (ts.TotalHours >= 1)
                return string.Format("{0:00}h {1:00}m {2:00}s", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            return string.Format("{0:00}m {1:00}s", ts.Minutes, ts.Seconds);
        }
    }
}
