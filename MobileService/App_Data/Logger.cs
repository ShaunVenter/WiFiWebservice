using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace AlwaysOnMobileService
{
    public class Logger
    {
        public static void Log(string Where, string Method, string Message, string Extras, string Parameters)
        {
            try
            {
                Where = (Where ?? "General").ToString().Trim();
                Method = (Method ?? "").ToString().Trim();
                Message = (Message ?? "").ToString().Trim();
                Extras = (Extras ?? "").ToString().Trim();
                Parameters = (Parameters ?? "").ToString().Trim();

                var logpath = HttpContext.Current.Server.MapPath("~") + "/log";
                if (!Directory.Exists(logpath)) Directory.CreateDirectory(logpath);

                using (var w = File.AppendText(logpath + "/" + DateTime.Now.ToString("yyyyMMdd") + "_" + Where + ".txt"))
                {
                    var tw = (TextWriter)w;
                    w.WriteLine("\n\n{0} : {1} {2} \n{3} {4} \n(Parameters: {5})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), Where, Method, Message, Extras, Parameters);
                }
            }
            catch { }
        }
    }
}