using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FX_TCP.Class;

namespace FX_TCP
{
    public partial class frm6063 : Form
    {
        // ── Configuration ──────────────────────────────────────────────────────
        private bool   _isLiveMode;
        private int    _portNo;
        private int    _utcOffsetSeconds;
        private double _distChangeRangeKm;
        private int    _dedupSeconds;     // skip DB if same pos within N seconds
        private int    _batchSize;        // packets per DB writer cycle

        // ── Network ────────────────────────────────────────────────────────────
        private Socket _serverSocket;
        private readonly ConcurrentDictionary<string, ClientState> _clients
            = new ConcurrentDictionary<string, ClientState>();

        // ── Write queue (TCP threads enqueue; background thread dequeues) ──────
        private readonly ConcurrentQueue<DB_Helper_Data> _writeQueue
            = new ConcurrentQueue<DB_Helper_Data>();
        private Thread   _writerThread;
        private volatile bool _writerRunning = false;

        // ── Per-IMEI dedup cache ──────────────────────────────────────────────
        // Key = IMEI, Value = (last saved time, last lat, last lon, last engine)
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

        // ── Counters (Interlocked) ─────────────────────────────────────────────
        private long _totalReceived    = 0;
        private long _totalProcessed   = 0;
        private long _totalDbErrors    = 0;
        private long _totalParseErrors = 0;
        private long _totalDeduped     = 0;   // packets skipped by dedup
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
            public ClientState(Socket s)
            {
                Socket   = s;
                EndPoint = s.RemoteEndPoint?.ToString() ?? "unknown";
            }
        }

