using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FX_TCP.Class
{
    public static class CommonClass
    {
        public static bool ToQuit = false;
        public static int Max_Connected_Socket_6062 { get; set; }
        public static double Distance_Change_Range_In_KM_6062 { get; set; }
        public static double Distance_Change_Range_In_KM_6063 { get; set; }
        public static double Distance_Change_Range_In_KM_6066 { get; set; }
        public static void Quit()
        {
            if (CommonClass.ToQuit == true)
            {
                try
                {
                    string AppPath = ConfigurationManager.AppSettings["openApp_Path"];
                    Process.Start(AppPath);

                    int nProcessID = Process.GetCurrentProcess().Id;
                    Process p = Process.GetProcessById(nProcessID);
                    p.Kill();
                }
                catch (Exception ex)
                {
                    Form1 frmMsg = new Form1(ex.Message);
                    frmMsg.Show();
                }
            }
        }
    }
}
