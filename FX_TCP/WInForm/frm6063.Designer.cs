namespace FX_TCP
{
    partial class frm6063
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.components         = new System.ComponentModel.Container();
            this.timerUI            = new System.Windows.Forms.Timer(this.components);
            this.statusStrip1       = new System.Windows.Forms.StatusStrip();
            this.lblStatus          = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblServerTime      = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblClearBtn        = new System.Windows.Forms.ToolStripStatusLabel();

            // ── Top header panel ─────────────────────────────────────────────
            this.pnlHeader          = new System.Windows.Forms.Panel();
            this.lblTitle           = new System.Windows.Forms.Label();
            this.lblPortBadge       = new System.Windows.Forms.Label();
            this.lblModeBadge       = new System.Windows.Forms.Label();
            this.picIndicator       = new System.Windows.Forms.PictureBox();

            // ── Stats panel (6 tiles) ────────────────────────────────────────
            this.pnlStats           = new System.Windows.Forms.Panel();
            this.tileConn           = new System.Windows.Forms.Panel();
            this.tileRx             = new System.Windows.Forms.Panel();
            this.tileOK             = new System.Windows.Forms.Panel();
            this.tileParseErr       = new System.Windows.Forms.Panel();
            this.tileDbErr          = new System.Windows.Forms.Panel();
            this.tileUptime         = new System.Windows.Forms.Panel();

            this.lblConnVal         = new System.Windows.Forms.Label();
            this.lblConnLbl         = new System.Windows.Forms.Label();
            this.lblRxVal           = new System.Windows.Forms.Label();
            this.lblRxLbl           = new System.Windows.Forms.Label();
            this.lblOkVal           = new System.Windows.Forms.Label();
            this.lblOkLbl           = new System.Windows.Forms.Label();
            this.lblParseErrVal     = new System.Windows.Forms.Label();
            this.lblParseErrLbl     = new System.Windows.Forms.Label();
            this.lblDbErrVal        = new System.Windows.Forms.Label();
            this.lblDbErrLbl        = new System.Windows.Forms.Label();
            this.lblUptimeVal       = new System.Windows.Forms.Label();
            this.lblUptimeLbl       = new System.Windows.Forms.Label();

            // ── System metrics panel ─────────────────────────────────────────
            this.pnlMetrics         = new System.Windows.Forms.Panel();
            this.lblCpuLbl          = new System.Windows.Forms.Label();
            this.lblCpuVal          = new System.Windows.Forms.Label();
            this.pbCPU              = new System.Windows.Forms.ProgressBar();
            this.lblMemLbl          = new System.Windows.Forms.Label();
            this.lblMemVal          = new System.Windows.Forms.Label();
            this.pbMem              = new System.Windows.Forms.ProgressBar();
            this.lblRateLbl         = new System.Windows.Forms.Label();
            this.lblRateVal         = new System.Windows.Forms.Label();
            this.lblDbLatLbl        = new System.Windows.Forms.Label();
            this.lblDbLatVal        = new System.Windows.Forms.Label();

            // ── Bottom split: connection list + live log ─────────────────────
            this.splitMain          = new System.Windows.Forms.SplitContainer();
            this.grpConnections     = new System.Windows.Forms.GroupBox();
            this.lvConnections      = new System.Windows.Forms.ListView();
            this.chIMEI             = new System.Windows.Forms.ColumnHeader();
            this.chEndpoint         = new System.Windows.Forms.ColumnHeader();
            this.chConnectedAt      = new System.Windows.Forms.ColumnHeader();
            this.chLastPacket       = new System.Windows.Forms.ColumnHeader();
            this.chStatus           = new System.Windows.Forms.ColumnHeader();
            this.grpLog             = new System.Windows.Forms.GroupBox();
            this.rtbLog             = new System.Windows.Forms.RichTextBox();

            this.statusStrip1.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.pnlStats.SuspendLayout();
            this.pnlMetrics.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.grpConnections.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.SuspendLayout();

            // ── timerUI (1 second) ───────────────────────────────────────────
            this.timerUI.Interval = 1000;
            this.timerUI.Tick    += new System.EventHandler(this.timerUI_Tick);

            // ── statusStrip ──────────────────────────────────────────────────
            this.statusStrip1.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.lblStatus, this.lblServerTime, this.lblClearBtn });
            this.statusStrip1.Size      = new System.Drawing.Size(1100, 22);
            this.statusStrip1.Dock      = System.Windows.Forms.DockStyle.Bottom;

            this.lblStatus.ForeColor    = System.Drawing.Color.LightGreen;
            this.lblStatus.Text         = "Initializing...";
            this.lblStatus.Spring       = true;
            this.lblStatus.TextAlign    = System.Drawing.ContentAlignment.MiddleLeft;

            this.lblServerTime.ForeColor= System.Drawing.Color.Silver;
            this.lblServerTime.Text     = "";
            this.lblServerTime.Alignment= System.Windows.Forms.ToolStripItemAlignment.Right;

            this.lblClearBtn.Text       = "  Clear Log  ";
            this.lblClearBtn.ForeColor  = System.Drawing.Color.White;
            this.lblClearBtn.BackColor  = System.Drawing.Color.FromArgb(60, 60, 60);
            this.lblClearBtn.Alignment  = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblClearBtn.Click     += new System.EventHandler(this.lblClearBtn_Click);

            // ── Header panel ─────────────────────────────────────────────────
            this.pnlHeader.Dock         = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height       = 52;
            this.pnlHeader.BackColor    = System.Drawing.Color.FromArgb(20, 20, 35);
            this.pnlHeader.Padding      = new System.Windows.Forms.Padding(10, 0, 10, 0);

            this.picIndicator.Size      = new System.Drawing.Size(14, 14);
            this.picIndicator.Location  = new System.Drawing.Point(14, 19);
            this.picIndicator.BackColor = System.Drawing.Color.Gray;

            this.lblTitle.Text          = "MeiTrack T711L  —  GPRS TCP Listener";
            this.lblTitle.Font          = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor     = System.Drawing.Color.White;
            this.lblTitle.AutoSize      = true;
            this.lblTitle.Location      = new System.Drawing.Point(36, 14);

            this.lblPortBadge.Text      = "PORT 6065";
            this.lblPortBadge.Font      = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblPortBadge.ForeColor = System.Drawing.Color.Black;
            this.lblPortBadge.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.lblPortBadge.AutoSize  = true;
            this.lblPortBadge.Location  = new System.Drawing.Point(400, 18);
            this.lblPortBadge.Padding   = new System.Windows.Forms.Padding(6, 2, 6, 2);

            this.lblModeBadge.Text      = "LIVE";
            this.lblModeBadge.Font      = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblModeBadge.ForeColor = System.Drawing.Color.Black;
            this.lblModeBadge.BackColor = System.Drawing.Color.Orange;
            this.lblModeBadge.AutoSize  = true;
            this.lblModeBadge.Location  = new System.Drawing.Point(476, 18);
            this.lblModeBadge.Padding   = new System.Windows.Forms.Padding(6, 2, 6, 2);

            this.pnlHeader.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.picIndicator, this.lblTitle, this.lblPortBadge, this.lblModeBadge });

            // ── Stats panel (6 tiles, 170px each) ───────────────────────────
            this.pnlStats.Dock          = System.Windows.Forms.DockStyle.Top;
            this.pnlStats.Height        = 80;
            this.pnlStats.BackColor     = System.Drawing.Color.FromArgb(18, 18, 28);
            this.pnlStats.Padding       = new System.Windows.Forms.Padding(8, 6, 8, 6);

            int[] tileX = { 8, 183, 358, 533, 708, 883 };
            var tiles   = new[] { tileConn, tileRx, tileOK, tileParseErr, tileDbErr, tileUptime };
            var tileColors = new[] {
                System.Drawing.Color.FromArgb(0, 120, 212),
                System.Drawing.Color.FromArgb(0, 153, 102),
                System.Drawing.Color.FromArgb(16, 124, 16),
                System.Drawing.Color.FromArgb(180, 80, 0),
                System.Drawing.Color.FromArgb(164, 38, 44),
                System.Drawing.Color.FromArgb(80, 50, 140)
            };

            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].Size      = new System.Drawing.Size(167, 66);
                tiles[i].Location  = new System.Drawing.Point(tileX[i], 6);
                tiles[i].BackColor = tileColors[i];
                this.pnlStats.Controls.Add(tiles[i]);
            }

            // Tile value labels (big number)
            var vals = new[] { lblConnVal, lblRxVal, lblOkVal, lblParseErrVal, lblDbErrVal, lblUptimeVal };
            var lbls = new[] { lblConnLbl, lblRxLbl, lblOkLbl, lblParseErrLbl, lblDbErrLbl, lblUptimeLbl };
            var lText= new[] { "CONNECTIONS","PACKETS RX","DB SUCCESS","PARSE ERR","DB ERR","UPTIME" };

            for (int i = 0; i < vals.Length; i++)
            {
                vals[i].Text      = "0";
                vals[i].Font      = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
                vals[i].ForeColor = System.Drawing.Color.White;
                vals[i].AutoSize  = false;
                vals[i].Size      = new System.Drawing.Size(167, 36);
                vals[i].Location  = new System.Drawing.Point(0, 4);
                vals[i].TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tiles[i].Controls.Add(vals[i]);

                lbls[i].Text      = lText[i];
                lbls[i].Font      = new System.Drawing.Font("Segoe UI", 7F);
                lbls[i].ForeColor = System.Drawing.Color.FromArgb(210, 210, 210);
                lbls[i].AutoSize  = false;
                lbls[i].Size      = new System.Drawing.Size(167, 20);
                lbls[i].Location  = new System.Drawing.Point(0, 42);
                lbls[i].TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                tiles[i].Controls.Add(lbls[i]);
            }

            // ── Metrics panel ─────────────────────────────────────────────────
            this.pnlMetrics.Dock        = System.Windows.Forms.DockStyle.Top;
            this.pnlMetrics.Height      = 52;
            this.pnlMetrics.BackColor   = System.Drawing.Color.FromArgb(25, 25, 40);
            this.pnlMetrics.Padding     = new System.Windows.Forms.Padding(10, 6, 10, 6);

            // CPU
            this.lblCpuLbl.Text         = "CPU:";
            this.lblCpuLbl.ForeColor    = System.Drawing.Color.Silver;
            this.lblCpuLbl.Font         = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblCpuLbl.AutoSize     = true;
            this.lblCpuLbl.Location     = new System.Drawing.Point(10, 16);

            this.lblCpuVal.Text         = "0 %";
            this.lblCpuVal.ForeColor    = System.Drawing.Color.LightGreen;
            this.lblCpuVal.Font         = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblCpuVal.AutoSize     = true;
            this.lblCpuVal.Location     = new System.Drawing.Point(42, 16);

            this.pbCPU.Size             = new System.Drawing.Size(120, 14);
            this.pbCPU.Location         = new System.Drawing.Point(85, 18);
            this.pbCPU.Minimum          = 0;
            this.pbCPU.Maximum          = 100;
            this.pbCPU.ForeColor        = System.Drawing.Color.LimeGreen;
            this.pbCPU.Style            = System.Windows.Forms.ProgressBarStyle.Continuous;

            // MEM
            this.lblMemLbl.Text         = "MEM:";
            this.lblMemLbl.ForeColor    = System.Drawing.Color.Silver;
            this.lblMemLbl.Font         = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblMemLbl.AutoSize     = true;
            this.lblMemLbl.Location     = new System.Drawing.Point(220, 16);

            this.lblMemVal.Text         = "0 MB";
            this.lblMemVal.ForeColor    = System.Drawing.Color.SkyBlue;
            this.lblMemVal.Font         = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblMemVal.AutoSize     = true;
            this.lblMemVal.Location     = new System.Drawing.Point(257, 16);

            this.pbMem.Size             = new System.Drawing.Size(120, 14);
            this.pbMem.Location         = new System.Drawing.Point(310, 18);
            this.pbMem.Minimum          = 0;
            this.pbMem.Maximum          = 100;
            this.pbMem.ForeColor        = System.Drawing.Color.DeepSkyBlue;
            this.pbMem.Style            = System.Windows.Forms.ProgressBarStyle.Continuous;

            // Rate
            this.lblRateLbl.Text        = "PKT/s:";
            this.lblRateLbl.ForeColor   = System.Drawing.Color.Silver;
            this.lblRateLbl.Font        = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblRateLbl.AutoSize    = true;
            this.lblRateLbl.Location    = new System.Drawing.Point(445, 16);

            this.lblRateVal.Text        = "0";
            this.lblRateVal.ForeColor   = System.Drawing.Color.Orange;
            this.lblRateVal.Font        = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblRateVal.AutoSize    = true;
            this.lblRateVal.Location    = new System.Drawing.Point(490, 16);

            // DB Latency
            this.lblDbLatLbl.Text       = "DB Latency:";
            this.lblDbLatLbl.ForeColor  = System.Drawing.Color.Silver;
            this.lblDbLatLbl.Font       = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblDbLatLbl.AutoSize   = true;
            this.lblDbLatLbl.Location   = new System.Drawing.Point(560, 16);

            this.lblDbLatVal.Text       = "-- ms";
            this.lblDbLatVal.ForeColor  = System.Drawing.Color.Plum;
            this.lblDbLatVal.Font       = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblDbLatVal.AutoSize   = true;
            this.lblDbLatVal.Location   = new System.Drawing.Point(643, 16);

            this.pnlMetrics.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.lblCpuLbl, this.lblCpuVal, this.pbCPU,
                this.lblMemLbl, this.lblMemVal, this.pbMem,
                this.lblRateLbl, this.lblRateVal,
                this.lblDbLatLbl, this.lblDbLatVal });

            // ── SplitContainer ───────────────────────────────────────────────
            this.splitMain.Dock             = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.SplitterDistance = 340;
            this.splitMain.SplitterWidth    = 4;
            this.splitMain.BackColor        = System.Drawing.Color.FromArgb(30, 30, 30);

            // Left: connections ListView
            this.grpConnections.Dock        = System.Windows.Forms.DockStyle.Fill;
            this.grpConnections.Text        = "  Active Connections";
            this.grpConnections.ForeColor   = System.Drawing.Color.Silver;
            this.grpConnections.Font        = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.grpConnections.BackColor   = System.Drawing.Color.FromArgb(22, 22, 34);

            this.lvConnections.Dock         = System.Windows.Forms.DockStyle.Fill;
            this.lvConnections.View         = System.Windows.Forms.View.Details;
            this.lvConnections.FullRowSelect= true;
            this.lvConnections.GridLines    = true;
            this.lvConnections.BackColor    = System.Drawing.Color.FromArgb(15, 15, 25);
            this.lvConnections.ForeColor    = System.Drawing.Color.LightGray;
            this.lvConnections.Font         = new System.Drawing.Font("Consolas", 8.5F);
            this.lvConnections.BorderStyle  = System.Windows.Forms.BorderStyle.None;
            this.lvConnections.HeaderStyle  = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;

            this.chIMEI.Text        = "IMEI";           this.chIMEI.Width        = 130;
            this.chEndpoint.Text    = "Endpoint";        this.chEndpoint.Width    = 130;
            this.chConnectedAt.Text = "Connected";       this.chConnectedAt.Width = 80;
            this.chLastPacket.Text  = "Last Packet";     this.chLastPacket.Width  = 75;
            this.chStatus.Text      = "Status";          this.chStatus.Width      = 60;

            this.lvConnections.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.chIMEI, this.chEndpoint, this.chConnectedAt, this.chLastPacket, this.chStatus });
            this.grpConnections.Controls.Add(this.lvConnections);
            this.splitMain.Panel1.Controls.Add(this.grpConnections);

            // Right: live log RichTextBox
            this.grpLog.Dock        = System.Windows.Forms.DockStyle.Fill;
            this.grpLog.Text        = "  Live Packet Log";
            this.grpLog.ForeColor   = System.Drawing.Color.Silver;
            this.grpLog.Font        = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.grpLog.BackColor   = System.Drawing.Color.FromArgb(22, 22, 34);

            this.rtbLog.Dock        = System.Windows.Forms.DockStyle.Fill;
            this.rtbLog.BackColor   = System.Drawing.Color.FromArgb(10, 10, 18);
            this.rtbLog.ForeColor   = System.Drawing.Color.LightGray;
            this.rtbLog.Font        = new System.Drawing.Font("Consolas", 8.5F);
            this.rtbLog.ReadOnly    = true;
            this.rtbLog.ScrollBars  = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbLog.WordWrap    = false;

            this.grpLog.Controls.Add(this.rtbLog);
            this.splitMain.Panel2.Controls.Add(this.grpLog);

            // ── Form ─────────────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor           = System.Drawing.Color.FromArgb(18, 18, 28);
            this.ClientSize          = new System.Drawing.Size(1100, 620);
            this.Font                = new System.Drawing.Font("Segoe UI", 9F);
            this.Text                = "PORT 6065  |  MeiTrack T711L GPRS Listener";
            this.Name                = "frm6063";

            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.pnlMetrics);
            this.Controls.Add(this.pnlStats);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.statusStrip1);

            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frm6063_FormClosing);
            this.Load        += new System.EventHandler(this.frm6063_Load);

            this.statusStrip1.ResumeLayout(false); this.statusStrip1.PerformLayout();
            this.pnlHeader.ResumeLayout(false);   this.pnlHeader.PerformLayout();
            this.pnlStats.ResumeLayout(false);
            this.pnlMetrics.ResumeLayout(false);  this.pnlMetrics.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.grpConnections.ResumeLayout(false);
            this.grpLog.ResumeLayout(false);
            this.ResumeLayout(false); this.PerformLayout();
        }
        #endregion

        // Designer field declarations
        private System.Windows.Forms.Timer              timerUI;
        private System.Windows.Forms.StatusStrip        statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblServerTime;
        private System.Windows.Forms.ToolStripStatusLabel lblClearBtn;

        private System.Windows.Forms.Panel              pnlHeader;
        private System.Windows.Forms.Label              lblTitle;
        private System.Windows.Forms.Label              lblPortBadge;
        private System.Windows.Forms.Label              lblModeBadge;
        private System.Windows.Forms.PictureBox         picIndicator;

        private System.Windows.Forms.Panel              pnlStats;
        private System.Windows.Forms.Panel              tileConn, tileRx, tileOK, tileParseErr, tileDbErr, tileUptime;
        private System.Windows.Forms.Label              lblConnVal, lblConnLbl;
        private System.Windows.Forms.Label              lblRxVal,   lblRxLbl;
        private System.Windows.Forms.Label              lblOkVal,   lblOkLbl;
        private System.Windows.Forms.Label              lblParseErrVal, lblParseErrLbl;
        private System.Windows.Forms.Label              lblDbErrVal, lblDbErrLbl;
        private System.Windows.Forms.Label              lblUptimeVal, lblUptimeLbl;

        private System.Windows.Forms.Panel              pnlMetrics;
        private System.Windows.Forms.Label              lblCpuLbl, lblCpuVal;
        private System.Windows.Forms.ProgressBar        pbCPU;
        private System.Windows.Forms.Label              lblMemLbl, lblMemVal;
        private System.Windows.Forms.ProgressBar        pbMem;
        private System.Windows.Forms.Label              lblRateLbl, lblRateVal;
        private System.Windows.Forms.Label              lblDbLatLbl, lblDbLatVal;

        private System.Windows.Forms.SplitContainer     splitMain;
        private System.Windows.Forms.GroupBox           grpConnections;
        private System.Windows.Forms.ListView           lvConnections;
        private System.Windows.Forms.ColumnHeader       chIMEI, chEndpoint, chConnectedAt, chLastPacket, chStatus;
        private System.Windows.Forms.GroupBox           grpLog;
        private System.Windows.Forms.RichTextBox        rtbLog;

        // Legacy aliases so the existing code still compiles
        private System.Windows.Forms.Timer timer1 => timerUI;
        private System.Windows.Forms.ToolStripStatusLabel lblMsg => lblStatus;
        // list_Client was used in old timer — mapped to lvConnections via adapter
    }
}
