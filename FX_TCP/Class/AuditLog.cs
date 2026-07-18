using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FX_TCP
{
    public static class AuditLog
    {
        static string KEEP_AUDIT_LOG = ConfigurationManager.AppSettings["KEEP_AUDIT_LOG"];
        public static void auditLog(string message, string OutboxID)
        {
            if (KEEP_AUDIT_LOG != "1")
            {
                return;
            }
            try
            {
                StackTrace stackTrace = new StackTrace();
                string callerMethodName = (stackTrace.GetFrame(1).GetMethod().Name).ToString();
                string callerFormName = (stackTrace.GetFrame(1).GetMethod().DeclaringType.FullName).ToString();

                string fileName = "";// string.Format(@"{0}\AuditTrail\{1}\{2}\{3}.csv", Directory.GetCurrentDirectory(), System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day);


                fileName = string.Format(@"{0}\AuditTrail\{1}\{2}\{3}\{4}_{5}.csv", Directory.GetCurrentDirectory(), System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day, callerFormName, System.DateTime.Now.Hour.ToString());


                string auditTrailPath = Path.GetDirectoryName(fileName);

                if (Directory.Exists(auditTrailPath))
                {
                }
                else
                {
                    Directory.CreateDirectory(auditTrailPath);
                }

                if (File.Exists(fileName))
                {
                }
                else
                {
                    using (StreamWriter writer = new StreamWriter(fileName, true))
                    {
                        writer.WriteLine("ID,DateTime,Form,Method,Message");
                    }
                }

                using (StreamWriter writer = new StreamWriter(fileName, true))
                {


                    writer.WriteLine(string.Format("ID : {3}, {0},{4}, {1}, {2}", System.DateTime.Now, callerMethodName, message, OutboxID, callerFormName));
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);


                //Secondary File

                try
                {

                    StackTrace stackTrace = new StackTrace();
                    string callerMethodName = (stackTrace.GetFrame(1).GetMethod().Name).ToString();
                    string callerFormName = (stackTrace.GetFrame(1).GetMethod().DeclaringType.FullName).ToString();

                    string fileName = "";// string.Format(@"{0}\AuditTrail\{1}\{2}\{3}.csv", Directory.GetCurrentDirectory(), System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day);

                    fileName = string.Format(@"{0}\AuditTrail\{1}\{2}\{3}\Secondary_{4}_{5}.csv", Directory.GetCurrentDirectory(), System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day, callerFormName, System.DateTime.Now.Hour.ToString());


                    string auditTrailPath = Path.GetDirectoryName(fileName);

                    if (Directory.Exists(auditTrailPath))
                    {
                    }
                    else
                    {
                        Directory.CreateDirectory(auditTrailPath);
                    }

                    if (File.Exists(fileName))
                    {
                    }
                    else
                    {
                        using (StreamWriter writer = new StreamWriter(fileName, true))
                        {
                            writer.WriteLine("ID,DateTime,Form,Method,Message");
                        }
                    }

                    using (StreamWriter writer = new StreamWriter(fileName, true))
                    {
                        writer.WriteLine(string.Format("ID : {3}, {0},{4}, {1}, {2}", System.DateTime.Now, callerMethodName, message, OutboxID, callerFormName));
                    }

                }
                catch (Exception exx)
                {


                }




            }

        }




        public static void auditLog(string IMEI, string message, string OutboxID)
        {
            if (KEEP_AUDIT_LOG != "1")
            {
                return;
            }
            try
            {
                StackTrace stackTrace = new StackTrace();
                string callerMethodName = (stackTrace.GetFrame(1).GetMethod().Name).ToString();
                string callerFormName = (stackTrace.GetFrame(1).GetMethod().DeclaringType.FullName).ToString();

                string fileName = "";// string.Format(@"{0}\AuditTrail\{1}\{2}\{3}.csv", Directory.GetCurrentDirectory(), System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day);


                fileName = string.Format(@"{0}\AuditTrail\{1}\{2}\{3}\IMEI\{6}\{4}_{5}.csv", Directory.GetCurrentDirectory(), System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day, callerFormName, System.DateTime.Now.Hour.ToString(), IMEI);


                string auditTrailPath = Path.GetDirectoryName(fileName);

                if (Directory.Exists(auditTrailPath))
                {
                }
                else
                {
                    Directory.CreateDirectory(auditTrailPath);
                }

                if (File.Exists(fileName))
                {
                }
                else
                {
                    using (StreamWriter writer = new StreamWriter(fileName, true))
                    {
                        writer.WriteLine("ID,DateTime,Form,Method,Message");
                    }
                }

                using (StreamWriter writer = new StreamWriter(fileName, true))
                {


                    writer.WriteLine(string.Format("ID : {3}, {0},{4}, {1}, {2}", System.DateTime.Now, callerMethodName, message, OutboxID, callerFormName));
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);


                //Secondary File

                try
                {

                    StackTrace stackTrace = new StackTrace();
                    string callerMethodName = (stackTrace.GetFrame(1).GetMethod().Name).ToString();
                    string callerFormName = (stackTrace.GetFrame(1).GetMethod().DeclaringType.FullName).ToString();

                    string fileName = "";// string.Format(@"{0}\AuditTrail\{1}\{2}\{3}.csv", Directory.GetCurrentDirectory(), System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day);

                    fileName = string.Format(@"{0}\AuditTrail\{1}\{2}\{3}\IMEI\{6}\Secondary_{4}_{5}.csv", Directory.GetCurrentDirectory(), System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day, callerFormName, System.DateTime.Now.Hour.ToString(), IMEI);


                    string auditTrailPath = Path.GetDirectoryName(fileName);

                    if (Directory.Exists(auditTrailPath))
                    {
                    }
                    else
                    {
                        Directory.CreateDirectory(auditTrailPath);
                    }

                    if (File.Exists(fileName))
                    {
                    }
                    else
                    {
                        using (StreamWriter writer = new StreamWriter(fileName, true))
                        {
                            writer.WriteLine("ID,DateTime,Form,Method,Message");
                        }
                    }

                    using (StreamWriter writer = new StreamWriter(fileName, true))
                    {
                        writer.WriteLine(string.Format("ID : {3}, {0},{4}, {1}, {2}", System.DateTime.Now, callerMethodName, message, OutboxID, callerFormName));
                    }

                }
                catch (Exception exx)
                {


                }




            }

        }






    }
}
