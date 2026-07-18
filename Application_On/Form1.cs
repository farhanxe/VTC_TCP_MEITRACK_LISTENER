using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;





namespace Application_On
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            label1.Text = "System On : " + System.DateTime.Now.ToString();

            timer1.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["Restart_Second"]) * 1000;

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                string AppPath = ConfigurationManager.AppSettings["openApp_Path"];

                // If path is relative, resolve it from the launcher's own folder.
                // This means you can put VTS_TCP.exe in the SAME folder as
                // Application_On.exe and just set openApp_Path = VTS_TCP.exe
                if (!System.IO.Path.IsPathRooted(AppPath))
                {
                    string launcherDir = System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location);
                    AppPath = System.IO.Path.Combine(launcherDir, AppPath);
                }

                if (!System.IO.File.Exists(AppPath))
                {
                    MessageBox.Show(
                        "Cannot find:\n" + AppPath +
                        "\n\nUpdate openApp_Path in Application_On.exe.config",
                        "Launcher Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                    return;
                }

                Process.Start(AppPath);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Launch failed:\n" + ex.Message,
                    "Launcher Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }
    }
}
