using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FX_TCP
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MDIParent1());
            }
            catch (Exception ex)
            {              
                #region AuditLog
                AuditLog.auditLog(ex.Message, "0000");
                #endregion

                MessageBox.Show(ex.Message, "Main Err : " + System.DateTime.Now.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);


            }

        }
    }
}
