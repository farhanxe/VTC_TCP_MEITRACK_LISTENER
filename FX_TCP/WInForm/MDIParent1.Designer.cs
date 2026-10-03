using System;

namespace FX_TCP
{
    partial class MDIParent1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && components != null) components.Dispose();
                base.Dispose(disposing);
            }
            catch { }
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.components       = new System.ComponentModel.Container();
            this.menuStrip        = new System.Windows.Forms.MenuStrip();
            this.servicePORTToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.t366ToolStripMenuItem  = new System.Windows.Forms.ToolStripMenuItem();
            this.t711LToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.vt200LToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip      = new System.Windows.Forms.StatusStrip();
            this.lblCPU_Usages    = new System.Windows.Forms.ToolStripStatusLabel();
            this.cpuUsages_ProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.lblMemUsage      = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblSysTime       = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolTip          = new System.Windows.Forms.ToolTip(this.components);
            this.timer1           = new System.Windows.Forms.Timer(this.components);
            this.backgroundWorker1= new System.ComponentModel.BackgroundWorker();
            this.timer_AutoOnOff  = new System.Windows.Forms.Timer(this.components);
            this.menuStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();

            // ── menuStrip ─────────────────────────────────────────────────────
            this.menuStrip.BackColor = System.Drawing.Color.FromArgb(20, 20, 32);
            this.menuStrip.ForeColor = System.Drawing.Color.White;
            this.menuStrip.Font      = new System.Drawing.Font("Segoe UI", 9.5F);
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.servicePORTToolStripMenuItem });
            this.menuStrip.Location  = new System.Drawing.Point(0, 0);
            this.menuStrip.Name      = "menuStrip";
            this.menuStrip.Size      = new System.Drawing.Size(1200, 26);
            this.menuStrip.Renderer  = new DarkMenuRenderer();

            // servicePORTToolStripMenuItem
            this.servicePORTToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.servicePORTToolStripMenuItem.Text      = "⚙  Service Ports";
            this.servicePORTToolStripMenuItem.Name      = "servicePORTToolStripMenuItem";
            this.servicePORTToolStripMenuItem.DropDownItems.AddRange(
                new System.Windows.Forms.ToolStripItem[] {
                    this.t366ToolStripMenuItem,
                    this.t711LToolStripMenuItem,
                    this.vt200LToolStripMenuItem });

            this.t366ToolStripMenuItem.Text         = "6062  —  T366 (Meitrack)";
            this.t366ToolStripMenuItem.Name         = "t366ToolStripMenuItem";
            this.t366ToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F3;
            this.t366ToolStripMenuItem.Image        = null;
            this.t366ToolStripMenuItem.Click       += new System.EventHandler(this.t366ToolStripMenuItem_Click);

            this.t711LToolStripMenuItem.Text         = "6065  —  T711L (Meitrack)";
            this.t711LToolStripMenuItem.Name         = "t711LToolStripMenuItem";
            this.t711LToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F4;
            this.t711LToolStripMenuItem.Click       += new System.EventHandler(this.t711LToolStripMenuItem_Click);

            this.vt200LToolStripMenuItem.Text         = "6066  —  VT200L";
            this.vt200LToolStripMenuItem.Name         = "vt200LToolStripMenuItem";
            this.vt200LToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.vt200LToolStripMenuItem.Click       += new System.EventHandler(this.vt200LToolStripMenuItem_Click);

            // ── statusStrip ───────────────────────────────────────────────────
            this.statusStrip.BackColor  = System.Drawing.Color.FromArgb(15, 15, 25);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.Font       = new System.Drawing.Font("Segoe UI", 8.5F);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.lblCPU_Usages,
                this.cpuUsages_ProgressBar,
                this.lblMemUsage,
                this.lblSysTime });
            this.statusStrip.Location = new System.Drawing.Point(0, 668);
            this.statusStrip.Name     = "statusStrip";
            this.statusStrip.Size     = new System.Drawing.Size(1200, 24);

            this.lblCPU_Usages.ForeColor = System.Drawing.Color.LightGreen;
            this.lblCPU_Usages.Text      = "CPU: 0 %";
            this.lblCPU_Usages.Name      = "lblCPU_Usages";

            this.cpuUsages_ProgressBar.Name  = "cpuUsages_ProgressBar";
            this.cpuUsages_ProgressBar.Size  = new System.Drawing.Size(120, 16);
            this.cpuUsages_ProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;

            this.lblMemUsage.ForeColor = System.Drawing.Color.SkyBlue;
            this.lblMemUsage.Text      = "  MEM: -- MB";
            this.lblMemUsage.Name      = "lblMemUsage";

            this.lblSysTime.ForeColor  = System.Drawing.Color.Silver;
            this.lblSysTime.Text       = "";
            this.lblSysTime.Name       = "lblSysTime";
            this.lblSysTime.Spring     = true;
            this.lblSysTime.TextAlign  = System.Drawing.ContentAlignment.MiddleRight;

            // ── timers ────────────────────────────────────────────────────────
            this.timer1.Enabled  = true;
            this.timer1.Interval = 2000;
            this.timer1.Tick    += new System.EventHandler(this.timer1_Tick);

            this.backgroundWorker1.DoWork             += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);

            this.timer_AutoOnOff.Enabled  = true;
            this.timer_AutoOnOff.Interval = 10000;
            this.timer_AutoOnOff.Tick    += new System.EventHandler(this.timer_AutoOnOff_Tick);

            // ── MDIParent1 form ───────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor           = System.Drawing.Color.FromArgb(18, 18, 28);
            this.ClientSize          = new System.Drawing.Size(1200, 692);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.Font                = new System.Drawing.Font("Segoe UI", 9F);
            this.IsMdiContainer      = true;
            this.MainMenuStrip       = this.menuStrip;
            this.MinimumSize         = new System.Drawing.Size(900, 600);
            this.Name                = "MDIParent1";
            this.Text                = "FX TCP  |  Vehicle Tracking Server";
            this.FormClosing        += new System.Windows.Forms.FormClosingEventHandler(this.MDIParent1_FormClosing);
            this.Load               += new System.EventHandler(this.MDIParent1_Load);

            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.MenuStrip             menuStrip;
        private System.Windows.Forms.StatusStrip           statusStrip;
        private System.Windows.Forms.ToolTip               toolTip;
        private System.Windows.Forms.ToolStripMenuItem     servicePORTToolStripMenuItem;
        private System.Windows.Forms.Timer                 timer1;
        private System.Windows.Forms.ToolStripStatusLabel  lblCPU_Usages;
        private System.Windows.Forms.ToolStripProgressBar  cpuUsages_ProgressBar;
        private System.Windows.Forms.ToolStripStatusLabel  lblMemUsage;
        private System.Windows.Forms.ToolStripStatusLabel  lblSysTime;
        private System.ComponentModel.BackgroundWorker     backgroundWorker1;
        private System.Windows.Forms.ToolStripMenuItem     t366ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem     t711LToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem     vt200LToolStripMenuItem;
        private System.Windows.Forms.Timer                 timer_AutoOnOff;
    }
}