        // ── Constructor ────────────────────────────────────────────────────────
        public frm6063()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        // ── Form Load ──────────────────────────────────────────────────────────
        private void frm6063_Load(object sender, EventArgs e)
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
                    "Server started on port {0}  |  Dedup={1}s  BatchSize={2}",
                    _portNo, _dedupSeconds, _batchSize), LogLevel.Info);
            }
            catch (Exception ex)
            {
                AuditLog.auditLog(ex.Message, "frm6063_Load");
                SetStatusError("FATAL: " + ex.Message);
                AppendLog("FATAL: " + ex.Message, LogLevel.Error);
            }
        }

        // ── Load settings ──────────────────────────────────────────────────────
        private void LoadConfig()
        {
            _portNo = int.Parse(
                ConfigurationManager.AppSettings["PORT_for_6065"] ?? "6065");
            _isLiveMode = (ConfigurationManager.AppSettings["IsLiveMode"] ?? "0") == "1";
            _distChangeRangeKm = double.Parse(
                ConfigurationManager.AppSettings["Distance_Change_Range_In_KM_6065"] ?? "0.05",
                CultureInfo.InvariantCulture);
            _utcOffsetSeconds = int.Parse(
                ConfigurationManager.AppSettings["UTC_Offset_Seconds_6065"] ?? "0");
            _dedupSeconds = int.Parse(
                ConfigurationManager.AppSettings["Dedup_Seconds_6065"] ?? "30");
            _batchSize = int.Parse(
                ConfigurationManager.AppSettings["Queue_BatchSize_6065"] ?? "50");
            CommonClass.Distance_Change_Range_In_KM_6063 = _distChangeRangeKm;
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
            AuditLog.auditLog("TCP server started on port " + _portNo, "SETUP");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  BACKGROUND DB WRITER THREAD
        //  Drains _writeQueue in batches. Completely decoupled from TCP threads.
        //  TCP never waits for DB — it just enqueues and returns immediately.
        // ═══════════════════════════════════════════════════════════════════════
        private void StartWriterThread()
        {
            _writerRunning = true;
            _writerThread  = new Thread(WriterLoop)
            {
                IsBackground = true,
                Name         = "T711L-DBWriter",
                Priority     = ThreadPriority.BelowNormal  // never starve TCP threads
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
                    // Drain up to _batchSize items per cycle
                    while (processed < _batchSize && _writeQueue.TryDequeue(out DB_Helper_Data data))
                    {
                        SaveToDatabase(data);
                        processed++;
                    }

                    // If queue is empty sleep 100 ms, otherwise run again immediately
                    if (processed == 0)
                        Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    AuditLog.auditLog(ex.Message, "WRITER_LOOP_ERR");
                    Thread.Sleep(500);   // back off on unexpected error
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
                    string response = dbHelper.PushDeviceData_PORT_6063(d);
                    sw.Stop();
                    Interlocked.Exchange(ref _lastDbMs, sw.ElapsedMilliseconds);
                    Interlocked.Increment(ref _totalProcessed);
                    AppendLog(string.Format("[DB] {0}  ({1} ms)  Q:{2}",
                        response, sw.ElapsedMilliseconds, _writeQueue.Count), LogLevel.Success);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Interlocked.Increment(ref _totalDbErrors);
                AppendLog("[DB-ERR] " + ex.Message, LogLevel.Error);
                AuditLog.auditLog(d.GpsIMEINumber, ex.Message, "6063_DB_ERR");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  PER-IMEI DEDUPLICATION
        //  Skip if ALL of these are unchanged within _dedupSeconds:
        //    - Latitude / Longitude (position)
        //    - Temperature
        //    - EventCode
        //    - EngineStatus
        //  ALWAYS save: moving vehicle, any alert event, any state change
        // ═══════════════════════════════════════════════════════════════════════
        private bool IsDuplicate(DB_Helper_Data d)
        {
            // Always save non-heartbeat alert events
            // 35=interval, 31=heartbeat, 34=passive reply → these are deduplicated
            // Everything else (ignition, speeding, geo-fence, tow...) → always save
            if (d.EventCode != "35" && d.EventCode != "31" && d.EventCode != "34")
                return false;

            if (!_dedupCache.TryGetValue(d.GpsIMEINumber, out DedupEntry last))
                return false;  // first packet from this IMEI

            // Window expired → save
            if ((d.UpdateTime - last.LastSaved).TotalSeconds > _dedupSeconds)
                return false;

            // Engine status changed → save
            if (d.EngineStatus != last.Engine)
                return false;

            // EventCode changed → save
            if (d.EventCode != last.EventCode)
                return false;

            // Temperature changed (round to 1 decimal to avoid float noise) → save
            if (Math.Abs(d.Temperature - last.Temperature) >= 0.1)
                return false;

            // Vehicle moved beyond threshold → save
            double distKm = new DB_Helper().distanceInKmBetweenEarthCoordinates(
                last.Lat, last.Lon, d.Latitude, d.Longitude);
            if (distKm > _distChangeRangeKm)
                return false;

            return true;  // everything same within window → duplicate, skip
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
                Interlocked.Increment(ref FX_TCP.Class.PublicClass.ActiveConnection_6063);
                cs.BeginReceive(state.Buffer, 0, state.Buffer.Length,
                    SocketFlags.None, ReceiveCallback, state);
                AppendLog("Connected: " + state.EndPoint, LogLevel.Info);
            }
            catch (ObjectDisposedException) { return; }
            catch (Exception ex) { AuditLog.auditLog(ex.Message, "ACCEPT_ERR"); }
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
                AuditLog.auditLog(ex.Message, "RECV_ERR");
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

        // ── Parse + dedup + enqueue (NO DB call here) ─────────────────────────
        private void ProcessPacket(ClientState state, string packet)
        {
            try
            {
                if (!packet.StartsWith("$$") || !packet.Contains(",AAA,"))
                {
                    AppendLog("[SKIP] Non-AAA: " + Truncate(packet, 60), LogLevel.Debug);
                    return;
                }

                int starPos = packet.LastIndexOf('*');
                if (starPos > 0) packet = packet.Substring(0, starPos);

                string[] f = packet.Split(',');
                if (f.Length < 19)
                {
                    Interlocked.Increment(ref _totalParseErrors);
                    AppendLog("[ERR] Too few fields (" + f.Length + "): "
                        + Truncate(packet, 60), LogLevel.Error);
                    return;
                }

                var d = new DB_Helper_Data();
                d.GpsIMEINumber            = f[1].Trim();
                state.LastIMEI             = d.GpsIMEINumber;
                d.EventCode                = SafeField(f, 3, "0");

                double.TryParse(SafeField(f, 4, "0"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double lat);
                double.TryParse(SafeField(f, 5, "0"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double lon);
                d.Latitude                 = lat;
                d.Longitude                = lon;
                d.UpdateTime               = ParseGpsDateTime(SafeField(f, 6, ""), _utcOffsetSeconds);
                d.Status_PostionValidity   = SafeField(f, 7, "V");
                d.Status_SateliteCount     = ParseInt(SafeField(f, 8, "0"));
                d.Status_GSMSignalStrength = ParseInt(SafeField(f, 9, "0"));
                d.Speed                    = ParseDouble(SafeField(f, 10, "0"));
                d.Course                   = ParseDouble(SafeField(f, 11, "0"));
                d.Altitude                 = ParseDouble(SafeField(f, 13, "0"));
                d.EngineStatus             = DeriveEngineStatus(d.EventCode, SafeField(f, 17, "0000"));
                d.Fuel                     = ParseFuelFromAnalog(SafeField(f, 18, "0|0|0|0|0"));
                d.Temperature              = ParseTemperatureFromAnalog(SafeField(f, 18, "0|0|0|0|0"));
                d.Distance                 = "0";
                d.RemainingCash            = 0;

                AppendLog(string.Format(
                    "[RX] IMEI:{0} Ev:{1} Lat:{2:F4} Lon:{3:F4} Spd:{4} Eng:{5} GPS:{6} Sats:{7} Temp:{8:F1} Analog:{9} Q:{10}",
                    d.GpsIMEINumber, d.EventCode, d.Latitude, d.Longitude,
                    d.Speed, d.EngineStatus, d.Status_PostionValidity,
                    d.Status_SateliteCount, d.Temperature,
                    SafeField(f, 18, "N/A"), _writeQueue.Count), LogLevel.Packet);

                // ── Deduplication ─────────────────────────────────────────────
                // Skip if same position/temp/engine/event within _dedupSeconds (10s)
                if (IsDuplicate(d))
                {
                    Interlocked.Increment(ref _totalDeduped);
                    AppendLog("[DEDUP] " + d.GpsIMEINumber, LogLevel.Debug);
                    return;
                }

                // Update cache BEFORE enqueue — handles burst replays
                UpdateDedupCache(d);

                // Enqueue — TCP thread never waits for DB
                _writeQueue.Enqueue(d);

                if (_isLiveMode)
                    AuditLog.auditLog(d.GpsIMEINumber,
                        JsonConvert.SerializeObject(d), "6063_PARSED");
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalParseErrors);
                AppendLog("[PARSE-ERR] " + ex.Message, LogLevel.Error);
                AuditLog.auditLog(ex.Message + " | " + Truncate(packet, 100),
                    "6063_PACKET_ERR");
            }
        }

        // ── UI timer (1 s) ─────────────────────────────────────────────────────
        private void timerUI_Tick(object sender, EventArgs e)
        {
            try
            {
                // Prune dead sockets
                var dead = new List<ClientState>();
                foreach (var kv in _clients)
                    if (!IsSocketAlive(kv.Value.Socket)) dead.Add(kv.Value);
                foreach (var d in dead) CloseClient(d);

                // Stat tiles
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

                // CPU / MEM
                float cpu = 0;
                try { cpu = _cpuCounter?.NextValue() ?? 0; } catch { }
                lblCpuVal.Text  = cpu.ToString("0.0") + " %";
                pbCPU.Value     = (int)Math.Min(cpu, 100);
                pbCPU.ForeColor = cpu > 80 ? Color.Red : cpu > 50 ? Color.Orange : Color.LimeGreen;

                long memMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                lblMemVal.Text = memMb + " MB";
                pbMem.Value    = (int)Math.Min(memMb, 100);

                // Packet rate
                long nowRx    = Interlocked.Read(ref _totalReceived);
                long rate     = nowRx - _prevReceived;
                _prevReceived = nowRx;
                lblRateVal.Text      = rate + " /s";
                lblRateVal.ForeColor = rate > 50 ? Color.Orange : Color.LightGreen;

                // DB latency
                long dbMs         = Interlocked.Read(ref _lastDbMs);
                lblDbLatVal.Text  = dbMs > 0 ? dbMs + " ms" : "-- ms";
                lblDbLatVal.ForeColor = dbMs > 500 ? Color.Red
                                      : dbMs > 200 ? Color.Orange : Color.Plum;

                // Server time
                lblServerTime.Text = "Server: " + DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");

                // Connection list
                RefreshConnectionList();

                // Status bar — includes queue depth + dedup count
                SetStatus(string.Format(
                    "Port:{0}  Conn:{1}  Rate:{2}/s  Q:{3}  Deduped:{4}  CPU:{5:0}%  Mem:{6}MB",
                    _portNo, _clients.Count, rate,
                    _writeQueue.Count,
                    _totalDeduped, cpu, memMb), Color.LightGreen);
            }
            catch (Exception ex) { AuditLog.auditLog(ex.Message, "TIMER_ERR"); }
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
            if (rtbLog.Lines.Length > LOG_MAX)
            {
                int trim = rtbLog.Text.IndexOf('\n', rtbLog.Text.Length / 2);
                if (trim > 0) { rtbLog.Select(0, trim + 1); rtbLog.SelectedText = ""; }
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
        private void frm6063_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                timerUI.Stop();
                _writerRunning = false;         // signal writer thread to stop
                _cpuCounter?.Dispose();
                foreach (var kv in _clients) CloseClient(kv.Value);
                _clients.Clear();
                try { _serverSocket?.Shutdown(SocketShutdown.Both); } catch { }
                try { _serverSocket?.Close(); }                        catch { }
                AuditLog.auditLog("frm6063 closed. QueueRemaining=" +
                    _writeQueue.Count, "SHUTDOWN");
            }
            catch (Exception ex) { AuditLog.auditLog(ex.Message, "CLOSE_FORM_ERR"); }
        }

        // ── Close one client ───────────────────────────────────────────────────
        private void CloseClient(ClientState state)
        {
            try
            {
                _clients.TryRemove(state.EndPoint, out _);
                Interlocked.Decrement(ref FX_TCP.Class.PublicClass.ActiveConnection_6063);
                try { state.Socket.Shutdown(SocketShutdown.Both); } catch { }
                try { state.Socket.Close(); }                        catch { }
                AppendLog("Disconnected: " + state.EndPoint +
                    (string.IsNullOrEmpty(state.LastIMEI) ? "" : "  IMEI:" + state.LastIMEI),
                    LogLevel.Warning);
            }
            catch (Exception ex) { AuditLog.auditLog(ex.Message, "CLOSE_ERR"); }
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

        // ── Protocol parsing helpers ───────────────────────────────────────────
        private static string DeriveEngineStatus(string eventCode, string ioPortHex)
        {
            if (eventCode == "1") return "1";
            if (eventCode == "9") return "0";
            try
            {
                string hex = ioPortHex.Trim();
                if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    hex = hex.Substring(2);
                return ((Convert.ToInt32(hex, 16) & 0x01) != 0) ? "1" : "0";
            }
            catch { return "0"; }
        }

        private static DateTime ParseGpsDateTime(string raw, int utcOffsetSec)
        {
            try
            {
                raw = raw.Trim();
                if (raw.Length < 12) return DateTime.Now;
                return new DateTime(
                    2000 + int.Parse(raw.Substring(4, 2)),
                    int.Parse(raw.Substring(2, 2)),
                    int.Parse(raw.Substring(0, 2)),
                    int.Parse(raw.Substring(6, 2)),
                    int.Parse(raw.Substring(8, 2)),
                    int.Parse(raw.Substring(10, 2)),
                    DateTimeKind.Utc).AddSeconds(utcOffsetSec);
            }
            catch { return DateTime.Now; }
        }

        private static double ParseFuelFromAnalog(string analog)
        {
            try
            {
                string[] p = analog.Trim().Split('|');
                return p.Length > 0
                    ? double.Parse(p[0].Trim(), CultureInfo.InvariantCulture) : 0;
            }
            catch { return 0; }
        }

        // MeiTrack T711L analog field: AD1|AD2|Temp1|Temp2|...
        // Temperature is at index 2 (0-based). Raw value is in 0.1°C units.
        // e.g. "1234|5678|256|0|0" → temp = 256 / 10.0 = 25.6°C
        // Adjust TEMP_INDEX or scaling below if your firmware differs.
        private const int TEMP_ANALOG_INDEX = 2;
        private static double ParseTemperatureFromAnalog(string analog)
        {
            try
            {
                string[] p = analog.Trim().Split('|');
                if (p.Length <= TEMP_ANALOG_INDEX) return 0;

                string raw = p[TEMP_ANALOG_INDEX].Trim();
                if (string.IsNullOrEmpty(raw) || raw == "0") return 0;

                // T711L encodes temperature as signed 16-bit in 0.1°C units
                if (!double.TryParse(raw, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double rawVal))
                    return 0;

                // Handle signed: values > 32767 are negative (e.g. 65436 = -1.0°C)
                if (rawVal > 32767) rawVal -= 65536;

                return rawVal / 10.0;
            }
            catch { return 0; }
        }

        private static bool IsSocketAlive(Socket s)
        {
            try { return !(s.Poll(1, SelectMode.SelectRead) && s.Available == 0); }
            catch { return false; }
        }

        // ── Tiny utilities ─────────────────────────────────────────────────────
        private static string SafeField(string[] a, int i, string def)
            => (i < a.Length && !string.IsNullOrWhiteSpace(a[i])) ? a[i].Trim() : def;

        private static double ParseDouble(string s)
        {
            double.TryParse(s, NumberStyles.Float,
                CultureInfo.InvariantCulture, out double v);
            return v;
        }

        private static int ParseInt(string s)
        { int.TryParse(s, out int v); return v; }

        private static string Truncate(string s, int max)
            => s.Length > max ? s.Substring(0, max) + "…" : s;

        private static string FormatBig(long n)
            => n >= 1_000_000 ? (n / 1_000_000.0).ToString("0.0") + "M"
             : n >= 1_000     ? (n / 1_000.0).ToString("0.0") + "K"
             : n.ToString();

        private static string FormatUptime(TimeSpan ts)
            => string.Format("{0}d {1:D2}h {2:D2}m",
                (int)ts.TotalDays, ts.Hours, ts.Minutes);
    }
}
