using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using FX_TCP.Class;

namespace FX_TCP
{
    // ── Dark theme renderer for MenuStrip ─────────────────────────────────────
    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected) { base.OnRenderMenuItemBackground(e); return; }
            e.Graphics.FillRectangle(
                new SolidBrush(Color.FromArgb(50, 50, 80)),
                new System.Drawing.Rectangle(System.Drawing.Point.Empty, e.Item.Size));
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.White;
            base.OnRenderItemText(e);
        }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        static readonly Color _bg     = Color.FromArgb(20, 20, 32);
        static readonly Color _hover  = Color.FromArgb(45, 45, 70);
        static readonly Color _border = Color.FromArgb(60, 60, 90);
        static readonly Color _drop   = Color.FromArgb(28, 28, 44);
        public override Color MenuStripGradientBegin        => _bg;
        public override Color MenuStripGradientEnd          => _bg;
        public override Color MenuItemSelectedGradientBegin => _hover;
        public override Color MenuItemSelectedGradientEnd   => _hover;
        public override Color MenuItemPressedGradientBegin  => _hover;
        public override Color MenuItemPressedGradientEnd    => _hover;
        public override Color MenuItemBorder                => _border;
        public override Color MenuBorder                    => _border;
        public override Color ToolStripDropDownBackground   => _drop;
        public override Color ImageMarginGradientBegin      => _drop;
        public override Color ImageMarginGradientMiddle     => _drop;
        public override Color ImageMarginGradientEnd        => _drop;
        public override Color SeparatorDark                 => _border;
        public override Color SeparatorLight                => _border;
    }

    // ── MDI Parent ────────────────────────────────────────────────────────────
    public partial class MDIParent1 : Form
    {
        VTS_Entities db = new VTS_Entities();

        public MDIParent1() { InitializeComponent(); }

        private void MDIParent1_Load(object sender, EventArgs e)
        {
            AuditLog.auditLog("App Start", "1");

            // Clean stale device data on startup
            using (var helper = new DB_Helper())
            {
                try { helper.CleanDeviceData(); }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
            }

            timer_AutoOnOff.Interval =
                Convert.ToInt32(ConfigurationManager.AppSettings["Restart_Second"]) * 1000;

            string ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.Text = "FX TCP  |  Vehicle Tracking Server  |  v" + ver +
                        "  |  Started: " + DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");

            // Auto-open both child forms
            t366ToolStripMenuItem_Click(null, null);
            t711LToolStripMenuItem_Click(null, null);
        }

        // ── CPU / MEM polling ─────────────────────────────────────────────────
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
                backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) { }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                var cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpu.NextValue();
                System.Threading.Thread.Sleep(200);
                int pct = (int)cpu.NextValue();
                cpu.Dispose();

                cpuUsages_ProgressBar.Value = Math.Min(pct, 100);
                cpuUsages_ProgressBar.ForeColor = pct > 80 ? Color.OrangeRed
                                                : pct > 50 ? Color.Orange
                                                : Color.LimeGreen;
                lblCPU_Usages.Text      = "CPU: " + pct + " %";
                lblCPU_Usages.ForeColor = cpuUsages_ProgressBar.ForeColor;

                long memMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                lblMemUsage.Text = "  MEM: " + memMb + " MB";

                lblSysTime.Text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
            }
            catch { }
        }

        // ── Child form launchers ──────────────────────────────────────────────
        private void t366ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AuditLog.auditLog("frm6062", "1");
            var frm = new frm6062();
            frm.MdiParent = this;
            frm.Show();
        }

        private void t711LToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AuditLog.auditLog("frm6063", "1");
            var frm = new frm6063();
            frm.MdiParent = this;
            frm.Show();
        }

        // ── Auto restart ──────────────────────────────────────────────────────
        private void timer_AutoOnOff_Tick(object sender, EventArgs e)
        {
            if (CommonClass.ToQuit == false)
            {
                CommonClass.ToQuit = true;
                System.Threading.Thread.Sleep(1000);
                CommonClass.Quit();
            }
        }

        private void MDIParent1_FormClosing(object sender, FormClosingEventArgs e)
        {
            int pid = Process.GetCurrentProcess().Id;
            Process.GetProcessById(pid).Kill();
        }

        private void statusStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }
    }
}
